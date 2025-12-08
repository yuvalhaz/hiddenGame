using System;
using UnityEngine;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
using GoogleMobileAds.Api;
#endif

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

    void Start()
    {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        InitializeAdMob();
#endif
    }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
    private void InitializeAdMob()
    {
        if (adMobConfig == null)
        {
            Debug.LogError("[RewardedAdsManager] AdMobConfig not found! Add it to the scene.");
            return;
        }

        // אתחול Google Mobile Ads SDK
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log($"[RewardedAdsManager] AdMob initialized. Status: {initStatus}");
            if (adMobConfig.IsTestMode())
            {
                Debug.Log("[RewardedAdsManager] Running in TEST MODE with Google demo ads");
            }
        });
    }
#endif

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
                SafeInvoke(onLoaded, false);
                return;
            }

            rewardedAd = ad;
            Debug.Log("[RewardedAdsManager] Ad loaded successfully!");

            SafeInvoke(onLoaded, true);
        });
    }
#endif

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

        // רישום callbacks
        rewardedAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[RewardedAdsManager] Ad opened");
            SafeInvoke(onOpened);
        };

        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log($"[RewardedAdsManager] Ad closed. Reward granted: {rewardGranted}");
            SafeInvoke(onClosed, rewardGranted);

            // טען פרסומת חדשה אוטומטית
            Preload();
        };

        rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            string errorMsg = $"Failed to show ad: {error}";
            Debug.LogError($"[RewardedAdsManager] {errorMsg}");
            SafeInvoke(onFailed, errorMsg);

            // טען פרסומת חדשה אוטומטית
            Preload();
        };

        // הצג פרסומת
        rewardedAd.Show((Reward reward) =>
        {
            rewardGranted = true;
            Debug.Log($"[RewardedAdsManager] Reward earned: {reward.Type}, {reward.Amount}");
            SafeInvoke(OnRewardGranted);
            SafeInvoke(onReward);
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