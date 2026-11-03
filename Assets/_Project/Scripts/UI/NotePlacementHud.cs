using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The note placement panel, copied from the food game ("Place Item Container": rotate anti clockwise, Place,
/// rotate clockwise). It is shown while NotePlacementManager has a preview, and only calls its public methods.
/// The panel stays active (only faded) so it is always ready to be pressed.
/// </summary>
public class NotePlacementHud : MonoBehaviour
{
    public GameObject container;
    public Button rotateAntiClockButton;
    public Button placeButton;
    public Button rotateClockButton;
    [Tooltip("Tint of the place button while the note cannot be put down at the previewed spot")]
    public Color blockedColor = new Color(1f, 0.6f, 0.6f, 1f);

    [Header("Hold circle")]
    [Tooltip("Ring shown at the crosshair while the Place button is held, before the repeat starts (the same as the motel pick-up circle)")]
    public GameObject holdFillContainer;
    [Tooltip("Radial filled image inside the container")]
    public Image holdFillImage;

    CanvasGroup group;

    void Awake()
    {
        if (container == null)
            return;

        group = container.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = container.AddComponent<CanvasGroup>();
        }
        SetVisible(false);

        if (rotateAntiClockButton != null)
        {
            rotateAntiClockButton.onClick.AddListener(() => Rotate(-1f));
        }
        if (rotateClockButton != null)
        {
            rotateClockButton.onClick.AddListener(() => Rotate(1f));
        }
        if (placeButton != null)
        {
            // press places a note, holding keeps placing (the button's onClick is not used)
            var _trigger = placeButton.GetComponent<EventTrigger>();
            if (_trigger == null)
            {
                _trigger = placeButton.gameObject.AddComponent<EventTrigger>();
            }
            AddTrigger(_trigger, EventTriggerType.PointerDown, () =>
            {
                if (NotePlacementManager.instance != null) NotePlacementManager.instance.OnPlaceDown();
            });
            AddTrigger(_trigger, EventTriggerType.PointerUp, ReleasePlace);
            AddTrigger(_trigger, EventTriggerType.PointerExit, ReleasePlace);
        }
    }

    static void AddTrigger(EventTrigger _trigger, EventTriggerType _type, UnityAction _action)
    {
        var _entry = new EventTrigger.Entry { eventID = _type };
        _entry.callback.AddListener(_ => _action());
        _trigger.triggers.Add(_entry);
    }

    static void ReleasePlace()
    {
        if (NotePlacementManager.instance != null)
        {
            NotePlacementManager.instance.OnPlaceUp();
        }
    }

    void OnDisable()
    {
        ReleasePlace();
    }

    static void Rotate(float _direction)
    {
        if (NotePlacementManager.instance != null)
        {
            NotePlacementManager.instance.RotatePreview(_direction);
        }
    }

    void Update()
    {
        if (group == null)
            return;

        var _manager = NotePlacementManager.instance;
        UpdateHoldCircle(_manager);
        // the preview is hidden for a frame after each placement, the panel must keep receiving the held press
        bool _show = _manager != null && (_manager.IsPreviewing || _manager.IsPlaceHeld);
        SetVisible(_show);
        if (_show && placeButton != null && placeButton.targetGraphic != null)
        {
            placeButton.targetGraphic.color = _manager.PreviewValid ? Color.white : blockedColor;
        }
    }

    void UpdateHoldCircle(NotePlacementManager _manager)
    {
        if (holdFillContainer == null)
            return;
        bool _charging = _manager != null && _manager.IsHoldCharging;
        if (holdFillContainer.activeSelf != _charging)
        {
            holdFillContainer.SetActive(_charging);
        }
        if (_charging && holdFillImage != null)
        {
            holdFillImage.fillAmount = _manager.HoldProgress;
        }
    }

    void SetVisible(bool _visible)
    {
        group.alpha = _visible ? 1f : 0f;
        group.blocksRaycasts = _visible;
        group.interactable = _visible;
    }
}
