using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Dialog UI for hint system that offers players a hint in exchange for watching a rewarded ad.
/// Shows "Watch Ad" and "Close" buttons, handles ad display, and triggers hint logic on reward.
/// </summary>
public class HintDialog : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup dialogGroup;

    [Header("Events")]
    [Tooltip("Invoked when user successfully watches ad and earns hint")]
    public UnityEvent onHintGranted;

    [Tooltip("Invoked when dialog is closed")]
    public UnityEvent onClosed;

    // Prevent infinite recursion if onClosed event is misconfigured
    private bool isClosing = false;

    private void Awake()
    {
        if (dialogGroup == null)
            dialogGroup = GetComponent<CanvasGroup>();

        if (watchAdButton != null)
            watchAdButton.onClick.AddListener(OnWatchAd);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (watchAdButton != null)
            watchAdButton.onClick.RemoveListener(OnWatchAd);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (RewardedAdsManager.Instance != null)
            RewardedAdsManager.Instance.OnRewardGranted -= HandleReward;
    }

    /// <summary>
    /// Opens the hint dialog and brings it to front.
    /// </summary>
    public void Open()
    {
        ShowImmediate();
        transform.SetAsLastSibling();
    }

    /// <summary>
    /// Closes the hint dialog and invokes onClosed event.
    /// </summary>
    public void Close()
    {
        // Prevent infinite recursion if onClosed is connected to Close()
        if (isClosing) return;

        isClosing = true;
        HideImmediate();
        onClosed?.Invoke();
        isClosing = false;
    }

    /// <summary>
    /// Called when user clicks "Watch Ad" button.
    /// Hides dialog and shows rewarded ad.
    /// </summary>
    private void OnWatchAd()
    {
        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogWarning("[HintDialog] RewardedAdsManager not found in scene.");
            return;
        }

        // לא לאפשר ספאם קליקים בזמן טעינה/הצגה
        if (watchAdButton != null)
            watchAdButton.interactable = false;

        // פונקציה מקומית שממש מציגה את הפרסומת
        void ShowNow()
        {
            HideImmediate();

            // נרשמים ל-Reward פעם אחת
            RewardedAdsManager.Instance.OnRewardGranted -= HandleReward;
            RewardedAdsManager.Instance.OnRewardGranted += HandleReward;

            RewardedAdsManager.Instance.ShowRewarded(
                onReward: null,
                onClosed: (completed) =>
                {
                    if (watchAdButton != null)
                        watchAdButton.interactable = true;
                },
                onFailed: (error) =>
                {
                    Debug.LogWarning($"[HintDialog] Failed to show ad: {error}");
                    if (watchAdButton != null)
                        watchAdButton.interactable = true;
                },
                onOpened: null
            );
        }

        // אם יש כבר מודעה טעונה – מציגים מיד
        if (RewardedAdsManager.Instance.IsReady())
        {
            Debug.Log("[HintDialog] Ad ready, showing now.");
            ShowNow();
        }
        else
        {
            // אם אין מודעה – טוענים ואז מציגים
            Debug.Log("[HintDialog] Ad not ready, loading...");
            RewardedAdsManager.Instance.Preload(success =>
            {
                if (success && RewardedAdsManager.Instance.IsReady())
                {
                    Debug.Log("[HintDialog] Ad loaded after click, showing now.");
                    ShowNow();
                }
                else
                {
                    Debug.LogWarning("[HintDialog] Ad failed to load, cannot show hint.");
                    if (watchAdButton != null)
                        watchAdButton.interactable = true;
                }
            });
        }
    }



    /// <summary>
    /// Called when user successfully watches rewarded ad.
    /// Grants hint and triggers onHintGranted event.
    /// </summary>
    private void HandleReward()
    {
        if (RewardedAdsManager.Instance != null)
            RewardedAdsManager.Instance.OnRewardGranted -= HandleReward;

        HideImmediate();

        #if UNITY_EDITOR
        Debug.Log("[HintDialog] Hint reward granted!");
        #endif

        // Trigger hint logic (connected in Unity Inspector or via VisualHintSystem)
        onHintGranted?.Invoke();
    }

    /// <summary>
    /// Shows the dialog immediately using CanvasGroup.
    /// </summary>
    private void ShowImmediate()
    {
        if (dialogGroup == null) return;

        dialogGroup.alpha = 1f;
        dialogGroup.interactable = true;
        dialogGroup.blocksRaycasts = true;
    }

    /// <summary>
    /// Hides the dialog immediately using CanvasGroup.
    /// </summary>
    private void HideImmediate()
    {
        if (dialogGroup == null) return;

        dialogGroup.alpha = 0f;
        dialogGroup.interactable = false;
        dialogGroup.blocksRaycasts = false;
    }
}