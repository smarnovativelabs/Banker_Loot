using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// HUD button used only for currency notes. It forwards press / release to NotePickupManager (tap = pick or
/// throw one note, hold = keep going) and shows itself while a note is hovered or notes are carried, with the
/// carried amount as "held/max". It stays active (only its visuals fade) so a held press is never lost.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class CurrencyInteractButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public Image icon;
    public Sprite pickSprite;
    public Sprite throwSprite;
    public Text countText;

    CanvasGroup group;
    bool pressed;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    void OnDisable()
    {
        Release();
    }

    void Update()
    {
        var _manager = NotePickupManager.instance;
        if (_manager == null)
        {
            SetVisible(false);
            return;
        }

        var _hovered = _manager.HoveredNote;
        bool _canPick = _manager.CanPick(_hovered);
        // never shown together with the generic interact button: this one owns "aiming at a note" and "carrying
        // notes", the generic one owns everything else (including a carried non-note item)
        bool _carryingOtherItem = GameController.instance != null && GameController.instance.currentPicketItem != null;
        bool _show = _hovered != null || (!_carryingOtherItem && (_manager.HeldCount > 0 || _manager.IsInteractHeld));
        SetVisible(_show);

        if (icon != null)
        {
            Sprite _sprite = !_canPick && _manager.HeldCount > 0 ? throwSprite : pickSprite;
            if (_sprite != null && icon.sprite != _sprite)
            {
                icon.sprite = _sprite;
            }
        }
        if (countText != null)
        {
            countText.text = _manager.HeldCount + "/" + _manager.MaxNotes;
        }
    }

    void SetVisible(bool _visible)
    {
        group.alpha = _visible ? 1f : 0f;
        group.blocksRaycasts = _visible;
        group.interactable = _visible;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (NotePickupManager.instance == null)
            return;
        pressed = true;
        NotePickupManager.instance.OnInteractDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Release();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Release();
    }

    void Release()
    {
        if (!pressed)
            return;
        pressed = false;
        if (NotePickupManager.instance != null)
        {
            NotePickupManager.instance.OnInteractUp();
        }
    }
}
