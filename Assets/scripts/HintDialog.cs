using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class HintDialog : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private CanvasGroup dialogGroup;

    [Header("Events")]
    public UnityEvent onHintGranted;
    public UnityEvent onClosed;

    private void Awake()
    {
        if (dialogGroup == null) dialogGroup = GetComponent<CanvasGroup>();
        if (watchAdButton != null) watchAdButton.onClick.AddListener(OnWatchAd);
        if (closeButton != null)   closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        HideImmediate();
    }

    private void OnDestroy()
    {
        if (watchAdButton != null) watchAdButton.onClick.RemoveListener(OnWatchAd);
        if (closeButton != null)   closeButton.onClick.RemoveListener(Close);

        if (RewardedAdsManager.Instance != null)
            RewardedAdsManager.Instance.OnRewardGranted -= HandleReward;
    }

    public void Open()
    {
        ShowImmediate();
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        HideImmediate();
        onClosed?.Invoke();
    }

    private void OnWatchAd()
    {
        if (RewardedAdsManager.Instance == null)
        {
            Debug.LogWarning("RewardedAdsManager missing in scene.");
            return;
        }

        HideImmediate();
        RewardedAdsManager.Instance.OnRewardGranted -= HandleReward;
        RewardedAdsManager.Instance.OnRewardGranted += HandleReward;
        RewardedAdsManager.Instance.ShowRewarded();
    }

    private void HandleReward()
    {
        if (RewardedAdsManager.Instance != null)
            RewardedAdsManager.Instance.OnRewardGranted -= HandleReward;

        HideImmediate();
        onHintGranted?.Invoke();
        
        // Find and call SimpleDragFromBar.RunHintOnce()
        bool result = SimpleDragFromBar.RunHintOnce();
        Debug.Log($"Hint executed: {result}");
    }

    private void ShowImmediate()
    {
        if (dialogGroup == null) return;
        dialogGroup.alpha = 1f;
        dialogGroup.interactable = true;
        dialogGroup.blocksRaycasts = true;
    }

    private void HideImmediate()
    {
        if (dialogGroup == null) return;
        dialogGroup.alpha = 0f;
        dialogGroup.interactable = false;
        dialogGroup.blocksRaycasts = false;
    }
}