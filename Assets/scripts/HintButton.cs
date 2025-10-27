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
    /// מוודא שהכפתור והכל ההורים שלו לא שקופים
    /// </summary>
    private void FixButtonTransparency()
    {
        if (debugMode)
            Debug.Log("[HintButton] 🔍 בודק שקיפות של הכפתור וההורים...");

        // ✅ תיקון 1: כל ה-CanvasGroups בהיררכיה (כולל הורים)
        Transform current = transform;
        int level = 0;

        while (current != null)
        {
            CanvasGroup cg = current.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                if (cg.alpha < 1f)
                {
                    if (debugMode)
                        Debug.Log($"[HintButton] תיקון CanvasGroup ברמה {level} ({current.name}): {cg.alpha} → 1");
                    cg.alpha = 1f;
                }
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            // עבור לאובייקט הבא בהיררכיה
            current = current.parent;
            level++;

            // הגבלה: לא ללכת יותר מ-10 רמות למעלה
            if (level > 10)
                break;
        }

        // ✅ תיקון 2: Image component על הכפתור עצמו
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

        // ✅ תיקון 3: בדוק אם יש Button transition שמוריד את ה-alpha
        if (button != null)
        {
            // אם Button מוגדר ל-Color transition עם alpha נמוך, תקן את זה
            var colors = button.colors;
            if (colors.normalColor.a < 1f)
            {
                if (debugMode)
                    Debug.Log($"[HintButton] תיקון Button normal color alpha: {colors.normalColor.a} → 1");

                Color normal = colors.normalColor;
                normal.a = 1f;
                colors.normalColor = normal;

                button.colors = colors;
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
