using UnityEngine;

/// <summary>
/// אתחול פרסומות - מטעין פרסומות מראש ושומר אותן זמינות.
/// הערה: דאג שיש AdMobConfig ו-RewardedAdsManager בסצנה הראשונה.
/// </summary>
[DisallowMultipleComponent]
public class AdInit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RewardedAdsManager rewardedAds;
    [SerializeField] private InterstitialAdsManager interstitialAds;
    [SerializeField] private AdMobConfig adMobConfig;

    [Header("Config")]
    [Tooltip("לטעון מודעת Rewarded מראש עם התחלת הסצנה.")]
    [SerializeField] private bool preloadOnStart = true;

    [Tooltip("להשאיר את אובייקט האתחול חי בין סצנות.")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    private void Awake()
    {
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        // מצא רפרנסים אוטומטית אם לא מוגדרים
        if (!rewardedAds)
            rewardedAds = FindObjectOfType<RewardedAdsManager>(true);

        if (!interstitialAds)
            interstitialAds = FindObjectOfType<InterstitialAdsManager>(true);

        if (!adMobConfig)
            adMobConfig = FindObjectOfType<AdMobConfig>(true);
    }

    private void Start()
    {
        // בדיקות אבחון
        if (!adMobConfig)
        {
            Debug.LogError("[AdInit] AdMobConfig not found! Add it to the scene for ads to work.");
            return;
        }

        // הצג מידע על מצב הפרסומות
        if (adMobConfig.IsTestMode())
        {
            Debug.Log($"[AdInit] Test Mode enabled. Using Test Ad Unit: {adMobConfig.GetRewardedAdUnitId()}");
        }
        else
        {
            Debug.Log("[AdInit] Production Mode - using REAL Ad Units");
        }

        // טען מראש אם מוגדר
        if (preloadOnStart)
        {
            // Preload rewarded ads (for hints)
            if (rewardedAds)
            {
                rewardedAds.Preload(success =>
                {
                    if (success)
                        Debug.Log("[AdInit] Rewarded ad preloaded successfully!");
                    else
                        Debug.LogWarning("[AdInit] Failed to preload rewarded ad");
                });
            }
            else
            {
                Debug.LogWarning("[AdInit] RewardedAdsManager not found - skipping rewarded ad preload");
            }

            // Preload interstitial ads (for batch completions)
            if (interstitialAds)
            {
                interstitialAds.Preload(success =>
                {
                    if (success)
                        Debug.Log("[AdInit] Interstitial ad preloaded successfully!");
                    else
                        Debug.LogWarning("[AdInit] Failed to preload interstitial ad");
                });
            }
            else
            {
                Debug.LogWarning("[AdInit] InterstitialAdsManager not found - skipping interstitial ad preload");
            }
        }
    }

    /// <summary>
    /// ניתן לקרוא מפאנל/כפתור כדי לטעון rewarded ad מראש בכל רגע.
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
    /// ניתן לקרוא כדי לטעון interstitial ad מראש בכל רגע.
    /// </summary>
    public void PreloadInterstitialNow()
    {
        if (!interstitialAds)
        {
            interstitialAds = FindObjectOfType<InterstitialAdsManager>(true);
            if (!interstitialAds)
            {
                Debug.LogWarning("[AdInit] Cannot preload – InterstitialAdsManager missing.");
                return;
            }
        }
        interstitialAds.Preload();
    }

    /// <summary>
    /// בדיקת מוכנות rewarded ad – שימושי לפני הצגה.
    /// </summary>
    public bool IsRewardedReady()
    {
        return rewardedAds != null && rewardedAds.IsReady();
    }

    /// <summary>
    /// בדיקת מוכנות interstitial ad – שימושי לפני הצגה.
    /// </summary>
    public bool IsInterstitialReady()
    {
        return interstitialAds != null && interstitialAds.IsReady();
    }
}
