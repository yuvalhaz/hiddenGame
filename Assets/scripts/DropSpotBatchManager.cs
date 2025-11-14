using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Manages batches of DropSpots with flexible batch sizes.
/// Coordinates batch progression, completion celebration, ads, and progress UI.
/// </summary>
public class DropSpotBatchManager : MonoBehaviour
{
    [Header("Batch Configuration")]
    [SerializeField] private bool useCustomBatchSizes = false;
    [Tooltip("Enable to define different sizes for each batch")]

    [Header("Simple Mode (All Batches Same Size)")]
    [SerializeField] private int numberOfBatches = 3;
    [SerializeField] private int spotsPerBatch = 7;

    [Header("Advanced Mode (Custom Size Per Batch)")]
    [SerializeField] private List<int> customBatchSizes = new List<int>() { 5, 7, 10 };
    [Tooltip("Define size for each batch")]

    [Header("DropSpot References")]
    [SerializeField] private List<DropSpot> allDropSpots = new List<DropSpot>();

    [Header("Components")]
    [SerializeField] private bool showCompletionMessage = true;
    [SerializeField] private BatchCompletionCelebration celebration = new BatchCompletionCelebration();
    [SerializeField] private BatchProgressUI progressUI = new BatchProgressUI();
    [SerializeField] private BatchAdController adController = new BatchAdController();

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    // State
    private int currentBatch = 0;
    private int totalPlacedInCurrentBatch = 0;

    #region Initialization

    private void OnValidate()
    {
        int required = GetTotalRequiredSpots();

        if (allDropSpots.Count > 0 && allDropSpots.Count != required)
        {
            Debug.LogWarning($"[DropSpotBatchManager] ⚠️ Mismatch! Required: {required}, Found: {allDropSpots.Count}");
        }

        if (useCustomBatchSizes && customBatchSizes.Count == 0)
        {
            Debug.LogError("[DropSpotBatchManager] ❌ Custom mode enabled but no batch sizes defined!");
        }
    }

    private void Awake()
    {
        celebration.Initialize(this);
        adController.Reset();

        if (allDropSpots.Count == 0)
        {
            allDropSpots.AddRange(FindObjectsOfType<DropSpot>(true));

            #if UNITY_EDITOR
            if (debugMode)
                Debug.Log($"[DropSpotBatchManager] Auto-found {allDropSpots.Count} DropSpots");
            #endif
        }

        int required = GetTotalRequiredSpots();
        if (allDropSpots.Count < required)
        {
            Debug.LogError($"[DropSpotBatchManager] ❌ NOT ENOUGH SPOTS! Required: {required}, Found: {allDropSpots.Count}");
        }
        else if (allDropSpots.Count > required)
        {
            Debug.LogWarning($"[DropSpotBatchManager] ⚠️ TOO MANY SPOTS! Extras will be ignored");
        }

        #if UNITY_EDITOR
        if (debugMode)
            PrintBatchConfiguration();
        #endif
    }

    private void Start()
    {
        StartCoroutine(InitializeAfterDelay());
    }

    private IEnumerator InitializeAfterDelay()
    {
        yield return null;

        #if UNITY_EDITOR
        if (debugMode)
            Debug.Log("[DropSpotBatchManager] 🚀 INITIALIZING");
        #endif

        // Connect to progress manager
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnItemPlaced -= OnItemPlaced;
            GameProgressManager.Instance.OnItemPlaced += OnItemPlaced;
        }
        else
        {
            Debug.LogError("[DropSpotBatchManager] ❌ GameProgressManager is NULL!");
        }

        CalculateCurrentBatch();
        HideAllDropSpots();
        RevealBatch(currentBatch);
        progressUI.UpdateProgress(
            currentBatch,
            totalPlacedInCurrentBatch,
            GetBatchSize(currentBatch),
            allDropSpots,
            GetTotalRequiredSpots(),
            GameProgressManager.Instance
        );

        #if UNITY_EDITOR
        if (debugMode)
            Debug.Log("[DropSpotBatchManager] ✅ Init complete!");
        #endif
    }

    private void OnDestroy()
    {
        if (GameProgressManager.Instance != null)
        {
            GameProgressManager.Instance.OnItemPlaced -= OnItemPlaced;
        }
    }

    #endregion

    #region Batch Calculation

    private void CalculateCurrentBatch()
    {
        if (GameProgressManager.Instance == null)
        {
            Debug.LogWarning("[DropSpotBatchManager] Cannot calculate - no GameProgressManager");
            return;
        }

        #if UNITY_EDITOR
        if (debugMode)
            Debug.Log("[DropSpotBatchManager] 🔄 Calculating current batch...");
        #endif

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
                }
            }

            if (placedCount < batchSize)
            {
                currentBatch = batch;
                totalPlacedInCurrentBatch = placedCount;

                #if UNITY_EDITOR
                if (debugMode)
                    Debug.Log($"🎯 Current Batch: {currentBatch}, Progress: {totalPlacedInCurrentBatch}/{batchSize}");
                #endif

                return;
            }
        }

        currentBatch = GetTotalBatches();
        totalPlacedInCurrentBatch = 0;

        #if UNITY_EDITOR
        if (debugMode)
            Debug.Log("🏆 All batches complete!");
        #endif
    }

    #endregion

    #region Item Placement Event

    private void OnItemPlaced(string itemId)
    {
        #if UNITY_EDITOR
        if (debugMode)
            Debug.Log($"[DropSpotBatchManager] 🔔 Item placed: {itemId}");
        #endif

        totalPlacedInCurrentBatch++;
        int batchSize = GetBatchSize(currentBatch);

        // Update progress UI
        progressUI.UpdateProgress(
            currentBatch,
            totalPlacedInCurrentBatch,
            batchSize,
            allDropSpots,
            GetTotalRequiredSpots(),
            GameProgressManager.Instance
        );

        // Check if batch is complete
        if (totalPlacedInCurrentBatch >= batchSize)
        {
            #if UNITY_EDITOR
            if (debugMode)
                Debug.Log("🎉 BATCH COMPLETE!");
            #endif

            int completedBatch = currentBatch;
            adController.IncrementBatchesCompleted();

            // Show completion message
            if (showCompletionMessage)
            {
                celebration.Show(completedBatch, this);
            }

            // Move to next batch
            currentBatch++;
            totalPlacedInCurrentBatch = 0;

            // Update UI for new batch
            progressUI.UpdateProgress(
                currentBatch,
                totalPlacedInCurrentBatch,
                GetBatchSize(currentBatch),
                allDropSpots,
                GetTotalRequiredSpots(),
                GameProgressManager.Instance
            );

            // Handle ads and next batch reveal
            if (adController.ShouldShowAd(completedBatch))
            {
                StartCoroutine(adController.ShowAdAndWait(
                    celebration.GetTotalDisplayTime(),
                    () => RevealNextBatch()
                ));
            }
            else
            {
                // No ad - just reveal next batch after delay
                if (currentBatch < GetTotalBatches())
                {
                    StartCoroutine(RevealNextBatchDelayed());
                }
            }
        }
    }

    #endregion

    #region Batch Reveal

    private void HideAllDropSpots()
    {
        foreach (var spot in allDropSpots)
        {
            if (spot != null)
            {
                if (GameProgressManager.Instance != null && GameProgressManager.Instance.IsItemPlaced(spot.spotId))
                {
                    // Already placed - show revealed
                    spot.gameObject.SetActive(true);
                    var reveal = spot.GetComponent<ImageRevealController>();
                    if (reveal != null && !reveal.IsRevealed)
                    {
                        reveal.RevealInstant();
                    }
                }
                else
                {
                    // Not placed - hide
                    spot.gameObject.SetActive(false);
                }
            }
        }
    }

    private IEnumerator RevealNextBatchDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        RevealNextBatch();
    }

    private void RevealNextBatch()
    {
        if (currentBatch < GetTotalBatches())
        {
            RevealBatch(currentBatch);
        }
        else
        {
            #if UNITY_EDITOR
            if (debugMode)
                Debug.Log("🏆 All batches complete!");
            #endif
        }
    }

    private void RevealBatch(int batch)
    {
        int start = GetBatchStartIndex(batch);
        int size = GetBatchSize(batch);
        int end = start + size;

        #if UNITY_EDITOR
        if (debugMode)
            Debug.Log($"[DropSpotBatchManager] Revealing batch {batch}: spots {start}-{end - 1}");
        #endif

        for (int i = start; i < end && i < allDropSpots.Count; i++)
        {
            if (allDropSpots[i] == null) continue;

            if (GameProgressManager.Instance != null && GameProgressManager.Instance.IsItemPlaced(allDropSpots[i].spotId))
            {
                continue; // Already placed
            }

            allDropSpots[i].gameObject.SetActive(true);
        }

        // Update progress UI after reveal
        progressUI.UpdateProgress(
            currentBatch,
            totalPlacedInCurrentBatch,
            GetBatchSize(currentBatch),
            allDropSpots,
            GetTotalRequiredSpots(),
            GameProgressManager.Instance
        );
    }

    #endregion

    #region Batch Configuration Helpers

    private int GetTotalRequiredSpots()
    {
        return useCustomBatchSizes ? customBatchSizes.Sum() : numberOfBatches * spotsPerBatch;
    }

    private int GetBatchSize(int batch)
    {
        if (useCustomBatchSizes && batch < customBatchSizes.Count)
        {
            return customBatchSizes[batch];
        }
        return spotsPerBatch;
    }

    private int GetBatchStartIndex(int batch)
    {
        if (useCustomBatchSizes)
        {
            int start = 0;
            for (int i = 0; i < batch && i < customBatchSizes.Count; i++)
            {
                start += customBatchSizes[i];
            }
            return start;
        }
        return batch * spotsPerBatch;
    }

    private int GetTotalBatches()
    {
        return useCustomBatchSizes ? customBatchSizes.Count : numberOfBatches;
    }

    #endregion

    #region Debug Commands

    [ContextMenu("📊 Show Status")]
    private void ShowStatus()
    {
        Debug.Log("========================================");
        Debug.Log($"Batch: {currentBatch}/{GetTotalBatches()}");
        Debug.Log($"Progress: {totalPlacedInCurrentBatch}/{GetBatchSize(currentBatch)}");
        Debug.Log($"Completed Batches: {adController.GetBatchesCompleted()}");
        Debug.Log($"Ads Enabled: {adController.showAdsOnBatchComplete}");
        Debug.Log("========================================");
    }

    [ContextMenu("🔍 Check UI")]
    private void CheckUI()
    {
        Debug.Log("========================================");
        Debug.Log("=== UI SETUP ===");
        Debug.Log($"Completion Panel: {(celebration.completionPanel != null ? "✅" : "❌")}");
        Debug.Log($"Completion Text: {(celebration.completionText != null ? "✅" : "❌")}");
        Debug.Log($"Game Progress Text: {(progressUI.gameProgressText != null ? "✅" : "❌")}");
        Debug.Log($"Batch Progress Text: {(progressUI.currentBatchProgressText != null ? "✅" : "❌")}");
        Debug.Log($"Progress Slider: {(progressUI.progressSlider != null ? "✅" : "❌")}");
        Debug.Log("========================================");
    }

    [ContextMenu("🎨 Test Message")]
    private void TestMessage()
    {
        if (celebration.completionText == null || celebration.completionPanel == null)
        {
            Debug.LogError("❌ UI not assigned!");
            return;
        }
        celebration.Show(0, this);
    }

    [ContextMenu("📺 Test Ad")]
    private void TestAd()
    {
        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogError("❌ RewardedAdsManager not found!");
            return;
        }

        Debug.Log("📺 Testing ad...");
        RewardedAdsManager.Instance.ShowRewarded(
            onReward: () => Debug.Log("✅ Reward!"),
            onClosed: (completed) => Debug.Log($"✅ Closed: {completed}"),
            onFailed: (error) => Debug.LogError($"❌ Failed: {error}"),
            onOpened: () => Debug.Log("📺 Opened!")
        );
    }

    [ContextMenu("🔧 Show Config")]
    private void PrintBatchConfiguration()
    {
        Debug.Log("=== BATCH CONFIG ===");
        Debug.Log($"Mode: {(useCustomBatchSizes ? "Custom" : "Simple")}");
        Debug.Log($"Total Batches: {GetTotalBatches()}");
        Debug.Log($"Total Spots Required: {GetTotalRequiredSpots()}");
        Debug.Log($"Total Spots Found: {allDropSpots.Count}");
        Debug.Log("====================");
    }

    #endregion
}
