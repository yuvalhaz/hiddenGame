using UnityEngine;
using System;

#if UNITY_PURCHASING
using UnityEngine.Purchasing;
#endif

/// <summary>
/// Manages In-App Purchases
/// Handles: Remove Ads, Hint Packages, Unlimited Hints
/// Works with or without Unity IAP package installed
/// </summary>
public class IAPManager : MonoBehaviour
#if UNITY_PURCHASING
    , IStoreListener
#endif
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

#if UNITY_PURCHASING
    private IStoreController storeController;
    private IExtensionProvider storeExtensionProvider;
#endif

    // Events
    public event Action OnPurchaseSuccess;
    public event Action<string> OnPurchaseFailed;

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

#if UNITY_PURCHASING
        InitializePurchasing();
#else
        Debug.LogWarning("[IAPManager] Unity IAP not installed - IAP features disabled");
#endif
    }

#if UNITY_PURCHASING
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
    }

    public void OnInitializeFailed(InitializationFailureReason error, string message)
    {
        Debug.LogError($"[IAPManager] IAP initialization failed: {error} - {message}");
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

    public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
    {
        Debug.LogWarning($"[IAPManager] Purchase failed: {product.definition.id} - {failureReason}");
        OnPurchaseFailed?.Invoke($"Purchase failed: {failureReason}");
    }
#endif

    // Purchase Methods
    public void BuyRemoveAds()
    {
#if UNITY_PURCHASING
        BuyProductID(REMOVE_ADS);
#else
        Debug.LogWarning("[IAPManager] Unity IAP not installed");
        OnPurchaseFailed?.Invoke("IAP not available");
#endif
    }

    public void BuyHints10()
    {
#if UNITY_PURCHASING
        BuyProductID(HINTS_10);
#else
        Debug.LogWarning("[IAPManager] Unity IAP not installed");
        OnPurchaseFailed?.Invoke("IAP not available");
#endif
    }

    public void BuyHints50()
    {
#if UNITY_PURCHASING
        BuyProductID(HINTS_50);
#else
        Debug.LogWarning("[IAPManager] Unity IAP not installed");
        OnPurchaseFailed?.Invoke("IAP not available");
#endif
    }

    public void BuyUnlimitedHints()
    {
#if UNITY_PURCHASING
        BuyProductID(HINTS_UNLIMITED);
#else
        Debug.LogWarning("[IAPManager] Unity IAP not installed");
        OnPurchaseFailed?.Invoke("IAP not available");
#endif
    }

#if UNITY_PURCHASING
    private void BuyProductID(string productId)
    {
        if (!IsInitialized())
        {
            Debug.LogWarning("[IAPManager] IAP not initialized");
            OnPurchaseFailed?.Invoke("Store not initialized");
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
            OnPurchaseFailed?.Invoke("Product not available");
        }
    }
#endif

    // Grant Methods (work without IAP - can be called for testing)
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

    // Query Methods (work without IAP)
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

#if UNITY_PURCHASING
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
            apple.RestoreTransactions((result) =>
            {
                Debug.Log($"[IAPManager] Restore result: {result}");
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
#else
    public void RestorePurchases()
    {
        Debug.LogWarning("[IAPManager] Unity IAP not installed");
    }

    public string GetProductPrice(string productId)
    {
        return "$?.??";
    }
#endif

    // ===== TESTING METHODS (Editor only) =====
#if UNITY_EDITOR
    [ContextMenu("Test: Grant Remove Ads")]
    public void TestGrantRemoveAds()
    {
        GrantRemoveAds();
    }

    [ContextMenu("Test: Add 10 Hints")]
    public void TestAdd10Hints()
    {
        AddHints(10);
    }

    [ContextMenu("Test: Grant Unlimited Hints")]
    public void TestGrantUnlimitedHints()
    {
        GrantUnlimitedHints();
    }

    [ContextMenu("Test: Clear All Purchases")]
    public void TestClearAllPurchases()
    {
        PlayerPrefs.DeleteKey(ADS_REMOVED_KEY);
        PlayerPrefs.DeleteKey(HINTS_COUNT_KEY);
        PlayerPrefs.DeleteKey(UNLIMITED_HINTS_KEY);
        PlayerPrefs.Save();
        Debug.Log("[IAPManager] All purchases cleared");
    }
#endif
}
