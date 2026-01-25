using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class DraggableButton : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
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

    [Header("Audio")]
    [SerializeField] private AudioClip dragStartSound;
    [SerializeField] private AudioClip dropSuccessSound;
    [SerializeField] private AudioClip dropFailSound;

    [Header("Raycast Blocking During Drag")]
    [Tooltip("Images to disable raycast on while dragging (e.g. clickable overlays)")]
    [SerializeField] private Image[] disableRaycastOnDrag;
    [Tooltip("CanvasGroups to disable during drag")]
    [SerializeField] private CanvasGroup[] disableCanvasGroupsOnDrag;

    // Runtime references (set programmatically for dynamically created buttons)
    private Image[] runtimeDisableRaycastImages;
    private CanvasGroup[] runtimeDisableCanvasGroups;

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

    private bool wasSuccessfullyPlaced = false;

    // ✅ הוספה: שמירת ההפניה ל-ScrollRect כדי להשבית אותו
    private ScrollRect parentScrollRect;

    // ✅ משתנים לזיהוי חציית גבול הבר
    private RectTransform barRectTransform;
    private Vector2 dragStartPosition;
    private bool hasCrossedBarBoundary = false;

    // ✅ משתנה לזיהוי אם אנחנו בגרירת ScrollRect
    private bool isDraggingScrollRect = false;

    private AudioSource audioSource;

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

        // ✅ הוסף AudioSource אם אין
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
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

        // ✅ שמור את ה-RectTransform של הבר לבדיקת גבולות
        if (bar != null)
        {
            barRectTransform = bar.GetComponent<RectTransform>();
        }
    }

    public void SetButtonID(string id)
    {
        buttonID = id;
    }

    public string GetButtonID()
    {
        return buttonID;
    }

    /// <summary>
    /// Set UI elements to disable raycasts on during drag (for dynamically created buttons)
    /// </summary>
    public void SetRaycastTargets(Image[] images, CanvasGroup[] canvasGroups)
    {
        runtimeDisableRaycastImages = images;
        runtimeDisableCanvasGroups = canvasGroups;

        if (debugMode)
            Debug.Log($"[DraggableButton] SetRaycastTargets: {images?.Length ?? 0} images, {canvasGroups?.Length ?? 0} canvas groups");
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    public void OnInitializePotentialDrag(PointerEventData eventData)
    {
        // ✅ תמיד נתן ל-ScrollRect להתחיל
        if (parentScrollRect != null)
        {
            ExecuteEvents.ExecuteHierarchy(parentScrollRect.gameObject, eventData, ExecuteEvents.initializePotentialDrag);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        originalPosition = rectTransform.anchoredPosition;
        isDragging = true;
        dragStartPosition = eventData.position;
        hasCrossedBarBoundary = false;
        isDraggingScrollRect = true; // ✅ נתחיל תמיד עם ScrollRect

        // ✅ לא נכבה את blocksRaycasts עד שנתחיל באמת לגרור כפתור!

        buttonBar.OnButtonDragStarted(this, originalIndex);

        // ✅ תמיד נתחיל את ScrollRect
        if (parentScrollRect != null)
        {
            ExecuteEvents.ExecuteHierarchy(parentScrollRect.gameObject, eventData, ExecuteEvents.beginDragHandler);
        }

        if (debugMode)
            Debug.Log($"[DraggableButton] OnBeginDrag - ScrollRect started, monitoring for boundary crossing");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        // ✅ בדוק אם עדיין לא חצינו את הגבול
        if (!hasCrossedBarBoundary && barRectTransform != null)
        {
            // בדוק אם האצבע חצתה 20% מעל הבר
            Vector2 localPointInBar;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                barRectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out localPointInBar
            );

            float barTopEdge = barRectTransform.rect.yMax;
            float barHeight = barRectTransform.rect.height;
            float threshold = barTopEdge + (barHeight * 0.2f); // 20% מעל הבר

            if (localPointInBar.y > threshold)
            {
                // ✅ חצינו את הגבול! נעצור את ScrollRect ונתחיל גרירת כפתור
                hasCrossedBarBoundary = true;
                isDraggingScrollRect = false;
                isDraggingOut = true;

                // עצור את ScrollRect
                if (parentScrollRect != null)
                {
                    ExecuteEvents.ExecuteHierarchy(parentScrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);
                    parentScrollRect.StopMovement();
                }

                // ✅ השמע את אפקט הסאונד של התחלת גרירה
                PlaySound(dragStartSound);

                if (debugMode)
                    Debug.Log($"[DraggableButton] ✅ Crossed 20% boundary! Starting button drag for {buttonID}");

                buttonBar.OnButtonDraggedOut(this, originalIndex);

                // ✅ Force refresh DropSpotCache to ensure all DropSpots are loaded
                // This is especially important on mobile where loading might be slower
                Debug.Log($"[DraggableButton] Checking DropSpotCache. Current count: {DropSpotCache.Count}");
                if (DropSpotCache.Count == 0)
                {
                    Debug.LogWarning($"[DraggableButton] ⚠️ DropSpotCache is EMPTY! Forcing refresh...");
                    DropSpotCache.Refresh();
                    Debug.Log($"[DraggableButton] DropSpotCache refreshed. New count: {DropSpotCache.Count}");

                    if (DropSpotCache.Count == 0)
                    {
                        Debug.LogError($"[DraggableButton] ❌ CRITICAL: DropSpotCache is STILL empty after refresh!");
                        Debug.LogError($"[DraggableButton] This means no DropSpots exist in the scene!");
                    }
                }
                else
                {
                    Debug.Log($"[DraggableButton] ✅ DropSpotCache has {DropSpotCache.Count} spots");
                }

                EnableMatchingDropSpot(true);

                // ✅ רק עכשיו נכבה את blocksRaycasts כשאנחנו באמת גוררים כפתור
                canvasGroup.blocksRaycasts = false;

                // ✅ השבת raycasts על אלמנטים חיצוניים
                DisableExternalRaycasts();

                CreateDragVisual();
                canvasGroup.alpha = 0f;
                rectTransform.anchoredPosition = originalPosition;
            }
        }

        // ✅ אם אנחנו במצב ScrollRect - העבר את האירועים
        if (isDraggingScrollRect && parentScrollRect != null)
        {
            ExecuteEvents.ExecuteHierarchy(parentScrollRect.gameObject, eventData, ExecuteEvents.dragHandler);
            return;
        }

        // ✅ כבר חצינו את הגבול - המשך גרירה רגילה של הכפתור
        if (hasCrossedBarBoundary && activeDragRT != null)
        {
            UpdateDragPosition(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // ✅ אם אנחנו במצב ScrollRect - העבר את האירוע וסיים
        if (isDraggingScrollRect && parentScrollRect != null)
        {
            ExecuteEvents.ExecuteHierarchy(parentScrollRect.gameObject, eventData, ExecuteEvents.endDragHandler);

            isDragging = false;
            isDraggingScrollRect = false;
            hasCrossedBarBoundary = false;

            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;

            buttonBar.OnButtonReturned(this, originalIndex);

            if (debugMode)
                Debug.Log($"[DraggableButton] ScrollRect drag ended");

            return;
        }

        isDragging = false;
        hasCrossedBarBoundary = false; // ✅ אפס את הדגל לגרירה הבאה
        isDraggingScrollRect = false;

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
                        PlaySound(dropSuccessSound);
                        HandleSuccessfulPlacement(hitSpot);
                    }
                    else
                    {
                        Debug.Log($"[DraggableButton] ❌ Too far! Distance: {distance} > {dropDistanceThreshold}");
                        PlaySound(dropFailSound);
                        StartCoroutine(AnimateReturnToBar());
                    }
                }
                else
                {
                    Debug.LogError($"[DraggableButton] ❌ DropSpot has no RectTransform!");
                    PlaySound(dropFailSound);
                    StartCoroutine(AnimateReturnToBar());
                }
            }
            else
            {
                if (hitSpot != null)
                    Debug.Log($"[DraggableButton] ❌ Wrong spot! Expected: {buttonID}, Got: {hitSpot.spotId}");
                else
                    Debug.Log($"[DraggableButton] ❌ No spot found");

                PlaySound(dropFailSound);
                StartCoroutine(AnimateReturnToBar());
            }
        }
        else
        {
            // אם אין drag visual - פשוט החזר את הכפתור למצב נורמלי
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
        }

        // ✅ כבה את DropSpot רק אם הפעלנו אותו
        EnableMatchingDropSpot(false);

        // ✅ הפעל מחדש raycasts על אלמנטים חיצוניים
        EnableExternalRaycasts();

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

    /// <summary>
    /// Check if this button has been successfully placed on a DropSpot.
    /// </summary>
    public bool HasBeenPlaced()
    {
        if (GameProgressManager.Instance == null)
            return false;

        return GameProgressManager.Instance.IsItemPlaced(buttonID);
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

        // ✅ MOBILE FIX: Set material to default UI material explicitly
        activeDragImage.material = null; // Forces default UI material

        Debug.Log($"[DraggableButton] Created GameObject, now getting photo...");

        Sprite realPhoto = GetRealPhotoFromDropSpot();
        
        Debug.Log($"[DraggableButton] GetRealPhotoFromDropSpot returned: {(realPhoto != null ? realPhoto.name : "NULL")}");

        if (realPhoto != null)
        {
            // ✅ MOBILE FIX: Verify texture is loaded before assigning
            if (realPhoto.texture == null)
            {
                Debug.LogError($"[DraggableButton] ❌ CRITICAL: Sprite '{realPhoto.name}' has NULL texture!");
                Debug.LogError($"[DraggableButton] This usually means texture compression issues on mobile");
            }
            else
            {
                Debug.Log($"[DraggableButton] ✅ Sprite texture verified: {realPhoto.texture.name} ({realPhoto.texture.width}x{realPhoto.texture.height})");
            }

            activeDragImage.sprite = realPhoto;
            activeDragImage.enabled = false; // Disable temporarily
            activeDragImage.enabled = true;  // Re-enable to force refresh

            Debug.Log($"[DraggableButton] ✅ SET SPRITE TO: {realPhoto.name}");

            // Verify sprite was actually set
            if (activeDragImage.sprite == null)
            {
                Debug.LogError($"[DraggableButton] ❌ CRITICAL: Sprite was NULL after assignment!");
            }
            else if (activeDragImage.sprite != realPhoto)
            {
                Debug.LogError($"[DraggableButton] ❌ CRITICAL: Sprite assignment failed - sprite doesn't match!");
            }
            else
            {
                Debug.Log($"[DraggableButton] ✅ Sprite verified: {activeDragImage.sprite.name}");
            }
        }
        else
        {
            // ⚠️ WARNING: Real photo not found! This should not happen in normal gameplay.
            // This means either:
            // 1. DropSpot with matching buttonID doesn't exist
            // 2. DropSpot's ImageRevealController is missing or misconfigured
            // 3. Background sprite is not assigned in the Inspector
            Debug.LogError($"[DraggableButton] ❌ CRITICAL: Could not find real photo for buttonID '{buttonID}'!");
            Debug.LogError($"[DraggableButton] This will cause white cube or button icon to appear during drag!");
            Debug.LogError($"[DraggableButton] Please check: 1) DropSpot exists with spotId='{buttonID}' 2) ImageRevealController is attached 3) Background sprite is assigned");

            // Try to use button's sprite as last resort fallback
            Image myImage = GetComponent<Image>();
            if (myImage != null && myImage.sprite != null)
            {
                activeDragImage.sprite = myImage.sprite;
                Debug.LogWarning($"[DraggableButton] ⚠️ Using button sprite as fallback: {myImage.sprite.name}");
            }
            else
            {
                // Even fallback failed - destroy the drag visual and abort
                Debug.LogError($"[DraggableButton] ❌ ABORT: No sprite available at all! Destroying drag visual.");
                Destroy(activeDragRT.gameObject);
                activeDragRT = null;
                activeDragImage = null;
                return;
            }
        }

        activeDragImage.color = Color.white;
        activeDragImage.raycastTarget = false;
        activeDragImage.preserveAspect = true;
        activeDragImage.type = Image.Type.Simple; // ✅ MOBILE FIX: Explicitly set to Simple
        activeDragImage.useSpriteMesh = false;     // ✅ MOBILE FIX: Disable sprite mesh
        
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

    private Sprite GetRealPhotoFromDropSpot()
    {
        Debug.Log($"[DraggableButton] === GET REAL PHOTO START === buttonID: '{buttonID}'");
        Debug.Log($"[DraggableButton] DropSpotCache.Count: {DropSpotCache.Count}");

        DropSpot spot = DropSpotCache.Get(buttonID);

        if (spot == null)
        {
            Debug.LogError($"[DraggableButton] ❌ buttonID '{buttonID}' NOT FOUND in DropSpotCache!");
            Debug.LogError($"[DraggableButton] Available spots in cache:");
            var allSpots = DropSpotCache.GetAll();
            foreach (var s in allSpots)
            {
                Debug.LogError($"   - '{s.spotId}'");
            }
            Debug.Log($"[DraggableButton] === GET REAL PHOTO END === returning NULL");
            return null;
        }

        Debug.Log($"[DraggableButton] ✅ FOUND DropSpot in cache: '{spot.spotId}' on GameObject: {spot.gameObject.name}");

        var revealController = spot.GetComponent<ImageRevealController>();
        if (revealController == null)
        {
            Debug.LogError($"[DraggableButton] ❌ No ImageRevealController component found on {spot.gameObject.name}!");
            Debug.Log($"[DraggableButton] === GET REAL PHOTO END === returning NULL");
            return null;
        }

        Debug.Log($"[DraggableButton] ✅ Found ImageRevealController");

        var backgroundImage = revealController.GetBackgroundImage();

        if (backgroundImage == null)
        {
            Debug.LogError($"[DraggableButton] ❌ GetBackgroundImage() returned NULL!");
            Debug.LogError($"[DraggableButton] This means backgroundImage is not assigned in Inspector!");
            Debug.Log($"[DraggableButton] === GET REAL PHOTO END === returning NULL");
            return null;
        }

        Debug.Log($"[DraggableButton] ✅ backgroundImage component exists");

        if (backgroundImage.sprite == null)
        {
            Debug.LogError($"[DraggableButton] ❌ backgroundImage.sprite is NULL!");
            Debug.LogError($"[DraggableButton] backgroundImage exists but has no sprite assigned!");
            Debug.Log($"[DraggableButton] === GET REAL PHOTO END === returning NULL");
            return null;
        }

        Debug.Log($"[DraggableButton] 🎉 SUCCESS! sprite name: '{backgroundImage.sprite.name}', texture: {backgroundImage.sprite.texture != null}");
        return backgroundImage.sprite;
    }

    private Vector2 GetRealPhotoSizeFromDropSpot()
    {
        DropSpot spot = DropSpotCache.Get(buttonID);

        if (spot != null)
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

            // Fallback to DropSpot size
            var spotRT = spot.GetComponent<RectTransform>();
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
        DropSpot spot = DropSpotCache.Get(buttonID);

        if (spot == null)
        {
            Debug.LogWarning($"[EnableMatchingDropSpot] No DropSpot found with buttonID: {buttonID}");
            return;
        }

        if (spot.IsSettled)
        {
            if (debugMode)
                Debug.Log($"[EnableMatchingDropSpot] DropSpot {spot.spotId} is already settled - skipping");
            return;
        }

        var revealController = spot.GetComponent<ImageRevealController>();

        if (revealController == null)
        {
            Debug.LogWarning($"[EnableMatchingDropSpot] No ImageRevealController for {spot.spotId}");
            return;
        }

        var backgroundImage = revealController.GetBackgroundImage();

        if (backgroundImage == null)
        {
            Debug.LogWarning($"[EnableMatchingDropSpot] Background image is NULL for {spot.spotId}");
            return;
        }

        backgroundImage.raycastTarget = enable;

        if (debugMode)
            Debug.Log($"[EnableMatchingDropSpot] DropSpot {spot.spotId} - raycastTarget set to: {enable}");
    }

    // ===== Audio =====

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // ===== Raycast Control During Drag =====

    private void DisableExternalRaycasts()
    {
        // Disable specific images (serialized)
        if (disableRaycastOnDrag != null)
        {
            foreach (var img in disableRaycastOnDrag)
            {
                if (img != null)
                {
                    img.raycastTarget = false;
                    if (debugMode)
                        Debug.Log($"[DraggableButton] Disabled raycast on: {img.name}");
                }
            }
        }

        // Disable specific images (runtime/dynamic)
        if (runtimeDisableRaycastImages != null)
        {
            foreach (var img in runtimeDisableRaycastImages)
            {
                if (img != null)
                {
                    img.raycastTarget = false;
                    if (debugMode)
                        Debug.Log($"[DraggableButton] Disabled runtime raycast on: {img.name}");
                }
            }
        }

        // Disable canvas groups (serialized)
        if (disableCanvasGroupsOnDrag != null)
        {
            foreach (var cg in disableCanvasGroupsOnDrag)
            {
                if (cg != null)
                {
                    cg.blocksRaycasts = false;
                    if (debugMode)
                        Debug.Log($"[DraggableButton] Disabled CanvasGroup raycasts on: {cg.name}");
                }
            }
        }

        // Disable canvas groups (runtime/dynamic)
        if (runtimeDisableCanvasGroups != null)
        {
            foreach (var cg in runtimeDisableCanvasGroups)
            {
                if (cg != null)
                {
                    cg.blocksRaycasts = false;
                    if (debugMode)
                        Debug.Log($"[DraggableButton] Disabled runtime CanvasGroup raycasts on: {cg.name}");
                }
            }
        }
    }

    private void EnableExternalRaycasts()
    {
        // Re-enable specific images (serialized)
        if (disableRaycastOnDrag != null)
        {
            foreach (var img in disableRaycastOnDrag)
            {
                if (img != null)
                {
                    img.raycastTarget = true;
                    if (debugMode)
                        Debug.Log($"[DraggableButton] Re-enabled raycast on: {img.name}");
                }
            }
        }

        // Re-enable specific images (runtime/dynamic)
        if (runtimeDisableRaycastImages != null)
        {
            foreach (var img in runtimeDisableRaycastImages)
            {
                if (img != null)
                {
                    img.raycastTarget = true;
                    if (debugMode)
                        Debug.Log($"[DraggableButton] Re-enabled runtime raycast on: {img.name}");
                }
            }
        }

        // Re-enable canvas groups (serialized)
        if (disableCanvasGroupsOnDrag != null)
        {
            foreach (var cg in disableCanvasGroupsOnDrag)
            {
                if (cg != null)
                {
                    cg.blocksRaycasts = true;
                    if (debugMode)
                        Debug.Log($"[DraggableButton] Re-enabled CanvasGroup raycasts on: {cg.name}");
                }
            }
        }

        // Re-enable canvas groups (runtime/dynamic)
        if (runtimeDisableCanvasGroups != null)
        {
            foreach (var cg in runtimeDisableCanvasGroups)
            {
                if (cg != null)
                {
                    cg.blocksRaycasts = true;
                    if (debugMode)
                        Debug.Log($"[DraggableButton] Re-enabled runtime CanvasGroup raycasts on: {cg.name}");
                }
            }
        }
    }
}