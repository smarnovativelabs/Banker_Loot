using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using LitJson;
//	In the Project panel, select the IAPDemo folder, then click the Create button and
//	 make a new C# script called Purchaser and paste-replace the entire contents with the following code:
// Deriving the Purchaser class from IStoreListener enables it to receive messages from Unity Purchasing.
namespace IAPProject
{
    public class Purchaser : MonoBehaviour, IStoreListener
    {
        public static Purchaser purchaser;
        private static IStoreController m_StoreController;          // The Unity Purchasing system.
        private static IExtensionProvider m_StoreExtensionProvider; // The store-specific Purchasing subsystems.

        // Product identifiers for all products capable of being purchased: 
        // "convenience" general identifiers for use with Purchasing, and their store-specific identifier 
        // counterparts for use with and outside of Unity Purchasing. Define store-specific identifiers 
        // also on each platform's publisher dashboard (iTunes Connect, Google Play Developer Console, etc.)

        // General product identifiers for the consumable, non-consumable, and subscription products.
        // Use these handles in the code to reference which product to purchase. Also use these values 
        // when defining the Product Identifiers on the store. Except, for illustration purposes, the 
        // kProductIDSubscription - it has custom Apple and Google identifiers. We declare their store-
        // specific mapping to Unity Purchasing's AddProduct, below.

        //------------------------

        string removeAdsId;
        // Apple App Store-specific product identifier for the subscription product.
        private static string kProductNameAppleSubscription = "com.unity3d.subscription.new";

        // Google Play Store-specific product identifier subscription product.
        private static string kProductNameGooglePlaySubscription = "com.unity3d.subscription.original";

        ConfigurationBuilder iapBuilder;

        void Awake()
        {
            // If we haven't set up the Unity Purchasing reference
            if (purchaser == null)
            {
                purchaser = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void InitializAllProducts(List<InAppProduct> _products)
        {
            iapBuilder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            if (IsInitialized())
            {
                // ... we are done here.
                return;
            }
            for (int i = 0; i < _products.Count; i++)
            {
                if (_products[i].purchaseMethod == PurchaseType.InAppPurchase)
                {
                    string _id = _products[i].productID;
                    ProductType _type = _products[i].type;
                    iapBuilder.AddProduct(_id, _type);
                }
            }
            InitializeProducts();
        }

        public void InitializeProducts()
        {

            // If we have already connected to Purchasing ...
            if (IsInitialized())
            {
                // ... we are done here.
                return;
            }

            // Create a builder, first passing in a suite of Unity provided stores.
            //var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

            // Add a product to sell / restore by way of its identifier, associating the general identifier
            // with its store-specific identifiers.
            // And finish adding the subscription product. Notice this uses store-specific IDs, illustrating
            // if the Product ID was configured differently between Apple and Google stores. Also note that
            // one uses the general kProductIDSubscription handle inside the game - the store-specific IDs 
            // must only be referenced here. 
            //			builder.AddProduct(kProductIDSubscription, ProductType.Subscription, new IDs(){
            //				{ kProductNameAppleSubscription, AppleAppStore.Name },
            //				{ kProductNameGooglePlaySubscription, GooglePlay.Name },
            //			});

            // Kick off the remainder of the set-up with an asynchrounous call, passing the configuration 
            // and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed.

            UnityPurchasing.Initialize(this, iapBuilder);
        }
        private bool IsInitialized()
        {
            // Only say we are initialized if both the Purchasing references are set.
            return m_StoreController != null && m_StoreExtensionProvider != null;

        }

        public void OnPurchaseItem(string productID)
        {
            Debug.Log("99999");
            BuyProductID(productID);
        }

        void BuyProductID(string productId)
        {
            // If Purchasing has been initialized ...
            if (IsInitialized())
            {
                Debug.Log("00000");
                // ... look up the Product reference with the general product identifier and the Purchasing 
                // system's products collection.
                Product product = m_StoreController.products.WithID(productId);
                // If the look up found a product for this device's store and that product is ready to be sold ... 
                if (product != null && product.availableToPurchase)
                {
                    Debug.Log(string.Format("Purchasing product asychronously: '{0}'", product.definition.id));
                    // ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed 
                    // asynchronously.
                    m_StoreController.InitiatePurchase(product);

                }
                // Otherwise ...
                else
                {
                    // ... report the product look-up failure situation  
                    Debug.Log("BuyProductID: FAIL. Not purchasing product, either is not found or is not available for purchase");
                    StoreManager.Instance.OnPurchaseFailed("Failed To Purchase Item");
                }
            }
            // Otherwise ...
            else
            {
                // ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or 
                // retrying initiailization.
                StoreManager.Instance.OnPurchaseFailed("Failed To Purchase Item");

                Debug.Log("BuyProductID FAIL. Not initialized.");
            }
        }
        // Restore purchases previously made by this customer. Some platforms automatically restore purchases, like Google. 
        // Apple currently requires explicit purchase restoration for IAP, conditionally displaying a password prompt.
        public void RestorePurchases()
        {
            // If Purchasing has not yet been set up ...
            if (!IsInitialized())
            {
                // ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization.
                Debug.Log("RestorePurchases FAIL. Not initialized.");
                StoreManager.Instance.OnPurchaseFailed("Failed To Restore Item");
                return;
            }

            // If we are running on an Apple device ... 
            if (Application.platform == RuntimePlatform.IPhonePlayer ||
                Application.platform == RuntimePlatform.OSXPlayer)
            {
                // ... begin restoring purchases
                Debug.Log("RestorePurchases started ...");

                // Fetch the Apple store-specific subsystem.
                var apple = m_StoreExtensionProvider.GetExtension<IAppleExtensions>();
                // Begin the asynchronous process of restoring purchases. Expect a confirmation response in 
                // the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore.
                apple.RestoreTransactions(OnRestore);
            }
            // Otherwise ...
            else
            {
                // We are not running on an Apple device. No work is necessary to restore purchases.
                Debug.Log("RestorePurchases FAIL. Not supported on this platform. Current = " + Application.platform);
                StoreManager.Instance.OnPurchaseFailed("Failed To Restore Item");
            }
        }

        void OnRestore(bool _success, string _error)
        {
            if (!_success)
            {
                StoreManager.Instance.OnPurchaseFailed("Failed To Restore Purchases");
            }
            else
            {
                //  ShopManager.instance.loadingPanel.SetActive(false);
            }
        }
        //  
        // --- IStoreListener
        //

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            // Purchasing has succeeded initializing. Collect our Purchasing references.
            Debug.Log("OnInitialized: PASS");

            // Overall Purchasing system, configured with products for this application.
            m_StoreController = controller;
            // Store specific subsystem, for accessing device-specific store features.
            m_StoreExtensionProvider = extensions;
            StoreManager.Instance.OnInitializationComplete(controller);
        }


        public void OnInitializeFailed(InitializationFailureReason error)
        {
            // Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
            Debug.Log("OnInitializeFailed InitializationFailureReason:" + error);
            StoreManager.Instance.OnInitializationFailed();
        }


        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            // A consumable product has been purchased by this user.


            //if (String.Equals(args.purchasedProduct.definition.id, kProductIDConsumable, StringComparison.Ordinal))
            //{
            //	Debug.Log(string.Format("ProcessPurchase: PASS. Product: '{0}'", args.purchasedProduct.definition.id));
            //	// The consumable item has been successfully purchased, add 100 coins to the player's in-game score.
            //}
            //else 
            //{
            //	Debug.Log(string.Format("ProcessPurchase: FAIL. Unrecognized product: '{0}'", args.purchasedProduct.definition.id));
            //}

            // Return a flag indicating whether this product has completely been received, or if the application needs 
            // to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
            // saving purchased products to the cloud, and when that save is delayed.


         //   StoreManager.Instance.OnSuccessPurchase(args.purchasedProduct.definition.id);

            var price = args.purchasedProduct.metadata.localizedPrice;
            double lPrice = decimal.ToDouble(price);
            var currencyCode = args.purchasedProduct.metadata.isoCurrencyCode;

            var wrapper = JsonMapper.ToObject(args.purchasedProduct.receipt);  // https://gist.github.com/darktable/1411710

            if (wrapper != null)
            {

                var store = (string)wrapper["Store"]; // GooglePlay, AmazonAppStore, AppleAppStore, etc.
                var payload = (string)wrapper["Payload"]; // For Apple this will be the base64 encoded ASN.1 receipt. For Android, it is the raw JSON receipt.
                var productId = args.purchasedProduct.definition.id;

#if UNITY_ANDROID

                if (store.Equals("GooglePlay"))
                {
                    var googleDetails = JsonMapper.ToObject(payload);
                    var googleJson = (string)googleDetails["json"];
                    var googleSig = (string)googleDetails["signature"];
                    TenjinManager.instance.OnAndroidPurcahaseTransaction(productId, currencyCode, 1, lPrice, googleJson, googleSig);
                }
#elif UNITY_IPHONE

                var transactionId = args.purchasedProduct.transactionID;

                TenjinManager.instance.OnIOSPurcahseTransaction(productId, currencyCode, 1, lPrice , transactionId, payload);

#endif

            }
            // Check for subscription product IDs
            if (args.purchasedProduct.definition.id == kProductNameAppleSubscription || args.purchasedProduct.definition.id == kProductNameGooglePlaySubscription)
            {
                Debug.Log("Subscription restored: " + args.purchasedProduct.definition.id);
                // Handle subscription-specific logic, like activating premium content
            }

            if (StoreManager.Instance.OnSuccessPurchase(args.purchasedProduct.definition.id))
            {
                m_StoreController.ConfirmPendingPurchase(args.purchasedProduct);
                return PurchaseProcessingResult.Complete;
            }
            return PurchaseProcessingResult.Pending;
        }

        public void OnCompletePurchase(string _productID)
        {
            if (IsInitialized())
            {
                Product product = m_StoreController.products.WithID(_productID);
                if (product != null)
                {
                    m_StoreController.ConfirmPendingPurchase(product);
                    //print(_productID + " --------Purchase Completed");
                }
            }
        }
        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            // A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
            // this reason with the user to guide their troubleshooting actions.
            StoreManager.Instance.OnPurchaseFailed("Failed To Purchase Item");
            Debug.Log(string.Format("OnPurchaseFailed: FAIL. Product: '{0}', PurchaseFailureReason: {1}", product.definition.storeSpecificId, failureReason));
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.Log("OnInitializeFailed InitializationFailureReason:" + message);
            StoreManager.Instance.OnInitializationFailed();

        }
        public bool IsProductPurchased(string productId)
        {
            if (IsInitialized())
            {
                Product product = m_StoreController.products.WithID(productId);

                // Check if the product exists and if it has been purchased (non-consumable or subscription).
                if (product != null)
                {
                    if (product.hasReceipt)
                    {
                        // The product has a receipt, which means it has been purchased.
                        return true;
                    }
                }
            }

            // If not initialized, or the product doesn't exist or hasn't been purchased.
            return false;
        }

        #region PendingPurchasesHandling
        public void SetPendingPurchaseToComplete(string _productId)
        {
            if (IsInitialized())
            {
                Product _product = m_StoreController.products.WithID(_productId);
                if (_product != null)
                {
                    m_StoreController.ConfirmPendingPurchase(_product);
                }
            }
        }
        #endregion
    }
}