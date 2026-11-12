using UnityEngine;
using UnityEngine.UI;

/// <summary>The picking capacity card of the shop's upgrades tab: shows "current > next" and the cash price.</summary>
public class ShopUpgradeCard : MonoBehaviour
{
    public Text amountText;
    public Text priceText;
    public Button buyButton;

    /// <summary>Raised after a successful purchase (true) or when the player cannot pay (false).</summary>
    public System.Action<bool> Bought;

    void Awake()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(OnBuy);
        }
    }

    public void Refresh()
    {
        if (PickingUpgrade.IsMaxed)
        {
            if (amountText != null) amountText.text = PickingUpgrade.Capacity.ToString();
            if (priceText != null) priceText.text = "MAX";
            if (buyButton != null) buyButton.interactable = false;
            return;
        }
        if (amountText != null)
        {
            amountText.text = PickingUpgrade.Capacity + " > " + PickingUpgrade.NextCapacity;
        }
        if (priceText != null)
        {
            priceText.text = PickingUpgrade.NextPrice > 0 ? "$" + PickingUpgrade.NextPrice.ToString("N0") : "FREE";
        }
        if (buyButton != null)
        {
            buyButton.interactable = true;
        }
    }

    void OnBuy()
    {
        bool _ok = PickingUpgrade.TryBuy();
        Refresh();
        if (Bought != null)
        {
            Bought(_ok);
        }
    }
}
