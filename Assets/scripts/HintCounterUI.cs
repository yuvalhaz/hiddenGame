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

    private float updateTimer = 0f;

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
    }

    /// <summary>
    /// Manually update the display (call this after purchasing/using hints)
    /// </summary>
    public void UpdateDisplay()
    {
        if (IAPManager.Instance == null)
        {
            SetText("רמזים: 0");
            return;
        }

        // Check if player has unlimited hints
        if (IAPManager.Instance.HasUnlimitedHints())
        {
            SetText(unlimitedText);
        }
        else
        {
            // Display current hint count
            int hintCount = IAPManager.Instance.GetHintsCount();
            string displayText = string.Format(normalFormat, hintCount);
            SetText(displayText);
        }
    }

    /// <summary>
    /// Set text on whichever UI component is assigned
    /// </summary>
    private void SetText(string text)
    {
        if (hintsText != null)
        {
            hintsText.text = text;
        }

        if (hintsTMPText != null)
        {
            hintsTMPText.text = text;
        }
    }
}
