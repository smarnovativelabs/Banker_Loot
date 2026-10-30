using UnityEngine;

/// <summary>
/// A currency note lying in the world. Hovering it shows the outline and prompt, but picking and throwing is
/// done through NotePickupManager (driven by the currency button), so a pile of notes can be carried at once.
/// </summary>
public class PickableNote : PickableItem
{
    [Tooltip("Extra space around the note in which resting neighbours are woken up when it is picked")]
    public float wakeNeighboursRadius = 0.05f;

    public override bool CanPickItem()
    {
        var _manager = NotePickupManager.instance;
        return _manager != null && _manager.CanPick(this);
    }

    public override void OnHoverItems()
    {
        var _manager = NotePickupManager.instance;
        if (_manager == null)
            return;

        // notes use the currency button, make sure the generic interact button of a previous target is gone
        if (UIController.instance != null)
        {
            UIController.instance.OnChangeInteraction(0, false);
        }
        if (_manager.CanPick(this))
        {
            SetOutline(true);
            ShowHover(pickText + " " + itemName + " (" + _manager.HeldCount + "/" + _manager.MaxNotes + ")", hoverType);
        }
        else
        {
            SetOutline(false);
            if (GameController.instance != null && GameController.instance.currentPicketItem != null)
            {
                ShowHover("Hands Busy", HoverInstructionType.Warning);
            }
            else if (_manager.IsFull)
            {
                ShowHover("Hands Full (" + _manager.HeldCount + "/" + _manager.MaxNotes + ")", HoverInstructionType.Warning);
            }
        }
    }

    /// <summary>Notes are only handled by the currency button, a plain screen tap does nothing.</summary>
    public override void OnInteract()
    {
    }

    static void ShowHover(string _text, HoverInstructionType _type)
    {
        if (UIController.instance != null)
        {
            UIController.instance.DisplayHoverObjectName(_text, true, _type);
        }
    }

    /// <summary>Wakes the sleeping notes lying on or around this one, they would float once it is gone.</summary>
    public void WakeNeighbours()
    {
        var _box = GetComponent<BoxCollider>();
        if (_box == null)
            return;
        Vector3 _extents = Vector3.Scale(_box.size, transform.lossyScale) * 0.5f + Vector3.one * wakeNeighboursRadius;
        var _hits = Physics.OverlapBox(transform.TransformPoint(_box.center), _extents, transform.rotation, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < _hits.Length; i++)
        {
            var _other = _hits[i].attachedRigidbody;
            if (_other != null && _other != body)
            {
                _other.WakeUp();
            }
        }
    }

    /// <summary>Throws the note out of the hand and gives it a little spin.</summary>
    public void ThrowFromHand(Vector3 _force, float _spin)
    {
        Release(_force);
        if (body != null && _spin > 0f)
        {
            body.AddTorque(Random.insideUnitSphere * _spin, ForceMode.Impulse);
        }
    }

    /// <summary>Hides the hover feedback after the note was taken.</summary>
    public void OnPickedUp()
    {
        SetOutline(false);
        if (UIController.instance != null)
        {
            UIController.instance.DisplayHoverObjectName("", false);
        }
    }
}
