using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One message popup of the gameplay HUD that is used for the purchase result (success / failed) and for "not enough cash".
/// It listens to the StoreManager itself, so it also answers purchases made while the store is closed. The popup is
/// shown over the store and does not pause the game by itself.
/// </summary>
public class GamePopups : MonoBehaviour
{
    public static GamePopups Instance { get; private set; }

    [Header("Panel")]
    public GameObject panel;
    public Text titleText;
    public Image iconImage;
    public Text messageText;
    public Button okButton;
    [Tooltip("Only shown for 'not enough cash': opens the store on the cash tab")]
    public Button buyCashButton;
    public Button closeButton;

    [Header("Icons")]
    public Sprite successIcon;
    public Sprite failedIcon;
    public Sprite cashIcon;

    [Header("Other")]
    public ShopPanel shopPanel;

    void Awake()
    {
        Instance = this;
        if (okButton != null) okButton.onClick.AddListener(Hide);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
        if (buyCashButton != null) buyCashButton.onClick.AddListener(OnBuyCash);
        if (panel != null) panel.SetActive(false);
    }

    void OnEnable()
    {
        StoreManager.PurchaseSucceeded += OnPurchaseSucceeded;
        StoreManager.PurchaseFailed += OnPurchaseFailed;
    }

    void OnDisable()
    {
        StoreManager.PurchaseSucceeded -= OnPurchaseSucceeded;
        StoreManager.PurchaseFailed -= OnPurchaseFailed;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void ShowSuccess(string _message, Sprite _icon = null)
    {
        Show("SUCCESS", _message, _icon != null ? _icon : successIcon, false);
    }

    public void ShowFailed(string _message)
    {
        Show("FAILED", string.IsNullOrEmpty(_message) ? "Purchase failed!" : _message, failedIcon, false);
    }

    public void ShowNotEnoughCash()
    {
        Show("NOT ENOUGH CASH", "You don't have enough cash!", cashIcon, true);
    }

    void Show(string _title, string _message, Sprite _icon, bool _offerCash)
    {
        if (panel == null)
            return;
        if (titleText != null) titleText.text = _title;
        if (messageText != null) messageText.text = _message;
        if (iconImage != null)
        {
            iconImage.sprite = _icon;
            iconImage.enabled = _icon != null;
        }
        if (okButton != null) okButton.gameObject.SetActive(!_offerCash);
        if (buyCashButton != null) buyCashButton.gameObject.SetActive(_offerCash && shopPanel != null);
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
    }

    void OnBuyCash()
    {
        Hide();
        if (shopPanel != null)
        {
            shopPanel.Open(0);
        }
    }

    #region Store events
    void OnPurchaseSucceeded(InAppProduct _product)
    {
        if (_product == null)
            return;
        string _text = "Purchase successful!";
        Sprite _icon = null;
        for (int i = 0; _product.rewards != null && i < _product.rewards.Count; i++)
        {
            var _reward = _product.rewards[i];
            if (_reward.rewardType == RewardType.Currency)
            {
                _text = "+$" + _reward.amount.ToString("N0") + " added!";
                _icon = cashIcon;
                if (UIController.instance != null)
                {
                    UIController.instance.UpdateCurrency(_reward.amount);
                }
            }
            else if (_reward.rewardType == RewardType.RemoveAds)
            {
                _text = "Ads removed!";   // the StoreManager already switched the ads off
            }
        }
        ShowSuccess(_text, _icon);
    }

    void OnPurchaseFailed(string _reason)
    {
        ShowFailed(_reason);
    }
    #endregion
}
