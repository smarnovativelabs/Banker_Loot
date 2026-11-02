using UnityEngine;
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
            placeButton.onClick.AddListener(() =>
            {
                if (NotePlacementManager.instance != null) NotePlacementManager.instance.ConfirmPlacement();
            });
        }
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
        bool _show = _manager != null && _manager.IsPreviewing;
        SetVisible(_show);
        if (_show && placeButton != null && placeButton.targetGraphic != null)
        {
            placeButton.targetGraphic.color = _manager.PreviewValid ? Color.white : blockedColor;
        }
    }

    void SetVisible(bool _visible)
    {
        group.alpha = _visible ? 1f : 0f;
        group.blocksRaycasts = _visible;
        group.interactable = _visible;
    }
}
