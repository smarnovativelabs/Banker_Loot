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

    [Header("Throw (floaty glide)")]
    [Tooltip("Speed in m/s the note leaves the hand with")]
    public float throwSpeed = 5.5f;
    [Tooltip("Extra upward lift of the throw direction, gives a soft arc instead of a straight line")]
    public float throwUpward = 0.45f;
    [Tooltip("Degrees the nose of the note is tilted up while it levels out, makes it glide")]
    public float angleOfAttack = 12f;
    [Tooltip("Spin in rad/s about the face of the note")]
    public float launchSpin = 5f;
    [Tooltip("Random sideways roll in degrees at launch")]
    public float rollJitter = 10f;
    [Tooltip("Random cone (degrees) around the throw direction")]
    public float throwSpread = 3f;
    [Tooltip("Extra spread per note when throwing in a quick series, so the notes fan out")]
    public float fanGrowth = 1.5f;
    public float maxFanSpread = 12f;
    [Tooltip("A new series starts when this many seconds passed since the last throw")]
    public float fanResetTime = 0.5f;
    public PaperFlightSettings flight = new PaperFlightSettings();

    [Header("Messages")]
    public string handsBusyMessage = "Throw the item in your hand first!";
    [Tooltip("Shown when the player tries to pick a note while carrying the maximum amount")]
    public string handFullMessage = "Your hand is full of note stack!";

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
    bool fullMessageShown;
    float lastThrowTime = -10f;
    int seriesCount;

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

    /// <summary>The note on top of the carried pile, the next one to be thrown or placed.</summary>
    public PickableNote TopNote => held.Count > 0 ? held[held.Count - 1] : null;

    /// <summary>Takes the top note out of the pile without throwing it (the caller puts it somewhere).</summary>
    public PickableNote RemoveTop()
    {
        if (held.Count == 0)
            return null;
        int _top = held.Count - 1;
        var _note = held[_top];
        held.RemoveAt(_top);
        slots.RemoveAt(_top);
        RetargetPile(-1);
        NotesChanged?.Invoke(held.Count, maxNotes);
        return _note;
    }

    /// <summary>
    /// The note a press really takes: aiming at any note of a placed tower takes the top note of that tower,
    /// every other note is taken as it is.
    /// </summary>
    public static PickableNote ResolvePickTarget(PickableNote _note)
    {
        if (_note == null)
            return null;
        var _placed = _note.GetComponent<PlacedNote>();
        if (_placed != null && _placed.pile != null && _placed.pile.Top != null)
        {
            var _top = _placed.pile.Top.GetComponent<PickableNote>();
            if (_top != null)
                return _top;
        }
        return _note;
    }

    public bool CanPick(PickableNote _note)
    {
        _note = ResolvePickTarget(_note);
        if (_note == null || held.Count >= maxNotes || held.Contains(_note))
            return false;
        if (_note.itemCondition == ItemPickandPlace.ItemState.picking || _note.itemCondition == ItemPickandPlace.ItemState.placing)
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
        fullMessageShown = false;
        if (CanPick(_hovered))
        {
            holdMode = HoldMode.Pick;
            TryPick(_hovered);
        }
        else if (_hovered != null && IsFull)
        {
            // trying to pick more than the hands can carry: tell the player instead of throwing a note
            ShowHandFullMessage();
            return;
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
        fullMessageShown = false;
    }

    void ShowHandFullMessage()
    {
        fullMessageShown = true;
        if (UIController.instance != null)
        {
            UIController.instance.DisplayInstructions(handFullMessage);
        }
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
            else if (_hovered != null && IsFull && !fullMessageShown)
            {
                // the pile became full during the hold, say so once
                ShowHandFullMessage();
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
        _note = ResolvePickTarget(_note);
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
        // the new note is the front one, every older note moves one step back
        RetargetPile(held.Count - 1);

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
        RetargetPile(-1);

        // a quick series of throws fans out more and more
        seriesCount = Time.time - lastThrowTime < fanResetTime ? seriesCount + 1 : 0;
        lastThrowTime = Time.time;
        float _spread = Mathf.Min(maxFanSpread, throwSpread + seriesCount * fanGrowth);

        var _camera = GameController.instance != null ? GameController.instance.gameMainCamera : null;
        Vector3 _forward = _camera != null ? _camera.transform.forward : _note.transform.forward;
        Vector3 _direction = (_forward + Vector3.up * throwUpward).normalized;
        if (_spread > 0f)
        {
            var _cone = Quaternion.AngleAxis(Random.Range(-_spread, _spread), Vector3.up)
                        * Quaternion.AngleAxis(Random.Range(-_spread, _spread), Vector3.Cross(Vector3.up, _direction));
            _direction = _cone * _direction;
        }

        // the note levels out flat along the flight direction, nose slightly up
        Vector3 _flat = Vector3.ProjectOnPlane(_direction, Vector3.up);
        if (_flat.sqrMagnitude < 0.0001f)
        {
            _flat = Vector3.ProjectOnPlane(_forward, Vector3.up);
        }
        Quaternion _level = Quaternion.LookRotation(_flat.normalized, Vector3.up)
                            * Quaternion.Euler(-angleOfAttack, 0f, Random.Range(-rollJitter, rollJitter));
        float _spin = (Random.value < 0.5f ? -1f : 1f) * Random.Range(0.5f, 1f) * launchSpin;
        _note.ThrowFromHand(_direction * throwSpeed, _level, _spin, flight);

        NoteThrown?.Invoke(_note);
        NotesChanged?.Invoke(held.Count, maxNotes);
        return true;
    }

    /// <summary>Moves every carried note (except the one at _skipIndex) to its place after the pile changed.</summary>
    void RetargetPile(int _skipIndex)
    {
        for (int i = 0; i < held.Count; i++)
        {
            if (i == _skipIndex)
                continue;
            GetPileTarget(i, out Vector3 _position, out Vector3 _angle);
            held[i].RetargetCarry(_position, _angle);
        }
    }

    /// <summary>Local pose of the note at the given position in the pile (0 = oldest, the last one is the newest).</summary>
    void GetPileTarget(int _index, out Vector3 _position, out Vector3 _angle)
    {
        var _slot = slots[_index];
        Quaternion _base = Quaternion.Euler(pileAngle);
        float _step = Mathf.Min(stackStep, maxStackHeight / Mathf.Max(1, maxNotes - 1));

        // the newest note is always in front at the pile position, the older ones lie behind it going away from
        // the camera, so a tall pile never reaches or passes the camera
        int _depth = held.Count - 1 - _index;
        Vector3 _stack = _base * Vector3.up * (-_step * _depth);
        _position = pilePosition + _stack + _base * new Vector3(_slot.offset.x, 0f, _slot.offset.y);
        _angle = (_base * Quaternion.Euler(0f, _slot.yaw, 0f)).eulerAngles;
    }
    #endregion
}
