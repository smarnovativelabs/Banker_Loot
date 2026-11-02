using UnityEngine;

/// <summary>
/// A currency note lying in the world. Hovering it shows the outline and prompt, but picking and throwing is
/// done through NotePickupManager (driven by the currency button), so a pile of notes can be carried at once.
/// </summary>
public class PickableNote : PickableItem
{
    [Tooltip("Extra space around the note in which resting neighbours are woken up when it is picked")]
    public float wakeNeighboursRadius = 0.05f;

    PickableNote highlighted;

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
            // aiming at any note of a tower highlights (and takes) the top note of that tower
            var _target = NotePickupManager.ResolvePickTarget(this);
            if (highlighted != null && highlighted != _target)
            {
                highlighted.SetOutline(false);
            }
            highlighted = _target;
            _target.SetOutline(true);
            ShowHover(pickText + " " + _target.itemName + " (" + _manager.HeldCount + "/" + _manager.MaxNotes + ")", hoverType);
        }
        else
        {
            TurnOffOutline();
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

    /// <summary>Turns off the outline of this note and of the tower top it was highlighting.</summary>
    public override void TurnOffOutline()
    {
        base.TurnOffOutline();
        if (highlighted != null)
        {
            highlighted.SetOutline(false);
            highlighted = null;
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

    /// <summary>
    /// Throws the note out of the hand: it leaves with the given velocity, levels out to the given rotation
    /// and then flies like a sheet of paper (PaperFlight) until it lands.
    /// </summary>
    public void ThrowFromHand(Vector3 _velocity, Quaternion _levelRotation, float _spin, PaperFlightSettings _flight)
    {
        Release(Vector3.zero);
        if (body == null)
            return;

        body.linearVelocity = _velocity;
        var _paper = GetComponent<PaperFlight>();
        if (_paper == null)
        {
            _paper = gameObject.AddComponent<PaperFlight>();
        }
        _paper.Begin(_flight, _levelRotation, _spin);
    }

    /// <summary>Hides the hover feedback after the note was taken.</summary>
    public void OnPickedUp()
    {
        // a note taken from a surface (or from the top of a tower) is not placed anymore
        var _placed = GetComponent<PlacedNote>();
        if (_placed != null)
        {
            _placed.OnPickedUp();
        }
        TurnOffOutline();
        if (UIController.instance != null)
        {
            UIController.instance.DisplayHoverObjectName("", false);
        }
    }
}
