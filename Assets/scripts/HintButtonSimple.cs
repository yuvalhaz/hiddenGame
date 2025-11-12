using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// כפתור הינט - עובד בלי Button component, רק עם Image
/// </summary>
public class HintButtonSimple : MonoBehaviour, IPointerClickHandler
{
    [Header("Target Dialog")]
    [SerializeField] private HintDialog hintDialog;
    [Tooltip("גרור לכאן את HintDialog מה-Hierarchy")]

    private CanvasGroup myCanvasGroup;
    private Image myImage;

    private void Awake()
    {
        Debug.Log("██████████████████████████████████████████");
        Debug.Log("HintButtonSimple - Awake!");
        Debug.Log("██████████████████████████████████████████");

        // ✅ תקן שקיפות
        myCanvasGroup = GetComponent<CanvasGroup>();
        if (myCanvasGroup == null)
        {
            myCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log("✅ יצר CanvasGroup חדש");
        }

        myImage = GetComponent<Image>();
        if (myImage != null)
        {
            Debug.Log("✅ Image נמצא");
        }
        else
        {
            Debug.LogError("❌ לא נמצא Image component!");
        }

        FixVisibility();
    }

    private void Start()
    {
        Debug.Log("HintButtonSimple - Start!");
        FixVisibility();
    }

    private void LateUpdate()
    {
        // שמור על הכפתור גלוי בכל frame
        if (myCanvasGroup != null)
        {
            myCanvasGroup.alpha = 1f;
            myCanvasGroup.interactable = true;
            myCanvasGroup.blocksRaycasts = true;
        }
    }

    private void FixVisibility()
    {
        // CanvasGroup
        if (myCanvasGroup != null)
        {
            myCanvasGroup.alpha = 1f;
            myCanvasGroup.interactable = true;
            myCanvasGroup.blocksRaycasts = true;
        }

        // Image - חובה ללחיצות!
        if (myImage != null)
        {
            Color c = myImage.color;
            c.a = 1f;
            myImage.color = c;
            myImage.raycastTarget = true; // ✅✅✅ זה המפתח ללחיצות בלי Button!
            Debug.Log("✅ Image.raycastTarget = true");
        }

        Debug.Log("✅ שקיפות תוקנה!");
    }

    private void Update()
    {
        // בדוק אם לוחצים על העכבר
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("🖱️ לחיצת עכבר זוהתה!");
        }
    }

    // ✅ זה נקרא כש לוחצים על ה-Image!
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("╔════════════════════════════════════════╗");
        Debug.Log("║   הכפתור נלחץ! זה עובד!                ║");
        Debug.Log("╚════════════════════════════════════════╝");

        // אם לא מחובר ידנית, נסה למצוא
        if (hintDialog == null)
        {
            Debug.Log("🔍 מחפש HintDialog בסצנה...");
            hintDialog = FindObjectOfType<HintDialog>(true); // true = include inactive objects
        }

        // פתח את HintDialog
        if (hintDialog != null)
        {
            Debug.Log("✅ מצאתי HintDialog - פותח אותו!");
            hintDialog.Open();
        }
        else
        {
            Debug.LogError("❌ לא מצאתי HintDialog בסצנה!");
            Debug.LogError("💡 פתרון: גרור את HintDialog GameObject לשדה 'Hint Dialog' ב-Inspector");
        }
    }
}
