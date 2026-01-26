using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Manages the visual "ghost" that appears when dragging a button.
/// Handles creation, positioning, and cleanup of the drag visual.
/// </summary>
public class DragVisualManager
{
    private readonly string buttonID;
    private readonly Canvas topCanvas;
    private readonly float sizeAnimationDuration;
    private readonly bool debugMode;

    // Offset configuration - physical distance above finger
    // Using approximate conversion: 1cm ≈ 160px on typical phones (420 DPI)
    private const float FINGER_OFFSET_CM = 1.5f;      // Target: 1.5cm above finger
    private const float CM_TO_PX = 160f;              // Conversion factor (adjust based on device DPI)

    // Alternative: Adaptive offset based on screen/object ratio (more flexible)
    private const float SCREEN_HEIGHT_RATIO = 0.08f;  // 8% of screen height
    private const float OBJECT_HEIGHT_RATIO = 0.6f;   // 60% of object height
    private const bool USE_PHYSICAL_OFFSET = false;   // Toggle: true = fixed cm, false = adaptive %

    // Small object visibility fix - ensures finger never hides dragged objects
    private const float MIN_OFFSET_PX = 200f;         // Minimum distance from finger (increased for small objects)
    private const float SMALL_BOOST_MAX = 250f;       // Max extra boost for very small objects
    private const float SMALL_BOOST_REF = 250f;       // Objects smaller than this get boost
    private const float X_OFFSET_RATIO = 0.7f;        // Diagonal ratio (move right as well as up)
    private const float MIN_X_OFFSET_PX = 100f;       // Minimum horizontal offset for small objects

    private RectTransform dragVisualRT;
    private Image dragVisualImage;
    private MonoBehaviour coroutineHost;

    // PERFORMANCE FIX: Cache canvas to avoid FindObjectsOfType in hot path
    private Canvas cachedCanvas;

    public bool IsActive => dragVisualRT != null;
    public RectTransform DragVisual => dragVisualRT;

    public DragVisualManager(string buttonID, Canvas topCanvas, float sizeAnimationDuration = 0.5f, bool debugMode = false)
    {
        this.buttonID = buttonID;
        this.topCanvas = topCanvas;
        this.sizeAnimationDuration = sizeAnimationDuration;
        this.debugMode = debugMode;

        // PERFORMANCE FIX: Cache canvas once during initialization
        cachedCanvas = FindTopCanvas();
    }

    /// <summary>
    /// Create the drag visual at the current button position.
    /// </summary>
    public void Create(RectTransform buttonRect, MonoBehaviour host)
    {
        if (dragVisualRT != null)
        {
            Debug.LogWarning("[DragVisualManager] Drag visual already exists!");
            return;
        }

        coroutineHost = host;

        // PERFORMANCE FIX: Use cached canvas
        if (cachedCanvas == null)
        {
            Debug.LogError("[DragVisualManager] No canvas found!");
            return;
        }

        // Create ghost object
        GameObject go = new GameObject("DragGhost_" + buttonID);
        go.AddComponent<RectTransform>();
        go.AddComponent<CanvasGroup>();
        go.AddComponent<Image>();

        dragVisualRT = go.GetComponent<RectTransform>();
        dragVisualRT.SetParent(cachedCanvas.transform, false);

        dragVisualImage = go.GetComponent<Image>();

        // Get the real photo sprite from the DropSpot
        Sprite realPhoto = GetRealPhotoSprite();

        if (realPhoto != null)
        {
            dragVisualImage.sprite = realPhoto;
        }
        else
        {
            // Fallback to button's sprite
            Image buttonImage = buttonRect.GetComponent<Image>();
            if (buttonImage != null)
            {
                dragVisualImage.sprite = buttonImage.sprite;
            }
        }

        dragVisualImage.color = Color.white;
        dragVisualImage.raycastTarget = false;
        dragVisualImage.preserveAspect = true;

        // Configure CanvasGroup
        CanvasGroup cg = dragVisualRT.GetComponent<CanvasGroup>();
        cg.interactable = false;
        cg.blocksRaycasts = false;
        cg.alpha = 1f;

        // Size animation
        Vector2 targetSize = GetRealPhotoSize();
        Vector2 startSize = targetSize * 0.3f;

        dragVisualRT.sizeDelta = startSize;

        if (coroutineHost != null)
        {
            coroutineHost.StartCoroutine(
                DragAnimator.AnimateSize(dragVisualRT, startSize, targetSize, sizeAnimationDuration)
            );
        }

        dragVisualRT.SetAsLastSibling();

        // Position at button location
        dragVisualRT.position = buttonRect.position;

        #if UNITY_EDITOR
        if (debugMode)
            Debug.Log($"[DragVisualManager] Created drag visual for {buttonID}");
        #endif
    }

    /// <summary>
    /// Calculate offset above finger, adjusted by screen position.
    /// - USE_PHYSICAL_OFFSET = true: Fixed physical distance (1.5cm ≈ 240px)
    /// - USE_PHYSICAL_OFFSET = false: Adaptive (screen/object) + strong boost for small objects
    /// - Automatically reduces offset in bottom third of screen
    /// </summary>
    private float GetAdaptiveOffset(float fingerScreenY)
    {
        if (dragVisualRT == null) return MIN_OFFSET_PX;

        // Calculate base offset
        float baseOffset;
        if (USE_PHYSICAL_OFFSET)
        {
            baseOffset = FINGER_OFFSET_CM * CM_TO_PX;
        }
        else
        {
            float screenBased = Screen.height * SCREEN_HEIGHT_RATIO;
            float objectHeight = dragVisualRT.rect.height;
            float objectBased = objectHeight * OBJECT_HEIGHT_RATIO;

            // Strong boost for small objects: smaller height => bigger boost
            float smallObjectBoost = Mathf.Clamp(
                SMALL_BOOST_REF - objectHeight,
                0f,
                SMALL_BOOST_MAX
            );

            // Build adaptive base, but never below MIN_OFFSET_PX
            baseOffset = Mathf.Max(
                MIN_OFFSET_PX,
                Mathf.Min(screenBased, objectBased) + smallObjectBoost
            );
        }

        // Calculate finger position (0 = bottom, 1 = top)
        float fingerPosNormalized = fingerScreenY / Screen.height;

        // Special case: In bottom 10% of screen, finger is at 20% from object bottom
        // This means finger is right on the lower part of the object for precise placement
        if (fingerPosNormalized < 0.1f)
        {
            return -0.2f * dragVisualRT.rect.height;
        }

        // Smart offset reduction in bottom third (10%-33%) of screen
        if (fingerPosNormalized < 0.33f)
        {
            float bottomThresholdOffset = -0.2f * dragVisualRT.rect.height;
            float t = (fingerPosNormalized - 0.1f) / (0.33f - 0.1f);
            return Mathf.Lerp(bottomThresholdOffset, baseOffset, t);
        }

        return baseOffset;
    }

    /// <summary>
    /// Update the position of the drag visual - follows finger with diagonal offset.
    /// PERFORMANCE: This is called 60 times per second during drag - must be fast!
    /// Smart offset: Moves diagonally (right + up) so finger never hides the object.
    /// Automatically reduces offset when finger is near bottom of screen.
    /// </summary>
    public void UpdatePosition(PointerEventData eventData)
    {
        if (dragVisualRT == null) return;
        if (cachedCanvas == null) return;

        // 1) Finger position in screen pixels
        Vector2 pointer = eventData.position;

        // 2) Adaptive Y offset (boosted for small objects)
        float yOffset = GetAdaptiveOffset(pointer.y);

        // 3) Diagonal movement: move right + up (or right + down if near top edge)
        float xOffset = Mathf.Max(yOffset * X_OFFSET_RATIO, MIN_X_OFFSET_PX);

        // Try positioning up-right first
        Vector2 candidateUp = pointer + new Vector2(xOffset, yOffset);
        float topY = candidateUp.y + dragVisualRT.rect.height * 0.5f;

        // If would go off top of screen, flip to down-right instead
        Vector2 finalScreenPos = (topY > Screen.height)
            ? pointer + new Vector2(xOffset, -yOffset)
            : candidateUp;

        // 4) Clamp within screen bounds
        float halfW = dragVisualRT.rect.width * 0.5f;
        float halfH = dragVisualRT.rect.height * 0.5f;

        finalScreenPos.x = Mathf.Clamp(finalScreenPos.x, halfW, Screen.width - halfW);
        finalScreenPos.y = Mathf.Clamp(finalScreenPos.y, halfH, Screen.height - halfH);

        // 5) Convert to world position and apply
        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)cachedCanvas.transform,
            finalScreenPos,
            eventData.pressEventCamera,
            out worldPos
        );

        dragVisualRT.position = worldPos;
    }

    /// <summary>
    /// Destroy the drag visual.
    /// </summary>
    public void Destroy()
    {
        if (dragVisualRT != null)
        {
            Object.Destroy(dragVisualRT.gameObject);
            dragVisualRT = null;
            dragVisualImage = null;
        }
    }

    private Canvas FindTopCanvas()
    {
        if (topCanvas != null) return topCanvas;

        Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay || c.isRootCanvas)
            {
                return c;
            }
        }

        return null;
    }

    private Sprite GetRealPhotoSprite()
    {
        DropSpot spot = DropSpotCache.Get(buttonID);
        if (spot == null) return null;

        var revealController = spot.GetComponent<ImageRevealController>();
        if (revealController == null) return null;

        var backgroundImage = revealController.GetBackgroundImage();
        if (backgroundImage == null) return null;

        return backgroundImage.sprite;
    }

    private Vector2 GetRealPhotoSize()
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
                        return bgRT.rect.size;
                    }
                }
            }

            // Fallback to DropSpot size
            var spotRT = spot.GetComponent<RectTransform>();
            if (spotRT != null)
            {
                return spotRT.rect.size;
            }
        }

        // Default size
        return new Vector2(350f, 350f);
    }
}