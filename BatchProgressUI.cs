using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages progress UI updates for batch completion tracking.
/// Handles percentage text, batch progress text, and progress sliders.
/// </summary>
[System.Serializable]
public class BatchProgressUI
{
    [Header("UI References")]
    public Text gameProgressText;
    [Tooltip("Text showing game progress in percentage (e.g., 'GAME PROGRESS: 35%')")]

    public Text currentBatchProgressText;
    [Tooltip("Text showing current batch progress (e.g., '3/7')")]

    public Slider progressSlider;
    [Tooltip("Optional slider to show visual progress")]

    [Header("Settings")]
    public bool updateProgressUI = true;

    [Header("Debug")]
    public bool debugMode = false;

    /// <summary>
    /// Update all progress UI elements.
    /// </summary>
    public void UpdateProgress(
        int currentBatch,
        int totalPlacedInCurrentBatch,
        int currentBatchSize,
        List<DropSpot> allDropSpots,
        int totalRequiredSpots,
        GameProgressManager progressManager)
    {
        if (!updateProgressUI) return;

        // Calculate game progress percentage
        float gameProgressPercentage = CalculateGameProgressPercentage(
            allDropSpots,
            totalRequiredSpots,
            progressManager
        );

        // Update game progress text
        if (gameProgressText != null)
        {
            gameProgressText.text = $"GAME PROGRESS: {gameProgressPercentage:F0}%";
        }

        // Update current batch progress text
        if (currentBatchProgressText != null)
        {
            currentBatchProgressText.text = $"{totalPlacedInCurrentBatch}/{currentBatchSize}";
        }

        // Update progress slider
        if (progressSlider != null)
        {
            if (currentBatchSize > 0)
            {
                progressSlider.maxValue = currentBatchSize;
                progressSlider.value = totalPlacedInCurrentBatch;
            }
        }

        #if UNITY_EDITOR
        if (debugMode)
        {
            Debug.Log($"[BatchProgressUI] Game: {gameProgressPercentage:F1}%, Batch: {totalPlacedInCurrentBatch}/{currentBatchSize}");
        }
        #endif
    }

    private float CalculateGameProgressPercentage(
        List<DropSpot> allDropSpots,
        int totalRequired,
        GameProgressManager progressManager)
    {
        if (totalRequired == 0) return 0f;

        int totalPlaced = 0;

        if (progressManager != null)
        {
            for (int i = 0; i < allDropSpots.Count && i < totalRequired; i++)
            {
                if (allDropSpots[i] != null &&
                    progressManager.IsItemPlaced(allDropSpots[i].spotId))
                {
                    totalPlaced++;
                }
            }
        }

        float percentage = (float)totalPlaced / totalRequired * 100f;
        return Mathf.Clamp(percentage, 0f, 100f);
    }

    /// <summary>
    /// Check if UI references are properly set.
    /// </summary>
    public bool ValidateReferences()
    {
        bool valid = true;

        if (gameProgressText == null)
        {
            Debug.LogWarning("[BatchProgressUI] gameProgressText is not assigned");
            valid = false;
        }

        if (currentBatchProgressText == null)
        {
            Debug.LogWarning("[BatchProgressUI] currentBatchProgressText is not assigned");
            valid = false;
        }

        return valid;
    }
}
