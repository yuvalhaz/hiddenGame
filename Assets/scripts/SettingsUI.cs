using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the settings menu UI including shop/IAP
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

    [Header("=== SHOP / IAP ===")]
    [SerializeField] private Button removeAdsButton;
    [SerializeField] private Text removeAdsPrice;
    [SerializeField] private GameObject removeAdsContainer; // Hide after purchase

    [SerializeField] private Button hints10Button;
    [SerializeField] private Text hints10Price;

    [SerializeField] private Button hints50Button;
    [SerializeField] private Text hints50Price;

    [SerializeField] private Button unlimitedHintsButton;
    [SerializeField] private Text unlimitedHintsPrice;
    [SerializeField] private GameObject unlimitedHintsContainer; // Hide after purchase

    [SerializeField] private Button restorePurchasesButton; // iOS only

    [Header("Hints Display (Optional)")]
    [SerializeField] private Text hintsCountText;

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

    [Header("UI Blocking")]
    [SerializeField] private HintDialog hintDialog;
    [Tooltip("Optional: HintDialog reference to prevent opening both panels together")]

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

        // Setup IAP buttons
        SetupIAPButtons();

        // Update music button state
        UpdateMusicButtonUI();

        // Subscribe to IAP events
        SubscribeToIAPEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromIAPEvents();
    }

    // ===== IAP SETUP =====

    private void SetupIAPButtons()
    {
        if (removeAdsButton != null)
            removeAdsButton.onClick.AddListener(OnRemoveAdsClicked);

        if (hints10Button != null)
            hints10Button.onClick.AddListener(OnHints10Clicked);

        if (hints50Button != null)
            hints50Button.onClick.AddListener(OnHints50Clicked);

        if (unlimitedHintsButton != null)
            unlimitedHintsButton.onClick.AddListener(OnUnlimitedHintsClicked);

        if (restorePurchasesButton != null)
        {
            restorePurchasesButton.onClick.AddListener(OnRestorePurchasesClicked);
            // Show only on iOS
            bool isApple = Application.platform == RuntimePlatform.IPhonePlayer ||
                           Application.platform == RuntimePlatform.OSXPlayer;
            restorePurchasesButton.gameObject.SetActive(isApple);
        }
    }

    private void SubscribeToIAPEvents()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSuccess += OnPurchaseSuccess;
            IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;
        }
    }

    private void UnsubscribeFromIAPEvents()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSuccess -= OnPurchaseSuccess;
            IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
        }
    }

    // ===== IAP BUTTON HANDLERS =====

    private void OnRemoveAdsClicked()
    {
        Debug.Log("[SettingsUI] Remove Ads clicked");
        if (IAPManager.Instance != null)
            IAPManager.Instance.BuyRemoveAds();
    }

    private void OnHints10Clicked()
    {
        Debug.Log("[SettingsUI] Hints 10 clicked");
        if (IAPManager.Instance != null)
            IAPManager.Instance.BuyHints10();
    }

    private void OnHints50Clicked()
    {
        Debug.Log("[SettingsUI] Hints 50 clicked");
        if (IAPManager.Instance != null)
            IAPManager.Instance.BuyHints50();
    }

    private void OnUnlimitedHintsClicked()
    {
        Debug.Log("[SettingsUI] Unlimited Hints clicked");
        if (IAPManager.Instance != null)
            IAPManager.Instance.BuyUnlimitedHints();
    }

    private void OnRestorePurchasesClicked()
    {
        Debug.Log("[SettingsUI] Restore Purchases clicked");
        if (IAPManager.Instance != null)
            IAPManager.Instance.RestorePurchases();
    }

    // ===== IAP EVENT HANDLERS =====

    private void OnPurchaseSuccess()
    {
        Debug.Log("[SettingsUI] Purchase successful!");
        UpdateShopUI();
    }

    private void OnPurchaseFailed(string error)
    {
        Debug.LogWarning($"[SettingsUI] Purchase failed: {error}");
    }

    // ===== SHOP UI UPDATES =====

    private void UpdateShopUI()
    {
        if (IAPManager.Instance == null) return;

        // Hide Remove Ads if already purchased
        if (removeAdsContainer != null)
            removeAdsContainer.SetActive(!IAPManager.Instance.AreAdsRemoved());
        if (removeAdsButton != null)
            removeAdsButton.interactable = !IAPManager.Instance.AreAdsRemoved();

        // Hide Unlimited Hints if already purchased
        if (unlimitedHintsContainer != null)
            unlimitedHintsContainer.SetActive(!IAPManager.Instance.HasUnlimitedHints());
        if (unlimitedHintsButton != null)
            unlimitedHintsButton.interactable = !IAPManager.Instance.HasUnlimitedHints();

        // Update hints count
        UpdateHintsCount();

        // Update prices
        UpdatePrices();
    }

    private void UpdatePrices()
    {
        if (IAPManager.Instance == null) return;

        if (removeAdsPrice != null)
            removeAdsPrice.text = IAPManager.Instance.GetProductPrice(IAPManager.REMOVE_ADS);

        if (hints10Price != null)
            hints10Price.text = IAPManager.Instance.GetProductPrice(IAPManager.HINTS_10);

        if (hints50Price != null)
            hints50Price.text = IAPManager.Instance.GetProductPrice(IAPManager.HINTS_50);

        if (unlimitedHintsPrice != null)
            unlimitedHintsPrice.text = IAPManager.Instance.GetProductPrice(IAPManager.HINTS_UNLIMITED);
    }

    private void UpdateHintsCount()
    {
        if (hintsCountText == null || IAPManager.Instance == null) return;

        if (IAPManager.Instance.HasUnlimitedHints())
            hintsCountText.text = "Unlimited";
        else
            hintsCountText.text = IAPManager.Instance.GetHintsCount().ToString();
    }

    /// <summary>
    /// Check if settings panel is currently open
    /// </summary>
    public bool IsPanelOpen()
    {
        return isPanelOpen;
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

        // Auto-find HintDialog if not assigned
        if (hintDialog == null)
        {
            hintDialog = FindObjectOfType<HintDialog>();
        }

        // Check if Hint Dialog is open
        if (hintDialog != null && hintDialog.IsOpen())
        {
            Debug.LogWarning("[SettingsUI] ❌ Cannot open - Hint Dialog is open!");
            return;
        }

        settingsPanel.SetActive(true);
        isPanelOpen = true;

        // Change color of the color-changing object
        ChangeRandomColor();

        // Update shop UI when opening
        UpdateShopUI();

        if (animatePanel && panelCanvasGroup != null)
        {
            StartCoroutine(FadePanel(0f, 1f));
        }

        Debug.Log("[SettingsUI] ✅ Settings opened");
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
