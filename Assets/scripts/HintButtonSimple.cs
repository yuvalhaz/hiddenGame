using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// גרסה פשוטה של כפתור הינט - רק כדי לבדוק שלחיצות עובדות
/// </summary>
public class HintButtonSimple : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("██████████████████████████████████████████");
        Debug.Log("HintButtonSimple - Awake!");
        Debug.Log("██████████████████████████████████████████");

        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(OnButtonClick);
            Debug.Log("Button נמצא ומאזין נוסף!");
        }
        else
        {
            Debug.LogError("לא נמצא Button component!");
        }
    }

    private void Start()
    {
        Debug.Log("HintButtonSimple - Start!");
    }

    private void Update()
    {
        // בדוק אם לוחצים על העכבר
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("לחיצת עכבר זוהתה!");
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
            Debug.Log("מצאתי HintDialog - פותח אותו!");
            dialog.Open();
        }
        else
        {
            Debug.LogError("לא מצאתי HintDialog בסצנה!");
        }
    }
}
