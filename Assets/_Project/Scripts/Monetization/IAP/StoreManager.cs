using AdsMediation;
using IAPProject;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;
public class StoreManager : MonoBehaviour
{
 
    public static StoreManager Instance { get; private set; }

    /// <summary>Raised after a product reward was granted. Subscribe from your shop UI.</summary>
    public static event Action<InAppProduct> PurchaseSucceeded;
    /// <summary>Raised when a purchase or rewarded purchase fails. Subscribe from your shop UI.</summary>
    public static event Action<string> PurchaseFailed;
    static void NotifyPurchaseSuccess(InAppProduct _product) { PurchaseSucceeded?.Invoke(_product); }
    static void NotifyPurchaseFailed(string _reason) { PurchaseFailed?.Invoke(_reason); }
    Purchaser purchaserScript;
    public List<InAppProduct> inAppProducts;
    InAppProduct _adsProductInstance;
    public bool isIAPInitialized;

    PluginInitializationStatus initializationStatus;
    public List<InAppProduct> pendingPurchasedProducts;


    // Image LoadingPanel;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;  
            DontDestroyOnLoad(gameObject);
            purchaserScript = gameObject.GetComponent<Purchaser>();
            initializationStatus = PluginInitializationStatus.Loading;
            pendingPurchasedProducts = new List<InAppProduct>();

            //initlilize all IAP based purchasable items
        }
        else
        {
            Destroy(gameObject);  
        }
    }

    private void Start()
    {
        
    }
    public void InitializeStore()
    {
        if (!Instance.isIAPInitialized)
        {
            purchaserScript.InitializAllProducts(inAppProducts);
        }
    }
    public void OnInitializationComplete(IStoreController _storeController)
    {
        isIAPInitialized = true;
        for (int i = 0; i < inAppProducts.Count; i++)
        {
            if (inAppProducts[i].purchaseMethod == PurchaseType.InAppPurchase)
            {
                // a product that is not created in the store yet (new SKU) must not stop the others from loading
                Product _storeProduct = _storeController.products.WithID(inAppProducts[i].productID);
                if (_storeProduct == null || _storeProduct.metadata == null)
                {
                    Debug.LogWarning("IAP product not found in the store: " + inAppProducts[i].productID);
                    continue;
                }
                inAppProducts[i].localizedPrice = _storeProduct.metadata.localizedPrice;
                inAppProducts[i].localizedPriceString = _storeProduct.metadata.localizedPriceString;
                Debug.Log("Remove ad related 1: " + inAppProducts[i].productID  + "...isPurchased: " + _storeController.products.WithID(inAppProducts[i].productID).hasReceipt);
                // the person already purchased the remove ads.. handle that issue
                if ((purchaserScript.IsProductPurchased(inAppProducts[i].productID)))
                {
                    Debug.Log("Remove ad related 2");

                    if (AdsMediationManager.instance != null)
                    {
                        AdsMediationManager.instance.OnRemoveAds();
                    }
                }
            }
        }
        initializationStatus = PluginInitializationStatus.Loaded;
    }
    public void OnUpdateRVCashPrice(int _val)
    {
        if (inAppProducts.Count < 2)
        {
            return;
        }
        inAppProducts[1].rewards[0].amount = _val;
    }
    public int GetCashRVAmount()
    {
        if (inAppProducts.Count < 2)
        {
            return 40;
        }
        return inAppProducts[1].rewards[0].amount;
    }
    public void OnInitializationFailed()
    {
        initializationStatus = PluginInitializationStatus.Failed;
    }
    public bool GetInitializationResponse()
    {
        return initializationStatus != PluginInitializationStatus.Loading;
    }
    public void OnRequestRestore()
    {
        if (!isIAPInitialized)
        {
            if (MainMenu.instance != null)
            {
                MainMenu.instance.DisplayResponseText("Unable To Restore!");
            }
            else
            {
                NotifyPurchaseFailed("Unable To Purchase Item");
            }
            return;
        }
        purchaserScript.RestorePurchases();
    }
    public void OnRequestPurchase(int _index)
    {
        PurchaseType purchaseMethod= inAppProducts[_index].purchaseMethod;

        if (purchaseMethod == PurchaseType.RewardedVideo)
        {
            //_adsProductInstance = new InAppProduct();
            _adsProductInstance = inAppProducts[_index];
            AdsMediation.AdsMediationManager.instance.ShowRewardedVideo(OnRVSuccess, OnRVFailed);
        }
        else
        {
            if (!isIAPInitialized)
            {
                NotifyPurchaseFailed("Unable To Purchase Item");
                return;
            }
            purchaserScript.OnPurchaseItem(inAppProducts[_index].productID);
        }
        GameManager.instance.CallFireBase("IAPBtn_" + _index.ToString());
    }
    public void OnRequestPurchase(InAppProduct _product)
    {
        Debug.Log("88888");
        if (_product.purchaseMethod == PurchaseType.RewardedVideo)
        {
            //_adsProductInstance = new InAppProduct();
            _adsProductInstance = _product;
            AdsMediation.AdsMediationManager.instance.ShowRewardedVideo(OnRVSuccess, OnRVFailed);
        }
        else
        {
            if (!isIAPInitialized)
            {
                NotifyPurchaseFailed("Unable To Purchase Item");
                return;
            }
            purchaserScript.OnPurchaseItem(_product.productID);
        }
        for (int i = 0; i < inAppProducts.Count; i++)
        {
            if (_product.productID == inAppProducts[i].productID)
            {
                GameManager.instance.CallFireBase("IAPBtn_" + i.ToString());
                break;
            }
        }
    }
    public bool OnSuccessPurchase(string _productId)
    {
        InAppProduct _product = inAppProducts.Find(p => p.productID == _productId);
    
        if (_product != null)
        {
            if (GameController.instance == null)
            {
                pendingPurchasedProducts.Add(_product);
                return false;
            }

            OnProccessPurchase(_product);
            NotifyPurchaseSuccess(_product);
            return true;
        }
        return false;
    }

    // Failure callback when the purchase fails
    public void OnPurchaseFailed(string message)
    {
        NotifyPurchaseFailed(message);
    }

    public void OnRVSuccess(string message)
    {
        if (_adsProductInstance != null)
        {
            OnProccessPurchase(_adsProductInstance);
            NotifyPurchaseSuccess(_adsProductInstance);
        }
    }
    /// <summary>
    /// When user fail to watch ad inform him about failure reason
    /// </summary>
    /// <param name="_reason"></param>
    public void OnRVFailed(string _reason)
    {
        NotifyPurchaseFailed(_reason);
    }

    /// <summary>
    /// Give Successfully Purchased Items To Player 
    /// </summary>
    /// <param name="_product"></param>
    void OnProccessPurchase(InAppProduct _product)
    {
        if (_product != null)
        {
            for (int j = 0; j < _product.rewards.Count; j++)
            {
                // Check reward type and concatenate the reward text
                if (_product.rewards[j].rewardType == RewardType.Currency)
                {
                    PlayerDataManager.instance.UpdateCash(_product.rewards[j].amount);
                }
                else if (_product.rewards[j].rewardType == RewardType.Blitz)
                {
                    PlayerDataManager.instance.UpdateBlitz(_product.rewards[j].amount);
                }
                else if(_product.rewards[j].rewardType == RewardType.RemoveAds)
                {
                   // AdsMediation.AdsMediationManager.instance.OnRemoveAds();
                }
            }
        }
    }

    #region pendingPurchase
    public IEnumerator CheckForPendingPurchases()
    {
        if (pendingPurchasedProducts.Count > 0)
        {
            for (int i = 0; i < pendingPurchasedProducts.Count; i++)
            {
                int _tmp = i;
                OnApplyPendingPurchase(pendingPurchasedProducts[_tmp]);
                yield return null;
            }
            pendingPurchasedProducts.Clear();
        }
    }

    public void OnApplyPendingPurchase(InAppProduct _product)
    {
        if (_product != null)
        {
            OnProccessPurchase(_product);
            NotifyPurchaseSuccess(_product);
            purchaserScript.SetPendingPurchaseToComplete(_product.productID);
        }
    }
    #endregion
}

// Enums for purchase method and reward type
public enum PurchaseType
{
    InAppPurchase,
    RewardedVideo
}

public enum RewardType
{
    Blitz = 5,
    RemoveAds = 6,
    Currency = 7
}
[System.Serializable]
public class RewardItem
{
    public RewardType rewardType;
    public Sprite rewardImg;
    public string rewardDesc;
    public int amount;

}
[System.Serializable]
public enum IAPCategory
{
    Package = 0,
    SingleItem = 2,
    Blitz = 4
}

[System.Serializable]
public class InAppProduct
{
    public string productID;
    public string productName;
    [HideInInspector] public float price;
    [HideInInspector] public decimal localizedPrice;
    public string localizedPriceString;
    public ProductType type;
    public PurchaseType purchaseMethod;
    public IAPCategory inAppCategory;
    [HideInInspector]public GameObject displayBtnRef;
    public GameObject iapBtnRef;
    public Sprite icon;
    public Sprite bgImg;
    public List<RewardItem> rewards;
}
