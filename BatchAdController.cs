using System.Collections;
using UnityEngine;

/// <summary>
/// Handles ad timing, display, and waiting logic for batch completions
/// </summary>
public class BatchAdController : MonoBehaviour
{
    [Header("Ad Settings")]
    [SerializeField] private bool showAdsOnBatchComplete = true;
    [Tooltip("Show ads when completing batches")]

    [SerializeField] private int adFrequency = 1;
    [Tooltip("Show ad every X batches (1 = every batch, 2 = every 2 batches)")]

    [SerializeField] private bool skipAdOnFirstBatch = false;
    [Tooltip("Don't show ad after completing the first batch")]

    [SerializeField] private float delayBeforeAd = 0.5f;
    [Tooltip("Extra delay after message disappears before showing ad")]

    [SerializeField] private bool waitForAdToClose = true;
    [Tooltip("Wait for ad to close before revealing next batch")]

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // State tracking
    private int batchesCompleted = 0;

    /// <summary>
    /// Increment completed batch counter
    /// </summary>
    public void IncrementBatchesCompleted()
    {
        batchesCompleted++;
    }

    /// <summary>
    /// Check if ad should be shown now
    /// </summary>
    public bool ShouldShowAd(int completedBatchIndex)
    {
        if (!showAdsOnBatchComplete)
        {
            if (debugMode)
                Debug.Log("📺 Ads disabled");
            return false;
        }

        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogWarning("📺 RewardedAdsManager not found!");
            return false;
        }

        if (skipAdOnFirstBatch && completedBatchIndex == 0)
        {
            if (debugMode)
                Debug.Log("📺 Skipping ad on first batch");
            return false;
        }

        if (adFrequency <= 0)
        {
            if (debugMode)
                Debug.Log("📺 Ad frequency is 0");
            return false;
        }

        bool shouldShow = (batchesCompleted % adFrequency) == 0;

        if (debugMode)
            Debug.Log($"📺 Completed: {batchesCompleted}, Freq: {adFrequency}, Show: {shouldShow}");

        return shouldShow;
    }

    /// <summary>
    /// Show ad and wait for it to complete, then invoke callback
    /// </summary>
    public IEnumerator ShowAdAndContinue(float messageTime, System.Action onAdComplete)
    {
        if (debugMode)
            Debug.Log("📺 Waiting for completion message to finish...");

        // Wait for message to disappear
        yield return new WaitForSeconds(messageTime + delayBeforeAd);

        if (debugMode)
            Debug.Log("📺 Message finished. Showing ad now...");

        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogWarning("📺 RewardedAdsManager missing!");
            onAdComplete?.Invoke();
            yield break;
        }

        bool adClosed = false;

        RewardedAdsManager.Instance.ShowRewarded(
            onReward: () =>
            {
                if (debugMode)
                    Debug.Log("📺 Ad reward granted!");
            },
            onClosed: (completed) =>
            {
                if (debugMode)
                    Debug.Log($"📺 Ad closed. Completed: {completed}");
                adClosed = true;
            },
            onFailed: (error) =>
            {
                Debug.LogWarning($"📺 Ad failed: {error}");
                adClosed = true;
            },
            onOpened: () =>
            {
                if (debugMode)
                    Debug.Log("📺 Ad opened!");
            }
        );

        if (waitForAdToClose)
        {
            // Check if ad is even ready
            if (!RewardedAdsManager.Instance.IsReady())
            {
                Debug.LogWarning("📺 Ad was not ready, skipping wait");
                adClosed = true;
            }

            float timeout = 60f;
            float elapsed = 0f;

            while (!adClosed && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (elapsed >= timeout)
                Debug.LogWarning("📺 Ad timeout!");
        }

        if (debugMode)
            Debug.Log("📺 Ad finished. Continuing...");

        // Invoke callback
        onAdComplete?.Invoke();
    }

    /// <summary>
    /// Reset the completed batches counter (for testing)
    /// </summary>
    public void ResetBatchCounter()
    {
        batchesCompleted = 0;
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
}
