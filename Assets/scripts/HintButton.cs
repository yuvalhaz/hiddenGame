using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// כפתור הינט - תמיד גלוי ועובד
/// </summary>
public class HintButton : MonoBehaviour
{
    [Header("Target UI (CanvasGroup to show)")]
    [SerializeField] private CanvasGroup targetGroup; // גרור כאן את CanvasGroup של UI ההינט

    [Header("Optional")]
    public UnityEvent onPressed;

    private Button button;
    private CanvasGroup myCanvasGroup;
    private Image myImage;

    private void Awake()
    {
        Debug.Log("🔷 [HintButton] Awake");

        // ✅ מצא Button
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnClick);
            Debug.Log("✅ [HintButton] Button מחובר");
        }

        // ✅ מצא/צור CanvasGroup
        myCanvasGroup = GetComponent<CanvasGroup>();
        if (myCanvasGroup == null)
        {
            myCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // ✅ מצא Image
        myImage = GetComponent<Image>();

        // ✅ תקן שקיפות מיד
        FixTransparency();
    }

    private void Start()
    {
        FixTransparency();
    }

    private void LateUpdate()
    {
        // ✅ שמור על הכפתור גלוי בכל frame
        if (myCanvasGroup != null)
        {
            myCanvasGroup.alpha = 1f;
            myCanvasGroup.interactable = true;
            myCanvasGroup.blocksRaycasts = true;
        }
    }

    /// <summary>
    /// מתקן שקיפות של הכפתור
    /// </summary>
    private void FixTransparency()
    {
        // ✅ CanvasGroup
        if (myCanvasGroup != null)
        {
            myCanvasGroup.alpha = 1f;
            myCanvasGroup.interactable = true;
            myCanvasGroup.blocksRaycasts = true;
        }

        // ✅ Image color
        if (myImage != null)
        {
            Color c = myImage.color;
            c.a = 1f;
            myImage.color = c;
            myImage.raycastTarget = true; // ✅ חובה ללחיצות!
        }

        // ✅ Button colors
        if (button != null)
        {
            var colors = button.colors;

            Color normal = colors.normalColor;
            normal.a = 1f;
            colors.normalColor = normal;

            Color highlighted = colors.highlightedColor;
            highlighted.a = 1f;
            colors.highlightedColor = highlighted;

            Color pressed = colors.pressedColor;
            pressed.a = 1f;
            colors.pressedColor = pressed;

            button.colors = colors;
        }

        Debug.Log("✅ [HintButton] שקיפות תוקנה");
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
        Debug.Log("🎯 [HintButton] הכפתור נלחץ!");
        Debug.Log("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");

        // מראה את UI ההינט
        if (targetGroup != null)
        {
            Debug.Log($"✅ [HintButton] פותח את {targetGroup.name}");
            targetGroup.alpha = 1f;
            targetGroup.interactable = true;
            targetGroup.blocksRaycasts = true;
        }
        else
        {
            Debug.LogWarning("⚠️ [HintButton] targetGroup לא מחובר ב-Inspector!");
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
