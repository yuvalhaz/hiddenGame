using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Button that opens the hint dialog when clicked.
/// Shows a CanvasGroup-based UI element (typically HintDialog).
/// </summary>
public class HintButton : MonoBehaviour
{
    [Header("Button Reference")]
    [SerializeField] private Button button;

    [Header("Hint System")]
    [Tooltip("Direct hint activation - shows hint immediately without dialog")]
    [SerializeField] private VisualHintSystem visualHintSystem;

    [Header("Target UI (Legacy - use VisualHintSystem instead)")]
    [Tooltip("The hint dialog CanvasGroup to show when button is clicked")]
    [SerializeField] private CanvasGroup targetGroup;

    [Header("Mode")]
    [Tooltip("If true, shows hint directly. If false, opens dialog.")]
    [SerializeField] private bool showHintDirectly = true;

    [Header("Events")]
    [Tooltip("Optional event triggered when button is pressed")]
    public UnityEvent onPressed;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClick);

        // Auto-find VisualHintSystem if not assigned
        if (showHintDirectly && visualHintSystem == null)
        {
            visualHintSystem = FindObjectOfType<VisualHintSystem>();
            if (visualHintSystem != null)
            {
                Debug.Log("[HintButton] VisualHintSystem found automatically!");
            }
            else
            {
                Debug.LogWarning("[HintButton] VisualHintSystem not found in scene. Hint button won't work!");
            }
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    /// <summary>
    /// Called when hint button is clicked.
    /// Shows the target hint dialog UI or triggers hint directly.
    /// </summary>
    private void OnClick()
    {
        if (showHintDirectly)
        {
            // NEW MODE: Show hint directly without dialog
            if (visualHintSystem != null)
            {
                #if UNITY_EDITOR
                Debug.Log("[HintButton] Triggering hint directly");
                #endif

                visualHintSystem.TriggerHint();
            }
            else
            {
                Debug.LogWarning("[HintButton] VisualHintSystem not assigned! Please assign in Inspector.");
            }
        }
        else
        {
            // LEGACY MODE: Show dialog
            if (targetGroup != null)
            {
                targetGroup.alpha = 1f;
                targetGroup.interactable = true;
                targetGroup.blocksRaycasts = true;
            }

            #if UNITY_EDITOR
            Debug.Log("[HintButton] Hint button clicked - showing dialog");
            #endif
        }

        // Notify TutorialSlideManager automatically (for stage 4)
        if (TutorialSlideManager.Instance != null)
        {
            TutorialSlideManager.Instance.OnHintButtonClicked();
            Debug.Log("[HintButton] Notified TutorialSlideManager");
        }

        // Trigger optional event (for tutorial tracking, etc.)
        onPressed?.Invoke();
    }

    /// <summary>
    /// Hides the hint dialog immediately.
    /// Can be called externally to close the dialog.
    /// </summary>
    public void HideImmediate()
    {
        if (targetGroup == null) return;

        targetGroup.alpha = 0f;
        targetGroup.interactable = false;
        targetGroup.blocksRaycasts = false;
    }
}