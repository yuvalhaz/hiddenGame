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
        HideImmediate();
        onClosed?.Invoke();
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

        HideImmediate();

        // Subscribe to reward event
        RewardedAdsManager.Instance.OnRewardGranted -= HandleReward;
        RewardedAdsManager.Instance.OnRewardGranted += HandleReward;

        // Show rewarded ad
        RewardedAdsManager.Instance.ShowRewarded();
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
