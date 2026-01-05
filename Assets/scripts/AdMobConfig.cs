using UnityEngine;

/// <summary>
/// AdMob configuration – holds App ID + Ad Unit IDs
/// </summary>
public class AdMobConfig : MonoBehaviour
{
    [Header("AdMob Setup")]
    [Tooltip("Use Google TEST ads (recommended while developing)")]
    [SerializeField] private bool useTestAds = true;

    [Header("Test Ad Unit IDs (Google Demo Ads)")]
    [Tooltip("Rewarded test ad unit (Google sample)")]
    [SerializeField] private string testRewardedAdUnitId_Android = "ca-app-pub-3940256099942544/5224354917";
    [SerializeField] private string testRewardedAdUnitId_iOS = "ca-app-pub-3940256099942544/1712485313";

    [Tooltip("Interstitial test ad unit (Google sample)")]
    [SerializeField] private string testInterstitialAdUnitId_Android = "ca-app-pub-3940256099942544/1033173712";
    [SerializeField] private string testInterstitialAdUnitId_iOS = "ca-app-pub-3940256099942544/4411468910";

    [Header("Production Ad Unit IDs")]
    [Tooltip("REAL Rewarded Ad Unit ID from your AdMob account (Android)")]
    [SerializeField] private string productionRewardedAdUnitId_Android = "ca-app-pub-7861548436347890/9905311499";
    [Tooltip("REAL Rewarded Ad Unit ID from your AdMob account (iOS, if you use it)")]
    [SerializeField] private string productionRewardedAdUnitId_iOS = "";

    [Tooltip("REAL Interstitial Ad Unit ID from your AdMob account (Android)")]
    [SerializeField] private string productionInterstitialAdUnitId_Android = "ca-app-pub-7861548436347890/1100930228";
    [Tooltip("REAL Interstitial Ad Unit ID from your AdMob account (iOS, if you use it)")]
    [SerializeField] private string productionInterstitialAdUnitId_iOS = "";

    [Header("AdMob App ID")]
    [Tooltip("REAL AdMob App ID (Android)")]
    [SerializeField] private string adMobAppId_Android = "ca-app-pub-7861548436347890~7127282529";

    [Tooltip("AdMob App ID for iOS (optional)")]
    [SerializeField] private string adMobAppId_iOS = "";

    /// <summary>
    /// Returns the correct Rewarded Ad Unit ID according to platform & test mode
    /// </summary>
    public string GetRewardedAdUnitId()
    {
        string adUnitId = "";

#if UNITY_ANDROID
        adUnitId = useTestAds ? testRewardedAdUnitId_Android : productionRewardedAdUnitId_Android;
#elif UNITY_IOS
        adUnitId = useTestAds ? testRewardedAdUnitId_iOS : productionRewardedAdUnitId_iOS;
#else
        Debug.LogWarning("[AdMobConfig] Platform not supported for ads");
        adUnitId = testRewardedAdUnitId_Android; // fallback
#endif

        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogWarning("[AdMobConfig] Rewarded Ad Unit ID is empty! Check configuration.");
        }

        return adUnitId;
    }

    /// <summary>
    /// Returns the correct Interstitial Ad Unit ID according to platform & test mode
    /// </summary>
    public string GetInterstitialAdUnitId()
    {
        string adUnitId = "";

#if UNITY_ANDROID
        adUnitId = useTestAds ? testInterstitialAdUnitId_Android : productionInterstitialAdUnitId_Android;
#elif UNITY_IOS
        adUnitId = useTestAds ? testInterstitialAdUnitId_iOS : productionInterstitialAdUnitId_iOS;
#else
        Debug.LogWarning("[AdMobConfig] Platform not supported for ads");
        adUnitId = testInterstitialAdUnitId_Android; // fallback
#endif

        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogWarning("[AdMobConfig] Interstitial Ad Unit ID is empty! Check configuration.");
        }

        return adUnitId;
    }

    /// <summary>
    /// Returns the App ID according to platform
    /// </summary>
    public string GetAppId()
    {
#if UNITY_ANDROID
        return adMobAppId_Android;
#elif UNITY_IOS
        return adMobAppId_iOS;
#else
        return adMobAppId_Android; // fallback
#endif
    }

    /// <summary>
    /// Are we in TEST mode?
    /// </summary>
    public bool IsTestMode()
    {
        return useTestAds;
    }
}
