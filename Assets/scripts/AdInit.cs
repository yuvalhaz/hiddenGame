using UnityEngine;

/// <summary>
/// אתחול פרסומות - מטעין פרסומות מראש ושומר אותן זמינות
/// הערה: דאג שיש AdMobConfig בסצנה!
/// </summary>
[DisallowMultipleComponent]
public class AdInit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RewardedAdsManager rewardedAds;
    [SerializeField] private AdMobConfig adMobConfig;

    [Header("Config")]
    [Tooltip("לטעון מודעת Rewarded מראש עם התחלת הסצנה.")]
    [SerializeField] private bool preloadOnStart = true;

    [Tooltip("להשאיר את אובייקט האתחול חי בין סצנות.")]
    [SerializeField] private bool dontDestroyOnLoad = true;

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
            return;
        }

        // הצג מידע על מצב הפרסומות
        if (adMobConfig.IsTestMode())
        {
            Debug.Log($"[AdInit] Test Mode enabled. Using Test Ad Unit: {adMobConfig.GetRewardedAdUnitId()}");
        }
        else
        {
            Debug.Log("[AdInit] Production Mode - using real Ad Units");
        }

        // טען מראש אם מוגדר
        if (preloadOnStart)
        {
            rewardedAds.Preload(success =>
            {
                if (success)
                    Debug.Log("[AdInit] Ad preloaded successfully!");
                else
                    Debug.LogWarning("[AdInit] Failed to preload ad");
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
}
