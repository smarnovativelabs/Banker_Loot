using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A currency note lying in the world. Hovering it shows the outline and prompt, but picking and throwing is
/// done through NotePickupManager (driven by the currency button), so a pile of notes can be carried at once.
/// </summary>
public class PickableNote : PickableItem
{
    /// <summary>Every note alive in the scene (heap, carried, thrown or placed), used by NoteHeapManager for saving.</summary>
    public static readonly List<PickableNote> All = new List<PickableNote>();

    // editor play mode may skip the domain reload, so the registry must not carry notes over from the last session
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetRegistry()
    {
        All.Clear();
    }

    [Tooltip("Note type, must match a NoteHeapManager entry so the note is saved and respawned (e.g. 1-Dollar)")]
    public string noteId;
    [Tooltip("Extra space around the note in which resting neighbours are woken up when it is picked")]
    public float wakeNeighboursRadius = 0.05f;

    [Header("Washing")]
    [Tooltip("Look of a note that was not washed yet (every note starts like this)")]
    public Material dirtyMaterial;
    [Tooltip("Look of a note that went through the washing machine")]
    public Material cleanMaterial;

    PickableNote highlighted;
    bool isWashed;
    Material glowMaterial;

    /// <summary>True once the note went through the washing machine. Saved with the note.</summary>
    public bool IsWashed => isWashed;

    /// <summary>Worth of the note in dollars, read from the start of the note id ("50-Dollar" is 50).</summary>
    public int Value
    {
        get
        {
            if (string.IsNullOrEmpty(noteId))
                return 0;
            int _end = 0;
            while (_end < noteId.Length && char.IsDigit(noteId[_end]))
            {
                _end++;
            }
            return _end > 0 && int.TryParse(noteId.Substring(0, _end), out int _value) ? _value : 0;
        }
    }

    protected override void Awake()
    {
        base.Awake();
        All.Add(this);
        ApplyLook();
    }

    /// <summary>
    /// Turns this note into another denomination (note id, name and look of the given note prefab). Used when the
    /// lockers cannot be completed with the notes that are left.
    /// </summary>
    public void ChangeDenomination(PickableNote _prefab)
    {
        if (_prefab == null)
            return;
        noteId = _prefab.noteId;
        itemName = _prefab.itemName;
        var _filter = GetComponent<MeshFilter>();
        var _other = _prefab.GetComponent<MeshFilter>();
        if (_filter != null && _other != null)
        {
            _filter.sharedMesh = _other.sharedMesh;
        }
    }

    /// <summary>Washes the note (clean texture) or makes it dirty again.</summary>
    public void SetWashed(bool _washed)
    {
        isWashed = _washed;
        ApplyLook();
    }

    /// <summary>
    /// Makes only this note glow: 0 is off, 1 is the full glow (emission of a temporary copy of the note material).
    /// </summary>
    public void SetGlow(float _level, Color _color, float _intensity = 2f)
    {
        var _renderer = GetComponent<MeshRenderer>();
        if (_renderer == null)
            return;
        if (_level <= 0.001f)
        {
            if (glowMaterial != null)
            {
                Destroy(glowMaterial);
                glowMaterial = null;
                ApplyLook();
            }
            return;
        }
        if (glowMaterial == null)
        {
            var _source = _renderer.sharedMaterial;
            if (_source == null)
                return;
            glowMaterial = new Material(_source);
            glowMaterial.EnableKeyword("_EMISSION");
            glowMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            _renderer.sharedMaterial = glowMaterial;
        }
        glowMaterial.SetColor("_EmissionColor", _color * (Mathf.Clamp01(_level) * _intensity));
    }

    void ApplyLook()
    {
        // while glowing the note keeps its glow material, the look is restored when the glow ends
        if (glowMaterial != null)
            return;
        var _material = isWashed ? cleanMaterial : dirtyMaterial;
        var _renderer = GetComponent<MeshRenderer>();
        if (_renderer != null && _material != null && _renderer.sharedMaterial != _material)
        {
            _renderer.sharedMaterial = _material;
        }
    }

    /// <summary>A note below this height fell out of the world.</summary>
    const float LostHeight = -10f;

    public override void Update()
    {
        base.Update();
        // Backup of the FallRespawnZone under the environment: a note that fell through the floor goes back to the
        // heap, it must never be lost (the lockers need every note)
        if (transform.position.y < LostHeight && !isPlayerPicked)
        {
            RespawnAtHeap();
        }
    }

    /// <summary>Puts the note back on the heap with no speed (used when it fell out of the world).</summary>
    public void RespawnAtHeap()
    {
        var _heap = NoteHeapManager.instance;
        Vector3 _center = _heap != null && _heap.heapRoot != null ? _heap.heapRoot.position : Vector3.zero;
        if (body != null)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }
        Vector2 _flat = Random.insideUnitCircle * 0.4f;
        transform.SetPositionAndRotation(_center + new Vector3(_flat.x, 0.4f, _flat.y), Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
    }

    /// <summary>A destroyed note simply disappears from the registry, so the next save drops it from the heap and the placed notes.</summary>
    protected virtual void OnDestroy()
    {
        All.Remove(this);
        if (glowMaterial != null)
        {
            Destroy(glowMaterial);
        }
    }

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
