using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One product card of the shop. It shows the data of an InAppProduct of the StoreManager (reward amount and the store's
/// price) and tells the ShopPanel when the buy button is pressed. The name and the picture are set on the prefab.
/// </summary>
public class ShopItemCard : MonoBehaviour
{
    [Tooltip("The product id of the StoreManager entry this card sells")]
    public string productID;
    [Tooltip("Price shown while the store has not answered yet (the real price is read from the store)")]
    public string fallbackPrice = "$0.99";
    [Tooltip("Shown instead of the reward amount, for example PERMANENT! (empty = show the reward amount)")]
    public string amountOverride;
    public Text nameText;
    public Text amountText;
    public Text priceText;
    public Button buyButton;

    /// <summary>The StoreManager entry of this card, null when the store has no such product.</summary>
    public InAppProduct Product { get; private set; }

    public void Refresh()
    {
        Product = FindProduct();
        string _price = fallbackPrice;
        if (Product != null && !string.IsNullOrEmpty(Product.localizedPriceString))
        {
            _price = Product.localizedPriceString;
        }

        bool _owned = IsOwned();
        if (priceText != null)
        {
            priceText.text = _owned ? "PURCHASED" : _price;
        }
        if (buyButton != null)
        {
            buyButton.interactable = !_owned;
        }
        if (amountText != null)
        {
            if (!string.IsNullOrEmpty(amountOverride))
            {
                amountText.text = amountOverride;
            }
            else if (Product != null && Product.rewards != null && Product.rewards.Count > 0)
            {
                amountText.text = "+" + Product.rewards[0].amount.ToString("N0");
            }
        }
    }

    InAppProduct FindProduct()
    {
        var _store = StoreManager.Instance;
        if (_store == null || _store.inAppProducts == null)
            return null;
        return _store.inAppProducts.Find(p => p.productID == productID);
    }

    /// <summary>A no-ads product that was already bought cannot be bought again.</summary>
    bool IsOwned()
    {
        if (Product == null || Product.rewards == null || Product.rewards.Count == 0)
            return false;
        if (Product.rewards[0].rewardType != RewardType.RemoveAds)
            return false;
        var _ads = AdsMediation.AdsMediationManager.instance;
        return _ads != null && _ads.isAdsRemoved;
    }
}
