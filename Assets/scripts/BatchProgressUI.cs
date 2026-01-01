using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Handles all UI updates for batch progress (sliders, text, percentages)
/// </summary>
public class BatchProgressUI : MonoBehaviour
{
    [Header("Progress UI References")]
    [SerializeField] private Text gameProgressText;
    [Tooltip("Text showing game progress in percentage (e.g., 'GAME PROGRESS: 35%')")]

    [SerializeField] private Text currentBatchProgressText;
    [Tooltip("Text showing current batch progress (e.g., '3/7')")]

    [SerializeField] private Slider progressSlider;
    [Tooltip("Optional slider to show visual progress")]

    [Header("Settings")]
    [SerializeField] private bool updateProgressUI = true;
    [Tooltip("Enable/disable progress UI updates")]

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // Reference to main manager (set by BatchManager)
    private DropSpotBatchManager batchManager;
    private List<DropSpot> allDropSpots;

    /// <summary>
    /// Initialize with reference to batch manager and drop spots
    /// </summary>
    public void Initialize(DropSpotBatchManager manager, List<DropSpot> dropSpots)
    {
        batchManager = manager;
        allDropSpots = dropSpots;
    }

    /// <summary>
    /// Update all progress UI elements
    /// </summary>
    public void UpdateProgress(int currentBatch, int totalPlacedInBatch, int batchSize)
    {
        if (!updateProgressUI) return;

        // Calculate game progress percentage
        float gameProgressPercentage = CalculateGameProgressPercentage();

        // Update "GAME PROGRESS" text
        if (gameProgressText != null)
        {
            gameProgressText.text = $"GAME PROGRESS: {gameProgressPercentage:F0}%";
        }

        // Update current batch progress text (e.g., "3/7")
        if (currentBatchProgressText != null)
        {
            currentBatchProgressText.text = $"{totalPlacedInBatch}/{batchSize}";
        }

        // Update slider
        if (progressSlider != null)
        {
            if (batchSize > 0)
            {
                progressSlider.maxValue = batchSize;
                progressSlider.value = totalPlacedInBatch;
            }
        }

        if (debugMode)
        {
            Debug.Log($"[BatchProgressUI] Game Progress: {gameProgressPercentage:F1}%, Current Batch: {totalPlacedInBatch}/{batchSize}");
        }
    }

    /// <summary>
    /// Calculate overall game progress as percentage
    /// </summary>
    private float CalculateGameProgressPercentage()
    {
        if (batchManager == null || allDropSpots == null)
            return 0f;

        int totalRequired = batchManager.GetTotalRequiredSpots();
        if (totalRequired == 0)
            return 0f;

        int totalPlaced = 0;

        if (GameProgressManager.Instance != null)
        {
            // Count how many items have been placed
            for (int i = 0; i < allDropSpots.Count && i < totalRequired; i++)
            {
                if (allDropSpots[i] != null &&
                    GameProgressManager.Instance.IsItemPlaced(allDropSpots[i].spotId))
                {
                    totalPlaced++;
                }
            }
        }

        float percentage = (float)totalPlaced / totalRequired * 100f;
        return Mathf.Clamp(percentage, 0f, 100f);
    }

    /// <summary>
    /// Calculate total remaining items in the game
    /// </summary>
    public int CalculateTotalRemainingItems()
    {
        if (batchManager == null || allDropSpots == null)
            return 0;

        if (GameProgressManager.Instance == null)
            return batchManager.GetTotalRequiredSpots();

        int totalPlaced = 0;

        // Count how many items have been placed across all batches
        for (int i = 0; i < allDropSpots.Count; i++)
        {
            if (allDropSpots[i] != null &&
                GameProgressManager.Instance.IsItemPlaced(allDropSpots[i].spotId))
            {
                totalPlaced++;
            }
        }

        int totalRequired = batchManager.GetTotalRequiredSpots();
        int remaining = totalRequired - totalPlaced;

        return Mathf.Max(0, remaining);
    }

    /// <summary>
    /// Force update progress UI (for testing)
    /// </summary>
    [ContextMenu("🔄 Force Update Progress UI")]
    public void ForceUpdate()
    {
        if (batchManager != null)
        {
            Debug.Log("🔄 Manually updating Progress UI...");
            int currentBatch = batchManager.GetCurrentBatchIndex();
            int batchSize = batchManager.GetBatchSize(currentBatch);
            UpdateProgress(currentBatch, 0, batchSize); // You'll need to track totalPlaced
            Debug.Log("✅ Progress UI updated!");
        }
    }
}
