using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the settings menu UI including shop/IAP
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button settingsButton; // The gear icon button
    [SerializeField] private Button closeButton; // Close settings button
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

    [Header("Level Selection")]
    [SerializeField] private Button levelSelectionButton;

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

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseSettings);
        }

        if (musicToggleButton != null)
        {
            musicToggleButton.onClick.AddListener(ToggleMusicSetting);
        }

        if (levelSelectionButton != null)
        {
            levelSelectionButton.onClick.AddListener(GoToLevelSelection);
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

    private bool isSubscribedToIAP = false;




    private string CleanStoreTitle(string title)
    {
        if (string.IsNullOrEmpty(title)) return "";

        // מסיר את "(unreviewed)"
        title = title.Replace("(unreviewed)", "").Trim();

        // אם יש סוגריים שמכילים com. (למשל com.yuval.mygame) – נחתוך אותם
        int open = title.IndexOf('(');
        int close = title.IndexOf(')', open + 1);
        if (open >= 0 && close > open)
        {
            string inside = title.Substring(open + 1, close - open - 1);
            if (inside.Contains("com."))
            {
                title = (title.Substring(0, open) + title.Substring(close + 1)).Trim();
            }
        }

        // ניקוי רווחים כפולים
        while (title.Contains("  "))
            title = title.Replace("  ", " ");

        return title.Trim();
    }









    private void SubscribeToIAPEvents()
    {
        if (isSubscribedToIAP) return;

        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSuccess += OnPurchaseSuccess;
            IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;
            isSubscribedToIAP = true;
            Debug.Log("[SettingsUI] Subscribed to IAP events");
        }
        else
        {
            // Retry subscription after delay
            StartCoroutine(RetrySubscription());
        }
    }

    private System.Collections.IEnumerator RetrySubscription()
    {
        yield return new WaitForSeconds(0.5f);

        if (!isSubscribedToIAP && IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSuccess += OnPurchaseSuccess;
            IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;
            isSubscribedToIAP = true;
            Debug.Log("[SettingsUI] Subscribed to IAP events (delayed)");
        }
    }

    private void UnsubscribeFromIAPEvents()
    {
        if (isSubscribedToIAP && IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSuccess -= OnPurchaseSuccess;
            IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
            isSubscribedToIAP = false;
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

        // Update prices
        UpdatePrices();
    }

    private void UpdatePrices()
    {
        if (IAPManager.Instance == null) return;

        if (removeAdsPrice != null)
            removeAdsPrice.text = FormatProductText(IAPManager.REMOVE_ADS);

        if (hints10Price != null)
            hints10Price.text = FormatProductText(IAPManager.HINTS_10);

        if (hints50Price != null)
            hints50Price.text = FormatProductText(IAPManager.HINTS_50);

        if (unlimitedHintsPrice != null)
            unlimitedHintsPrice.text = FormatProductText(IAPManager.HINTS_UNLIMITED);
    }

    private string FormatProductText(string productId)
    {
        string title = CleanStoreTitle(IAPManager.Instance.GetProductTitle(productId));
        string price = IAPManager.Instance.GetProductPrice(productId);

        // אם אין כותרת נקייה – רק מחיר
        if (!string.IsNullOrEmpty(title))
            return $"{title} - {price}";
        else
            return price;
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

    public void GoToLevelSelection()
    {
        SceneManager.LoadScene("LevelSelection");
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
