using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class HintButton : MonoBehaviour
{
    [Header("Button")]
    [SerializeField] private Button button;

    [Header("Target UI (CanvasGroup to show)")]
    [SerializeField] private CanvasGroup targetGroup; // גרור כאן את CanvasGroup של UI ההינט

    [Header("Optional")]
    public UnityEvent onPressed;

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);

        // ✅ תקן את השקיפות של הכפתור עצמו
        FixButtonTransparency();
    }

    private void Start()
    {
        // ✅ ודא שהכפתור נשאר גלוי
        FixButtonTransparency();
    }

    /// <summary>
    /// מוודא שהכפתור עצמו לא שקוף
    /// </summary>
    private void FixButtonTransparency()
    {
        // בדיקה 1: CanvasGroup על הכפתור
        CanvasGroup buttonCanvasGroup = GetComponent<CanvasGroup>();
        if (buttonCanvasGroup != null)
        {
            if (buttonCanvasGroup.alpha < 1f)
            {
                if (debugMode)
                    Debug.Log($"[HintButton] תיקון CanvasGroup alpha: {buttonCanvasGroup.alpha} → 1");
                buttonCanvasGroup.alpha = 1f;
            }
            buttonCanvasGroup.interactable = true;
            buttonCanvasGroup.blocksRaycasts = true;
        }

        // בדיקה 2: Image component על הכפתור
        Image buttonImage = GetComponent<Image>();
        if (buttonImage != null)
        {
            Color c = buttonImage.color;
            if (c.a < 1f)
            {
                if (debugMode)
                    Debug.Log($"[HintButton] תיקון Image alpha: {c.a} → 1");
                c.a = 1f;
                buttonImage.color = c;
            }
        }

        if (debugMode)
            Debug.Log("[HintButton] ✅ כפתור הרמז אמור להיות גלוי לחלוטין");
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        // מראה את UI ההינט מיד דרך CanvasGroup
        if (targetGroup != null)
        {
            targetGroup.alpha = 1f;
            targetGroup.interactable = true;
            targetGroup.blocksRaycasts = true;
        }

        onPressed?.Invoke();

        // אם בעתיד תרצה שמכאן תתחיל גם מודעת Rewarded – תעדכן, כרגע השארתי מכובה.
        // RewardedAdsManager.Instance?.ShowRewarded();
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
