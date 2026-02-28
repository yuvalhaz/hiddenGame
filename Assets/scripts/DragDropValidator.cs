using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Handles raycasting and validation for drop operations.
/// Determines if a dragged item can be dropped at a location.
/// </summary>
public class DragDropValidator
{
    private readonly Canvas canvas;
    private readonly float dropDistanceThreshold;
    private readonly bool debugMode;

    public DragDropValidator(Canvas canvas, float dropDistanceThreshold = 150f, bool debugMode = false)
    {
        this.canvas = canvas;
        this.dropDistanceThreshold = dropDistanceThreshold;
        this.debugMode = debugMode;
    }

    /// <summary>
    /// Check if the drag visual can be dropped at the current position.
    /// Returns the DropSpot if valid, null otherwise.
    /// </summary>
    public DropSpot ValidateDrop(string buttonID, RectTransform dragVisual, PointerEventData eventData, out string failureReason)
    {
        failureReason = null;

        // Find DropSpot under pointer
        DropSpot hitSpot = RaycastForDropSpot(dragVisual, eventData);

        if (hitSpot == null)
        {
            failureReason = "No DropSpot found";
            return null;
        }

        // Check if IDs match
        if (!hitSpot.Accepts(buttonID))
        {
            failureReason = $"Wrong spot: expected {buttonID}, got {hitSpot.spotId}";
            return null;
        }

        // Check distance
        RectTransform spotRT = hitSpot.GetComponent<RectTransform>();
        if (spotRT == null)
        {
            failureReason = "DropSpot has no RectTransform";
            return null;
        }

        float distance = Vector3.Distance(dragVisual.position, spotRT.position);

        // ✅ סף דינמי ביחס לגודל התמונה - 50% מהמימד הגדול, עם מינימום 80
        float spotMaxDim = Mathf.Max(spotRT.rect.width, spotRT.rect.height);
        float dynamicThreshold = Mathf.Max(spotMaxDim * 0.5f, 80f);

        if (distance > dynamicThreshold)
        {
            failureReason = $"Too far: {distance:F0} > {dynamicThreshold:F0} (spot size: {spotRT.rect.size})";
            return null;
        }

        // Valid drop!
        return hitSpot;
    }

    /// <summary>
    /// Raycast to find DropSpot at the drag visual position.
    /// </summary>
    private DropSpot RaycastForDropSpot(RectTransform dragVisual, PointerEventData eventData)
    {
        if (canvas == null)
        {
            Debug.LogWarning("[DragDropValidator] Canvas is null");
            return null;
        }

        var gr = canvas.GetComponent<GraphicRaycaster>();
        if (gr == null)
        {
            Debug.LogWarning("[DragDropValidator] No GraphicRaycaster found on canvas");
            return null;
        }

        if (EventSystem.current == null)
        {
            Debug.LogError("[DragDropValidator] No EventSystem found");
            return null;
        }

        // Create custom event data at drag visual center
        PointerEventData customEvent = new PointerEventData(EventSystem.current);

        if (dragVisual != null)
        {
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
                eventData.pressEventCamera,
                dragVisual.position
            );
            customEvent.position = screenPos;
        }
        else
        {
            customEvent.position = eventData.position;
        }

        // Raycast
        var results = new List<RaycastResult>();
        gr.Raycast(customEvent, results);

        // Find first DropSpot in results
        foreach (var result in results)
        {
            var spot = result.gameObject.GetComponentInParent<DropSpot>();
            if (spot != null)
            {
                #if UNITY_EDITOR
                if (debugMode)
                    Debug.Log($"[DragDropValidator] Found DropSpot: {spot.spotId}");
                #endif
                return spot;
            }
        }

        return null;
    }

    /// <summary>
    /// Enable/disable raycast target on matching DropSpot background image.
    /// </summary>
    public static void SetDropSpotRaycastEnabled(string buttonID, bool enabled)
    {
        DropSpot spot = DropSpotCache.Get(buttonID);
        if (spot == null) return;

        if (spot.IsSettled) return; // Already placed, don't change

        var revealController = spot.GetComponent<ImageRevealController>();
        if (revealController != null)
        {
            var backgroundImage = revealController.GetBackgroundImage();
            if (backgroundImage != null)
            {
                backgroundImage.raycastTarget = enabled;
            }
        }
    }
}