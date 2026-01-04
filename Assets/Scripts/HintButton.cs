using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class HintButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;

    [Header("🎓 Tutorial Mode")]
    [SerializeField] private bool isTutorialLevel = false;
    [Tooltip("אם מסומן - רמז יופעל ישירות ללא דיאלוג")]
    [SerializeField] private VisualHintSystem visualHintSystem;
    [Tooltip("גרור כאן את VisualHintSystem אם זה Tutorial")]

    [Header("Target UI (CanvasGroup to show)")]
    [SerializeField] private CanvasGroup targetGroup; // גרור כאן את CanvasGroup של UI ההינט

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
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        // ✅ Tutorial Mode - הפעל רמז ישירות!
        if (isTutorialLevel)
        {
            if (visualHintSystem != null)
            {
                Debug.Log("🎓 [HintButton] Tutorial mode - triggering hint directly!");
                visualHintSystem.TriggerHint();
            }
            else
            {
                Debug.LogWarning("⚠️ [HintButton] Tutorial mode enabled but VisualHintSystem not assigned!");
            }
        }
        // ✅ Normal Mode - הצג דיאלוג רמז
        else
        {
            if (targetGroup != null)
            {
                Debug.Log("💬 [HintButton] Normal mode - showing hint dialog");
                targetGroup.alpha = 1f;
                targetGroup.interactable = true;
                targetGroup.blocksRaycasts = true;
            }
        }

        onPressed?.Invoke();
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
