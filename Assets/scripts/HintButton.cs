using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// כפתור רמז שתמיד נשאר גלוי (alpha=1) ללא קשר להורים
/// </summary>
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

    private CanvasGroup myCanvasGroup;
    private Image myImage;

    private void Reset()
    {
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🔷 [HintButton] Awake מתחיל");
        Debug.Log("═══════════════════════════════════════");

        // ✅ בדוק EventSystem
        UnityEngine.EventSystems.EventSystem eventSystem = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("❌ [HintButton] אין EventSystem בסצנה! הכפתור לא יעבוד!");
        }
        else
        {
            Debug.Log("✅ [HintButton] EventSystem נמצא");
        }

        // ✅ מצא Button
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(OnClick);
            Debug.Log($"✅ [HintButton] Button מחובר ול-onClick נוסף מאזין");
        }
        else
        {
            Debug.LogError("❌ [HintButton] אין Button component על האובייקט הזה!");
        }

        // ✅ מצא/צור CanvasGroup
        myCanvasGroup = GetComponent<CanvasGroup>();
        if (myCanvasGroup == null)
        {
            myCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log("[HintButton] ✅ יצר CanvasGroup חדש");
        }
        else
        {
            Debug.Log("[HintButton] ✅ מצא CanvasGroup קיים");
        }

        // ✅ מצא Image
        myImage = GetComponent<Image>();
        if (myImage != null)
        {
            Debug.Log($"✅ [HintButton] Image נמצא: {myImage.sprite?.name ?? "NULL sprite"}");
        }
        else
        {
            Debug.LogWarning("⚠️ [HintButton] אין Image component");
        }

        // ✅ בדוק targetGroup
        if (targetGroup != null)
        {
            Debug.Log($"✅ [HintButton] targetGroup מחובר: {targetGroup.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ [HintButton] targetGroup לא מחובר ב-Inspector!");
        }

        // ✅ כפה גלוי מיד
        ForceVisible();

        Debug.Log("═══════════════════════════════════════\n");
    }

    private void Start()
    {
        ForceVisible();
    }

    private void LateUpdate()
    {
        // ✅ כפה גלוי בכל frame - אחרי כל העדכונים האחרים
        ForceVisible();
    }

    /// <summary>
    /// כופה על הכפתור להיות גלוי לחלוטין
    /// </summary>
    private void ForceVisible()
    {
        // ✅ 1. CanvasGroup - תמיד alpha=1 ומתעלם מהורים
        if (myCanvasGroup != null)
        {
            myCanvasGroup.alpha = 1f;
            myCanvasGroup.interactable = true;
            myCanvasGroup.blocksRaycasts = true;
            myCanvasGroup.ignoreParentGroups = true; // שומר על הגלוי גם אם ההורה שקוף
        }

        // ✅ 2. Image - תמיד alpha=1 ו-raycastTarget=true
        if (myImage != null)
        {
            Color c = myImage.color;
            if (c.a != 1f)
            {
                c.a = 1f;
                myImage.color = c;

                if (debugMode)
                    Debug.Log($"[HintButton] תיקון Image alpha → 1");
            }

            // ✅ וודא שה-Image יכול לקבל לחיצות!
            if (!myImage.raycastTarget)
            {
                myImage.raycastTarget = true;
                if (debugMode)
                    Debug.Log("[HintButton] הפעלתי raycastTarget על Image");
            }
        }

        // ✅ 3. Button colors - תמיד alpha=1
        if (button != null)
        {
            var colors = button.colors;
            bool needsUpdate = false;

            if (colors.normalColor.a != 1f)
            {
                Color normal = colors.normalColor;
                normal.a = 1f;
                colors.normalColor = normal;
                needsUpdate = true;
            }

            if (colors.highlightedColor.a != 1f)
            {
                Color highlighted = colors.highlightedColor;
                highlighted.a = 1f;
                colors.highlightedColor = highlighted;
                needsUpdate = true;
            }

            if (colors.pressedColor.a != 1f)
            {
                Color pressed = colors.pressedColor;
                pressed.a = 1f;
                colors.pressedColor = pressed;
                needsUpdate = true;
            }

            if (needsUpdate)
            {
                button.colors = colors;

                if (debugMode)
                    Debug.Log("[HintButton] תיקון Button colors → alpha=1");
            }
        }
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🎯 [HintButton] OnClick נקרא! הכפתור נלחץ!");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // מראה את UI ההינט מיד דרך CanvasGroup
        if (targetGroup != null)
        {
            Debug.Log($"[HintButton] מציג את targetGroup: {targetGroup.name}");
            targetGroup.alpha = 1f;
            targetGroup.interactable = true;
            targetGroup.blocksRaycasts = true;
        }
        else
        {
            Debug.LogError("[HintButton] ❌ targetGroup הוא NULL! חבר אותו ב-Inspector!");
        }

        onPressed?.Invoke();

        Debug.Log("[HintButton] ✅ OnClick הסתיים");
    }

    // ניתן לקרוא מבחוץ כדי להסתיר את ה-dialog
    public void HideImmediate()
    {
        if (targetGroup == null) return;
        targetGroup.alpha = 0f;
        targetGroup.interactable = false;
        targetGroup.blocksRaycasts = false;
    }
}
