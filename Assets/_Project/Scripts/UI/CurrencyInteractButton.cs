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

    /// <summary>Set by the tutorial while its hand points at this button, so the button is on screen to be pressed.</summary>
    public static bool ForceVisible;

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
        bool _show = ForceVisible || _hovered != null || (!_carryingOtherItem && (_manager.HeldCount > 0 || _manager.IsInteractHeld));
        SetVisible(_show);

        if (icon != null)
        {
            // a press throws only when no note is aimed at (aiming at a note while full shows a message instead)
            Sprite _sprite = _hovered == null && _manager.HeldCount > 0 ? throwSprite : pickSprite;
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
