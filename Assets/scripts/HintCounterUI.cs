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
    [SerializeField] private bool debugClearHints = false;

    // PlayerPrefs key (same as IAPManager)
    private const string HINTS_COUNT_KEY = "HintsCount";
    private const string UNLIMITED_HINTS_KEY = "UnlimitedHints";

    private void Start()
    {
        // Auto-find text components if not assigned
        if (countText == null)
        {
            countText = GetComponent<Text>();
            if (countText == null)
                countText = GetComponentInChildren<Text>();
        }

        if (countTextTMP == null)
        {
            countTextTMP = GetComponent<TextMeshProUGUI>();
            if (countTextTMP == null)
                countTextTMP = GetComponentInChildren<TextMeshProUGUI>();
        }

        // Debug: איפוס רמזים
        if (debugClearHints)
        {
            PlayerPrefs.SetInt(HINTS_COUNT_KEY, 0);
            PlayerPrefs.Save();
            Debug.Log($"[HintCounterUI] DEBUG: Cleared hints to 0");
        }
        // Debug: הוספת רמזים לבדיקה
        else if (debugApplyOnStart && debugAddHints > 0)
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

        Debug.Log($"[HintCounterUI] UpdateDisplay: count={hintCount}, hasHints={hasHints}, text='{displayText}'");
        Debug.Log($"[HintCounterUI] countText={countText}, countTextTMP={countTextTMP}");

        if (countText != null)
        {
            countText.text = displayText;
            countText.enabled = hasHints; // רק מכבה את הטקסט, לא את ה-GameObject
            Debug.Log($"[HintCounterUI] Set Text to '{displayText}', enabled={hasHints}");
        }

        if (countTextTMP != null)
        {
            countTextTMP.text = displayText;
            countTextTMP.enabled = hasHints; // רק מכבה את הטקסט, לא את ה-GameObject
            Debug.Log($"[HintCounterUI] Set TMP to '{displayText}', enabled={hasHints}");
        }

        // אם צריך להסתיר את כל ה-GameObject
        if (hideGameObjectWhenZero && !hasHints)
        {
            gameObject.SetActive(false);
        }
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
