using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component that displays the hint counter.
/// Shows the count when hints > 0, hides when hints = 0.
/// </summary>
public class HintCounterUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text countText;
    [SerializeField] private TextMeshProUGUI countTextTMP;

    [Header("Settings")]
    [Tooltip("אם מסומן - יסתיר את כל ה-GameObject כשאין רמזים")]
    [SerializeField] private bool hideGameObjectWhenZero = false;

    [Header("Debug / Testing")]
    [Tooltip("לבדיקה בלבד - מספר רמזים להוספה")]
    [SerializeField] private int debugAddHints = 0;
    [SerializeField] private bool debugApplyOnStart = false;

    // PlayerPrefs key (same as IAPManager)
    private const string HINTS_COUNT_KEY = "HintsCount";
    private const string UNLIMITED_HINTS_KEY = "UnlimitedHints";

    private void Start()
    {
        // Debug: הוספת רמזים לבדיקה
        if (debugApplyOnStart && debugAddHints > 0)
        {
            PlayerPrefs.SetInt(HINTS_COUNT_KEY, debugAddHints);
            PlayerPrefs.Save();
            Debug.Log($"[HintCounterUI] DEBUG: Set hints to {debugAddHints}");
        }

        UpdateDisplay();
    }

    private void OnEnable()
    {
        UpdateDisplay();
    }

    /// <summary>
    /// עדכון התצוגה - קרא לזה אחרי שינוי במספר הרמזים
    /// </summary>
    public void UpdateDisplay()
    {
        int hintCount = GetHintsCount();
        bool hasHints = hintCount > 0;

        // עדכון הטקסט
        string displayText = hasHints ? hintCount.ToString() : "";

        if (countText != null)
        {
            countText.text = displayText;
            countText.gameObject.SetActive(hasHints);
        }

        if (countTextTMP != null)
        {
            countTextTMP.text = displayText;
            countTextTMP.gameObject.SetActive(hasHints);
        }

        // אם צריך להסתיר את כל ה-GameObject
        if (hideGameObjectWhenZero)
        {
            gameObject.SetActive(hasHints);
        }

        Debug.Log($"[HintCounterUI] Updated: count={hintCount}, visible={hasHints}");
    }

    /// <summary>
    /// קבלת מספר הרמזים מ-PlayerPrefs
    /// </summary>
    private int GetHintsCount()
    {
        // בדיקה אם יש רמזים ללא הגבלה
        if (PlayerPrefs.GetInt(UNLIMITED_HINTS_KEY, 0) == 1)
        {
            return -1; // סימון לרמזים ללא הגבלה - לא מציגים מספר
        }

        return PlayerPrefs.GetInt(HINTS_COUNT_KEY, 0);
    }

    /// <summary>
    /// בדיקה אם יש רמזים ללא הגבלה
    /// </summary>
    public bool HasUnlimitedHints()
    {
        return PlayerPrefs.GetInt(UNLIMITED_HINTS_KEY, 0) == 1;
    }
}
