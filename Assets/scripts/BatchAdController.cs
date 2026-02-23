using System.Collections;
using UnityEngine;

/// <summary>
/// Handles ad timing, display, and waiting logic for batch completions.
/// Shows interstitial ad only after 15 minutes of cumulative gameplay,
/// triggered at the next batch completion.
/// </summary>
public class BatchAdController : MonoBehaviour
{
    [Header("🎓 Tutorial Mode")]
    [SerializeField] private bool isTutorialMode = false;
    [Tooltip("Enable this for Level0 - completely disables all ads in this level")]

    [Header("Ad Settings")]
    [SerializeField] private bool showAdsOnBatchComplete = true;
    [Tooltip("Show ads when completing batches (after 15 minutes of gameplay)")]

    [SerializeField] private float delayBeforeAd = 0.5f;
    [Tooltip("Extra delay after message disappears before showing ad")]

    [SerializeField] private bool waitForAdToClose = true;
    [Tooltip("Wait for ad to close before revealing next batch")]

    [Header("Debug")]
    [SerializeField] private bool debugMode = true;

    // 15-minute threshold
    private const float AD_TIME_THRESHOLD = 15f * 60f;
    private const string PLAY_TIME_KEY = "AdTimer_TotalPlayTime";
    private const float SAVE_INTERVAL = 15f;

    // In-memory accumulator (flushed to PlayerPrefs periodically)
    private float sessionPlayTime = 0f;
    private float saveTimer = 0f;

    void Start()
    {
        if (isTutorialMode)
        {
            Debug.Log("[BatchAdController] 🎓 TUTORIAL MODE - All ads disabled for this level");
        }

        if (debugMode)
        {
            float stored = PlayerPrefs.GetFloat(PLAY_TIME_KEY, 0f);
            Debug.Log($"[BatchAdController] ⏱ Stored play time: {stored:F0}s / {AD_TIME_THRESHOLD:F0}s");
        }
    }

    void Update()
    {
        sessionPlayTime += Time.deltaTime;
        saveTimer += Time.deltaTime;

        if (saveTimer >= SAVE_INTERVAL)
        {
            PersistPlayTime();
            saveTimer = 0f;
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause) PersistPlayTime();
    }

    private void OnApplicationQuit()
    {
        PersistPlayTime();
    }

    /// <summary>
    /// Flush in-memory time to PlayerPrefs
    /// </summary>
    private void PersistPlayTime()
    {
        float stored = PlayerPrefs.GetFloat(PLAY_TIME_KEY, 0f);
        PlayerPrefs.SetFloat(PLAY_TIME_KEY, stored + sessionPlayTime);
        PlayerPrefs.Save();
        sessionPlayTime = 0f;

        if (debugMode)
            Debug.Log($"[BatchAdController] ⏱ Saved play time: {PlayerPrefs.GetFloat(PLAY_TIME_KEY):F0}s");
    }

    /// <summary>
    /// Total cumulative gameplay time (stored + current session)
    /// </summary>
    private float GetTotalPlayTime()
    {
        return PlayerPrefs.GetFloat(PLAY_TIME_KEY, 0f) + sessionPlayTime;
    }

    /// <summary>
    /// Reset the play time timer (called after ad is shown)
    /// </summary>
    private void ResetPlayTimer()
    {
        sessionPlayTime = 0f;
        PlayerPrefs.SetFloat(PLAY_TIME_KEY, 0f);
        PlayerPrefs.Save();

        if (debugMode)
            Debug.Log("[BatchAdController] ⏱ Play time timer reset after ad");
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

        // ✅ IAP CHECK - User bought "Remove Ads"
        if (IAPManager.Instance != null && IAPManager.Instance.AreAdsRemoved())
        {
            if (debugMode)
                Debug.Log("📺 Ads removed by IAP - skipping");
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

        // ✅ TIME CHECK - 19 minutes of gameplay required
        float totalTime = GetTotalPlayTime();
        bool enoughTimePassed = totalTime >= AD_TIME_THRESHOLD;

        if (debugMode)
            Debug.Log($"📺 Play time: {totalTime:F0}s / {AD_TIME_THRESHOLD:F0}s → Show ad: {enoughTimePassed}");

        return enoughTimePassed;
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
            if (!InterstitialAdsManager.Instance.IsReady())
            {
                Debug.LogWarning("📺 Ad was not ready, skipping wait");
                adClosed = true;
            }

            float timeout = 5f;
            float elapsed = 0f;

            while (!adClosed && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (elapsed >= timeout)
                Debug.LogWarning("📺 Ad timeout!");
        }

        // ✅ Reset timer after ad is shown
        ResetPlayTimer();

        if (debugMode)
            Debug.Log("📺 Ad finished. Continuing...");

        onAdComplete?.Invoke();
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

    [ContextMenu("⏱ Show Play Time")]
    private void ShowPlayTime()
    {
        float total = GetTotalPlayTime();
        Debug.Log($"[BatchAdController] ⏱ Total play time: {total:F0}s ({total / 60f:F1} min) / {AD_TIME_THRESHOLD / 60f:F0} min needed");
    }

    [ContextMenu("🔄 Reset Play Time (Testing)")]
    public void ResetPlayTimerForTesting()
    {
        ResetPlayTimer();
        Debug.Log("[BatchAdController] ⏱ Play time reset for testing");
    }

    /// <summary>
    /// Reset the ad timer counter (called on level/progress reset)
    /// </summary>
    public void ResetBatchCounter()
    {
        ResetPlayTimer();
    }
}
