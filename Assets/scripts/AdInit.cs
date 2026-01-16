using UnityEngine;

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
using GoogleMobileAds.Api;
using System.Collections.Generic;
#endif

/// <summary>
/// Centralized AdMob initialization - initializes SDK once and preloads ads when ready.
/// FIXED: RequestConfiguration is now applied ONLY AFTER MobileAds.Initialize() callback,
/// so GetClientFactory errors should stop.
/// </summary>
[DisallowMultipleComponent]
public class AdInit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RewardedAdsManager rewardedAds;
    [SerializeField] private AdMobConfig adMobConfig;

    [Header("Config")]
    [Tooltip("Load a Rewarded ad on startup.")]
    [SerializeField] private bool preloadRewardedOnInit = true;

    [Tooltip("Load an Interstitial ad on startup.")]
    [SerializeField] private bool preloadInterstitialOnInit = true;

    [Tooltip("Keep this initializer alive between scenes.")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    // Prevent duplicate initialization across scenes.
    private static bool s_initializingOrInitialized = false;

    private bool isInitialized = false;

    private void Awake()
    {
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        // Auto-find references if not assigned
        if (!rewardedAds)
            rewardedAds = FindObjectOfType<RewardedAdsManager>(true);

        if (!adMobConfig)
            adMobConfig = FindObjectOfType<AdMobConfig>(true);
    }

    private void Start()
    {
        if (s_initializingOrInitialized)
        {
            // Already initializing/initialized somewhere else
            return;
        }

        // Diagnostics
        if (!adMobConfig)
        {
            Debug.LogError("[AdInit] AdMobConfig not found! Add it to the scene for ads to work.");
            return;
        }

        if (!rewardedAds)
        {
            Debug.LogWarning("[AdInit] RewardedAdsManager not found in scene.");
        }

        s_initializingOrInitialized = true;

#if UNITY_EDITOR
        Debug.Log("[AdInit] Editor mode - skipping AdMob initialization");
        isInitialized = true;
        OnAdMobInitialized();
#else
        InitializeAdMob();
#endif
    }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
    private void InitializeAdMob()
    {
        Debug.Log("[AdInit] Initializing AdMob SDK...");

        if (adMobConfig != null && adMobConfig.IsTestMode())
        {
            Debug.Log($"[AdInit] Test Mode enabled. Rewarded: {adMobConfig.GetRewardedAdUnitId()}, Interstitial: {adMobConfig.GetInterstitialAdUnitId()}");
        }
        else
        {
            Debug.Log("[AdInit] Production Mode - using REAL Ad Units");
        }

        // Build request configuration (do NOT apply before Initialize)
        RequestConfiguration requestConfiguration = new RequestConfiguration
        {
            TestDeviceIds = new List<string>
            {
                AdRequest.TestDeviceSimulator,
                "f8a502bf-0bb6-4ad5-974f-80295916dbaa" // your real device
            }
        };

        // ✅ Initialize first, THEN apply RequestConfiguration
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log($"[AdInit] ✅ AdMob initialized successfully! Status: {initStatus}");
            isInitialized = true;

            // ✅ Now it is safe to call MobileAds APIs (prevents GetClientFactory)
            MobileAds.SetRequestConfiguration(requestConfiguration);
            Debug.Log("[AdInit] ✅ Test device configuration set");

            // Only preload AFTER initialization is complete
            OnAdMobInitialized();
        });
    }
#endif

    /// <summary>
    /// Called when AdMob is fully initialized and ready.
    /// </summary>
    private void OnAdMobInitialized()
    {
        Debug.Log("[AdInit] AdMob ready - preloading ads...");

        // Preload rewarded
        if (preloadRewardedOnInit && rewardedAds != null)
        {
            rewardedAds.Preload(success =>
            {
                if (success) Debug.Log("[AdInit] ✅ Rewarded ad preloaded successfully!");
                else Debug.LogWarning("[AdInit] ⚠️ Failed to preload rewarded ad");
            });
        }

        // Preload interstitial (if you have this manager in your project)
        if (preloadInterstitialOnInit && InterstitialAdsManager.Instance != null)
        {
            InterstitialAdsManager.Instance.Preload(success =>
            {
                if (success) Debug.Log("[AdInit] ✅ Interstitial ad preloaded successfully!");
                else Debug.LogWarning("[AdInit] ⚠️ Failed to preload interstitial ad");
            });
        }
    }

    /// <summary>
    /// Can be called from UI to preload at any time.
    /// </summary>
    public void PreloadRewardedNow()
    {
        if (!rewardedAds)
        {
            rewardedAds = FindObjectOfType<RewardedAdsManager>(true);
            if (!rewardedAds)
            {
                Debug.LogWarning("[AdInit] Cannot preload – RewardedAdsManager missing.");
                return;
            }
        }

        rewardedAds.Preload();
    }

    public bool IsRewardedReady()
    {
        return rewardedAds != null && rewardedAds.IsReady();
    }

    public bool IsAdMobInitialized()
    {
        return isInitialized;
    }
}
