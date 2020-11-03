#if UNITY_ANDROID || UNITY_IPHONE
#define RECEIPT_VALIDATION
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_IAP
using UnityEngine.Purchasing;

#if RECEIPT_VALIDATION
using UnityEngine.Purchasing.Security;
#endif

#endif

namespace ZUnityIAP
{
    public class IAPManager
#if UNITY_IAP
        : IStoreListener
#endif
    {
        private static IAPManager instance;
        public static IAPManager Instance()
        {
            if (instance == null)
            {
                instance = new IAPManager();
            }

            return instance;
        }

#if UNITY_IAP
        private static IStoreController m_StoreController; // The Unity Purchasing system.
        private static IExtensionProvider m_StoreExtensionProvider; // The store-specific Purchasing subsystems.
        private IAppleExtensions m_AppleExtensions;
        private IGooglePlayStoreExtensions m_GooglePlayStoreExtensions;
        private ITransactionHistoryExtensions m_TransactionHistoryExtensions;
#endif

        private List<string> nonConsumableProducts = new List<string>();
        private List<string> consumableProducts = new List<string>();

        private Action<bool, string> callbackPay;
        private Action<bool> callbackInit;

        private string productId;

        private Dictionary<string, string> localPricesString = new Dictionary<string, string>();
        private Dictionary<string, decimal> localPrice = new Dictionary<string, decimal>();

        private bool isWaitForInit = false;
        private bool m_PurchaseInProgress;
        private bool m_IsGooglePlayStoreSelected;
        private bool m_IsAppleStoreSelected;
        private bool isTestIAP = false;

        #region Init
        public void Init(Action<bool> callback)
        {
            this.callbackInit = callback;
#if UNITY_IAP
            m_AppleExtensions = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
            m_TransactionHistoryExtensions = m_StoreExtensionProvider.GetExtension<ITransactionHistoryExtensions>();
            m_GooglePlayStoreExtensions = m_StoreExtensionProvider.GetExtension<IGooglePlayStoreExtensions>();

            var module = StandardPurchasingModule.Instance();
            m_IsGooglePlayStoreSelected = Application.platform == RuntimePlatform.Android && module.appStore == AppStore.GooglePlay;
            m_IsAppleStoreSelected = Application.platform == RuntimePlatform.IPhonePlayer && module.appStore == AppStore.AppleAppStore;
#endif
            // If we have already connected to Purchasing ...
            if (IsInitialized())
            {
                InvokeCallbackInitIAP(true);
                return;
            }

#if UNITY_IAP
            // If we haven't set up the Unity Purchasing reference
            if (m_StoreController == null && !isWaitForInit)
            {
                isWaitForInit = true;
                // Begin to configure our connection to Purchasing
                try
                {
                    // Create a builder, first passing in a suite of Unity provided stores.
                    var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

                    //Consumable
                    foreach (var item in consumableProducts)
                    {
                        var productId = item.ToString();
                        builder.AddProduct(productId, ProductType.Consumable, new IDs() { { productId, GooglePlay.Name }, { productId, AppleAppStore.Name }, });
                    }

                    foreach (var item in nonConsumableProducts)
                    {
                        var productId = item.ToString();
                        builder.AddProduct(productId, ProductType.NonConsumable, new IDs() { { productId, GooglePlay.Name }, { productId, AppleAppStore.Name }, });
                    }

                    // Kick off the remainder of the set-up with an asynchrounous call, passing the configuration 
                    // and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed.
                    Debug.Log("[IAP] Initialize IAP");
                    UnityPurchasing.Initialize(this, builder);
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }
            else
            {
                InvokeCallbackInitIAP(false);
            }
#else
            InvokeCallbackInitIAP(false);
#endif
        }



        public void SetIsTestIAP(bool value)
        {
            isTestIAP = value;
        }

        public bool IsInitialized()
        {
#if UNITY_IAP
            // Only say we are initialized if both the Purchasing references are set.
            return m_StoreController != null && m_StoreExtensionProvider != null;
#else
            return false;
#endif
        }
        #endregion

        #region Payment actions
        public void Buy(string productID, Action<bool, string> callback)
        {
            try
            {
                if (m_PurchaseInProgress == true)
                {
                    Debug.Log("Please wait, purchase in progress");
                    return;
                }

#if UNITY_IAP
                if (m_StoreController == null)
                {
                    Debug.LogError("Purchasing is not initialized");
                    return;
                }

                if (m_StoreController.products.WithID(productID) == null)
                {
                    Debug.LogError("No product has id " + productID);
                    return;
                }
#endif

                this.callbackPay = callback;
                this.productId = productID;
                m_PurchaseInProgress = true;
                Debug.Log("[IAP] Purchasing product: " + productId);

                if (isTestIAP)
                {
                    FakeProcessPurchase(productID);
                }
                else
                {
                    if (IsInitialized())
                    {
#if UNITY_IAP
                        Product product = m_StoreController.products.WithID(productId);

                        if (product != null && product.availableToPurchase)
                        {
                            Debug.Log(string.Format("[IAP] Purchasing product asychronously: '{0}'", product.definition.id));

                            m_StoreController.InitiatePurchase(product);
                        }
                        else
                        {
                            Debug.Log("[IAP] BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");

                            if (this.callbackPay != null)
                            {
                                this.callbackPay(false, null);
                            }
                            this.callbackPay = null;
                        }
#else
                        Debug.Log("[IAP] BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");

                        if (this.callbackPay != null)
                        {
                            this.callbackPay(false, null);
                        }
                        this.callbackPay = null;
#endif
                    }
                    else
                    {
                        // Show warrning iap not initialized
                        Debug.Log("[IAP] BuyProductID FAIL. Not initialized.");
                        if (this.callbackPay != null)
                        {
                            this.callbackPay(false, null);
                        }
                        this.callbackPay = null;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        public void RestorePurchases()
        {
#if UNITY_IAP
            try
            {
                // If Purchasing has not yet been set up ...
                if (!IsInitialized())
                {
                    // ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
                    Debug.Log("[IAP] RestorePurchases FAIL. Not initialized.");
                    return;
                }

                if (m_IsGooglePlayStoreSelected)
                {
                    m_GooglePlayStoreExtensions.RestoreTransactions(OnTransactionsRestored);
                }
                else if (m_IsAppleStoreSelected)
                {
                    m_AppleExtensions.RestoreTransactions(OnTransactionsRestored);
                }
                else
                {
                    Debug.LogError("[IAP] RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
#endif
        }

        private void OnTransactionsRestored(bool success)
        {
            Debug.Log("Transactions restored." + success);
        }

        private void InvokeCallbackInitIAP(bool result)
        {
            this.callbackInit?.Invoke(result);
            this.callbackInit = null;
        }

        #endregion

#if UNITY_IAP
        #region Payment event listener
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {

            // Purchasing has succeeded initializing. Collect our Purchasing references.
            Debug.Log("[IAP] OnInitialized: PASS");

            // Overall Purchasing system, configured with products for this application.
            m_StoreController = controller;
            // Store specific subsystem, for accessing device-specific store features.
            m_StoreExtensionProvider = extensions;

            foreach (var item in controller.products.all)
            {
                try
                {
                    if (item.availableToPurchase)
                    {
                        //Log.Info (item.metadata.localizedPriceString);
                        localPricesString[item.definition.id] = item.metadata.localizedPriceString;
                        localPrice[item.definition.id] = item.metadata.localizedPrice;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }

            InvokeCallbackInitIAP(true);
            LogProductDefinitions();
            isWaitForInit = false;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
            Debug.Log("[IAP] Billing failed to initialize!");
            switch (error)
            {
                case InitializationFailureReason.AppNotKnown:
                    Debug.LogError("[Buy] Is your App correctly uploaded on the relevant publisher console?");
                    break;

                case InitializationFailureReason.PurchasingUnavailable:
                    // Ask the user if billing is disabled in device settings.
                    Debug.Log("[Buy] Billing disabled!");
                    break;

                case InitializationFailureReason.NoProductsAvailable:
                    // Developer configuration error; check product metadata.
                    Debug.Log("[Buy] No products available for purchase!");
                    break;
            }
            InvokeCallbackInitIAP(false);
            isWaitForInit = false;
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            // Presume valid for platforms with no R.V.
            bool isSandboxPurchase = false;
            bool validPurchase = true;
            m_PurchaseInProgress = false;

#if (UNITY_ANDROID || UNITY_IOS) && RECEIPT_VALIDATION
            try
            {
                // Prepare the validator with the secrets we prepared in the Editor obfuscation window.
                var validator = new CrossPlatformValidator(GooglePlayTangle.Data(),
                AppleTangle.Data(), Application.identifier);

                // On Google Play, result has a single product ID.
                // On Apple stores, receipts contain multiple products.
                var result = validator.Validate(args.purchasedProduct.receipt);
                // For informational purposes, we list the receipt(s)
                Debug.Log("[IAP] Receipt is valid. Contents:");
                foreach (IPurchaseReceipt productReceipt in result)
                {
                    Debug.Log(productReceipt.productID);
                    Debug.Log(productReceipt.purchaseDate.ToShortDateString());
                    Debug.Log(productReceipt.transactionID);

                    if (m_IsGooglePlayStoreSelected)
                    {
                        GooglePlayReceipt google = productReceipt as GooglePlayReceipt;
                        if (null != google)
                        {
                            // This is Google's Order ID.
                            // Note that it is null when testing in the sandbox
                            // because Google's sandbox does not provide Order IDs.
                            Debug.Log(google.transactionID);
                            Debug.Log(google.purchaseState);
                            Debug.Log(google.purchaseToken);
                            isSandboxPurchase = false;
                        }
                        else
                        {
                            isSandboxPurchase = true;
                        }
                    }

                    if (m_IsAppleStoreSelected)
                    {
                        AppleInAppPurchaseReceipt apple = productReceipt as AppleInAppPurchaseReceipt;
                        if (null != apple)
                        {
                            Debug.Log(apple.originalTransactionIdentifier);
                            Debug.Log(apple.subscriptionExpirationDate);
                            Debug.Log(apple.cancellationDate);
                            Debug.Log(apple.quantity);
                            isSandboxPurchase = false;
                        }
                        else
                        {
                            isSandboxPurchase = true;
                        }
                    }
                }
            }
            catch (IAPSecurityException e)
            {
                Debug.LogError("[IAP] Invalid receipt, not unlocking content " + e);
                validPurchase = false;
            }
#endif

            try
            {
                if (validPurchase)
                {
                    // Unlock the appropriate content here.
                    productId = args.purchasedProduct.definition.id;
                    var title = args.purchasedProduct.metadata.localizedTitle;
                    var transactionId = args.purchasedProduct.transactionID;

                    if (callbackPay != null)
                    {
                        callbackPay(true, productId);
                    }
                    else
                    {
                        Debug.Log("[IAP] Restore purchase id: " + productId);
                    }

                    Debug.Log(string.Format("[IAP] ProcessPurchase: PASS. Product: '{0}'", productId));
                    callbackPay = null;
                }
                else
                {
                    Debug.Log("[IAP] IAP cheat detected");
                    if (callbackPay != null)
                    {
                        callbackPay(false, null);
                    }
                    callbackPay = null;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return PurchaseProcessingResult.Pending;
            }
            // Return a flag indicating whether this product has completely been received, or if the application needs 
            // to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
            // saving purchased products to the cloud, and when that save is delayed. 
            return PurchaseProcessingResult.Complete;
        }



        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            try
            {
                // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
                // this reason with the user to guide their troubleshooting actions.
                Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));

                // Detailed debugging information
                Debug.Log("Store specific error code: " + m_TransactionHistoryExtensions.GetLastStoreSpecificPurchaseErrorCode());
                if (m_TransactionHistoryExtensions.GetLastPurchaseFailureDescription() != null)
                {
                    Debug.Log("Purchase failure description message: " +
                              m_TransactionHistoryExtensions.GetLastPurchaseFailureDescription().message);
                }

                if (callbackPay != null)
                {
                    callbackPay(false, null);
                }
                callbackPay = null;
                m_PurchaseInProgress = false;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }
        #endregion
#endif
        internal void FakeProcessPurchase(string productId)
        {
            // Unlock the appropriate content here.            

            if (callbackPay != null)
            {
                callbackPay(true, productId);
            }
            else
            {
                Debug.Log("[IAP] Restore purchase id: " + productId);
            }

            Debug.Log(string.Format("[IAP] ProcessPurchase: PASS. Product: '{0}'", productId));
            callbackPay = null;
            m_PurchaseInProgress = false;
            // Return a flag indicating whether this product has completely been received, or if the application needs 
            // to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
            // saving purchased products to the cloud, and when that save is delayed. 
            //return PurchaseProcessingResult.Complete;
        }

#if UNITY_IAP
        private void LogProductDefinitions()
        {
            var products = m_StoreController.products.all;
            foreach (var product in products)
            {
#if UNITY_5_6_OR_NEWER
                Debug.Log(string.Format("id: {0}\nstore-specific id: {1}\ntype: {2}\nenabled: {3}\n", product.definition.id, product.definition.storeSpecificId, product.definition.type.ToString(), product.definition.enabled ? "enabled" : "disabled"));
#else
            Debug.Log(string.Format("id: {0}\nstore-specific id: {1}\ntype: {2}\n", product.definition.id,
                product.definition.storeSpecificId, product.definition.type.ToString()));
#endif
            }
        }
#endif
    }
}