using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Main draggable button component. Handles drag events and coordinates
/// with helper classes for visuals, validation, and animations.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class DraggableButton : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Settings")]
    [SerializeField] private float dragThreshold = 50f;
    [SerializeField] private float dropDistanceThreshold = 5f;  // Distance from dragged object to DropSpot - nearly perfect!
    [SerializeField] private Canvas topCanvas;

    [Header("Animation")]
    [SerializeField] private float sizeAnimationDuration = 0.5f;

    [Header("Success Effects")]
    [SerializeField] private bool showConfettiOnSuccess = true;
    [SerializeField] private int confettiCount = 50;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dragStartSound;
    [Tooltip("Sound when starting to drag from bar")]
    [SerializeField] private AudioClip returnToBarSound;
    [Tooltip("Sound when item returns to bar (wrong placement)")]
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.6f;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Components
    private ScrollableButtonBar buttonBar;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private ScrollRect parentScrollRect;

    // State
    private string buttonID;
    private int originalIndex;
    private Vector2 originalPosition;
    private bool isDragging = false;
    private bool isDraggingOut = false;
    private bool wasSuccessfullyPlaced = false;

    // Helper classes
    private DragVisualManager visualManager;
    private DragDropValidator dropValidator;

    void Awake()
    {
        // Force precise drop distance (override any Inspector value)
        dropDistanceThreshold = 70f;

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Initialize audio source
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        parentScrollRect = GetComponentInParent<ScrollRect>();
    }

    void Start()
    {
        // Don't initialize topCanvas here - wait until we need it (lazy init)
        // This fixes the issue when coming from Level Selection scene
    }

    void OnDestroy()
    {
        visualManager?.Destroy();
    }

    #region Public API

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

    #endregion

    #region Audio

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.volume = soundVolume;
            audioSource.PlayOneShot(clip);
        }
    }

    #endregion

    #region Drag Event Handlers

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Check with TutorialSlideManager if dragging is allowed
        if (TutorialSlideManager.Instance != null)
        {
            if (!TutorialSlideManager.Instance.CanStartDrag())
            {
                // Dragging blocked by tutorial (stage 4: must click hint first)
                #if UNITY_EDITOR
                if (debugMode)
                    Debug.Log($"[DraggableButton] Drag blocked by tutorial for {buttonID}");
                #endif
                return; // BLOCK THE DRAG!
            }
        }

        // ✅ LAZY INIT: Initialize Canvas and managers on first drag
        // This ensures scene is fully loaded (fixes issue when coming from Level Selection)
        if (topCanvas == null)
        {
            topCanvas = FindTopCanvas();
        }

        // Initialize helper classes if not already done
        if (visualManager == null)
        {
            visualManager = new DragVisualManager(buttonID, topCanvas, sizeAnimationDuration, debugMode);
        }

        if (dropValidator == null)
        {
            Canvas canvas = topCanvas != null ? topCanvas : GetComponentInParent<Canvas>();
            dropValidator = new DragDropValidator(canvas, dropDistanceThreshold, debugMode);
        }

        originalPosition = rectTransform.anchoredPosition;
        isDragging = true;

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = false;

        // Disable ScrollRect to prevent interference
        if (parentScrollRect != null)
        {
            parentScrollRect.enabled = false;
        }

        // ✅ Disable ALL SmlAnimManager buttons during drag to prevent blocking
        if (SmlAnimManager.Instance != null)
        {
            SmlAnimManager.Instance.DisableAllButtonsForDrag();
        }

        buttonBar?.OnButtonDragStarted(this, originalIndex);

        // Enable raycast on matching DropSpot
        DragDropValidator.SetDropSpotRaycastEnabled(buttonID, true);
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

        // Create drag visual when threshold is crossed
        if (!wasOut && isDraggingOut)
        {
            buttonBar?.OnButtonDraggedOut(this, originalIndex);
            visualManager.Create(rectTransform, this);
            canvasGroup.alpha = 0f;

            // Play drag start sound
            PlaySound(dragStartSound);

            // Return original button to its position
            rectTransform.anchoredPosition = originalPosition;
        }

        // Update drag visual position
        if (isDraggingOut && visualManager.IsActive)
        {
            visualManager.UpdatePosition(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;

        // Re-enable ScrollRect
        if (parentScrollRect != null)
        {
            parentScrollRect.enabled = true;
        }

        // ✅ Restore SmlAnimManager buttons after drag ends
        if (SmlAnimManager.Instance != null)
        {
            SmlAnimManager.Instance.RestoreButtonsAfterDrag();
        }

        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (visualManager.IsActive)
        {
            // Validate drop
            string failureReason;
            DropSpot hitSpot = dropValidator.ValidateDrop(
                buttonID,
                visualManager.DragVisual,
                eventData,
                out failureReason
            );

            if (hitSpot != null)
            {
                // SUCCESS!
                #if UNITY_EDITOR
                if (debugMode)
                    Debug.Log($"[DraggableButton] ✅ Successful placement: {buttonID}");
                #endif

                wasSuccessfullyPlaced = true;
                canvasGroup.alpha = 1f;
                HandleSuccessfulPlacement(hitSpot);
            }
            else
            {
                // FAILURE - return to bar
                #if UNITY_EDITOR
                if (debugMode)
                    Debug.Log($"[DraggableButton] ❌ Invalid drop: {failureReason}");
                #endif

                StartCoroutine(AnimateReturnToBar());
            }
        }
        else
        {
            // No drag visual - just restore
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
        }

        // Disable raycast on DropSpot
        DragDropValidator.SetDropSpotRaycastEnabled(buttonID, false);

        // Notify button bar
        if (!wasSuccessfullyPlaced)
        {
            buttonBar?.OnButtonReturned(this, originalIndex);
        }

        isDraggingOut = false;
    }

    #endregion

    #region Success Handling

    private void HandleSuccessfulPlacement(DropSpot hitSpot)
    {
        // Get sprite for saving
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

        // SAVE IMMEDIATELY
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.MarkItemAsPlaced(buttonID, itemSprite);
            GameProgressManager.Instance.ForceSave();

            #if UNITY_EDITOR
            if (debugMode)
                Debug.Log($"[DraggableButton] ✅ Saved: {buttonID}");
            #endif
        }
        else
        {
            Debug.LogError("[DraggableButton] GameProgressManager is NULL!");
        }

        // Settle the item on the spot
        hitSpot.SettleItem(visualManager.DragVisual);

        // Show confetti
        if (showConfettiOnSuccess && topCanvas && visualManager.IsActive)
        {
            StartCoroutine(ShowConfetti(visualManager.DragVisual));
        }

        // Cleanup
        visualManager.Destroy();

        // Notify button bar
        buttonBar?.OnButtonSuccessfullyPlaced(this, originalIndex);

        // Destroy this button after delay
        StartCoroutine(DestroyButtonAfterDelay());
    }

    private IEnumerator ShowConfetti(RectTransform target)
    {
        if (target == null || topCanvas == null) yield break;

        UIConfetti.Burst(topCanvas, target, confettiCount, 1.2f);

        yield return new WaitForSeconds(0.1f);
    }

    private IEnumerator DestroyButtonAfterDelay()
    {
        yield return new WaitForSeconds(0.2f);

        if (gameObject != null)
        {
            #if UNITY_EDITOR
            if (debugMode)
                Debug.Log($"[DraggableButton] Destroying button: {buttonID}");
            #endif

            Destroy(gameObject);
        }
    }

    #endregion

    #region Return Animation

    private IEnumerator AnimateReturnToBar()
    {
        if (!visualManager.IsActive)
        {
            canvasGroup.alpha = 1f;
            yield break;
        }

        // Play return to bar sound
        PlaySound(returnToBarSound);

        yield return StartCoroutine(
            DragAnimator.AnimateReturnToBar(
                visualManager.DragVisual,
                rectTransform,
                onComplete: () =>
                {
                    visualManager.Destroy();
                    canvasGroup.alpha = 1f;
                }
            )
        );
    }

    #endregion

    #region Canvas Finding

    /// <summary>
    /// Find the top-level Canvas for drag visuals
    /// </summary>
    private Canvas FindTopCanvas()
    {
        // First try to find a ScreenSpaceOverlay canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();

        foreach (var c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return c;
            }
        }

        // Fallback to any root canvas
        foreach (var c in canvases)
        {
            if (c.isRootCanvas)
            {
                return c;
            }
        }

        // Last resort - use parent canvas
        return GetComponentInParent<Canvas>();
    }

    #endregion
}
