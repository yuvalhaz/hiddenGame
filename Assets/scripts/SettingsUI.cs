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

    [Header("Color Changing Objects (Optional)")]
    [SerializeField] private Image colorChangingImage1; // First object that changes color
    [SerializeField] private Image colorChangingImage2; // Second object that changes color (always different from first)
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
            OpenSettings(); // OpenSettings now handles UIManager check internally
        }
    }

    /// <summary>
    /// Open settings panel
    /// </summary>
    public void OpenSettings()
    {
        if (settingsPanel == null) return;

        // Check with UIManager if we can open
        if (!UIManager.Instance.RequestOpenSettings())
        {
            Debug.Log("[SettingsUI] Cannot open - another panel is active");
            return;
        }

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
                // Notify UIManager that settings closed
                UIManager.Instance.NotifySettingsClosed();
            }));
        }
        else
        {
            settingsPanel.SetActive(false);
            isPanelOpen = false;
            // Notify UIManager that settings closed
            UIManager.Instance.NotifySettingsClosed();
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
    /// Change the color of the color-changing objects to random colors from the list
    /// Makes sure both objects have different colors
    /// Preserves the alpha (transparency) of each object
    /// </summary>
    private void ChangeRandomColor()
    {
        if (availableColors == null || availableColors.Length < 2)
        {
            Debug.LogWarning("[SettingsUI] Need at least 2 colors for color changing!");
            return;
        }

        // Handle first object
        if (colorChangingImage1 != null)
        {
            int randomIndex1 = Random.Range(0, availableColors.Length);
            Color newColor1 = availableColors[randomIndex1];

            // Create new color with current alpha (don't modify the object's color directly)
            Color finalColor1 = new Color(newColor1.r, newColor1.g, newColor1.b, colorChangingImage1.color.a);
            colorChangingImage1.color = finalColor1;

            Debug.Log($"[SettingsUI] Object 1 color: RGB({finalColor1.r}, {finalColor1.g}, {finalColor1.b}) Alpha: {finalColor1.a}");

            // Handle second object - must be different from first
            if (colorChangingImage2 != null)
            {
                int randomIndex2;
                do
                {
                    randomIndex2 = Random.Range(0, availableColors.Length);
                }
                while (randomIndex2 == randomIndex1); // Keep trying until we get a different color

                Color newColor2 = availableColors[randomIndex2];

                // Create new color with current alpha
                Color finalColor2 = new Color(newColor2.r, newColor2.g, newColor2.b, colorChangingImage2.color.a);
                colorChangingImage2.color = finalColor2;

                Debug.Log($"[SettingsUI] Object 2 color: RGB({finalColor2.r}, {finalColor2.g}, {finalColor2.b}) Alpha: {finalColor2.a}");
            }
        }
        else if (colorChangingImage2 != null)
        {
            // If only second object exists
            int randomIndex = Random.Range(0, availableColors.Length);
            Color newColor = availableColors[randomIndex];

            // Create new color with current alpha
            Color finalColor = new Color(newColor.r, newColor.g, newColor.b, colorChangingImage2.color.a);
            colorChangingImage2.color = finalColor;

            Debug.Log($"[SettingsUI] Object 2 color: RGB({finalColor.r}, {finalColor.g}, {finalColor.b}) Alpha: {finalColor.a}");
        }
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
