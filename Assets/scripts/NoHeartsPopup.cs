using UnityEngine;
using UnityEngine.UI;

public class NoHeartsPopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private HeartsManager heartsManager;
    [SerializeField] private RewardedAdsManager rewardedAds;

    [Header("Optional UI")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private GameObject loadingSpinner; // אופציונלי: אייקון טעינה קטן

    [Header("Config")]
    [SerializeField] private bool startHidden = true;
    [SerializeField] private int heartsOnReward = 5;

    void Reset()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();
        if (!heartsManager) heartsManager = FindObjectOfType<HeartsManager>(true);
        if (!rewardedAds) rewardedAds = FindObjectOfType<RewardedAdsManager>(true);

        if (startHidden) Hide(); else Show();
        SetBusy(false);
    }

    public void Show()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    // YES: פותח מודעת Rewarded. מוסיף לבבות רק אם התקבל reward.
    public void OnYes()
    {
        if (rewardedAds == null)
        {
            Debug.LogWarning("[NoHeartsPopup] RewardedAdsManager missing.");
            return;
        }

        if (!rewardedAds.IsReady())
        {
            Debug.Log("[NoHeartsPopup] Rewarded not ready yet.");
            // אפשר: להדליק preloading, או להציג הודעה למשתמש.
            rewardedAds.Preload(); // לא חובה, תלוי במימוש שלך
            return;
        }

        SetBusy(true);

        rewardedAds.ShowRewarded(
            onReward: () =>
            {
                if (heartsManager != null && heartsOnReward > 0)
                    heartsManager.AddHearts(heartsOnReward);

                SetBusy(false);
                Hide();
            },
            onClosed: () =>
            {
                // נסגר בלי Reward – נשאיר את הפופאפ פתוח כדי שינסה שוב או ילחץ NO
                SetBusy(false);
            },
            onFailed: (err) =>
            {
                Debug.LogWarning("[NoHeartsPopup] Failed to show rewarded: " + err);
                SetBusy(false);
            }
        );
    }

    // NO: סוגר בלי להוסיף לבבות
    public void OnNo() => Hide();

    private void SetBusy(bool busy)
    {
        if (yesButton) yesButton.interactable = !busy;
        if (noButton)  noButton.interactable  = !busy;
        if (loadingSpinner) loadingSpinner.SetActive(busy);
    }
}
