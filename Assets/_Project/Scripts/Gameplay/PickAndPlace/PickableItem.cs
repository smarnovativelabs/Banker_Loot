using UnityEngine;

/// <summary>
/// A world item the player can look at (outline + prompt), pick up with the interact button and throw with a
/// second press. Add it to a prefab together with a Collider and Rigidbody; the QuickOutline Outline is added
/// automatically when missing.
/// </summary>
public class PickableItem : ItemPickandPlace, InteractableObjects
{
    public string pickText = "Pick Up";
    public HoverInstructionType hoverType = HoverInstructionType.General;
    public Color outlineColor = new Color(1f, 0.85f, 0.1f);
    public float outlineWidth = 4f;
    Outline outline;

    protected override void Awake()
    {
        base.Awake();
        outline = GetComponent<Outline>();
        if (outline == null)
        {
            outline = gameObject.AddComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = outlineColor;
            outline.OutlineWidth = outlineWidth;
        }
        outline.enabled = false;
    }

    public virtual void OnHoverItems()
    {
        if (!CanPickItem())
            return;
        SetOutline(true);
        if (UIController.instance == null)
            return;
        UIController.instance.DisplayHoverObjectName(string.IsNullOrEmpty(itemName) ? pickText : pickText + " " + itemName, true, hoverType);
        UIController.instance.OnChangeInteraction(0, true);
    }

    public virtual void OnInteract()
    {
        if (!CanPickItem())
            return;
        SetOutline(false);
        if (UIController.instance != null)
        {
            UIController.instance.DisplayHoverObjectName("", false);
        }
        SetObjectToCam();
    }

    public void TurnOffOutline()
    {
        SetOutline(false);
    }

    protected void SetOutline(bool _enable)
    {
        if (outline != null && outline.enabled != _enable)
        {
            outline.enabled = _enable;
        }
    }
}
