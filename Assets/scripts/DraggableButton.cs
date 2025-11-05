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
    
    private static Dictionary<string, DropSpot> dropSpotCache;
    
    private bool wasSuccessfullyPlaced = false;
    
    // ✅ הוספה: שמירת ההפניה ל-ScrollRect כדי להשבית אותו
    private ScrollRect parentScrollRect;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // ✅ מצא את ה-ScrollRect ההורה
        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    void OnDestroy()
    {
        if (activeDragRT != null)
        {
            Debug.Log($"[DraggableButton] OnDestroy - מנקה activeDragRT");
            Destroy(activeDragRT.gameObject);
            activeDragRT = null;
            activeDragImage = null;
        }
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
        // ✅ אם כבר יש drag visual - אל תיצור עוד אחד!
        if (activeDragRT != null)
        {
            Debug.LogWarning($"[DraggableButton] Drag visual already exists! Skipping creation.");
            return;
        }
        
        Debug.Log($"[DraggableButton] === CREATE DRAG VISUAL START === buttonID: {buttonID}");
        
        Canvas host = topCanvas;
        if (host == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.isRootCanvas)
                {
                    host = c;
                    break;
                }
            }
        }
        
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
        
        Debug.Log($"[DraggableButton] Created GameObject, now getting photo...");
        
        Sprite realPhoto = GetRealPhotoFromDropSpot();
        
        Debug.Log($"[DraggableButton] GetRealPhotoFromDropSpot returned: {(realPhoto != null ? realPhoto.name : "NULL")}");
        
        if (realPhoto != null)
        {
            activeDragImage.sprite = realPhoto;
            Debug.Log($"[DraggableButton] ✅ SET SPRITE TO: {realPhoto.name}");
        }
        else
        {
            Image myImage = GetComponent<Image>();
            if (myImage != null)
            {
                activeDragImage.sprite = myImage.sprite;
                Debug.Log($"[DraggableButton] ⚠️ Using button sprite: {(myImage.sprite != null ? myImage.sprite.name : "NULL")}");
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
        
        Debug.Log($"[DraggableButton] 📏 Animating size from {startSize} to {targetSize}");
        
        activeDragRT.sizeDelta = startSize;
        StartCoroutine(AnimateSizeCoroutine(activeDragRT, startSize, targetSize, sizeAnimationDuration));
        
        activeDragRT.SetAsLastSibling();
        
        // ✅ מקם את התמונה במיקום הנוכחי של הכפתור המקורי
        activeDragRT.position = rectTransform.position;
        
        Debug.Log($"[DraggableButton] === CREATE DRAG VISUAL END ===");
    }
    
    private void UpdateDragPosition(PointerEventData eventData)
    {
        // ✅ אם אין drag visual - אל תעשה כלום!
        if (activeDragRT == null)
        {
            if (debugMode)
                Debug.LogWarning($"[DraggableButton] UpdateDragPosition called but activeDragRT is null!");
            return;
        }
        
        Canvas host = topCanvas;
        if (host == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.isRootCanvas)
                {
                    host = c;
                    break;
                }
            }
        }
        
        if (host == null) return;
        
        // ✅ חישוב מיקום מדויק יותר
        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)host.transform, 
            eventData.position, 
            eventData.pressEventCamera, 
            out worldPos
        );
        
        // ✅ אופסט מדויק - התמונה ממורכזת מעל האצבע
        Vector3 offset = new Vector3(0, activeDragRT.rect.height * 0.5f, 0);
        activeDragRT.position = worldPos + offset;
        
        if (debugMode)
        {
            Debug.Log($"[DraggableButton] UpdateDragPosition: screen={eventData.position}, world={worldPos}, final={activeDragRT.position}");
        }
    }

    // ===== DropSpot Cache =====

    private static void RefreshDropSpotCache()
    {
        Debug.Log($"[DraggableButton] === REFRESH CACHE START ===");

        if (dropSpotCache == null)
        {
            dropSpotCache = new Dictionary<string, DropSpot>();
            Debug.Log($"[DraggableButton] Created new cache dictionary");
        }

        dropSpotCache.Clear();

        // ✅ תיקון: מצא גם objects לא פעילים!
        var allDropSpots = FindObjectsOfType<DropSpot>(true); // ← הוסף true!

        Debug.Log($"[DraggableButton] Found {allDropSpots.Length} DropSpots in scene");

        foreach (var spot in allDropSpots)
        {
            Debug.Log($"[DraggableButton] Checking spot: spotId='{spot.spotId}', isEmpty={string.IsNullOrEmpty(spot.spotId)}");

            if (!string.IsNullOrEmpty(spot.spotId))
            {
                if (!dropSpotCache.ContainsKey(spot.spotId))
                {
                    dropSpotCache[spot.spotId] = spot;
                    Debug.Log($"[DraggableButton] ✅ Cached: '{spot.spotId}'");
                }
                else
                {
                    Debug.LogWarning($"[DraggableButton] ⚠️ Duplicate spotId: '{spot.spotId}'");
                }
            }
            else
            {
                Debug.LogWarning($"[DraggableButton] ⚠️ Spot has empty spotId!");
            }
        }

        Debug.Log($"[DraggableButton] === REFRESH CACHE END === Total cached: {dropSpotCache.Count}");
    }
    
    private Sprite GetRealPhotoFromDropSpot()
    {
        Debug.Log($"[DraggableButton] === GET REAL PHOTO START === buttonID: '{buttonID}'");
        
        if (dropSpotCache == null || dropSpotCache.Count == 0)
        {
            Debug.Log($"[DraggableButton] Cache is empty, refreshing...");
            RefreshDropSpotCache();
        }
        else
        {
            Debug.Log($"[DraggableButton] Cache already has {dropSpotCache.Count} items");
        }

        Debug.Log($"[DraggableButton] Searching for buttonID: '{buttonID}' in cache...");

        if (dropSpotCache.TryGetValue(buttonID, out DropSpot spot))
        {
            Debug.Log($"[DraggableButton] ✅ FOUND DropSpot in cache: '{spot.spotId}'");
            
            var revealController = spot.GetComponent<ImageRevealController>();
            if (revealController != null)
            {
                Debug.Log($"[DraggableButton] ✅ Found ImageRevealController");
                
                var backgroundImage = revealController.GetBackgroundImage();
                
                if (backgroundImage != null)
                {
                    Debug.Log($"[DraggableButton] ✅ backgroundImage component exists");
                    
                    if (backgroundImage.sprite != null)
                    {
                        Debug.Log($"[DraggableButton] 🎉 SUCCESS! sprite name: '{backgroundImage.sprite.name}'");
                        return backgroundImage.sprite;
                    }
                    else
                    {
                        Debug.LogError($"[DraggableButton] ❌ backgroundImage.sprite is NULL!");
                    }
                }
                else
                {
                    Debug.LogError($"[DraggableButton] ❌ GetBackgroundImage() returned NULL!");
                }
            }
            else
            {
                Debug.LogError($"[DraggableButton] ❌ No ImageRevealController component found!");
            }
        }
        else
        {
            Debug.LogError($"[DraggableButton] ❌ buttonID '{buttonID}' NOT FOUND in cache!");
            Debug.Log($"[DraggableButton] Available keys in cache:");
            foreach (var key in dropSpotCache.Keys)
            {
                Debug.Log($"  - '{key}'");
            }
        }

        Debug.Log($"[DraggableButton] === GET REAL PHOTO END === returning NULL");
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

        Debug.Log($"[DraggableButton] 🔙 Starting return animation");

        Canvas host = topCanvas;
        if (host == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.isRootCanvas)
                {
                    host = c;
                    break;
                }
            }
        }

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
        Canvas host = topCanvas;
        if (host == null)
        {
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var c in canvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.isRootCanvas)
                {
                    host = c;
                    break;
                }
            }
        }
        
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
        Debug.Log($"[DraggableButton] 🎉 HandleSuccessfulPlacement for {buttonID}");

        // ✅ קבל את התמונה
        var revealController = hitSpot.GetComponent<ImageRevealController>();
        Sprite itemSprite = null;

        if (revealController != null)
        {
            var bgImage = revealController.GetBackgroundImage();
            if (bgImage != null && bgImage.sprite != null)
            {
                itemSprite = bgImage.sprite;
                Debug.Log($"[DraggableButton] Found sprite: {bgImage.sprite.name}");
            }
        }

        // ✅✅✅ שמור לפני כל דבר אחר!
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.MarkItemAsPlaced(buttonID, itemSprite);
            Debug.Log($"[DraggableButton] ✅ SAVED: {buttonID}");

            // ✅ שמור מיד (לא מחכים ל-autosave)
            GameProgressManager.Instance.ForceSave();
            Debug.Log($"[DraggableButton] ✅ FORCE SAVED!");
        }
        else
        {
            Debug.LogError($"[DraggableButton] ❌ GameProgressManager is NULL!");
        }

        // עכשיו תעשה את שאר הדברים
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

        Debug.Log($"[DraggableButton] ✨ SPARKLES! Spreading across entire revealing area");

        // Find the revealing area (the area containing all drop spots)
        RectTransform revealingArea = FindRevealingArea();

        // Create sparkle burst across the entire revealing area
        SparkleBurstEffect.Burst(topCanvas, revealingArea, confettiCount, 2f);

        yield return new WaitForSeconds(0.1f);
    }

    private RectTransform FindRevealingArea()
    {
        // Try to find a DropSpotBatchManager which manages all drop spots
        var batchManager = FindObjectOfType<DropSpotBatchManager>();
        if (batchManager != null)
        {
            return batchManager.GetComponent<RectTransform>();
        }

        // Fallback: find the first DropSpot and get its parent container
        var firstDropSpot = FindObjectOfType<DropSpot>();
        if (firstDropSpot != null && firstDropSpot.transform.parent != null)
        {
            return firstDropSpot.transform.parent.GetComponent<RectTransform>();
        }

        // Last fallback: use the entire canvas
        if (debugMode)
            Debug.Log("[DraggableButton] Using entire canvas for sparkle area");

        return topCanvas.GetComponent<RectTransform>();
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