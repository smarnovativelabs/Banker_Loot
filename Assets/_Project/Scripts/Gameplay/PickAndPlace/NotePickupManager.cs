using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Owns the pile of notes the player carries. The currency button reports press / release:
/// - a tap picks the hovered note (until maxNotes is reached) or, when nothing can be picked, throws the top note
/// - holding the button repeats the action chosen at press time: it keeps picking whatever note is under the
///   crosshair, or keeps throwing notes from the pile, at the configured speed.
/// Notes are stacked on top of each other in front of the camera; the newest one is on top and thrown first.
/// </summary>
public class NotePickupManager : MonoBehaviour
{
    public static NotePickupManager instance;

    [Header("Capacity")]
    [Min(1)] public int maxNotes = 10;

    [Header("Hold To Repeat")]
    [Tooltip("Seconds the button must stay down before it starts repeating")]
    public float holdDelay = 0.3f;
    [Tooltip("Seconds between two notes while holding to pick")]
    public float pickInterval = 0.12f;
    [Tooltip("Seconds between two notes while holding to throw")]
    public float throwInterval = 0.1f;

    [Header("Pile In Hand (local to the camera held-item point)")]
    public Vector3 pilePosition = new Vector3(0.25f, -0.25f, 0.65f);
    [Tooltip("Euler angles of the bottom note: face towards the camera, long edge horizontal, leaning back a little")]
    public Vector3 pileAngle = (Quaternion.Euler(-70f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f)).eulerAngles;
    public float noteScale = 1.6f;
    [Tooltip("Distance between two stacked notes")]
    public float stackStep = 0.008f;
    [Tooltip("The pile never gets taller than this, the step shrinks when it would")]
    public float maxStackHeight = 0.12f;
    public float positionJitter = 0.01f;
    public float angleJitter = 8f;

    [Header("Throw")]
    public float throwForceMul = 1f;
    [Tooltip("Random cone (degrees) around the camera direction")]
    public float throwSpread = 3f;
    public float throwSpin = 0.02f;

    [Header("Messages")]
    public string handsBusyMessage = "Throw the item in your hand first!";

    /// <summary>Raised with (held notes, max notes) whenever one of them changes.</summary>
    public event Action<int, int> NotesChanged;
    public event Action<PickableNote> NotePicked;
    public event Action<PickableNote> NoteThrown;

    struct PileSlot
    {
        public Vector3 offset;
        public float yaw;
    }

    enum HoldMode
    {
        None,
        Pick,
        Throw
    }

    readonly List<PickableNote> held = new List<PickableNote>();
    readonly List<PileSlot> slots = new List<PileSlot>();
    HoldMode holdMode = HoldMode.None;
    bool isDown;
    float downTime;
    float nextRepeatTime;

    public int HeldCount => held.Count;
    public int MaxNotes => maxNotes;
    public bool IsFull => held.Count >= maxNotes;
    public bool IsInteractHeld => isDown;

    /// <summary>The note under the crosshair, if any.</summary>
    public PickableNote HoveredNote
    {
        get
        {
            var _player = PlayerInteraction.instance;
            return _player != null ? _player.currentInteractingObj as PickableNote : null;
        }
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>Raises or lowers the capacity (for upgrades). Notes already in hand are kept.</summary>
    public void SetMaxNotes(int _max)
    {
        maxNotes = Mathf.Max(1, _max);
        NotesChanged?.Invoke(held.Count, maxNotes);
    }

    public bool CanPick(PickableNote _note)
    {
        if (_note == null || held.Count >= maxNotes || held.Contains(_note))
            return false;
        if (_note.itemCondition == ItemPickandPlace.ItemState.picking)
            return false;
        var _controller = GameController.instance;
        return _controller != null && _controller.parentOFPickedObj != null && _controller.currentPicketItem == null;
    }

    #region Input (called by the currency button)
    public void OnInteractDown()
    {
        var _controller = GameController.instance;
        if (_controller == null || !_controller.gameStarted)
            return;

        var _hovered = HoveredNote;
        if (_controller.currentPicketItem != null)
        {
            // another item is in the hands, notes cannot be touched until it is thrown
            if (_hovered != null && UIController.instance != null)
            {
                UIController.instance.DisplayInstructions(handsBusyMessage);
            }
            return;
        }
        if (CanPick(_hovered))
        {
            holdMode = HoldMode.Pick;
            TryPick(_hovered);
        }
        else if (held.Count > 0)
        {
            holdMode = HoldMode.Throw;
            TryThrow();
        }
        else
        {
            return;
        }
        isDown = true;
        downTime = 0f;
        nextRepeatTime = holdDelay;
    }

    public void OnInteractUp()
    {
        isDown = false;
        holdMode = HoldMode.None;
    }

    void Update()
    {
        if (!isDown)
            return;

        downTime += Time.deltaTime;
        if (downTime < nextRepeatTime)
            return;

        if (holdMode == HoldMode.Pick)
        {
            // keeps picking while the crosshair is over notes, waits otherwise
            var _hovered = HoveredNote;
            if (CanPick(_hovered) && TryPick(_hovered))
            {
                nextRepeatTime = downTime + pickInterval;
            }
        }
        else if (holdMode == HoldMode.Throw)
        {
            if (TryThrow())
            {
                nextRepeatTime = downTime + throwInterval;
            }
            else
            {
                OnInteractUp();
            }
        }
    }
    #endregion

    #region Pick / Throw
    public bool TryPick(PickableNote _note)
    {
        if (!CanPick(_note))
            return false;

        _note.WakeNeighbours();
        _note.OnPickedUp();

        var _slot = new PileSlot
        {
            offset = new Vector3(Random.Range(-positionJitter, positionJitter), Random.Range(-positionJitter, positionJitter), 0f),
            yaw = Random.Range(-angleJitter, angleJitter)
        };
        held.Add(_note);
        slots.Add(_slot);
        GetPileTarget(held.Count - 1, out Vector3 _position, out Vector3 _angle);
        _note.BeginCarry(_position, _angle, noteScale);

        NotePicked?.Invoke(_note);
        NotesChanged?.Invoke(held.Count, maxNotes);
        return true;
    }

    /// <summary>Throws the top note of the pile.</summary>
    public bool TryThrow()
    {
        if (held.Count == 0)
            return false;

        int _top = held.Count - 1;
        var _note = held[_top];
        held.RemoveAt(_top);
        slots.RemoveAt(_top);

        var _camera = GameController.instance != null ? GameController.instance.gameMainCamera : null;
        Vector3 _direction = _camera != null ? _camera.transform.forward : _note.transform.forward;
        if (_camera != null && throwSpread > 0f)
        {
            var _cone = Quaternion.AngleAxis(Random.Range(-throwSpread, throwSpread), _camera.transform.up)
                        * Quaternion.AngleAxis(Random.Range(-throwSpread, throwSpread), _camera.transform.right);
            _direction = _cone * _direction;
        }
        _note.ThrowFromHand(_direction * 100f * throwForceMul, throwSpin);

        NoteThrown?.Invoke(_note);
        NotesChanged?.Invoke(held.Count, maxNotes);
        return true;
    }

    /// <summary>Local pose of the note at the given position in the pile (0 = bottom).</summary>
    void GetPileTarget(int _index, out Vector3 _position, out Vector3 _angle)
    {
        var _slot = slots[_index];
        Quaternion _base = Quaternion.Euler(pileAngle);
        float _step = Mathf.Min(stackStep, maxStackHeight / Mathf.Max(1, maxNotes - 1));

        // stack along the note normal (towards the camera), newest on top
        Vector3 _stack = _base * Vector3.up * (_step * _index);
        _position = pilePosition + _stack + _base * new Vector3(_slot.offset.x, 0f, _slot.offset.y);
        _angle = (_base * Quaternion.Euler(0f, _slot.yaw, 0f)).eulerAngles;
    }
    #endregion
}
