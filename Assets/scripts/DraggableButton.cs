using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class DraggableButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Settings")]
    [SerializeField] private float dragThreshold = 50f;
    [SerializeField] private float dropDistanceThreshold = 150f;
    [SerializeField] private Canvas topCanvas;
    [SerializeField] private bool debugMode = false;
    [SerializeField] private bool animateSizeChange = true;
    [SerializeField] private float sizeAnimationDuration = 0.5f;

    [Header("Success Effects")]
    [SerializeField] private bool showConfettiOnSuccess = true;
    [SerializeField] private int confettiCount = 50;

    private ScrollableButtonBar buttonBar;
    private RectTransform rectTransform;
    private Canvas canvas;
    private Vector2 originalPosition;
    private int originalIndex;
    private bool isDraggingOut = false;
    private CanvasGroup canvasGroup;
    private string buttonID;
    private bool isDragging = false;

    private RectTransform activeDragRT;
    private Image activeDragImage;

    // PERFORMANCE FIX: Instance-based cache to prevent memory leaks across scenes
    private Dictionary<string, DropSpot> dropSpotCache;

    private bool wasSuccessfullyPlaced = false;

    private ScrollRect parentScrollRect;

    // PERFORMANCE FIX: Cached canvas reference to avoid repeated FindObjectsOfType calls
    private Canvas cachedCanvas;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        parentScrollRect = GetComponentInParent<ScrollRect>();

        // PERFORMANCE FIX: Cache canvas once at startup instead of during drag
        cachedCanvas = GetOrCacheCanvas();

        // PERFORMANCE FIX: Initialize instance-based cache
        if (dropSpotCache == null)
        {
            dropSpotCache = new Dictionary<string, DropSpot>();
        }
    }

    void OnDestroy()
    {
        if (activeDragRT != null)
        {
#if UNITY_EDITOR
            if (debugMode)
                Debug.Log($"[DraggableButton] OnDestroy - cleaning activeDragRT");
#endif
            Destroy(activeDragRT.gameObject);
            activeDragRT = null;
            activeDragImage = null;
        }

        // Clear cache to prevent memory leaks
        dropSpotCache?.Clear();
    }

    // PERFORMANCE FIX: Get canvas without expensive FindObjectsOfType in hot paths
    private Canvas GetOrCacheCanvas()
    {
        if (topCanvas != null) return topCanvas;
        if (cachedCanvas != null) return cachedCanvas;

        // Only search scene once during initialization
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.isRootCanvas)
            {
                return c;
            }
        }
        return canvas; // Fallback to parent canvas
    }

    public void SetButtonBar(ScrollableButtonBar bar, int index)
    {
        buttonBar = bar;
        originalIndex = index;
    }

    public void SetButtonID(string id)
    {
        buttonID = id;
    }

    public string GetButtonID()
    {
        return buttonID;
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        isDragging = true;
        
        // ✅ כרגע עדיין מאפשרים אינטראקציה (עד שעוברים threshold)
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = false;  // לא חוסמים raycasts כדי לאפשר גרירה
        
        // ✅ השבת את ה-ScrollRect כדי שלא יפריע לגרירה
        if (parentScrollRect != null)
        {
            parentScrollRect.enabled = false;
            if (debugMode)
                Debug.Log($"[DraggableButton] ScrollRect disabled");
        }
        
        buttonBar.OnButtonDragStarted(this, originalIndex);
        
        EnableMatchingDropSpot(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        float distanceFromOriginal = Vector2.Distance(localPoint, originalPosition);
        bool wasOut = isDraggingOut;
        isDraggingOut = distanceFromOriginal > dragThreshold;
        
        // ✅ רק לפני שיוצרים drag visual - תזיז את הכפתור
        // אחרי שיצרנו drag visual - אל תזיז את הכפתור המקורי!
        if (!wasOut && isDraggingOut)
        {
            // כאן אנחנו עוברים את ה-threshold בפעם הראשונה
            if (debugMode)
                Debug.Log($"[DraggableButton] Button crossed threshold! Creating drag visual for {buttonID}");
            
            buttonBar.OnButtonDraggedOut(this, originalIndex);
            
            CreateDragVisual();
            canvasGroup.alpha = 0f;
            
            // ✅ החזר את הכפתור המקורי למיקום המקורי שלו!
            rectTransform.anchoredPosition = originalPosition;
        }
        
        // ✅ רק עדכן את מיקום ה-drag visual, אל תזיז את הכפתור המקורי
        if (isDraggingOut && activeDragRT != null)
        {
            UpdateDragPosition(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        
        // ✅ הפעל מחדש את ה-ScrollRect
        if (parentScrollRect != null)
        {
            parentScrollRect.enabled = true;
            if (debugMode)
                Debug.Log($"[DraggableButton] ScrollRect re-enabled");
        }
        
        // ✅ החזר את הכפתור המקורי להיות אינטראקטיבי (למקרה שחוזרים)
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        
        if (activeDragRT != null)
        {
            Debug.Log($"[DraggableButton] OnEndDrag - checking for drop...");
            
            DropSpot hitSpot = RaycastForDropSpot(eventData);
            
            if (hitSpot != null && hitSpot.Accepts(buttonID))
            {
                RectTransform spotRT = hitSpot.GetComponent<RectTransform>();
                if (spotRT != null)
                {
                    float distance = Vector3.Distance(activeDragRT.position, spotRT.position);
                    
                    Debug.Log($"[DraggableButton] Distance to spot: {distance}, Max allowed: {dropDistanceThreshold}");
                    
                    if (distance <= dropDistanceThreshold)
                    {
                        Debug.Log($"[DraggableButton] ✅ SUCCESS! Dropped on correct spot and close enough: {hitSpot.spotId}");
                        wasSuccessfullyPlaced = true;
                        canvasGroup.alpha = 1f;
                        HandleSuccessfulPlacement(hitSpot);
                    }
                    else
                    {
                        Debug.Log($"[DraggableButton] ❌ Too far! Distance: {distance} > {dropDistanceThreshold}");
                        StartCoroutine(AnimateReturnToBar());
                    }
                }
                else
                {
                    Debug.LogError($"[DraggableButton] ❌ DropSpot has no RectTransform!");
                    StartCoroutine(AnimateReturnToBar());
                }
            }
            else
            {
                if (hitSpot != null)
                    Debug.Log($"[DraggableButton] ❌ Wrong spot! Expected: {buttonID}, Got: {hitSpot.spotId}");
                else
                    Debug.Log($"[DraggableButton] ❌ No spot found");
                
                StartCoroutine(AnimateReturnToBar());
            }
        }
        else
        {
            // אם אין drag visual - פשוט החזר את הכפתור למצב נורמלי
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
        }
        
        EnableMatchingDropSpot(false);

        // רק אם לא הושם בהצלחה - תחזיר אותו לבר
        if (!wasSuccessfullyPlaced)
        {
            buttonBar.OnButtonReturned(this, originalIndex);
        }
        
        isDraggingOut = false;
    }

    public void ReturnToPosition(Vector2 position)
    {
        rectTransform.anchoredPosition = position;
    }
    
    // ===== Drag Visual =====
    
    private void CreateDragVisual()
    {
        if (activeDragRT != null)
        {
#if UNITY_EDITOR
            if (debugMode)
                Debug.LogWarning($"[DraggableButton] Drag visual already exists! Skipping creation.");
#endif
            return;
        }

#if UNITY_EDITOR
        if (debugMode)
            Debug.Log($"[DraggableButton] Creating drag visual for {buttonID}");
#endif

        // PERFORMANCE FIX: Use cached canvas instead of FindObjectsOfType
        Canvas host = cachedCanvas;

        if (host == null)
        {
            Debug.LogError("[DraggableButton] No canvas found!");
            return;
        }
        
        GameObject go = new GameObject("DragGhost_" + buttonID);
        go.AddComponent<RectTransform>();
        go.AddComponent<CanvasGroup>();
        go.AddComponent<Image>();
        
        activeDragRT = go.GetComponent<RectTransform>();
        activeDragRT.SetParent(host.transform, false);
        
        activeDragImage = go.GetComponent<Image>();

        Sprite realPhoto = GetRealPhotoFromDropSpot();

        if (realPhoto != null)
        {
            activeDragImage.sprite = realPhoto;
        }
        else
        {
            Image myImage = GetComponent<Image>();
            if (myImage != null)
            {
                activeDragImage.sprite = myImage.sprite;
            }
        }
        
        activeDragImage.color = Color.white;
        activeDragImage.raycastTarget = false;
        activeDragImage.preserveAspect = true;
        
        CanvasGroup cg = activeDragRT.GetComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.alpha = 1f;
        
        Vector2 targetSize = GetRealPhotoSizeFromDropSpot();
        Vector2 startSize = targetSize * 0.3f;

        activeDragRT.sizeDelta = startSize;
        StartCoroutine(AnimateSizeCoroutine(activeDragRT, startSize, targetSize, sizeAnimationDuration));

        activeDragRT.SetAsLastSibling();

        // Position image at button's current location
        activeDragRT.position = rectTransform.position;
    }
    
    private void UpdateDragPosition(PointerEventData eventData)
    {
        // PERFORMANCE FIX: This runs every frame during drag - must be fast!
        if (activeDragRT == null) return;

        // PERFORMANCE FIX: Use cached canvas
        Canvas host = cachedCanvas;
        if (host == null) return;

        // Calculate accurate position
        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)host.transform,
            eventData.position,
            eventData.pressEventCamera,
            out worldPos
        );

        // Center image above finger
        Vector3 offset = new Vector3(0, activeDragRT.rect.height * 0.5f, 0);
        activeDragRT.position = worldPos + offset;
    }

    // ===== DropSpot Cache =====

    // PERFORMANCE FIX: Changed from static to instance method (cache is now instance-based)
    private void RefreshDropSpotCache()
    {
#if UNITY_EDITOR
        if (debugMode)
            Debug.Log($"[DraggableButton] Refreshing cache");
#endif

        if (dropSpotCache == null)
        {
            dropSpotCache = new Dictionary<string, DropSpot>();
        }

        dropSpotCache.Clear();

        // Find all DropSpots including inactive ones
        var allDropSpots = FindObjectsOfType<DropSpot>(true);

#if UNITY_EDITOR
        if (debugMode)
            Debug.Log($"[DraggableButton] Found {allDropSpots.Length} DropSpots in scene");
#endif

        foreach (var spot in allDropSpots)
        {
            if (!string.IsNullOrEmpty(spot.spotId))
            {
                if (!dropSpotCache.ContainsKey(spot.spotId))
                {
                    dropSpotCache[spot.spotId] = spot;
                }
                else
                {
                    Debug.LogWarning($"[DraggableButton] Duplicate spotId: '{spot.spotId}'");
                }
            }
        }
    }
    
    private Sprite GetRealPhotoFromDropSpot()
    {
        if (dropSpotCache == null || dropSpotCache.Count == 0)
        {
            RefreshDropSpotCache();
        }

        if (dropSpotCache.TryGetValue(buttonID, out DropSpot spot))
        {
            var revealController = spot.GetComponent<ImageRevealController>();
            if (revealController != null)
            {
                var backgroundImage = revealController.GetBackgroundImage();
                if (backgroundImage != null && backgroundImage.sprite != null)
                {
                    return backgroundImage.sprite;
                }
            }
        }

#if UNITY_EDITOR
        if (debugMode)
            Debug.LogWarning($"[DraggableButton] Could not find sprite for '{buttonID}'");
#endif

        return null;
    }
    
    private Vector2 GetRealPhotoSizeFromDropSpot()
    {
        if (dropSpotCache == null || dropSpotCache.Count == 0)
        {
            RefreshDropSpotCache();
        }

        if (dropSpotCache.TryGetValue(buttonID, out DropSpot spot))
        {
            var revealController = spot.GetComponent<ImageRevealController>();
            if (revealController != null)
            {
                var backgroundImage = revealController.GetBackgroundImage();
                if (backgroundImage != null)
                {
                    var bgRT = backgroundImage.GetComponent<RectTransform>();
                    if (bgRT != null)
                    {
                        Vector2 size = bgRT.rect.size;
                        
                        if (debugMode)
                            Debug.Log($"[DraggableButton] ✅ Real photo size: {size}");
                        
                        return size;
                    }
                }
            }
        }

        if (dropSpotCache.TryGetValue(buttonID, out DropSpot fallbackSpot))
        {
            var spotRT = fallbackSpot.GetComponent<RectTransform>();
            if (spotRT != null)
            {
                if (debugMode)
                    Debug.Log($"[DraggableButton] Using DropSpot size as fallback: {spotRT.rect.size}");
                return spotRT.rect.size;
            }
        }

        if (debugMode)
            Debug.LogWarning($"[DraggableButton] ⚠️ Could not find size, using default 350x350");
        
        return new Vector2(350f, 350f);
    }
    
    // ===== Animation =====
    
    private IEnumerator AnimateSizeCoroutine(RectTransform target, Vector2 startSize, Vector2 endSize, float duration)
    {
        float elapsed = 0f;
        
        while (elapsed < duration && target != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            
            // ✅ שימוש ב-EaseOutQuad - חלק יותר בלי bounce
            float easedT = EaseOutQuad(t);
            
            target.sizeDelta = Vector2.Lerp(startSize, endSize, easedT);
            
            yield return null;
        }

        if (target != null)
        {
            target.sizeDelta = endSize;
        }
    }

    private IEnumerator AnimateReturnToBar()
    {
        if (activeDragRT == null)
        {
            canvasGroup.alpha = 1f;
            yield break;
        }

        // PERFORMANCE FIX: Use cached canvas
        Canvas host = cachedCanvas;

        Vector3 startPos = activeDragRT.position;
        Vector2 startSize = activeDragRT.sizeDelta;
        Vector2 buttonSize = new Vector2(rectTransform.rect.width, rectTransform.rect.height);

        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration && activeDragRT != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float easedT = EaseInOutQuad(t);

            Vector3 targetWorldPos = rectTransform.position;

            if (activeDragRT != null)
            {
                activeDragRT.position = Vector3.Lerp(startPos, targetWorldPos, easedT);
                activeDragRT.sizeDelta = Vector2.Lerp(startSize, buttonSize, easedT);

                CanvasGroup cg = activeDragRT.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f - (t * 0.5f);
                }
            }

            yield return null;
        }

        if (activeDragRT != null)
        {
            Destroy(activeDragRT.gameObject);
            activeDragRT = null;
            activeDragImage = null;
        }

        canvasGroup.alpha = 1f;

        Debug.Log($"[DraggableButton] ✅ Return animation complete");
    }

    
    private float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
    
    private float EaseInOutQuad(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }
    
    // ✅ easing חלק וטבעי יותר לחזרה למקום
    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
    
    // ===== Raycast & Drop =====

    private DropSpot RaycastForDropSpot(PointerEventData eventData)
    {
        // PERFORMANCE FIX: Use cached canvas
        Canvas host = cachedCanvas;
        if (host == null) return null;
        
        var gr = host.GetComponent<GraphicRaycaster>();
        if (!gr)
        {
            Debug.LogWarning("[RaycastForDropSpot] No GraphicRaycaster found!");
            return null;
        }
        
        var results = new List<RaycastResult>();
        
        if (EventSystem.current == null)
        {
            Debug.LogError("[RaycastForDropSpot] No EventSystem found!");
            return null;
        }
        
        PointerEventData customEvent = new PointerEventData(EventSystem.current);
        
        if (activeDragRT != null)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                eventData.pressEventCamera, 
                activeDragRT.position
            );
            customEvent.position = screenPos;
            
            if (debugMode)
                Debug.Log($"[RaycastForDropSpot] Using image center: {screenPos}");
        }
        else
        {
            customEvent.position = eventData.position;
        }
        
        gr.Raycast(customEvent, results);
        
        foreach (var r in results)
        {
            var spot = r.gameObject.GetComponentInParent<DropSpot>();
            if (spot)
            {
                if (debugMode)
                    Debug.Log($"[RaycastForDropSpot] Found DropSpot: {spot.spotId}");
                return spot;
            }
        }
        
        if (debugMode)
            Debug.Log($"[RaycastForDropSpot] No DropSpot found in {results.Count} results");
        
        return null;
    }

    private void HandleSuccessfulPlacement(DropSpot hitSpot)
    {
        var revealController = hitSpot.GetComponent<ImageRevealController>();
        Sprite itemSprite = null;

        if (revealController != null)
        {
            var bgImage = revealController.GetBackgroundImage();
            if (bgImage != null && bgImage.sprite != null)
            {
                itemSprite = bgImage.sprite;
            }
        }

        // Save progress before anything else
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.MarkItemAsPlaced(buttonID, itemSprite);
            GameProgressManager.Instance.ForceSave();
        }
        else
        {
            Debug.LogError($"[DraggableButton] GameProgressManager is NULL!");
        }

        hitSpot.SettleItem(activeDragRT);

        if (showConfettiOnSuccess && topCanvas && activeDragRT != null)
        {
            StartCoroutine(ShowConfetti(activeDragRT));
        }

        activeDragRT = null;
        activeDragImage = null;

        if (buttonBar != null)
        {
            buttonBar.OnButtonSuccessfullyPlaced(this, originalIndex);
            StartCoroutine(DestroyButtonAfterDelay());
        }

        Debug.Log($"[DraggableButton] ✅ Complete!");
    }


    
    private IEnumerator ShowConfetti(RectTransform target)
    {
        if (!target || !topCanvas) yield break;
        
        Debug.Log($"[DraggableButton] 🎊 CONFETTI! (add UIConfetti script for visual effect)");
        
        yield return new WaitForSeconds(0.1f);
    }
    
    private IEnumerator DestroyButtonAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);
        
        if (gameObject != null)
        {
            Debug.Log($"[DraggableButton] Destroying button: {buttonID}");
            Destroy(gameObject);
        }
    }
    
    // ===== אפשור/כיבוי Raycast על DropSpot =====
    
    private void EnableMatchingDropSpot(bool enable)
    {
        if (dropSpotCache == null || dropSpotCache.Count == 0)
        {
            RefreshDropSpotCache();
        }

        if (dropSpotCache.TryGetValue(buttonID, out DropSpot spot))
        {
            if (spot.IsSettled)
            {
                if (debugMode)
                    Debug.Log($"[EnableMatchingDropSpot] DropSpot {spot.spotId} is already settled - skipping");
                return;
            }

            var revealController = spot.GetComponent<ImageRevealController>();

            if (revealController != null)
            {
                var backgroundImage = revealController.GetBackgroundImage();

                if (backgroundImage != null)
                {
                    backgroundImage.raycastTarget = enable;

                    if (debugMode)
                        Debug.Log($"[EnableMatchingDropSpot] DropSpot {spot.spotId} - raycastTarget set to: {enable}");
                }
                else
                {
                    Debug.LogWarning($"[EnableMatchingDropSpot] Background image is NULL for {spot.spotId}");
                }
            }
            else
            {
                Debug.LogWarning($"[EnableMatchingDropSpot] No ImageRevealController for {spot.spotId}");
            }
        }
        else
        {
            Debug.LogWarning($"[EnableMatchingDropSpot] No DropSpot found with buttonID: {buttonID}");
        }
    }
}