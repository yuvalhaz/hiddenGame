using UnityEngine;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
using GoogleMobileAds.Api;
#endif

/// <summary>
/// ✅ FIXED: Centralized AdMob initialization - initializes SDK once and preloads ads when ready
/// </summary>
[DisallowMultipleComponent]
public class AdInit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RewardedAdsManager rewardedAds;
    [SerializeField] private AdMobConfig adMobConfig;

    [Header("Config")]
    [Tooltip("לטעון מודעת Rewarded מראש עם התחלת הסצנה.")]
    [SerializeField] private bool preloadRewardedOnInit = true;

    [Tooltip("לטעון מודעת Interstitial מראש עם התחלת הסצנה.")]
    [SerializeField] private bool preloadInterstitialOnInit = true;

    [Tooltip("להשאיר את אובייקט האתחול חי בין סצנות.")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    private bool isInitialized = false;

    void Awake()
    {
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        // מצא רפרנסים אוטומטית אם לא מוגדרים
        if (!rewardedAds)
            rewardedAds = FindObjectOfType<RewardedAdsManager>(true);

        if (!adMobConfig)
            adMobConfig = FindObjectOfType<AdMobConfig>(true);
    }

    void Start()
    {
        // בדיקות אבחון
        if (!adMobConfig)
        {
            Debug.LogError("[AdInit] AdMobConfig not found! Add it to the scene for ads to work.");
            return;
        }

        if (!rewardedAds)
        {
            Debug.LogWarning("[AdInit] RewardedAdsManager not found in scene.");
        }

#if UNITY_EDITOR
        Debug.Log("[AdInit] Editor mode - skipping AdMob initialization");
        OnAdMobInitialized();
#else
        InitializeAdMob();
#endif
    }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
    /// <summary>
    /// ✅ FIXED: Centralized AdMob initialization with proper callback
    /// </summary>
    private void InitializeAdMob()
    {
        Debug.Log("[AdInit] Initializing AdMob SDK...");

        // הצג מידע על מצב הפרסומות
        if (adMobConfig.IsTestMode())
        {
            Debug.Log($"[AdInit] Test Mode enabled. Rewarded: {adMobConfig.GetRewardedAdUnitId()}, Interstitial: {adMobConfig.GetInterstitialAdUnitId()}");
        }
        else
        {
            Debug.Log("[AdInit] Production Mode - using REAL Ad Units");
        }

        // ✅ Configure test device (CRITICAL for test ads to work on real phone!)
        // Using new syntax for modern AdMob SDK versions
        RequestConfiguration requestConfiguration = new RequestConfiguration
        {
            TestDeviceIds = new System.Collections.Generic.List<string> {
                AdRequest.TestDeviceSimulator, // Include emulator
                "f8a502bf-0bb6-4ad5-974f-80295916dbaa" // Your real device
            }
        };
        MobileAds.SetRequestConfiguration(requestConfiguration);
        Debug.Log("[AdInit] ✅ Test device configuration set");

        // ✅ Initialize SDK and wait for callback before preloading
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log($"[AdInit] ✅ AdMob initialized successfully! Status: {initStatus}");
            isInitialized = true;

            // ✅ Only preload AFTER initialization is complete
            OnAdMobInitialized();
        });
    }
#endif

    /// <summary>
    /// Called when AdMob is fully initialized and ready
    /// </summary>
    private void OnAdMobInitialized()
    {
        Debug.Log("[AdInit] AdMob ready - preloading ads...");

        // Preload rewarded ad
        if (preloadRewardedOnInit && rewardedAds != null)
        {
            rewardedAds.Preload(success =>
            {
                if (success)
                    Debug.Log("[AdInit] ✅ Rewarded ad preloaded successfully!");
                else
                    Debug.LogWarning("[AdInit] ⚠️ Failed to preload rewarded ad");
            });
        }

        // Preload interstitial ad
        if (preloadInterstitialOnInit && InterstitialAdsManager.Instance != null)
        {
            InterstitialAdsManager.Instance.Preload(success =>
            {
                if (success)
                    Debug.Log("[AdInit] ✅ Interstitial ad preloaded successfully!");
                else
                    Debug.LogWarning("[AdInit] ⚠️ Failed to preload interstitial ad");
            });
        }
    }

    /// <summary>
    /// ניתן לקרוא מפאנל/כפתור כדי לטעון מראש בכל רגע.
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

    /// <summary>
    /// בדיקת מוכנות – שימושי לפני הצגה.
    /// </summary>
    public bool IsRewardedReady()
    {
        return rewardedAds != null && rewardedAds.IsReady();
    }

    /// <summary>
    /// בדיקה אם AdMob אותחל
    /// </summary>
    public bool IsAdMobInitialized()
    {
        return isInitialized;
    }
}
