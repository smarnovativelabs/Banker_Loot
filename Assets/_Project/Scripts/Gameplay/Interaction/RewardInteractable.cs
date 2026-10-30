using UnityEngine;

/// <summary>
/// Gives cash / blitz / xp when the player interacts with it. Add it next to a SimpleInteractable
/// (it hooks itself up) to make quick pickups and test the HUD.
/// </summary>
[RequireComponent(typeof(SimpleInteractable))]
public class RewardInteractable : MonoBehaviour
{
    public int cash = 100;
    public int blitz = 0;
    public int xp = 50;
    public bool destroyAfterUse = false;

    void Awake()
    {
        GetComponent<SimpleInteractable>().onInteract.AddListener(Collect);
    }

    public void Collect()
    {
        var _player = PlayerDataManager.instance;
        if (_player == null)
            return;

        if (cash != 0)
        {
            _player.UpdateCash(cash);
            if (UIController.instance != null) UIController.instance.UpdateCurrency(cash);
        }
        if (blitz != 0)
        {
            _player.UpdateBlitz(blitz);
            if (UIController.instance != null) UIController.instance.UpdateBlitz(blitz);
        }
        if (xp != 0 && LevelManager.Instance != null)
        {
            LevelManager.Instance.AddXP(xp);
        }
        if (destroyAfterUse)
        {
            if (UIController.instance != null)
            {
                UIController.instance.DisplayHoverObjectName("", false);
                UIController.instance.OnChangeInteraction(0, false);
            }
            Destroy(gameObject);
        }
    }
}
