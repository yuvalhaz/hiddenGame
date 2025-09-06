using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// SimpleDragFromBar (Viewport-corrected, full rewrite)
/// - Drag from bottom bar with a canvas-level ghost
/// - Block drag when out of hearts; charge exactly 1 heart only on wrong drop
/// - Correct drop: settle at DropSpot; optional confetti
/// - Hint: chooses only items visible in the bottom bar ScrollRect viewport
///         and starts the ghost from the item's on-viewport position
///         (clamped to viewport edges if the item is off-screen)
/// - Drag visual grows to sprite's native size during drag; shrinks back on return
/// - No extra scripts; keeps project manifest class names
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class SimpleDragFromBar : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // ===== Inspector =====

    [Header("Canvas / Raycast")]
    [Tooltip("Topmost UI Canvas (must have GraphicRaycaster + EventSystem in scene)")]
    public Canvas topCanvas;

    [Header("Identity (Item → Spot)")]
    [Tooltip("Must match DropSpot.spotId that accepts this item")]
    public string itemId;

    [Header("Bottom Bar (Hint Viewport Filter)")]
    [Tooltip("The ScrollRect of the bottom bar")]
    [SerializeField] private ScrollRect bottomBar; // set in Inspector or auto-wired
    [Tooltip("Viewport (usually bottomBar.viewport)")]
    [SerializeField] private RectTransform barViewport; // set in Inspector or auto-wired
    [Tooltip("Bar content (usually bottomBar.content)")]
    [SerializeField] private RectTransform barContent; // set in Inspector or auto-wired

    [Header("Line Pointer (Optional)")]
    [SerializeField] private string linePointerName = "";

    [Header("Drag Visual / Hint Ghost")]
    [Tooltip("Optional prefab (RectTransform + Image + CanvasGroup)")]
    public RectTransform dragVisualPrefab;
    [Tooltip("Optional override sprite; otherwise uses this object's Image sprite")]
    public Sprite overrideSprite;
    [Tooltip("Optional parent for drag/hint visuals (default: topCanvas)")]
    [SerializeField] private Transform dragGhostContainer = null;
    [Tooltip("Sibling index for ghost: -1 keep, -2 set first, >=0 exact index")]
    [SerializeField] private int dragGhostSiblingIndex = -1;

    [Header("Drag Size")]
    [Tooltip("Grow to sprite's native pixel size during drag")]
    [SerializeField] private bool useNativeSizeForDrag = true;
    [Tooltip("If non-zero, overrides drag size (pixels)")]
    [SerializeField] private Vector2 targetDragSize = Vector2.zero;
    [Tooltip("Fallback multiplier when not using native size or explicit target")]
    [SerializeField] private float dragSizeMultiplier = 1.3f;
    [Tooltip("Animate growth to drag size on begin")]
    [SerializeField] private bool animateSizeChange = true;
    [SerializeField] private float sizeAnimationDuration = 0.15f;

    [Header("Hint Tiebreak")]
    [Tooltip("Higher prefers this item when multiple are visible; secondary sort = distance to viewport center")]
    [SerializeField] private int hintPriority = 0;

    [Header("Celebration")]
    [SerializeField] private bool showConfettiOnSuccess = true;
    [SerializeField] private int confettiCount = 50;

    public enum ItemBehaviorAfterSuccess { MakeNonInteractable, AllowReDrag, Hide }

    [Header("After Correct Placement")]
    [SerializeField] private ItemBehaviorAfterSuccess afterSuccessBehavior = ItemBehaviorAfterSuccess.MakeNonInteractable;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // ===== Private =====
    private RectTransform _rt;
    private CanvasGroup _cg;
    private Image _image;
    private Color _originalColor;

    private RectTransform _activeDragRT;
    private Image _activeDragImage;
    private bool _hasBeenSuccessfullyPlaced = false;
    private HeartsManager _hearts; // per project manifest

    private static readonly Dictionary<string, SimpleDragFromBar> Registry = new Dictionary<string, SimpleDragFromBar>();
    private static bool hintRunning = false;

    // ===== Unity lifecycle =====
    private void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _cg = GetComponent<CanvasGroup>();
        _image = GetComponent<Image>();
        _hearts = FindObjectOfType<HeartsManager>();
        if (_image) _originalColor = _image.color;

        AutoWireBarRefs(); // auto-assign bottomBar / viewport / content if possible
    }

    private void AutoWireBarRefs()
    {
        // Try find a ScrollRect if not set
        if (bottomBar == null)
            bottomBar = GetComponentInParent<ScrollRect>();

        // Fill viewport/content from ScrollRect if missing
        if (barViewport == null && bottomBar != null)
            barViewport = bottomBar.viewport;
        if (barContent == null && bottomBar != null)
            barContent = bottomBar.content;

        // Final safety: warn in Console (won't crash)
        if ((barViewport == null || barContent == null) && debugMode)
            Debug.LogWarning("[SimpleDragFromBar] Bottom bar refs not fully assigned. Hint will use a safe fallback.");
    }

    private void OnEnable()
    {
        RegisterItem();
    }

    private void OnDisable()
    {
        UnregisterItem();
    }

    private void RegisterItem()
    {
        if (!string.IsNullOrEmpty(itemId)) Registry[itemId] = this;
    }

    private void UnregisterItem()
    {
        if (!string.IsNullOrEmpty(itemId) && Registry.TryGetValue(itemId, out var me) && me == this)
            Registry.Remove(itemId);
    }

    // ===== Drag Handlers =====
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Block when no hearts
        if (_hearts != null && !_hearts.HasHearts())
        {
            var popup = FindObjectOfType<NoHeartsPopup>();
            if (popup) popup.Show();
            return;
        }

        if (!CanBeDragged()) return;

        _activeDragRT = CreateDragVisual(out _activeDragImage);
        UpdateDragPosition(eventData);

        if (animateSizeChange && _activeDragRT)
            StartCoroutine(AnimateDragStart(_activeDragRT));

        if (_cg) _cg.blocksRaycasts = false; // allow raycasts through original while we drag ghost
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_activeDragRT == null) return;
        UpdateDragPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_activeDragRT == null)
        {
            if (_cg) _cg.blocksRaycasts = true;
            return;
        }

        var hitSpot = RaycastForDropSpot(eventData);
        bool correct = hitSpot != null && hitSpot.Accepts(itemId);

        if (correct)
        {
            _hasBeenSuccessfullyPlaced = true;

            // Settle the dragged visual at the target
            hitSpot.SettleItem(_activeDragRT);
            var dCG = _activeDragRT.GetComponent<CanvasGroup>();
            if (dCG) { dCG.alpha = 1f; dCG.blocksRaycasts = false; }

            if (showConfettiOnSuccess && topCanvas)
                StartCoroutine(ShowSuccessConfetti(_activeDragRT));

            HandleSuccessfulPlacement();

            _activeDragRT = null;
            _activeDragImage = null;
            if (_cg) _cg.blocksRaycasts = true;
            return;
        }

        // Wrong drop → charge exactly 1 heart, animate back and shrink
        if (_hearts != null) _hearts.SpendHeart(1);
        StartCoroutine(AnimateReturnToOriginalPosition(_activeDragRT));
        _activeDragImage = null;
        if (_cg) _cg.blocksRaycasts = true;
    }

    private bool CanBeDragged()
    {
        if (!_hasBeenSuccessfullyPlaced) return true;
        switch (afterSuccessBehavior)
        {
            case ItemBehaviorAfterSuccess.AllowReDrag: return true;
            case ItemBehaviorAfterSuccess.MakeNonInteractable:
            case ItemBehaviorAfterSuccess.Hide: return false;
        }
        return false;
    }

    private void HandleSuccessfulPlacement()
    {
        if (_cg) _cg.blocksRaycasts = false;
        HidePointingLine();

        switch (afterSuccessBehavior)
        {
            case ItemBehaviorAfterSuccess.MakeNonInteractable:
                if (_cg) { _cg.interactable = false; _cg.alpha = 0.6f; }
                if (_image) _image.color = Color.Lerp(_originalColor, Color.gray, 0.5f);
                break;
            case ItemBehaviorAfterSuccess.AllowReDrag:
                // leave as-is
                break;
            case ItemBehaviorAfterSuccess.Hide:
                if (_cg) { _cg.alpha = 0f; _cg.interactable = false; }
                break;
        }
    }

    private void HidePointingLine()
    {
        if (string.IsNullOrEmpty(linePointerName)) return;
        var go = GameObject.Find(linePointerName);
        if (go) go.SetActive(false);
    }

    public void ResetToAvailable()
    {
        _hasBeenSuccessfullyPlaced = false;
        gameObject.SetActive(true);
        if (_cg) { _cg.alpha = 1f; _cg.interactable = true; _cg.blocksRaycasts = true; }
        if (_image) _image.color = _originalColor;
        if (_activeDragRT) Destroy(_activeDragRT.gameObject);
        _activeDragRT = null; _activeDragImage = null;
    }

    // ===== Drag visuals =====

    private void UpdateDragPosition(PointerEventData ev)
    {
        if (topCanvas == null || _activeDragRT == null) return;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)topCanvas.transform, ev.position, ev.pressEventCamera, out var worldPos);
        _activeDragRT.position = worldPos;
        }
    private RectTransform CreateDragVisual(out Image img)
    {
        RectTransform host = dragGhostContainer
            ? (RectTransform)dragGhostContainer
            : (topCanvas ? (RectTransform)topCanvas.transform : (RectTransform)transform.root);

        if (dragVisualPrefab)
        {
            var inst = Instantiate(dragVisualPrefab, host, false);
            img = inst.GetComponent<Image>();
            if (img)
            {
                var selfImg = GetComponent<Image>();
                if (img.sprite == null) img.sprite = overrideSprite ? overrideSprite : (selfImg ? selfImg.sprite : null);
                img.raycastTarget = false; img.preserveAspect = true;
            }
            var cg = inst.GetComponent<CanvasGroup>();
            if (cg) { cg.interactable = false; cg.blocksRaycasts = false; cg.alpha = 0.95f; }

            // התחל מגודל קטן יותר
            inst.sizeDelta = _rt.sizeDelta * 0.6f;  // 80% מגודל הכפתור
            
            ApplyGhostSiblingIndex(inst);
            return inst;
        }
        else
        {
            var go = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(host, false);
            
            // התחל מגודל קטן יותר
            rt.sizeDelta = _rt.sizeDelta * 0.6f;  // 80% מגודל הכפתור

            img = go.GetComponent<Image>();
            img.raycastTarget = false; img.preserveAspect = true;
            var selfImg2 = GetComponent<Image>();
            img.sprite = overrideSprite ? overrideSprite : (selfImg2 ? selfImg2.sprite : null);

            var cg2 = go.GetComponent<CanvasGroup>();
            cg2.interactable = false; cg2.blocksRaycasts = false; cg2.alpha = 0.95f;

            ApplyGhostSiblingIndex(rt);
            return rt;
        }
    }
    private void ApplyGhostSiblingIndex(RectTransform rt)
    {
        if (dragGhostSiblingIndex >= 0) rt.SetSiblingIndex(dragGhostSiblingIndex);
        else if (dragGhostSiblingIndex == -2) rt.SetAsFirstSibling();
        // -1: leave default order
    }

    private Vector2 CalculateDragSize()
    {
        Vector2 originalSize = _rt.rect.size;
        
        if (useNativeSizeForDrag)
        {
            var srcImg = GetComponent<Image>();
            var sp = overrideSprite ? overrideSprite : (srcImg ? srcImg.sprite : null);
            if (sp != null) 
            {
                Vector2 nativeSize = new Vector2(sp.rect.width, sp.rect.height);
                Debug.Log($"Button size: {originalSize}, Native size: {nativeSize}");
                return nativeSize;
            }
        }

        if (targetDragSize != Vector2.zero) 
        {
            Debug.Log($"Using target size: {targetDragSize}");
            return targetDragSize;
        }
        
        Vector2 multipliedSize = originalSize * dragSizeMultiplier;
        Debug.Log($"Using multiplied size: {multipliedSize}");
        return multipliedSize;
    }

    private IEnumerator AnimateDragStart(RectTransform dragRT)
    {
        Vector2 startSize = _rt.sizeDelta * 0.6f;     // מתחיל ב-60%
        Vector2 targetSize = _rt.sizeDelta * 1.0f;    // מגיע לגודל הכפתור המקורי
        float el = 0f;
        
        while (el < sizeAnimationDuration)
        {
            el += Time.deltaTime; 
            float t = Mathf.Clamp01(el / sizeAnimationDuration); 
            t = EaseOutQuad(t);
            if (dragRT) dragRT.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
            yield return null;
        }
        if (dragRT) dragRT.sizeDelta = targetSize;
    }

    private IEnumerator AnimateReturnToOriginalPosition(RectTransform dragRT)
    {
        if (dragRT == null) yield break;
        Vector3 startPos = dragRT.position;
        Vector3 endPos = _rt.TransformPoint(_rt.rect.center);
        Vector2 startSize = dragRT.sizeDelta;
        Vector2 endSize = _rt.rect.size; // shrink back
        float dur = 0.4f, el = 0f;

        while (el < dur && dragRT)
        {
            el += Time.deltaTime; float t = Mathf.Clamp01(el / dur); t = EaseInOutQuad(t);
            dragRT.position = Vector3.Lerp(startPos, endPos, t);
            if (animateSizeChange) dragRT.sizeDelta = Vector2.Lerp(startSize, endSize, t);
            yield return null;
        }
        if (dragRT) Destroy(dragRT.gameObject);
        _activeDragRT = null; _activeDragImage = null;
        if (_cg) { _cg.interactable = true; _cg.blocksRaycasts = true; _cg.alpha = 1f; }
    }

    // ===== Confetti =====
    private IEnumerator ShowSuccessConfetti(RectTransform targetItem)
    {
        if (targetItem == null || topCanvas == null) yield break;
        yield return new WaitForSeconds(0.1f);
        try { UIConfetti.Burst(topCanvas, targetItem, confettiCount, 1.2f); } catch { }
    }

    // ===== Raycast =====
    private DropSpot RaycastForDropSpot(PointerEventData ev)
    {
        var gr = topCanvas ? topCanvas.GetComponent<GraphicRaycaster>() : null;
        if (!gr) return null;
        var results = new List<RaycastResult>();
        gr.Raycast(ev, results);
        foreach (var r in results)
        {
            var spot = r.gameObject.GetComponentInParent<DropSpot>();
            if (spot != null) return spot;
        }
        return null;
    }

    // ===== Public State =====
    public bool IsDraggable() =>
        gameObject.activeInHierarchy && enabled && CanBeDragged() &&
        (_cg == null || (_cg.interactable && _cg.alpha > 0.1f));

    public bool HasBeenPlaced() => _hasBeenSuccessfullyPlaced;

    /// <summary>
    /// Picks the item closest to the center of the bottom bar viewport (most centered item gets priority).
    /// </summary>
    public static bool RunHintOnce()
    {
        if (hintRunning) return false;

        var spots = GameObject.FindObjectsOfType<DropSpot>();
        var valid = new List<HintPair>();

        foreach (var sp in spots)
        {
            if (sp == null || sp.IsSettled || string.IsNullOrEmpty(sp.spotId)) continue;
            if (Registry.TryGetValue(sp.spotId, out var item))
            {
                if (!item.IsDraggable()) continue;
                if (!item.IsInViewport()) continue; // **viewport filter**

                valid.Add(new HintPair
                {
                    spot = sp,
                    src = item,
                    priority = item.hintPriority,
                    viewportDistance = item.DistanceFromViewportCenter()
                });
            }
        }

        if (valid.Count == 0) return false;

        // Sort by distance to viewport center (closest to center wins)
        // Secondary sort by priority if distances are very close
        valid.Sort((a, b) =>
        {
            float distDiff = a.viewportDistance - b.viewportDistance;
            if (Mathf.Abs(distDiff) < 10f) // If very close, use priority
                return b.priority.CompareTo(a.priority);
            return a.viewportDistance.CompareTo(b.viewportDistance);
        });

        var pick = valid[0];
        pick.src.StartCoroutine(pick.src.PlayHintRoutine(pick.spot));
        return true;
    }

    private struct HintPair
    {
        public DropSpot spot;
        public SimpleDragFromBar src;
        public int priority;
        public float viewportDistance;
    }

    /// <summary>
    /// True if this item's Rect overlaps the viewport in world space.
    /// </summary>
    private bool IsInViewport()
    {
        if (barViewport == null) return true; // fail-open if not wired
        Vector3[] vp = new Vector3[4]; Vector3[] ch = new Vector3[4];
        barViewport.GetWorldCorners(vp); _rt.GetWorldCorners(ch);
        Rect vpRect = new Rect(vp[0], vp[2] - vp[0]); Rect chRect = new Rect(ch[0], ch[2] - ch[0]);
        return vpRect.Overlaps(chRect, true);
    }

    /// <summary>
    /// Distance of the item's center to the viewport center (screen space).
    /// </summary>
    private float DistanceFromViewportCenter()
    {
        if (barViewport == null) return float.MaxValue;
        Vector3 vpCenterWorld = (GetWorldRect(barViewport)).center;
        Vector2 vpCenterScreen = RectTransformUtility.WorldToScreenPoint(null, vpCenterWorld);
        Vector2 meScreen = RectTransformUtility.WorldToScreenPoint(null, _rt.TransformPoint(_rt.rect.center));
        return Vector2.Distance(vpCenterScreen, meScreen);
    }

    // ===== HINT COROUTINE (complete & safe) =====
    private IEnumerator PlayHintRoutine(DropSpot target)
    {
        hintRunning = true;

        // Ensure refs exist
        AutoWireBarRefs();

        // Choose which RectTransform to treat as viewport, if any
        RectTransform vp = barViewport != null ? barViewport : (bottomBar != null ? bottomBar.viewport : null);

        Vector3 worldStart;
        if (vp != null)
        {
            // Start from THIS ITEM'S position (the one closest to center that was selected)
            Vector2 startLocalInViewport = GetViewportLocalOfChildCenterClamped(_rt, vp, 12f);
            worldStart = vp.TransformPoint(new Vector3(startLocalInViewport.x, startLocalInViewport.y, 0f));
        }
        else
        {
            // Fallback: start from the button's world center (no viewport)
            worldStart = (GetWorldRect(_rt)).center;
        }

        // 2) Create ghost on top canvas (or custom host) - מתחיל קטן
        var ghostGO = CreateGhostImage(out RectTransform ghostRT);
        ghostRT.position = worldStart;
        ghostRT.localScale = Vector3.one;

        // הגדל את הרמז לגודל הגירירה
        if (animateSizeChange)
        {
            StartCoroutine(AnimateHintGrowth(ghostRT));
        }
        else
        {
            ghostRT.sizeDelta = CalculateDragSize();
        }

        // 3) Compute target world pos (center of DropSpot)
        RectTransform targetRT = target.transform as RectTransform;
        Vector3 worldTarget = (GetWorldRect(targetRT)).center;

        // 4) Fly to target (slight arc)
        float flyDur = 0.6f;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, flyDur))
        {
            float e = EaseOutQuad(Mathf.Clamp01(t));
            Vector3 mid = Vector3.Lerp(worldStart, worldTarget, 0.5f) + new Vector3(0f, 60f, 0f);
            Vector3 p1 = Vector3.Lerp(worldStart, mid, e);
            Vector3 p2 = Vector3.Lerp(mid, worldTarget, e);
            ghostRT.position = Vector3.Lerp(p1, p2, e);
            ghostRT.localScale = Vector3.one * Mathf.Lerp(1f, 1.05f, e);
            yield return null;
        }
        ghostRT.position = worldTarget;

        // 5) Pulse on target
        yield return PulseAtTarget(ghostRT);
        yield return new WaitForSecondsRealtime(0.25f);

        // 6) Return with shrinking
        Vector3 returnStart = ghostRT.position;
        Vector3 worldBack;
        if (vp != null)
        {
            Vector2 backLocal = GetViewportLocalOfChildCenterClamped(_rt, vp, 12f);
            worldBack = vp.TransformPoint(new Vector3(backLocal.x, backLocal.y, 0f));
        }
        else
        {
            worldBack = (GetWorldRect(_rt)).center;
        }

        // 7) Return with shrinking animation
        Vector2 startSize = ghostRT.sizeDelta;
        Vector2 endSize = _rt.rect.size; // מתכווץ חזרה לגודל הכפתור
        float retDur = 0.4f;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, retDur))
        {
            float e = EaseInQuad(Mathf.Clamp01(t));
            ghostRT.position = Vector3.Lerp(returnStart, worldBack, e);
            ghostRT.localScale = Vector3.one * Mathf.Lerp(1.05f, 1f, e);
            
            // הקטן את הגודל בדרך חזרה
            if (animateSizeChange)
                ghostRT.sizeDelta = Vector2.Lerp(startSize, endSize, e);
            
            yield return null;
        }
        ghostRT.position = worldBack;

        Destroy(ghostGO);
        hintRunning = false;
    }

    // ===== Viewport math helpers =====

    /// <summary>
    /// Returns the child's center in viewport local space, clamped inside viewport rect by 'margin' px.
    /// If viewport is null, returns (0,0).
    /// </summary>
    private static Vector2 GetViewportLocalOfChildCenterClamped(RectTransform child, RectTransform viewport, float margin)
    {
        if (viewport == null || child == null)
            return Vector2.zero;

        // Child world center → viewport local
        Vector3 worldCenter = (GetWorldRect(child)).center;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport,
            RectTransformUtility.WorldToScreenPoint(null, worldCenter),
            null,
            out Vector2 localInViewport);

        // Clamp to viewport rect
        Rect vr = viewport.rect;
        float x = Mathf.Clamp(localInViewport.x, vr.xMin + margin, vr.xMax - margin);
        float y = Mathf.Clamp(localInViewport.y, vr.yMin + margin, vr.yMax - margin);
        return new Vector2(x, y);
    }

    private static Rect GetWorldRect(RectTransform rt)
    {
        Vector3[] c = new Vector3[4];
        rt.GetWorldCorners(c);
        return new Rect(c[0], c[2] - c[0]); // bottom-left to top-right
    }

    // ===== Single, non-duplicated helpers =====
    private GameObject CreateGhostImage(out RectTransform ghostRT)
    {
        RectTransform host = dragGhostContainer
            ? (RectTransform)dragGhostContainer
            : (topCanvas ? (RectTransform)topCanvas.transform : (RectTransform)transform.root);

        if (dragVisualPrefab)
        {
            var inst = Instantiate(dragVisualPrefab, host, false);
            ghostRT = inst;

            var img = inst.GetComponent<Image>();
            if (img)
            {
                var selfImg = GetComponent<Image>();
                if (img.sprite == null) img.sprite = overrideSprite ? overrideSprite : (selfImg ? selfImg.sprite : null);
                img.raycastTarget = false; img.preserveAspect = true;
            }

            var cg = inst.GetComponent<CanvasGroup>();
            if (cg) { cg.interactable = false; cg.blocksRaycasts = false; cg.alpha = 0.95f; }

            // רמז מתחיל בגודל קטן כמו בגרירה
            inst.sizeDelta = _rt.sizeDelta * 0.6f;
            ApplyGhostSiblingIndex(inst);
            return inst.gameObject;
        }
        else
        {
            var go = new GameObject("HintGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            ghostRT = go.GetComponent<RectTransform>();
            ghostRT.SetParent(host, false);
            
            // רמז מתחיל בגודל קטן כמו בגרירה
            ghostRT.sizeDelta = _rt.sizeDelta * 0.6f;

            var img = go.GetComponent<Image>();
            img.raycastTarget = false; img.preserveAspect = true;
            var selfImg2 = GetComponent<Image>();
            img.sprite = overrideSprite ? overrideSprite : (selfImg2 ? selfImg2.sprite : null);

            var cg2 = go.GetComponent<CanvasGroup>();
            cg2.interactable = false; cg2.blocksRaycasts = false; cg2.alpha = 0.95f;

            ApplyGhostSiblingIndex(ghostRT);
            return go;
        }
    }

    // מתודה נוספת לאנימציית הגדלה של הרמז
    private IEnumerator AnimateHintGrowth(RectTransform hintRT)
    {
        Vector2 startSize = _rt.sizeDelta * 0.4f;     // מתחיל ב-60%
        Vector2 targetSize = _rt.sizeDelta * 1.0f;    // מגיע לגודל הכפתור
        float el = 0f;
        while (el < sizeAnimationDuration)
        {
            el += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(el / sizeAnimationDuration);
            t = EaseOutQuad(t);
            if (hintRT) hintRT.sizeDelta = Vector2.Lerp(startSize, targetSize, t);
            yield return null;
        }
        if (hintRT) hintRT.sizeDelta = targetSize;
    }

    private IEnumerator PulseAtTarget(RectTransform target)
    {
        float dur = 0.25f; var baseScale = target.localScale;
        for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / Mathf.Max(0.01f, dur))
        {
            float k = 1f + 0.12f * Mathf.Sin(t * Mathf.PI);
            target.localScale = baseScale * k;
            yield return null;
        }
        target.localScale = baseScale;
    }

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    private float EaseInQuad(float t) => t * t;
    private float EaseInOutQuad(float t) => (t < 0.5f) ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
}