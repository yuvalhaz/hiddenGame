using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI component that displays the hint counter.
/// Shows the count when hints > 0, hides when hints = 0.
/// If UnlimitedHints is active - shows infinity "∞"
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
            PlayerPrefs.SetInt(UNLIMITED_HINTS_KEY, 0);
            PlayerPrefs.Save();
            Debug.Log($"[HintCounterUI] DEBUG: Cleared hints/unlimited to 0");
        }
        // Debug: הוספת רמזים לבדיקה
        else if (debugApplyOnStart && debugAddHints > 0)
        {
            PlayerPrefs.SetInt(HINTS_COUNT_KEY, debugAddHints);
            PlayerPrefs.SetInt(UNLIMITED_HINTS_KEY, 0);
            PlayerPrefs.Save();
            Debug.Log($"[HintCounterUI] DEBUG: Set hints to {debugAddHints}");
        }

        UpdateDisplay();
    }

    private void OnEnable()
    {
        // ✅ Subscribe to clean event-driven updates
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnHintsChanged += HandleHintsChanged;
        }

        UpdateDisplay();
    }

    private void OnDisable()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnHintsChanged -= HandleHintsChanged;
        }
    }

    private void HandleHintsChanged(int count, bool unlimited)
    {
        UpdateDisplay();
    }

    /// <summary>
    /// עדכון התצוגה - נקרא אוטומטית אחרי קניה/שימוש (Event)
    /// </summary>
    public void UpdateDisplay()
    {
        bool unlimited = HasUnlimitedHints();
        int hintCount = GetHintsCount();

        bool hasHints = unlimited || hintCount > 0;
        string displayText = unlimited ? "∞" : (hintCount > 0 ? hintCount.ToString() : "");

        Debug.Log($"[HintCounterUI] UpdateDisplay: count={hintCount}, unlimited={unlimited}, hasHints={hasHints}, text='{displayText}'");
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
