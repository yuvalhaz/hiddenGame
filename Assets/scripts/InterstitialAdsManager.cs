using System;
using System.Collections;
using UnityEngine;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
using GoogleMobileAds.Api;
#endif

/// <summary>
/// Manages Interstitial Ads (full-screen ads shown between batches/levels)
/// </summary>
public class InterstitialAdsManager : MonoBehaviour
{
    public static InterstitialAdsManager Instance;

    /// <summary>נורה כשהפרסומת נסגרת.</summary>
    public event Action<bool> OnAdClosed; // bool = האם הפרסומת הושלמה

    [Header("Configuration")]
    [SerializeField] private AdMobConfig adMobConfig;

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
    private InterstitialAd interstitialAd;
    private bool isAdLoading = false;

    // Main thread flags for callbacks
    private Action pendingOnMainThread = null;
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
    void Update()
    {
        // Execute pending callbacks on main thread
        if (pendingOnMainThread != null)
        {
            Action action = pendingOnMainThread;
            pendingOnMainThread = null;
            action.Invoke();
        }
    }
#endif

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
    private void InitializeAdMob()
    {
        if (adMobConfig == null)
        {
            Debug.LogError("[InterstitialAdsManager] AdMobConfig not found! Add it to the scene.");
            return;
        }

        // ✅ Configure test devices for better test ad delivery
        if (adMobConfig.IsTestMode())
        {
            Debug.Log("[InterstitialAdsManager] Configuring TEST MODE");

            // Set test device configuration
            var requestConfiguration = new RequestConfiguration
                .Builder()
                .SetTestDeviceIds(System.Collections.Generic.List<string> { AdRequest.TestDeviceSimulator })
                .build();

            MobileAds.SetRequestConfiguration(requestConfiguration);
        }

        // אתחול Google Mobile Ads SDK (אם עוד לא אותחל)
        MobileAds.Initialize(initStatus =>
        {
            Debug.Log($"[InterstitialAdsManager] AdMob initialized for Interstitial ads");
            if (adMobConfig.IsTestMode())
            {
                Debug.Log("[InterstitialAdsManager] Running in TEST MODE with Google demo ads");
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
        return interstitialAd != null && interstitialAd.CanShowAd();
#endif
    }

    public void Preload(Action<bool> onLoaded = null)
    {
#if UNITY_EDITOR
        Debug.Log("[InterstitialAdsManager] Editor mode - simulating ad load");
        SafeInvoke(onLoaded, true);
#else
        LoadInterstitialAd(onLoaded);
#endif
    }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
    private void LoadInterstitialAd(Action<bool> onLoaded = null)
    {
        if (adMobConfig == null)
        {
            Debug.LogError("[InterstitialAdsManager] Cannot load ad - AdMobConfig is missing");
            SafeInvoke(onLoaded, false);
            return;
        }

        // נקה פרסומת קודמת
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
        }

        if (isAdLoading)
        {
            Debug.LogWarning("[InterstitialAdsManager] Ad is already loading");
            SafeInvoke(onLoaded, false);
            return;
        }

        isAdLoading = true;
        string adUnitId = adMobConfig.GetInterstitialAdUnitId();

        Debug.Log($"[InterstitialAdsManager] Loading ad with ID: {adUnitId}");

        // בקשת פרסומת
        var adRequest = new AdRequest();

        InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            isAdLoading = false;

            if (error != null || ad == null)
            {
                Debug.LogError($"[InterstitialAdsManager] Failed to load ad: {error}");
                SafeInvoke(onLoaded, false);
                return;
            }

            interstitialAd = ad;
            Debug.Log("[InterstitialAdsManager] Ad loaded successfully!");

            SafeInvoke(onLoaded, true);
        });
    }
#endif

    // ===================== MAIN API =====================

    /// <summary>
    /// Show interstitial ad with optional callbacks
    /// </summary>
    /// <param name="onClosed">Called when ad is closed - receives bool indicating if completed</param>
    /// <param name="onFailed">Called if ad fails to show - receives error message</param>
    /// <param name="onOpened">Called when ad opens</param>
    public void ShowInterstitial(
        Action<bool> onClosed = null,
        Action<string> onFailed = null,
        Action onOpened = null)
    {
#if UNITY_EDITOR
        Debug.Log("[InterstitialAdsManager] Editor simulate: opened -> closed(true).");
        SafeInvoke(onOpened);
        SafeInvoke(OnAdClosed, true);
        SafeInvoke(onClosed, true);
#else
        ShowInterstitialAdReal(onClosed, onFailed, onOpened);
#endif
    }

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
    private void ShowInterstitialAdReal(
        Action<bool> onClosed,
        Action<string> onFailed,
        Action onOpened)
    {
        if (interstitialAd == null || !interstitialAd.CanShowAd())
        {
            string errorMsg = "Interstitial ad is not ready to show";
            Debug.LogWarning($"[InterstitialAdsManager] {errorMsg}");
            SafeInvoke(onFailed, errorMsg);

            // נסה לטעון פרסומת חדשה
            Preload();
            return;
        }

        bool adWasShown = false;

        // רישום callbacks - ✅ Run on main thread to avoid Android UI issues
        interstitialAd.OnAdFullScreenContentOpened += () =>
        {
            pendingOnMainThread = () =>
            {
                Debug.Log("[InterstitialAdsManager] Ad opened");
                adWasShown = true;
                SafeInvoke(onOpened);
            };
        };

        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            pendingOnMainThread = () =>
            {
                Debug.Log($"[InterstitialAdsManager] Ad closed. Was shown: {adWasShown}");
                SafeInvoke(OnAdClosed, adWasShown);
                SafeInvoke(onClosed, adWasShown);

                // טען פרסומת חדשה אוטומטית
                Preload();
            };
        };

        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            string errorMsg = $"Failed to show ad: {error}";
            pendingOnMainThread = () =>
            {
                Debug.LogError($"[InterstitialAdsManager] {errorMsg}");
                SafeInvoke(onFailed, errorMsg);

                // טען פרסומת חדשה אוטומטית
                Preload();
            };
        };

        // הצג פרסומת - ✅ Must be called on main thread
        Debug.Log("[InterstitialAdsManager] Calling Show() on main thread");
        interstitialAd.Show();
    }
#endif

    // ===================== Cleanup =====================

    void OnDestroy()
    {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
        if (interstitialAd != null)
        {
            interstitialAd.Destroy();
            interstitialAd = null;
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
