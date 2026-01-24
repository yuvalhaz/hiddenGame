using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the settings menu UI
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton; // The gear icon button
    [SerializeField] private Button musicToggleButton;
    [SerializeField] private Image musicButtonIcon; // Optional - icon that shows music state

    [Header("Music Icons (Optional)")]
    [SerializeField] private Sprite musicOnIcon;
    [SerializeField] private Sprite musicOffIcon;

    [Header("Color Changing Object (Optional)")]
    [SerializeField] private Image colorChangingImage; // The object that changes color
    [SerializeField] private Color[] availableColors = new Color[]
    {
        Color.red,
        Color.blue,
        Color.green,
        Color.yellow,
        new Color(1f, 0.5f, 0f) // Orange
    };

    [Header("Animation (Optional)")]
    [SerializeField] private bool animatePanel = true;
    [SerializeField] private float animationDuration = 0.3f;

    private bool isPanelOpen = false;
    private CanvasGroup panelCanvasGroup;

    void Start()
    {
        // Setup panel
        if (settingsPanel != null)
        {
            // Add canvas group for fade animation if needed
            panelCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null && animatePanel)
            {
                panelCanvasGroup = settingsPanel.AddComponent<CanvasGroup>();
            }

            // Start with panel closed
            settingsPanel.SetActive(false);
            isPanelOpen = false;
        }

        // Setup buttons
        if (settingsButton != null)
        {
            settingsButton.onClick.AddListener(ToggleSettingsPanel);
        }

        if (musicToggleButton != null)
        {
            musicToggleButton.onClick.AddListener(ToggleMusicSetting);
        }

        // Update music button state
        UpdateMusicButtonUI();
    }

    /// <summary>
    /// Toggle settings panel open/close
    /// </summary>
    public void ToggleSettingsPanel()
    {
        if (isPanelOpen)
        {
            CloseSettings();
        }
        else
        {
            OpenSettings();
        }
    }

    /// <summary>
    /// Open settings panel
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel == null) return;

        settingsPanel.SetActive(true);
        isPanelOpen = true;

        // Change color of the color-changing object
        ChangeRandomColor();

        if (animatePanel && panelCanvasGroup != null)
        {
            StartCoroutine(FadePanel(0f, 1f));
        }

        Debug.Log("[SettingsUI] Settings opened");
    }

    /// <summary>
    /// Close settings panel
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel == null) return;

        if (animatePanel && panelCanvasGroup != null)
        {
            StartCoroutine(FadePanel(1f, 0f, () => {
                settingsPanel.SetActive(false);
                isPanelOpen = false;
            }));
        }
        else
        {
            settingsPanel.SetActive(false);
            isPanelOpen = false;
        }

        Debug.Log("[SettingsUI] Settings closed");
    }

    /// <summary>
    /// Toggle music on/off
    /// </summary>
    public void ToggleMusicSetting()
    {
        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.ToggleMusic();
            UpdateMusicButtonUI();
            Debug.Log($"[SettingsUI] Music toggled: {MusicManager.Instance.IsMusicEnabled()}");
        }
    }

    /// <summary>
    /// Update the music button icon based on current state
    /// </summary>
    private void UpdateMusicButtonUI()
    {
        if (musicButtonIcon == null || MusicManager.Instance == null) return;

        bool isMusicOn = MusicManager.Instance.IsMusicEnabled();

        if (isMusicOn && musicOnIcon != null)
        {
            musicButtonIcon.sprite = musicOnIcon;
        }
        else if (!isMusicOn && musicOffIcon != null)
        {
            musicButtonIcon.sprite = musicOffIcon;
        }
    }

    /// <summary>
    /// Change the color of the color-changing object to a random color from the list
    /// </summary>
    private void ChangeRandomColor()
    {
        if (colorChangingImage == null || availableColors == null || availableColors.Length == 0)
        {
            return;
        }

        // Pick a random color from the available colors
        int randomIndex = Random.Range(0, availableColors.Length);
        Color selectedColor = availableColors[randomIndex];

        colorChangingImage.color = selectedColor;

        Debug.Log($"[SettingsUI] Changed color to: {selectedColor}");
    }

    /// <summary>
    /// Fade panel animation
    /// </summary>
    private System.Collections.IEnumerator FadePanel(float startAlpha, float endAlpha, System.Action onComplete = null)
    {
        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        panelCanvasGroup.alpha = endAlpha;
        onComplete?.Invoke();
    }
}
