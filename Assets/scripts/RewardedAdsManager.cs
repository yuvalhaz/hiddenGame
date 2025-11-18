using System;
using UnityEngine;

#if GOOGLE_MOBILE_ADS
using GoogleMobileAds.Api;
#endif

public class RewardedAdsManager : MonoBehaviour
{
    public static RewardedAdsManager Instance;

    /// <summary>נורה כשהתקבל Reward.</summary>
    public event Action OnRewardGranted;

    [Header("Ad Settings")]
    [SerializeField] private bool useTestAds = true;
    [SerializeField] private string androidAdUnitId = "";
    [SerializeField] private string iosAdUnitId = "";

    // Test Ad Unit IDs from Google
    private const string ANDROID_TEST_AD_UNIT = "ca-app-pub-3940256099942544/5224354917";
    private const string IOS_TEST_AD_UNIT = "ca-app-pub-3940256099942544/1712485313";

#if GOOGLE_MOBILE_ADS
    private RewardedAd rewardedAd;
    private bool isAdLoaded = false;
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

#if GOOGLE_MOBILE_ADS
        // Initialize Google Mobile Ads
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log("[RewardedAdsManager] Google Mobile Ads initialized!");
            Preload();
        });
#else
        Debug.Log("[RewardedAdsManager] Running without Google Mobile Ads SDK");
#endif
    }

    // ===================== Availability / Preload =====================

    public bool IsReady()
    {
#if GOOGLE_MOBILE_ADS
        return isAdLoaded && rewardedAd != null;
#else
        return true; // Editor/simulation mode
#endif
    }

    public void Preload(Action<bool> onLoaded = null)
    {
#if GOOGLE_MOBILE_ADS
        string adUnitId = GetAdUnitId();

        Debug.Log($"[RewardedAdsManager] Loading ad from: {adUnitId}");

        var adRequest = new AdRequest();

        RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null)
            {
                Debug.LogError($"[RewardedAdsManager] Failed to load ad: {error.GetMessage()}");
                isAdLoaded = false;
                SafeInvoke(onLoaded, false);
                return;
            }

            rewardedAd = ad;
            isAdLoaded = true;
            Debug.Log("[RewardedAdsManager] ✅ Rewarded ad loaded successfully!");
            SafeInvoke(onLoaded, true);

            // Register callbacks
            RegisterAdCallbacks();
        });
#else
        Debug.Log("[RewardedAdsManager] Preload simulated (no SDK)");
        SafeInvoke(onLoaded, true);
#endif
    }

    private string GetAdUnitId()
    {
        if (useTestAds)
        {
#if UNITY_ANDROID
            return ANDROID_TEST_AD_UNIT;
#elif UNITY_IOS
            return IOS_TEST_AD_UNIT;
#else
            return ANDROID_TEST_AD_UNIT; // Default
#endif
        }

        // Real ad units
#if UNITY_ANDROID
        return string.IsNullOrEmpty(androidAdUnitId) ? ANDROID_TEST_AD_UNIT : androidAdUnitId;
#elif UNITY_IOS
        return string.IsNullOrEmpty(iosAdUnitId) ? IOS_TEST_AD_UNIT : iosAdUnitId;
#else
        return ANDROID_TEST_AD_UNIT;
#endif
    }

#if GOOGLE_MOBILE_ADS
    private void RegisterAdCallbacks()
    {
        if (rewardedAd == null) return;

        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[RewardedAdsManager] Ad closed");
            isAdLoaded = false;
            // Preload next ad
            Preload();
        };

        rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"[RewardedAdsManager] Ad failed: {error.GetMessage()}");
            isAdLoaded = false;
            // Preload next ad
            Preload();
        };
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
#if GOOGLE_MOBILE_ADS
        if (rewardedAd == null || !isAdLoaded)
        {
            Debug.LogWarning("[RewardedAdsManager] Ad not ready!");
            SafeInvoke(onFailed, "Ad not loaded");
            return;
        }

        bool rewardEarned = false;

        // Register one-time callbacks for this show
        rewardedAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[RewardedAdsManager] 📺 Ad opened!");
            SafeInvoke(onOpened);
        };

        rewardedAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log($"[RewardedAdsManager] 📺 Ad closed. Reward earned: {rewardEarned}");
            SafeInvoke(onClosed, rewardEarned);
            isAdLoaded = false;
            rewardedAd = null;
            // Preload next ad
            Preload();
        };

        rewardedAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError($"[RewardedAdsManager] ❌ Ad failed to show: {error.GetMessage()}");
            SafeInvoke(onFailed, error.GetMessage());
            isAdLoaded = false;
            rewardedAd = null;
            // Preload next ad
            Preload();
        };

        // Show the ad
        rewardedAd.Show((Reward reward) =>
        {
            Debug.Log($"[RewardedAdsManager] 🎉 Reward earned! Amount: {reward.Amount}");
            rewardEarned = true;
            SafeInvoke(OnRewardGranted);
            SafeInvoke(onReward);
        });
#else
        // Editor/simulation mode
        Debug.Log("[RewardedAdsManager] 🎬 Editor simulate: opened -> reward -> closed(true)");
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
