using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The in-game store (gameplay HUD). It lists the products of the StoreManager (remove ads and the cash packs) and
/// buys them through StoreManager.OnRequestPurchase, the same flow as the other games. The game is paused while the
/// store is open. Tabs that have no products yet only show a short message.
/// </summary>
public class ShopPanel : MonoBehaviour
{
    [System.Serializable]
    public class Tab
    {
        public Button button;
        public Image image;
        public Sprite onSprite;
        public Sprite offSprite;
        [Tooltip("The cards of this tab (empty for a tab without products)")]
        public GameObject page;
        [Tooltip("Message shown when the tab has no page")]
        public string emptyMessage = "Coming soon!";
    }

    public GameObject container;
    public Button closeButton;
    public Text cashText;
    public Text messageText;
    public Tab[] tabs;
    public ShopItemCard[] cards;
    [Tooltip("The picking capacity upgrade (bought with game cash)")]
    public ShopUpgradeCard upgradeCard;
    [Tooltip("The sideways scrolling row of cards; it starts at the first card every time the store opens")]
    public ScrollRect cardScroll;
    [Tooltip("Seconds a message stays on the screen")]
    public float messageTime = 2.5f;

    int currentTab;
    bool opened;
    float timeScaleBefore = 1f;
    Coroutine messageRoutine;

    void Awake()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
        for (int i = 0; i < tabs.Length; i++)
        {
            int _index = i;
            if (tabs[i].button != null)
            {
                tabs[i].button.onClick.AddListener(() => SelectTab(_index));
            }
        }
        if (upgradeCard != null)
        {
            upgradeCard.Bought = OnUpgradeBought;
        }
        for (int i = 0; i < cards.Length; i++)
        {
            var _card = cards[i];
            if (_card != null && _card.buyButton != null)
            {
                _card.buyButton.onClick.AddListener(() => OnBuy(_card));
            }
        }
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
        RestoreTime();
    }

    /// <summary>Opens the store on the given tab and pauses the game.</summary>
    public void Open(int _tab = 0)
    {
        gameObject.SetActive(true);
        if (!opened)
        {
            opened = true;
            timeScaleBefore = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
        }
        if (messageText != null)
        {
            messageText.gameObject.SetActive(false);
        }
        RefreshAll();
        SelectTab(_tab);
        if (cardScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            cardScroll.StopMovement();
            cardScroll.horizontalNormalizedPosition = 0f;
        }
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    void RestoreTime()
    {
        if (opened)
        {
            opened = false;
            Time.timeScale = timeScaleBefore;
        }
    }

    void OnUpgradeBought(bool _ok)
    {
        if (!_ok && GamePopups.Instance != null)
        {
            GamePopups.Instance.ShowNotEnoughCash();
            return;
        }
        if (_ok && GamePopups.Instance != null)
        {
            GamePopups.Instance.ShowSuccess("Hand capacity: " + PickingUpgrade.Capacity + " notes!", GamePopups.Instance.successIcon);
            return;
        }
        ShowMessage(_ok ? "Hand capacity: " + PickingUpgrade.Capacity + " notes!" : "Not enough cash!");
    }

    void RefreshAll()
    {
        RefreshCash();
        if (upgradeCard != null)
        {
            upgradeCard.Refresh();
        }
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null)
            {
                cards[i].Refresh();
            }
        }
    }

    void RefreshCash()
    {
        if (cashText != null && PlayerDataManager.instance != null)
        {
            cashText.text = PlayerDataManager.instance.playerData.playerCash.ToString("N0");
        }
    }

    public void SelectTab(int _index)
    {
        if (_index < 0 || _index >= tabs.Length)
            return;
        if (tabs[_index].page == null)
        {
            // a tab without products: keep the current page and tell the player
            ShowMessage(tabs[_index].emptyMessage);
            return;
        }
        currentTab = _index;
        for (int i = 0; i < tabs.Length; i++)
        {
            bool _on = i == currentTab;
            if (tabs[i].image != null && (_on ? tabs[i].onSprite : tabs[i].offSprite) != null)
            {
                tabs[i].image.sprite = _on ? tabs[i].onSprite : tabs[i].offSprite;
            }
            if (tabs[i].page != null)
            {
                tabs[i].page.SetActive(_on);
            }
        }
    }

    #region Buying
    void OnBuy(ShopItemCard _card)
    {
        var _store = StoreManager.Instance;
        if (_store == null || _card.Product == null)
        {
            ShowMessage("Store is not available right now!");
            return;
        }
        _store.OnRequestPurchase(_card.Product);
    }

    void OnPurchaseSucceeded(InAppProduct _product)
    {
        if (_product == null)
            return;
        if (GamePopups.Instance != null)
        {
            // the popup shows the result and plays the cash animation
            RefreshAll();
            return;
        }
        string _text = "Purchase successful!";
        for (int i = 0; _product.rewards != null && i < _product.rewards.Count; i++)
        {
            var _reward = _product.rewards[i];
            if (_reward.rewardType == RewardType.Currency)
            {
                _text = "+$" + _reward.amount.ToString("N0") + " added!";
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
        RefreshAll();
        ShowMessage(_text);
    }

    void OnPurchaseFailed(string _reason)
    {
        if (GamePopups.Instance != null)
            return;
        ShowMessage(string.IsNullOrEmpty(_reason) ? "Purchase failed!" : _reason);
    }
    #endregion

    #region Message
    void ShowMessage(string _message)
    {
        if (messageText == null)
            return;
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }
        messageText.text = _message;
        messageText.gameObject.SetActive(true);
        messageRoutine = StartCoroutine(HideMessage());
    }

    IEnumerator HideMessage()
    {
        // the game is paused, so the wait must not depend on the time scale
        yield return new WaitForSecondsRealtime(messageTime);
        messageText.gameObject.SetActive(false);
        messageRoutine = null;
    }
    #endregion
}
