using System;
using UnityEngine;

public class RewardedAdsManager : MonoBehaviour
{
    public static RewardedAdsManager Instance;

    /// <summary>נורה כשהתקבל Reward.</summary>
    public event Action OnRewardGranted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ===================== Availability / Preload =====================

    public bool IsReady()
    {
#if UNITY_EDITOR
        return true;
#else
        // TODO: החזר true רק כשהמודעה באמת טעונה.
        return true;
#endif
    }

    public void Preload(Action<bool> onLoaded = null)
    {
#if UNITY_EDITOR
        SafeInvoke(onLoaded, true);
#else
        // TODO: טען מודעת Rewarded אמיתית ואז קרא onLoaded(true/false)
        SafeInvoke(onLoaded, true);
#endif
    }

    // ===================== MAIN API - Single method with optional parameters =====================

    /// <summary>
    /// Show rewarded ad with optional callbacks
    /// </summary>
    /// <param name="onReward">Called when user earns reward</param>
    /// <param name="onClosed">Called when ad is closed - receives bool indicating if completed</param>
    /// <param name="onFailed">Called if ad fails to show - receives error message</param>
    /// <param name="onOpened">Called when ad opens</param>
    public void ShowRewarded(
        Action onReward = null,
        Action<bool> onClosed = null,
        Action<string> onFailed = null,
        Action onOpened = null)
    {
#if UNITY_EDITOR
        Debug.Log("[RewardedAdsManager] Editor simulate: opened -> reward -> closed(true).");
        SafeInvoke(onOpened);
        SafeInvoke(OnRewardGranted);
        SafeInvoke(onReward);
        SafeInvoke(onClosed, true);
#else
        // TODO: חבר Google Mobile Ads:
        // rewardedAd.OnAdFullScreenContentOpened += () => SafeInvoke(onOpened);
        // rewardedAd.OnAdFullScreenContentClosed += () => SafeInvoke(onClosed, true);
        // rewardedAd.OnAdFailedToPresentFullScreenContent += err => SafeInvoke(onFailed, err.ToString());
        // rewardedAd.Show(reward => { SafeInvoke(OnRewardGranted); SafeInvoke(onReward); });

        // סימולציה זמנית:
        SafeInvoke(onOpened);
        SafeInvoke(OnRewardGranted);
        SafeInvoke(onReward);
        SafeInvoke(onClosed, true);
#endif
    }

    /// <summary>
    /// Alias for ShowRewarded - same functionality
    /// </summary>
    public void ShowHintAd(
        Action onReward = null,
        Action<bool> onClosed = null,
        Action<string> onFailed = null,
        Action onOpened = null)
    {
        ShowRewarded(onReward, onClosed, onFailed, onOpened);
    }

    // ===================== Utils =====================

    private static void SafeInvoke(Action action)
    {
        try { action?.Invoke(); }
        catch (Exception e) { Debug.LogException(e); }
    }

    private static void SafeInvoke<T>(Action<T> action, T value)
    {
        try { action?.Invoke(value); }
        catch (Exception e) { Debug.LogException(e); }
    }
}
