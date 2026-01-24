using System;
using UnityEngine;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
using GoogleMobileAds.Api;
#endif

/// <summary>
/// ✅ FIXED: Manages Rewarded Ads with main thread dispatcher for callbacks
/// </summary>
public class RewardedAdsManager : MonoBehaviour
{
    public static RewardedAdsManager Instance;

    /// <summary>נורה כשהתקבל Reward.</summary>
    public event Action OnRewardGranted;

    [Header("Configuration")]
    [SerializeField] private AdMobConfig adMobConfig;

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
    private RewardedAd rewardedAd;
    private bool isAdLoading = false;
#endif

    // ✅ FIXED: Main thread dispatcher queue to prevent race conditions
    private System.Collections.Generic.Queue<Action> mainThreadActions = new System.Collections.Generic.Queue<Action>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // מצא AdMobConfig אם לא מוגדר
        if (adMobConfig == null)
        {
            adMobConfig = FindObjectOfType<AdMobConfig>();
        }
    }

    // ✅ FIXED: Update loop to execute ALL queued main thread actions safely
    void Update()
    {
        while (mainThreadActions.Count > 0)
        {
            var action = mainThreadActions.Dequeue();
            try
            {
                action?.Invoke();
            }
            catch (System.Exception e)
            {
                Debug.LogException(e);
            }
        }
    }

    // ===================== Availability / Preload =====================

    public bool IsReady()
    {
#if UNITY_EDITOR
        return true;
#else
        return rewardedAd != null && rewardedAd.CanShowAd();
#endif
    }

    public void Preload(Action<bool> onLoaded = null)
    {
#if UNITY_EDITOR
        Debug.Log("[RewardedAdsManager] Editor mode - simulating ad load");
        SafeInvoke(onLoaded, true);
#else
        LoadRewardedAd(onLoaded);
#endif
    }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
    private void LoadRewardedAd(Action<bool> onLoaded = null)
    {
        if (adMobConfig == null)
        {
            Debug.LogError("[RewardedAdsManager] Cannot load ad - AdMobConfig is missing");
            SafeInvoke(onLoaded, false);
            return;
        }

        // נקה פרסומת קודמת
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }

        if (isAdLoading)
        {
            Debug.LogWarning("[RewardedAdsManager] Ad is already loading");
            SafeInvoke(onLoaded, false);
            return;
        }

        isAdLoading = true;
        string adUnitId = adMobConfig.GetRewardedAdUnitId();

        Debug.Log($"[RewardedAdsManager] Loading ad with ID: {adUnitId}");

        // בקשת פרסומת
        var adRequest = new AdRequest();

        RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            isAdLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogError($"[RewardedAdsManager] Failed to load ad: {error}");
                // ✅ Use main thread dispatcher for callback
                mainThreadActions.Enqueue(() => SafeInvoke(onLoaded, false));
                return;
            }

            rewardedAd = ad;
            Debug.Log("[RewardedAdsManager] Ad loaded successfully!");

            // ✅ Use main thread dispatcher for callback
            mainThreadActions.Enqueue(() => SafeInvoke(onLoaded, true));
        });
    }
#endif

    // ===================== MAIN API =====================

    /// <summary>
    /// Show rewarded ad with optional callbacks
    /// </summary>
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
        ShowRewardedAdReal(onReward, onClosed, onFailed, onOpened);
#endif
    }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
    private void ShowRewardedAdReal(
        Action onReward,
        Action<bool> onClosed,
        Action<string> onFailed,
        Action onOpened)
    {
        if (rewardedAd == null || !rewardedAd.CanShowAd())
        {
            string errorMsg = "Ad is not ready to show";
            Debug.LogWarning($"[RewardedAdsManager] {errorMsg}");
            SafeInvoke(onFailed, errorMsg);

            // נסה לטעון פרסומת חדשה
            Preload();
            return;
        }

        bool rewardGranted = false;

        // ✅ FIXED: Use main thread dispatcher for all callbacks
        rewardedAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[RewardedAdsManager] Ad opened");
            mainThreadActions.Enqueue(() => SafeInvoke(onOpened));
        };

        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log($"[RewardedAdsManager] Ad closed. Reward granted: {rewardGranted}");
            mainThreadActions.Enqueue(() => SafeInvoke(onClosed, rewardGranted));

            // טען פרסומת חדשה אוטומטית
            Preload();
        };

        rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            string errorMsg = $"Failed to show ad: {error}";
            Debug.LogError($"[RewardedAdsManager] {errorMsg}");
            mainThreadActions.Enqueue(() => SafeInvoke(onFailed, errorMsg));

            // טען פרסומת חדשה אוטומטית
            Preload();
        };

        // ✅ CRITICAL FIX: Show ad and handle reward on main thread
        rewardedAd.Show((Reward reward) =>
        {
            rewardGranted = true;
            Debug.Log($"[RewardedAdsManager] Reward earned: {reward.Type}, {reward.Amount}");

            // ✅ FIXED: Execute reward logic on main thread using queue to prevent race conditions
            mainThreadActions.Enqueue(() =>
            {
                SafeInvoke(OnRewardGranted);
                SafeInvoke(onReward);
            });
        });
    }
#endif

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

    // ===================== Cleanup =====================

    void OnDestroy()
    {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        if (rewardedAd != null)
        {
            rewardedAd.Destroy();
            rewardedAd = null;
        }
#endif
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
