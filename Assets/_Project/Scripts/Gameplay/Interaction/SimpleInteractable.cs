using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Ready to use interactable: shows a hover prompt while targeted and fires an event on interact.
/// Use it for quick setups, or implement InteractableObjects yourself for custom behaviour.
/// </summary>
public class SimpleInteractable : MonoBehaviour, InteractableObjects
{
    public string hoverText = "Tap To Interact";
    public HoverInstructionType hoverType = HoverInstructionType.General;
    public UnityEvent onInteract;

    public void OnHoverItems()
    {
        if (UIController.instance == null)
            return;
        UIController.instance.DisplayHoverObjectName(hoverText, true, hoverType);
        UIController.instance.OnChangeInteraction(0, true);
        SetOutline(true);
    }

    public void OnInteract()
    {
        onInteract?.Invoke();
    }

    public void TurnOffOutline()
    {
        SetOutline(false);
    }

    void SetOutline(bool _enable)
    {
        var _outline = GetComponent<Outline>();
        if (_outline != null)
        {
            _outline.enabled = _enable;
        }
    }
}
