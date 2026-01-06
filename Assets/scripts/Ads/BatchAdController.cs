using System.Collections;
using UnityEngine;

/// <summary>
/// Handles ad timing, display, and waiting logic for batch completions
/// </summary>
public class BatchAdController : MonoBehaviour
{
    [Header("🎓 Tutorial Mode")]
    [SerializeField] private bool isTutorialMode = false;
    [Tooltip("Enable this for Level0 - completely disables all ads in this level")]

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

    void Start()
    {
        if (isTutorialMode)
        {
            Debug.Log("[BatchAdController] 🎓 TUTORIAL MODE - All ads disabled for this level");
        }
    }

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
        // ✅ TUTORIAL MODE - Never show ads
        if (isTutorialMode)
        {
            if (debugMode)
                Debug.Log("🎓 Tutorial mode - ads disabled");
            return false;
        }

        if (!showAdsOnBatchComplete)
        {
            if (debugMode)
                Debug.Log("📺 Ads disabled");
            return false;
        }

        if (InterstitialAdsManager.Instance == null)
        {
            Debug.LogWarning("📺 InterstitialAdsManager not found!");
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
        // ✅ TUTORIAL MODE - Skip immediately
        if (isTutorialMode)
        {
            if (debugMode)
                Debug.Log("🎓 Tutorial mode - skipping ad wait, continuing immediately");
            
            // Still wait for message to finish for better UX
            yield return new WaitForSeconds(messageTime);
            onAdComplete?.Invoke();
            yield break;
        }

        if (debugMode)
            Debug.Log("📺 Waiting for completion message to finish...");

        // Wait for message to disappear
        yield return new WaitForSeconds(messageTime + delayBeforeAd);

        if (debugMode)
            Debug.Log("📺 Message finished. Showing ad now...");

        if (InterstitialAdsManager.Instance == null)
        {
            Debug.LogWarning("📺 InterstitialAdsManager missing!");
            onAdComplete?.Invoke();
            yield break;
        }

        // ✅ Check if ad is ready BEFORE trying to show it
        if (!InterstitialAdsManager.Instance.IsReady())
        {
            Debug.LogWarning("📺 Ad not ready, skipping");
            onAdComplete?.Invoke();
            yield break;
        }

        bool adClosed = false;

        InterstitialAdsManager.Instance.ShowInterstitial(
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
            if (!InterstitialAdsManager.Instance.IsReady())
            {
                Debug.LogWarning("📺 Ad was not ready, skipping wait");
                adClosed = true;
            }

            float timeout = 5f; // Short timeout for better UX
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
        if (isTutorialMode)
        {
            Debug.LogWarning("🎓 Cannot test ad in Tutorial Mode!");
            return;
        }

        if (InterstitialAdsManager.Instance == null)
        {
            Debug.LogError("❌ InterstitialAdsManager not found!");
            return;
        }

        Debug.Log("📺 Testing ad...");
        InterstitialAdsManager.Instance.ShowInterstitial(
            onClosed: (completed) => Debug.Log($"✅ Closed: {completed}"),
            onFailed: (error) => Debug.LogError($"❌ Failed: {error}"),
            onOpened: () => Debug.Log("📺 Opened!")
        );
    }
}
