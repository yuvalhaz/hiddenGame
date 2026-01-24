using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using System;

/// <summary>
/// Manages In-App Purchases
/// Handles: Remove Ads, Hint Packages, Unlimited Hints
/// </summary>
public class IAPManager : MonoBehaviour, IStoreListener
{
    private static IAPManager instance;
    public static IAPManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("IAPManager");
                instance = go.AddComponent<IAPManager>();
            }
            return instance;
        }
    }

    // Product IDs
    public const string REMOVE_ADS = "remove_ads";
    public const string HINTS_10 = "hints_10";
    public const string HINTS_50 = "hints_50";
    public const string HINTS_UNLIMITED = "hints_unlimited";

    // PlayerPrefs keys
    private const string ADS_REMOVED_KEY = "AdsRemoved";
    private const string HINTS_COUNT_KEY = "HintsCount";
    private const string UNLIMITED_HINTS_KEY = "UnlimitedHints";

    private IStoreController storeController;
    private IExtensionProvider storeExtensionProvider;

    // Events
    public event Action OnPurchaseSuccess;

    // חשוב: לא לקרוא לזה OnPurchaseFailed כי Unity IAP משתמשת בשם הזה למתודה של IStoreListener
    public event Action<string> OnPurchaseFailedEvent;

    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        InitializePurchasing();
    }

    /// <summary>
    /// Initialize Unity IAP
    /// </summary>
    private void InitializePurchasing()
    {
        if (IsInitialized())
        {
            return;
        }

        var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

        // Add products
        builder.AddProduct(REMOVE_ADS, ProductType.NonConsumable);
        builder.AddProduct(HINTS_10, ProductType.Consumable);
        builder.AddProduct(HINTS_50, ProductType.Consumable);
        builder.AddProduct(HINTS_UNLIMITED, ProductType.NonConsumable);

        UnityPurchasing.Initialize(this, builder);
        Debug.Log("[IAPManager] Initializing IAP...");
    }

    private bool IsInitialized()
    {
        return storeController != null && storeExtensionProvider != null;
    }

    // IStoreListener Implementation
    public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
    {
        storeController = controller;
        storeExtensionProvider = extensions;
        Debug.Log("[IAPManager] IAP initialized successfully");
    }

    public void OnInitializeFailed(InitializationFailureReason error)
    {
        Debug.LogError($"[IAPManager] IAP initialization failed: {error}");
        OnPurchaseFailedEvent?.Invoke($"IAP init failed: {error}");
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"[IAPManager] IAP initialization failed: {error} - {message}");
        OnPurchaseFailedEvent?.Invoke($"IAP init failed: {error} - {message}");
    }

    public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
    {
        string productId = args.purchasedProduct.definition.id;
        Debug.Log($"[IAPManager] Processing purchase: {productId}");

        switch (productId)
        {
            case REMOVE_ADS:
                GrantRemoveAds();
                break;

            case HINTS_10:
                AddHints(10);
                break;

            case HINTS_50:
                AddHints(50);
                break;

            case HINTS_UNLIMITED:
                GrantUnlimitedHints();
                break;

            default:
                Debug.LogWarning($"[IAPManager] Unknown product: {productId}");
                break;
        }

        OnPurchaseSuccess?.Invoke();
        return PurchaseProcessingResult.Complete;
    }

    // Unity IAP callback
    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($"[IAPManager] Purchase failed: {product.definition.id} - {failureReason}");
        OnPurchaseFailedEvent?.Invoke($"Purchase failed: {failureReason}");
    }

    // Purchase Methods
    public void BuyRemoveAds()
    {
        BuyProductID(REMOVE_ADS);
    }

    public void BuyHints10()
    {
        BuyProductID(HINTS_10);
    }

    public void BuyHints50()
    {
        BuyProductID(HINTS_50);
    }

    public void BuyUnlimitedHints()
    {
        BuyProductID(HINTS_UNLIMITED);
    }

    private void BuyProductID(string productId)
    {
        if (!IsInitialized())
        {
            Debug.LogWarning("[IAPManager] IAP not initialized");
            OnPurchaseFailedEvent?.Invoke("Store not initialized");
            return;
        }

        Product product = storeController.products.WithID(productId);

        if (product != null && product.availableToPurchase)
        {
            Debug.Log($"[IAPManager] Purchasing: {productId}");
            storeController.InitiatePurchase(product);
        }
        else
        {
            Debug.LogWarning($"[IAPManager] Product not available: {productId}");
            OnPurchaseFailedEvent?.Invoke("Product not available");
        }
    }

    // Grant Methods
    private void GrantRemoveAds()
    {
        PlayerPrefs.SetInt(ADS_REMOVED_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[IAPManager] ✅ Ads removed!");
    }

    private void AddHints(int count)
    {
        int current = GetHintsCount();
        int newCount = current + count;
        PlayerPrefs.SetInt(HINTS_COUNT_KEY, newCount);
        PlayerPrefs.Save();
        Debug.Log($"[IAPManager] ✅ Added {count} hints (total: {newCount})");
    }

    private void GrantUnlimitedHints()
    {
        PlayerPrefs.SetInt(UNLIMITED_HINTS_KEY, 1);
        PlayerPrefs.Save();
        Debug.Log("[IAPManager] ✅ Unlimited hints granted!");
    }

    // Query Methods
    public bool AreAdsRemoved()
    {
        return PlayerPrefs.GetInt(ADS_REMOVED_KEY, 0) == 1;
    }

    public int GetHintsCount()
    {
        if (HasUnlimitedHints())
        {
            return 999999; // Unlimited
        }
        return PlayerPrefs.GetInt(HINTS_COUNT_KEY, 0);
    }

    public bool HasUnlimitedHints()
    {
        return PlayerPrefs.GetInt(UNLIMITED_HINTS_KEY, 0) == 1;
    }

    public bool CanUseHint()
    {
        return HasUnlimitedHints() || GetHintsCount() > 0;
    }

    public void UseHint()
    {
        if (HasUnlimitedHints())
        {
            Debug.Log("[IAPManager] Using hint (unlimited)");
            return;
        }

        int current = GetHintsCount();
        if (current > 0)
        {
            PlayerPrefs.SetInt(HINTS_COUNT_KEY, current - 1);
            PlayerPrefs.Save();
            Debug.Log($"[IAPManager] Used hint (remaining: {current - 1})");
        }
        else
        {
            Debug.LogWarning("[IAPManager] No hints available!");
        }
    }

    // Restore Purchases (for iOS mainly)
    public void RestorePurchases()
    {
        if (!IsInitialized())
        {
            Debug.LogWarning("[IAPManager] Cannot restore - not initialized");
            return;
        }

        if (Application.platform == RuntimePlatform.IPhonePlayer ||
            Application.platform == RuntimePlatform.OSXPlayer)
        {
            Debug.Log("[IAPManager] Restoring purchases...");
            var apple = storeExtensionProvider.GetExtension<IAppleExtensions>();
            apple.RestoreTransactions((success, message) =>
            {
                Debug.Log($"[IAPManager] Restore result: success={success}, message={message}");
            });

        }
        else
        {
            Debug.Log("[IAPManager] Restore not supported on this platform");
        }
    }

    // Get product price for display
    public string GetProductPrice(string productId)
    {
        if (!IsInitialized())
        {
            return "$?.??";
        }

        Product product = storeController.products.WithID(productId);
        if (product != null && product.availableToPurchase)
        {
            return product.metadata.localizedPriceString;
        }

        return "$?.??";
    }
}
