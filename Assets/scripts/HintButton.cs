using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class HintButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;

    [Header("Target UI (CanvasGroup to show)")]
    [SerializeField] private CanvasGroup targetGroup; // גרור כאן את CanvasGroup של UI ההינט

    [Header("Hint Dialog (for Rewarded Ads)")]
    [SerializeField] private HintDialog hintDialog;
    [Tooltip("Dialog that shows rewarded ad option. Leave empty for direct hints.")]

    [Header("Tutorial Mode")]
    [SerializeField] private bool isTutorialLevel = false;
    [Tooltip("Enable for tutorial/Level 1 - shows hint directly without ad")]

    [Header("Optional")]
    public UnityEvent onPressed;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);

        // Auto-find HintDialog if not assigned
        if (hintDialog == null && !isTutorialLevel)
        {
            hintDialog = FindObjectOfType<HintDialog>();
        }

        // Subscribe to hint granted event
        if (hintDialog != null)
        {
            hintDialog.onHintGranted.AddListener(OnHintGranted);
        }
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);

        // Unsubscribe from hint granted event
        if (hintDialog != null)
        {
            hintDialog.onHintGranted.RemoveListener(OnHintGranted);
        }
    }

    private void OnHintGranted()
    {
        // Show the hint after watching the ad
        ShowHintDirectly();
        Debug.Log("[HintButton] Hint granted after watching ad!");
    }

    private void OnClick()
    {
        // Tutorial mode or no dialog - show hint directly
        if (isTutorialLevel || hintDialog == null)
        {
            ShowHintDirectly();
        }
        else
        {
            // Normal level - open dialog for rewarded ad
            hintDialog.Open();
        }

        onPressed?.Invoke();
    }

    private void ShowHintDirectly()
    {
        // מראה את UI ההינט מיד דרך CanvasGroup
        if (targetGroup != null)
        {
            targetGroup.alpha = 1f;
            targetGroup.interactable = true;
            targetGroup.blocksRaycasts = true;
        }

        Debug.Log("[HintButton] Showing hint directly (Tutorial mode or no dialog)");
    }

    // ניתן לקרוא מבחוץ כדי להסתיר מיד
    public void HideImmediate()
    {
        if (targetGroup == null) return;
        targetGroup.alpha = 0f;
        targetGroup.interactable = false;
        targetGroup.blocksRaycasts = false;
    }
}
