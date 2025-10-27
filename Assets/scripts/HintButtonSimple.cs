using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// כפתור הינט - גרסה עובדת ופשוטה
/// </summary>
public class HintButtonSimple : MonoBehaviour
{
    private CanvasGroup myCanvasGroup;
    private Image myImage;

    private void Awake()
    {
        Debug.Log("██████████████████████████████████████████");
        Debug.Log("HintButtonSimple - Awake!");
        Debug.Log("██████████████████████████████████████████");

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnButtonClick);
            Debug.Log("✅ Button נמצא ומאזין נוסף!");
        }
        else
        {
            Debug.LogError("❌ לא נמצא Button component!");
        }

        // ✅ תקן שקיפות
        myCanvasGroup = GetComponent<CanvasGroup>();
        if (myCanvasGroup == null)
        {
            myCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            Debug.Log("✅ יצר CanvasGroup חדש");
        }

        myImage = GetComponent<Image>();

        FixVisibility();
    }

    private void Start()
    {
        Debug.Log("HintButtonSimple - Start!");
        FixVisibility();
    }

    private void LateUpdate()
    {
        // תקן שקיפות בכל frame
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

        // Image
        if (myImage != null)
        {
            Color c = myImage.color;
            c.a = 1f;
            myImage.color = c;
            myImage.raycastTarget = true;
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

    private void OnButtonClick()
    {
        Debug.Log("╔════════════════════════════════════════╗");
        Debug.Log("║   הכפתור נלחץ! זה עובד!                ║");
        Debug.Log("╚════════════════════════════════════════╝");

        // מצא את HintDialog ופתח אותו
        HintDialog dialog = FindObjectOfType<HintDialog>();
        if (dialog != null)
        {
            Debug.Log("✅ מצאתי HintDialog - פותח אותו!");
            dialog.Open();
        }
        else
        {
            Debug.LogError("❌ לא מצאתי HintDialog בסצנה!");
        }
    }
}
