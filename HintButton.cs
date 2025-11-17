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

    [Header("Target UI")]
    [Tooltip("The hint dialog CanvasGroup to show when button is clicked")]
    [SerializeField] private CanvasGroup targetGroup;

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
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    /// <summary>
    /// Called when hint button is clicked.
    /// Shows the target hint dialog UI.
    /// </summary>
    private void OnClick()
    {
        // Show hint UI via CanvasGroup
        if (targetGroup != null)
        {
            targetGroup.alpha = 1f;
            targetGroup.interactable = true;
            targetGroup.blocksRaycasts = true;
        }

        #if UNITY_EDITOR
        Debug.Log("[HintButton] Hint button clicked - showing dialog");
        #endif

        // Trigger optional event
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