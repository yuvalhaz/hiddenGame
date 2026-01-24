using System;
using UnityEngine;
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)
using GoogleMobileAds.Api;
#endif

/// <summary>
/// ✅ FIXED: Manages Interstitial Ads with main thread dispatcher for callbacks
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
#endif

    // ✅ FIXED: Main thread dispatcher to prevent crashes
    private Action mainThreadAction;

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

    // ✅ FIXED: Update loop to execute main thread actions safely
    void Update()
    {
        if (mainThreadAction != null)
        {
            mainThreadAction.Invoke();
            mainThreadAction = null;
        }
    }

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
                // ✅ Use main thread dispatcher for callback
                mainThreadAction = () => SafeInvoke(onLoaded, false);
                return;
            }

            interstitialAd = ad;
            Debug.Log("[InterstitialAdsManager] Ad loaded successfully!");

            // ✅ Use main thread dispatcher for callback
            mainThreadAction = () => SafeInvoke(onLoaded, true);
        });
    }
#endif

    // ===================== MAIN API =====================

    /// <summary>
    /// Show interstitial ad with optional callbacks
    /// Skips ad if player purchased ad removal
    /// </summary>
    public void ShowInterstitial(
        Action<bool> onClosed = null,
        Action<string> onFailed = null,
        Action onOpened = null)
    {
        // Check if ads were removed via IAP
        if (IAPManager.Instance != null && IAPManager.Instance.AreAdsRemoved())
        {
            Debug.Log("[InterstitialAdsManager] 💎 Ads removed - skipping ad");
            SafeInvoke(onClosed, true);
            return;
        }

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

        // ✅ FIXED: Use main thread dispatcher for all callbacks
        interstitialAd.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("[InterstitialAdsManager] Ad opened");
            adWasShown = true;
            mainThreadAction = () => SafeInvoke(onOpened);
        };

        interstitialAd.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log($"[InterstitialAdsManager] Ad closed. Was shown: {adWasShown}");
            mainThreadAction = () =>
            {
                SafeInvoke(OnAdClosed, adWasShown);
                SafeInvoke(onClosed, adWasShown);
            };

            // טען פרסומת חדשה אוטומטית
            Preload();
        };

        interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
        {
            string errorMsg = $"Failed to show ad: {error}";
            Debug.LogError($"[InterstitialAdsManager] {errorMsg}");
            mainThreadAction = () => SafeInvoke(onFailed, errorMsg);

            // טען פרסומת חדשה אוטומטית
            Preload();
        };

        // הצג פרסומת
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
