using UnityEngine;

/// <summary>
/// הגדרות AdMob - Ad Unit IDs למבחן ולפרודקשן
/// </summary>
public class AdMobConfig : MonoBehaviour
{
    [Header("AdMob Setup")]
    [Tooltip("האם להשתמש ב-Test Ads של Google")]
    [SerializeField] private bool useTestAds = true;

    [Header("Test Ad Unit IDs (Google Demo Ads)")]
    [Tooltip("Ad Unit ID לבדיקות - מציג פרסומות דמו אמיתיות")]
    [SerializeField] private string testAdUnitId_Android = "ca-app-pub-3940256099942544/5224354917";
    [SerializeField] private string testAdUnitId_iOS = "ca-app-pub-3940256099942544/1712485313";

    [Header("Production Ad Unit IDs")]
    [Tooltip("Ad Unit ID אמיתי מחשבון AdMob שלך")]
    [SerializeField] private string productionAdUnitId_Android = "";
    [SerializeField] private string productionAdUnitId_iOS = "";

    [Header("AdMob App ID")]
    [Tooltip("App ID מחשבון AdMob - נדרש לאתחול")]
    [SerializeField] private string adMobAppId_Android = "ca-app-pub-3940256099942544~3347511713"; // Test App ID
    [SerializeField] private string adMobAppId_iOS = "ca-app-pub-3940256099942544~1458002511"; // Test App ID

    /// <summary>
    /// מחזיר את ה-Ad Unit ID הנכון לפי הפלטפורמה ומצב הבדיקה
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
    /// מחזיר את ה-AdMob App ID לפי הפלטפורמה
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
    /// האם נמצאים במצב Test
    /// </summary>
    public bool IsTestMode()
    {
        return useTestAds;
    }
}
