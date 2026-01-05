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

    // Offset above finger/cursor (in pixels)
    private const float FINGER_OFFSET = 120f;

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
    /// Update the position of the drag visual - follows finger with offset above.
    /// PERFORMANCE: This is called 60 times per second during drag - must be fast!
    /// </summary>
    public void UpdatePosition(PointerEventData eventData)
    {
        if (dragVisualRT == null) return;

        // PERFORMANCE FIX: Use cached canvas instead of FindTopCanvas()
        if (cachedCanvas == null) return;

        Vector3 worldPos;
        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            (RectTransform)cachedCanvas.transform,
            eventData.position,
            eventData.pressEventCamera,
            out worldPos
        );

        // Position the image above the finger/cursor
        // Center it (half height) + additional offset to keep it above the finger
        Vector3 offset = new Vector3(0, dragVisualRT.rect.height * 0.5f + FINGER_OFFSET, 0);
        dragVisualRT.position = worldPos + offset;
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