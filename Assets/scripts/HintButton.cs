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
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(OnClick);

        // ✅ מצא/צור CanvasGroup
        myCanvasGroup = GetComponent<CanvasGroup>();
        if (myCanvasGroup == null)
        {
            myCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            if (debugMode)
                Debug.Log("[HintButton] יצר CanvasGroup חדש");
        }

        // ✅ מצא Image
        myImage = GetComponent<Image>();

        // ✅ כפה גלוי מיד
        ForceVisible();
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
            myCanvasGroup.ignoreParentGroups = true; // ✅ המפתח!
        }

        // ✅ 2. Image - תמיד alpha=1
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
        // מראה את UI ההינט מיד דרך CanvasGroup
        if (targetGroup != null)
        {
            targetGroup.alpha = 1f;
            targetGroup.interactable = true;
            targetGroup.blocksRaycasts = true;
        }

        onPressed?.Invoke();
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
