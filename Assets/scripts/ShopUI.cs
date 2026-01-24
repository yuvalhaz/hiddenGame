using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shop UI Manager - connects store products to the app
/// Handles: Remove Ads, Hint Packages, Unlimited Hints purchases
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("Shop Panel")]
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button openShopButton;
    [SerializeField] private Button closeShopButton;

    [Header("Remove Ads")]
    [SerializeField] private Button removeAdsButton;
    [SerializeField] private TextMeshProUGUI removeAdsPrice;
    [SerializeField] private GameObject removeAdsContainer; // Hide after purchase

    [Header("Hints 10 Package")]
    [SerializeField] private Button hints10Button;
    [SerializeField] private TextMeshProUGUI hints10Price;

    [Header("Hints 50 Package")]
    [SerializeField] private Button hints50Button;
    [SerializeField] private TextMeshProUGUI hints50Price;

    [Header("Unlimited Hints")]
    [SerializeField] private Button unlimitedHintsButton;
    [SerializeField] private TextMeshProUGUI unlimitedHintsPrice;
    [SerializeField] private GameObject unlimitedHintsContainer; // Hide after purchase

    [Header("Restore Purchases (iOS)")]
    [SerializeField] private Button restorePurchasesButton;

    [Header("Hints Counter Display")]
    [SerializeField] private TextMeshProUGUI hintsCountText;

    [Header("Animation")]
    [SerializeField] private bool animatePanel = true;
    [SerializeField] private float animationDuration = 0.3f;

    private CanvasGroup panelCanvasGroup;
    private bool isOpen = false;

    void Start()
    {
        SetupPanel();
        SetupButtons();
        SubscribeToEvents();
        UpdateUI();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    /// <summary>
    /// Setup the shop panel
    /// </summary>
    private void SetupPanel()
    {
        if (shopPanel != null)
        {
            panelCanvasGroup = shopPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null && animatePanel)
            {
                panelCanvasGroup = shopPanel.AddComponent<CanvasGroup>();
            }
            shopPanel.SetActive(false);
            isOpen = false;
        }
    }

    /// <summary>
    /// Setup button click listeners
    /// </summary>
    private void SetupButtons()
    {
        // Shop open/close
        if (openShopButton != null)
            openShopButton.onClick.AddListener(OpenShop);

        if (closeShopButton != null)
            closeShopButton.onClick.AddListener(CloseShop);

        // Purchase buttons
        if (removeAdsButton != null)
            removeAdsButton.onClick.AddListener(OnRemoveAdsClicked);

        if (hints10Button != null)
            hints10Button.onClick.AddListener(OnHints10Clicked);

        if (hints50Button != null)
            hints50Button.onClick.AddListener(OnHints50Clicked);

        if (unlimitedHintsButton != null)
            unlimitedHintsButton.onClick.AddListener(OnUnlimitedHintsClicked);

        if (restorePurchasesButton != null)
            restorePurchasesButton.onClick.AddListener(OnRestorePurchasesClicked);

        // Show restore button only on iOS
        if (restorePurchasesButton != null)
        {
            bool isApple = Application.platform == RuntimePlatform.IPhonePlayer ||
                           Application.platform == RuntimePlatform.OSXPlayer;
            restorePurchasesButton.gameObject.SetActive(isApple);
        }
    }

    /// <summary>
    /// Subscribe to IAP events
    /// </summary>
    private void SubscribeToEvents()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSuccess += OnPurchaseSuccess;
            IAPManager.Instance.OnPurchaseFailedEvent += OnPurchaseFailed;
        }
    }

    /// <summary>
    /// Unsubscribe from IAP events
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.OnPurchaseSuccess -= OnPurchaseSuccess;
            IAPManager.Instance.OnPurchaseFailedEvent -= OnPurchaseFailed;
        }
    }

    /// <summary>
    /// Open the shop panel
    /// </summary>
    public void OpenShop()
    {
        if (shopPanel == null) return;

        shopPanel.SetActive(true);
        isOpen = true;
        UpdateUI();
        UpdatePrices();

        if (animatePanel && panelCanvasGroup != null)
        {
            StartCoroutine(FadePanel(0f, 1f));
        }

        Debug.Log("[ShopUI] Shop opened");
    }

    /// <summary>
    /// Close the shop panel
    /// </summary>
    public void CloseShop()
    {
        if (shopPanel == null) return;

        if (animatePanel && panelCanvasGroup != null)
        {
            StartCoroutine(FadePanel(1f, 0f, () => {
                shopPanel.SetActive(false);
                isOpen = false;
            }));
        }
        else
        {
            shopPanel.SetActive(false);
            isOpen = false;
        }

        Debug.Log("[ShopUI] Shop closed");
    }

    /// <summary>
    /// Check if shop is open
    /// </summary>
    public bool IsOpen()
    {
        return isOpen;
    }

    // ===== Purchase Button Handlers =====

    private void OnRemoveAdsClicked()
    {
        Debug.Log("[ShopUI] Remove Ads clicked");
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.BuyRemoveAds();
        }
    }

    private void OnHints10Clicked()
    {
        Debug.Log("[ShopUI] Hints 10 clicked");
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.BuyHints10();
        }
    }

    private void OnHints50Clicked()
    {
        Debug.Log("[ShopUI] Hints 50 clicked");
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.BuyHints50();
        }
    }

    private void OnUnlimitedHintsClicked()
    {
        Debug.Log("[ShopUI] Unlimited Hints clicked");
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.BuyUnlimitedHints();
        }
    }

    private void OnRestorePurchasesClicked()
    {
        Debug.Log("[ShopUI] Restore Purchases clicked");
        if (IAPManager.Instance != null)
        {
            IAPManager.Instance.RestorePurchases();
        }
    }

    // ===== Event Handlers =====

    private void OnPurchaseSuccess()
    {
        Debug.Log("[ShopUI] Purchase successful!");
        UpdateUI();
    }

    private void OnPurchaseFailed(string error)
    {
        Debug.LogWarning($"[ShopUI] Purchase failed: {error}");
        // You can show an error dialog here
    }

    // ===== UI Updates =====

    /// <summary>
    /// Update UI based on current purchase state
    /// </summary>
    public void UpdateUI()
    {
        if (IAPManager.Instance == null) return;

        // Hide Remove Ads if already purchased
        if (removeAdsContainer != null)
        {
            removeAdsContainer.SetActive(!IAPManager.Instance.AreAdsRemoved());
        }
        if (removeAdsButton != null)
        {
            removeAdsButton.interactable = !IAPManager.Instance.AreAdsRemoved();
        }

        // Hide Unlimited Hints if already purchased
        if (unlimitedHintsContainer != null)
        {
            unlimitedHintsContainer.SetActive(!IAPManager.Instance.HasUnlimitedHints());
        }
        if (unlimitedHintsButton != null)
        {
            unlimitedHintsButton.interactable = !IAPManager.Instance.HasUnlimitedHints();
        }

        // Update hints count display
        UpdateHintsCount();
    }

    /// <summary>
    /// Update product prices from store
    /// </summary>
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

    /// <summary>
    /// Update hints count display
    /// </summary>
    private void UpdateHintsCount()
    {
        if (hintsCountText == null || IAPManager.Instance == null) return;

        if (IAPManager.Instance.HasUnlimitedHints())
        {
            hintsCountText.text = "Unlimited";
        }
        else
        {
            int count = IAPManager.Instance.GetHintsCount();
            hintsCountText.text = count.ToString();
        }
    }

    /// <summary>
    /// Fade animation for panel
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
