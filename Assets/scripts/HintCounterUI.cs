using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the number of available hints to the player
/// Updates automatically when hints are purchased or used
/// </summary>
public class HintCounterUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text hintsText;
    [SerializeField] private TextMeshProUGUI hintsTMPText;
    [Tooltip("Use either Text or TextMeshPro - leave the other one empty")]

    [Header("Display Format")]
    [SerializeField] private string normalFormat = "רמזים: {0}";
    [SerializeField] private string unlimitedText = "רמזים: ∞";
    [Tooltip("{0} will be replaced with the hint count")]

    [Header("Auto Update")]
    [SerializeField] private bool autoUpdate = true;
    [SerializeField] private float updateInterval = 0.5f;
    [Tooltip("How often to check for hint count updates (in seconds)")]

    [Header("Debug")]
    [SerializeField] private bool debugMode = false;

    private float updateTimer = 0f;
    private string lastSetText = "";

    void Start()
    {
        UpdateDisplay();
    }

    void Update()
    {
        if (autoUpdate)
        {
            updateTimer += Time.deltaTime;
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                UpdateDisplay();
            }
        }

        // Debug: Check if text was changed externally
        if (debugMode)
        {
            string currentText = "";
            if (hintsText != null) currentText = hintsText.text;
            else if (hintsTMPText != null) currentText = hintsTMPText.text;

            if (currentText != lastSetText && !string.IsNullOrEmpty(lastSetText))
            {
                Debug.LogWarning($"[HintCounterUI] Text changed externally! Was: '{lastSetText}', Now: '{currentText}'");
            }
        }
    }

    /// <summary>
    /// Manually update the display (call this after purchasing/using hints)
    /// </summary>
    public void UpdateDisplay()
    {
        if (IAPManager.Instance == null)
        {
            if (debugMode) Debug.Log("[HintCounterUI] IAPManager.Instance is NULL");
            SetText("רמזים: 0");
            return;
        }

        // Check if player has unlimited hints
        if (IAPManager.Instance.HasUnlimitedHints())
        {
            if (debugMode) Debug.Log("[HintCounterUI] Has unlimited hints");
            SetText(unlimitedText);
        }
        else
        {
            // Display current hint count
            int hintCount = IAPManager.Instance.GetHintsCount();
            string displayText = string.Format(normalFormat, hintCount);
            if (debugMode) Debug.Log($"[HintCounterUI] Hint count: {hintCount}, Display: '{displayText}'");
            SetText(displayText);
        }
    }

    /// <summary>
    /// Set text on whichever UI component is assigned
    /// </summary>
    private void SetText(string text)
    {
        lastSetText = text;

        if (hintsText != null)
        {
            hintsText.text = text;
            if (debugMode) Debug.Log($"[HintCounterUI] Set hintsText to: '{text}'");
        }

        if (hintsTMPText != null)
        {
            hintsTMPText.text = text;
            if (debugMode) Debug.Log($"[HintCounterUI] Set hintsTMPText to: '{text}'");
        }

        if (hintsText == null && hintsTMPText == null)
        {
            Debug.LogError("[HintCounterUI] No text component assigned!");
        }
    }
}
