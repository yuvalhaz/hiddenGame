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
    [SerializeField] private string testAdUnitId_Android = "ca-app-pub-3940256099942544/5224354917";
    [SerializeField] private string testAdUnitId_iOS = "ca-app-pub-3940256099942544/1712485313";

    [Header("Production Ad Unit IDs")]
    [Tooltip("REAL Rewarded Ad Unit ID from your AdMob account (Android)")]
    [SerializeField] private string productionAdUnitId_Android = "ca-app-pub-7861548436347890/5250139476";
    [Tooltip("REAL Rewarded Ad Unit ID from your AdMob account (iOS, if you use it)")]
    [SerializeField] private string productionAdUnitId_iOS = "";

    [Header("AdMob App ID")]
    [Tooltip("REAL AdMob App ID (Android)")]
    [SerializeField] private string adMobAppId_Android = "ca-app-pub-7861548436347890~8618538845";

    [Tooltip("AdMob App ID for iOS (optional)")]
    [SerializeField] private string adMobAppId_iOS = "";

    /// <summary>
    /// Returns the correct Rewarded Ad Unit ID according to platform & test mode
    /// </summary>
    public string GetRewardedAdUnitId()
    {
        string adUnitId = "";

#if UNITY_ANDROID
        adUnitId = useTestAds ? testAdUnitId_Android : productionAdUnitId_Android;
#elif UNITY_IOS
        adUnitId = useTestAds ? testAdUnitId_iOS : productionAdUnitId_iOS;
#else
        Debug.LogWarning("[AdMobConfig] Platform not supported for ads");
        adUnitId = testAdUnitId_Android; // fallback
#endif

        if (string.IsNullOrEmpty(adUnitId))
        {
            Debug.LogWarning("[AdMobConfig] Ad Unit ID is empty! Check configuration.");
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
