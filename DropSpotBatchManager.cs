using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Core batch manager - coordinates batch progression and delegates to sub-systems
/// REFACTORED: Now uses BatchProgressUI, BatchMessageController, and BatchAdController
/// </summary>
public class DropSpotBatchManager : MonoBehaviour
{
    [Header("🎯 Batch Mode")]
    [SerializeField] private bool useCustomBatchSizes = false;
    [Tooltip("Enable to define different sizes for each batch")]

    [Header("⚙️ Simple Mode (All Batches Same Size)")]
    [SerializeField] private int numberOfBatches = 3;
    [SerializeField] private int spotsPerBatch = 7;

    [Header("🔧 Advanced Mode (Custom Size Per Batch)")]
    [SerializeField] private List<int> customBatchSizes = new List<int>() { 5, 7, 10 };
    [Tooltip("Define size for each batch: [Batch 0: 5 spots, Batch 1: 7 spots, Batch 2: 10 spots]")]

    [Header("📋 DropSpot References")]
    [SerializeField] private List<DropSpot> allDropSpots = new List<DropSpot>();

    [Header("📺 UI Sub-Systems")]
    [SerializeField] private BatchProgressUI progressUI;
    [SerializeField] private BatchMessageController messageController;
    [SerializeField] private BatchAdController adController;

    [Header("🎬 Level Complete")]
    [SerializeField] private LevelCompleteController levelCompleteController;
    [Tooltip("Controller that handles level completion and ending dialog")]

    [Header("🐛 Debug")]
    [SerializeField] private bool debugMode = true;

    // State
    private int currentBatch = 0;
    private int totalPlacedInCurrentBatch = 0;
    private bool isLastBatchCompletion = false;

    private void OnValidate()
    {
        int required = GetTotalRequiredSpots();

        if (allDropSpots.Count > 0 && allDropSpots.Count != required)
        {
            Debug.LogWarning($"[DropSpotBatchManager] ⚠️ Mismatch! Required: {required} spots, Found: {allDropSpots.Count} spots in list");
        }

        if (useCustomBatchSizes && customBatchSizes.Count == 0)
        {
            Debug.LogError("[DropSpotBatchManager] ❌ Custom mode enabled but no batch sizes defined!");
        }
    }

    private void Awake()
    {
        // Auto-create components if missing
        if (progressUI == null)
        {
            progressUI = gameObject.AddComponent<BatchProgressUI>();
            Debug.Log("[DropSpotBatchManager] ✅ Created BatchProgressUI");
        }

        if (messageController == null)
        {
            messageController = gameObject.AddComponent<BatchMessageController>();
            Debug.Log("[DropSpotBatchManager] ✅ Created BatchMessageController");
        }

        if (adController == null)
        {
            adController = gameObject.AddComponent<BatchAdController>();
            Debug.Log("[DropSpotBatchManager] ✅ Created BatchAdController");
        }

        if (allDropSpots.Count == 0)
        {
            allDropSpots.AddRange(FindObjectsOfType<DropSpot>(true));

            if (debugMode)
                Debug.Log($"[DropSpotBatchManager] Auto-found {allDropSpots.Count} DropSpots");
        }

        int required = GetTotalRequiredSpots();
        if (allDropSpots.Count < required)
        {
            Debug.LogError($"[DropSpotBatchManager] ❌ NOT ENOUGH SPOTS! Required: {required}, Found: {allDropSpots.Count}");
        }
        else if (allDropSpots.Count > required)
        {
            Debug.LogWarning($"[DropSpotBatchManager] ⚠️ TOO MANY SPOTS! Required: {required}, Found: {allDropSpots.Count} (extras will be ignored)");
        }

        PrintBatchConfiguration();
    }

    private void Start()
    {
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        yield return null;

        if (debugMode)
        {
            Debug.Log("========================================");
            Debug.Log("[DropSpotBatchManager] 🚀 INITIALIZING");
            Debug.Log("========================================");
        }

        // Initialize sub-systems
        if (progressUI != null)
            progressUI.Initialize(this, allDropSpots);

        // Connect to GameProgressManager event
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnItemPlaced -= OnItemPlaced;
            GameProgressManager.Instance.OnItemPlaced += OnItemPlaced;

            if (debugMode)
                Debug.Log("[DropSpotBatchManager] ✅ Connected to event");
        }
        else
        {
            Debug.LogError("[DropSpotBatchManager] ❌ GameProgressManager is NULL!");
        }

        CalculateCurrentBatch();

        if (debugMode)
        {
            Debug.Log($"[DropSpotBatchManager] currentBatch = {currentBatch}");
            Debug.Log($"[DropSpotBatchManager] totalPlaced = {totalPlacedInCurrentBatch}");
            Debug.Log($"[DropSpotBatchManager] Need {GetBatchSize(currentBatch) - totalPlacedInCurrentBatch} more");
        }

        HideAllDropSpots();
        RevealBatch(currentBatch);

        // Update UI
        UpdateProgressUI();

        if (debugMode)
            Debug.Log("[DropSpotBatchManager] ✅ Init complete!");
    }

    private void OnDestroy()
    {
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnItemPlaced -= OnItemPlaced;
        }
    }

    // ===== CORE BATCH LOGIC =====

    private void CalculateCurrentBatch()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogWarning("[DropSpotBatchManager] Cannot calculate - no GameProgressManager");
            return;
        }

        if (debugMode)
            Debug.Log("[DropSpotBatchManager] 🔄 Calculating current batch...");

        for (int batch = 0; batch < GetTotalBatches(); batch++)
        {
            int startIdx = GetBatchStartIndex(batch);
            int batchSize = GetBatchSize(batch);
            int placedCount = 0;

            for (int i = startIdx; i < startIdx + batchSize && i < allDropSpots.Count; i++)
            {
                if (allDropSpots[i] != null && GameProgressManager.Instance.IsItemPlaced(allDropSpots[i].spotId))
                {
                    placedCount++;
                    if (debugMode)
                        Debug.Log($"  ✅ {allDropSpots[i].spotId} placed");
                }
            }

            if (debugMode)
                Debug.Log($"Batch {batch}: {placedCount}/{batchSize}");

            if (placedCount < batchSize)
            {
                currentBatch = batch;
                totalPlacedInCurrentBatch = placedCount;
                if (debugMode)
                    Debug.Log($"🎯 Current = Batch {currentBatch}");
                return;
            }
        }

        currentBatch = GetTotalBatches();
        totalPlacedInCurrentBatch = 0;
        if (debugMode)
            Debug.Log("🏆 All batches complete!");
    }

    private void OnItemPlaced(string itemId)
    {
        if (debugMode)
        {
            Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Debug.Log($"🔔 Item placed: {itemId}");
            Debug.Log($"Before: batch={currentBatch}, placed={totalPlacedInCurrentBatch}");
        }

        totalPlacedInCurrentBatch++;
        int batchSize = GetBatchSize(currentBatch);

        // Update UI
        UpdateProgressUI();

        if (debugMode)
            Debug.Log($"Progress: {totalPlacedInCurrentBatch}/{batchSize}");

        if (totalPlacedInCurrentBatch >= batchSize)
        {
            if (debugMode)
            {
                Debug.Log("🎉🎉🎉 BATCH COMPLETE! 🎉🎉🎉");
                Debug.Log($"Batch size: {batchSize}");
                Debug.Log($"Current batch: {currentBatch}");
                Debug.Log($"Total batches: {GetTotalBatches()}");
            }

            int completedBatch = currentBatch;

            // Increment ad controller counter
            if (adController != null)
                adController.IncrementBatchesCompleted();

            // Check if this is the last batch
            bool isLastBatch = (currentBatch >= GetTotalBatches() - 1);

            if (isLastBatch)
            {
                // Last batch - show "WELL DONE!" + confetti, then trigger level complete
                isLastBatchCompletion = true;

                // ✅ Check if tutorial is active - let TutorialSlideManager handle completion
                if (TutorialSlideManager.Instance != null)
                {
                    if (debugMode)
                        Debug.Log("🎓 Tutorial active - skipping batch level completion (tutorial will handle it)");
                    isLastBatchCompletion = false;
                    return;
                }

                if (messageController != null)
                {
                    messageController.ShowCustomMessage("WELL DONE!", showConfetti: true, onComplete: () =>
                    {
                        // ✅ After message, trigger level completion!
                        if (levelCompleteController != null)
                        {
                            if (debugMode)
                                Debug.Log("🎯 Triggering level complete after WELL DONE!");
                            levelCompleteController.TriggerLevelComplete();
                        }
                        else
                        {
                            Debug.LogError("❌ LevelCompleteController is NULL! Cannot complete level!");
                        }
                        isLastBatchCompletion = false;
                    });
                }
                else
                {
                    // No message controller - trigger immediately
                    if (levelCompleteController != null)
                    {
                        if (debugMode)
                            Debug.Log("🎯 Triggering level complete (no message controller)!");
                        levelCompleteController.TriggerLevelComplete();
                    }
                }

                if (debugMode)
                    Debug.Log("🏆 Last batch! Showing WELL DONE with confetti!");
            }
            else
            {
                // Regular batch - show encouragement message
                if (messageController != null)
                {
                    messageController.ShowCompletionMessage(completedBatch);
                }

                if (debugMode)
                    Debug.Log($"📝 Regular batch {completedBatch}, showing encouragement message");
            }

            currentBatch++;
            totalPlacedInCurrentBatch = 0;

            // Update UI after advancing
            UpdateProgressUI();

            // Check if should show ad
            if (adController != null && adController.ShouldShowAd(completedBatch))
            {
                if (debugMode)
                    Debug.Log($"📺 Will show ad after message for batch {completedBatch}");

                StartCoroutine(adController.ShowAdAndContinue(
                    messageTime: 2.5f, // message duration + animation
                    onAdComplete: () =>
                    {
                        // After ad, reveal next batch
                        if (currentBatch < GetTotalBatches())
                        {
                            StartCoroutine(RevealNextBatchDelayed());
                        }
                    }
                ));
            }
            else
            {
                // No ad - reveal next batch directly
                if (currentBatch < GetTotalBatches())
                {
                    StartCoroutine(RevealNextBatchDelayed());
                }
            }

            if (debugMode)
                Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━");
        }
    }

    private IEnumerator RevealNextBatchDelayed()
    {
        yield return new WaitForSeconds(4f);
        RevealBatch(currentBatch);
    }

    private void UpdateProgressUI()
    {
        if (progressUI != null)
        {
            int batchSize = GetBatchSize(currentBatch);
            progressUI.UpdateProgress(currentBatch, totalPlacedInCurrentBatch, batchSize);
        }
    }

    private void HideAllDropSpots()
    {
        foreach (var spot in allDropSpots)
        {
            if (spot != null)
            {
                if (GameProgressManager.Instance != null && GameProgressManager.Instance.IsItemPlaced(spot.spotId))
                {
                    spot.gameObject.SetActive(true);
                    var reveal = spot.GetComponent<ImageRevealController>();
                    if (reveal != null && !reveal.IsRevealed)
                        reveal.RevealInstant();
                }
                else
                {
                    spot.gameObject.SetActive(false);
                }
            }
        }
    }

    private void RevealBatch(int batch)
    {
        int start = GetBatchStartIndex(batch);
        int size = GetBatchSize(batch);
        int end = start + size;

        if (debugMode)
            Debug.Log($"Revealing batch {batch}: spots {start}-{end - 1}");

        for (int i = start; i < end && i < allDropSpots.Count; i++)
        {
            if (allDropSpots[i] == null) continue;

            if (GameProgressManager.Instance != null && GameProgressManager.Instance.IsItemPlaced(allDropSpots[i].spotId))
                continue;

            allDropSpots[i].gameObject.SetActive(true);
        }

        // Update UI after revealing batch
        UpdateProgressUI();
    }

    // ===== PUBLIC API =====

    public int GetTotalRequiredSpots()
    {
        return useCustomBatchSizes ? customBatchSizes.Sum() : numberOfBatches * spotsPerBatch;
    }

    public int GetBatchSize(int batch)
    {
        if (useCustomBatchSizes && batch < customBatchSizes.Count)
            return customBatchSizes[batch];
        return spotsPerBatch;
    }

    public int GetBatchStartIndex(int batch)
    {
        if (useCustomBatchSizes)
        {
            int start = 0;
            for (int i = 0; i < batch && i < customBatchSizes.Count; i++)
                start += customBatchSizes[i];
            return start;
        }
        return batch * spotsPerBatch;
    }

    public int GetTotalBatches()
    {
        return useCustomBatchSizes ? customBatchSizes.Count : numberOfBatches;
    }

    public int GetCurrentBatchIndex()
    {
        return currentBatch;
    }

    public List<DropSpot> GetCurrentBatchAvailableSpots()
    {
        List<DropSpot> availableSpots = new List<DropSpot>();

        if (currentBatch >= GetTotalBatches())
        {
            if (debugMode)
                Debug.Log("[DropSpotBatchManager] No more batches available");
            return availableSpots;
        }

        int start = GetBatchStartIndex(currentBatch);
        int size = GetBatchSize(currentBatch);
        int end = start + size;

        for (int i = start; i < end && i < allDropSpots.Count; i++)
        {
            if (allDropSpots[i] == null) continue;

            // Only include spots that haven't been placed yet
            if (GameProgressManager.Instance != null &&
                !GameProgressManager.Instance.IsItemPlaced(allDropSpots[i].spotId))
            {
                availableSpots.Add(allDropSpots[i]);
            }
        }

        if (debugMode)
            Debug.Log($"[DropSpotBatchManager] Found {availableSpots.Count} available spots in batch {currentBatch}");

        return availableSpots;
    }

    public void ShowCustomMessage(string message)
    {
        if (messageController != null)
        {
            messageController.ShowCustomMessage(message, showConfetti: true);
        }
    }

    public List<DropSpot> GetAllDropSpots()
    {
        return allDropSpots;
    }

    // ===== DEBUG/TESTING =====

    [ContextMenu("📊 Show Status")]
    private void ShowStatus()
    {
        Debug.Log("========================================");
        Debug.Log($"Batch: {currentBatch}/{GetTotalBatches()}");
        Debug.Log($"Progress: {totalPlacedInCurrentBatch}/{GetBatchSize(currentBatch)}");

        if (progressUI != null)
        {
            int remaining = progressUI.CalculateTotalRemainingItems();
            Debug.Log($"Items Remaining: {remaining}");
        }

        Debug.Log("========================================");
    }

    [ContextMenu("🔧 Show Config")]
    private void PrintBatchConfiguration()
    {
        Debug.Log("=== CONFIG ===");
        Debug.Log($"Mode: {(useCustomBatchSizes ? "Custom" : "Simple")}");
        Debug.Log($"Batches: {GetTotalBatches()}");
        Debug.Log($"Total spots: {GetTotalRequiredSpots()}");
    }
}
