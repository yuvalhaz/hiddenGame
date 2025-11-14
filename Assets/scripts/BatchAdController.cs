using System.Collections;
using UnityEngine;

/// <summary>
/// Controls when and how ads are shown between batch completions.
/// Handles timing, frequency, and coordination with batch progression.
/// </summary>
[System.Serializable]
public class BatchAdController
{
    [Header("Ad Settings")]
    public bool showAdsOnBatchComplete = true;
    [Tooltip("Show ads when completing batches")]

    public int adFrequency = 1;
    [Tooltip("Show ad every X batches (1 = every batch, 2 = every 2 batches)")]

    public bool skipAdOnFirstBatch = false;
    [Tooltip("Don't show ad after completing the first batch")]

    public float delayBeforeAd = 0.5f;
    [Tooltip("Extra delay after message disappears before showing ad")]

    public bool waitForAdToClose = true;
    [Tooltip("Wait for ad to close before revealing next batch")]

    [Header("Debug")]
    public bool debugMode = false;

    private int batchesCompleted = 0;

    /// <summary>
    /// Reset the batch counter (for new game/level).
    /// </summary>
    public void Reset()
    {
        batchesCompleted = 0;
    }

    /// <summary>
    /// Increment batch completion counter.
    /// </summary>
    public void IncrementBatchesCompleted()
    {
        batchesCompleted++;
    }

    /// <summary>
    /// Check if an ad should be shown for the given completed batch.
    /// </summary>
    public bool ShouldShowAd(int completedBatchIndex)
    {
        if (!showAdsOnBatchComplete)
        {
            #if UNITY_EDITOR
            if (debugMode)
                Debug.Log("[BatchAdController] Ads disabled");
            #endif
            return false;
        }

        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogWarning("[BatchAdController] RewardedAdsManager not found!");
            return false;
        }

        if (skipAdOnFirstBatch && completedBatchIndex == 0)
        {
            #if UNITY_EDITOR
            if (debugMode)
                Debug.Log("[BatchAdController] Skipping ad on first batch");
            #endif
            return false;
        }

        if (adFrequency <= 0)
        {
            #if UNITY_EDITOR
            if (debugMode)
                Debug.Log("[BatchAdController] Ad frequency is 0");
            #endif
            return false;
        }

        bool shouldShow = (batchesCompleted % adFrequency) == 0;

        #if UNITY_EDITOR
        if (debugMode)
            Debug.Log($"[BatchAdController] Completed: {batchesCompleted}, Freq: {adFrequency}, Show: {shouldShow}");
        #endif

        return shouldShow;
    }

    /// <summary>
    /// Show ad with proper timing and callbacks.
    /// </summary>
    public IEnumerator ShowAdAndWait(float messageDisplayTime, System.Action onAdComplete)
    {
        #if UNITY_EDITOR
        if (debugMode)
            Debug.Log("[BatchAdController] Waiting for completion message...");
        #endif

        // Wait for message to finish
        yield return new WaitForSeconds(messageDisplayTime + delayBeforeAd);

        #if UNITY_EDITOR
        if (debugMode)
            Debug.Log("[BatchAdController] Showing ad now...");
        #endif

        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogWarning("[BatchAdController] RewardedAdsManager missing!");
            onAdComplete?.Invoke();
            yield break;
        }

        bool adClosed = false;

        RewardedAdsManager.Instance.ShowRewarded(
            onReward: () =>
            {
                #if UNITY_EDITOR
                if (debugMode)
                    Debug.Log("[BatchAdController] Ad reward granted!");
                #endif
            },
            onClosed: (completed) =>
            {
                #if UNITY_EDITOR
                if (debugMode)
                    Debug.Log($"[BatchAdController] Ad closed. Completed: {completed}");
                #endif
                adClosed = true;
            },
            onFailed: (error) =>
            {
                Debug.LogWarning($"[BatchAdController] Ad failed: {error}");
                adClosed = true;
            },
            onOpened: () =>
            {
                #if UNITY_EDITOR
                if (debugMode)
                    Debug.Log("[BatchAdController] Ad opened!");
                #endif
            }
        );

        // Wait for ad to close if enabled
        if (waitForAdToClose)
        {
            float timeout = 60f;
            float elapsed = 0f;

            while (!adClosed && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (elapsed >= timeout)
            {
                Debug.LogWarning("[BatchAdController] Ad timeout!");
            }
        }

        #if UNITY_EDITOR
        if (debugMode)
            Debug.Log("[BatchAdController] Ad finished. Continuing...");
        #endif

        onAdComplete?.Invoke();
    }

    public int GetBatchesCompleted() => batchesCompleted;
}
