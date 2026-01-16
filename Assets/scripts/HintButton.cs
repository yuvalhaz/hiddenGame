using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class HintButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;

    [Header("🎯 Hint System")]
    [SerializeField] private VisualHintSystem visualHintSystem;
    [Tooltip("גרור כאן את VisualHintSystem - נדרש לאנימציית הרמז")]

    [Header("Hint Dialog (for Rewarded Ads)")]
    [SerializeField] private HintDialog hintDialog;
    [Tooltip("Dialog that shows rewarded ad option. Leave empty for direct hints.")]

    [Header("🎓 Tutorial Mode")]
    [SerializeField] private bool isTutorialLevel = false;
    [Tooltip("אם מסומן - רמז יופעל ישירות ללא דיאלוג")]

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

        // Auto-find components if not assigned
        if (visualHintSystem == null)
        {
            visualHintSystem = FindObjectOfType<VisualHintSystem>();
        }

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
        // Called after watching the ad - trigger the hint animation
        Debug.Log("💰 [HintButton] Hint granted after watching ad - triggering hint!");
        TriggerHintAnimation();
    }

    private void OnClick()
    {
        // ✅ Notify TutorialSlideManager that hint was clicked
        if (TutorialSlideManager.Instance != null)
        {
            TutorialSlideManager.Instance.OnHintButtonClicked();
        }

        // Tutorial mode - trigger hint directly without dialog
        if (isTutorialLevel)
        {
            Debug.Log("🎓 [HintButton] Tutorial mode - triggering hint directly!");
            TriggerHintAnimation();
        }
        // Normal level - open dialog for rewarded ad
        else if (hintDialog != null)
        {
            Debug.Log("💬 [HintButton] Normal mode - opening hint dialog");
            hintDialog.Open();
        }
        // Fallback - no dialog available, trigger directly
        else
        {
            Debug.LogWarning("⚠️ [HintButton] No HintDialog found - triggering hint directly");
            TriggerHintAnimation();
        }

        onPressed?.Invoke();
    }

    private void TriggerHintAnimation()
    {
        if (visualHintSystem != null)
        {
            visualHintSystem.TriggerHint();
        }
        else
        {
            Debug.LogError("❌ [HintButton] VisualHintSystem not assigned!");
        }
    }
}
