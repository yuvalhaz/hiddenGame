using UnityEngine;

[DisallowMultipleComponent]
public class AdInit : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RewardedAdsManager rewardedAds;

    [Header("Config")]
    [Tooltip("לטעון מודעת Rewarded מראש עם התחלת הסצנה.")]
    [SerializeField] private bool preloadOnStart = true;

    [Tooltip("להשאיר את אובייקט האתחול חי בין סצנות.")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    void Awake()
    {
        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);

        if (!rewardedAds)
            rewardedAds = FindObjectOfType<RewardedAdsManager>(true);
    }

    void Start()
    {
        if (!rewardedAds)
        {
            Debug.LogWarning("[AdInit] RewardedAdsManager not found in scene.");
            return;
        }

        if (preloadOnStart)
            rewardedAds.Preload(); // ← תואם לממשק שלך; אין LoadRewarded
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
