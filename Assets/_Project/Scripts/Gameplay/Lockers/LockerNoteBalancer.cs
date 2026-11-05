using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the lockers completable. A locker only takes notes that fit its amount, so the notes that are left can end up
/// unable to complete the lockers (for example a locker needs $4 and only $20 notes are left).
/// When that is really the case ("no possible combination left", it lasts a few seconds) the balancer:
/// 1. shows the message "No possible combination left!",
/// 2. changes the notes that are not in a locker so that they add up to exactly what the lockers still need, none is
///    left over and none is missing (notes are turned into other denominations, surplus notes are removed and missing
///    notes appear in the heap),
/// 3. shows the message that the notes were changed; only the changed and the new notes glow, then the glow fades out.
/// Nothing happens while a combination still exists, or while a machine holds notes.
/// </summary>
[RequireComponent(typeof(LockerManager))]
public class LockerNoteBalancer : MonoBehaviour
{
    [Header("Timing")]
    [Tooltip("Seconds between two checks")]
    public float checkInterval = 1f;
    [Tooltip("Seconds the situation must last before the notes are changed (so a short pause of the player is not enough)")]
    public float stuckDelay = 3f;
    [Tooltip("Seconds the first message is on the screen before the notes change")]
    public float messageTime = 2f;
    [Tooltip("Seconds the changed and new notes glow (the glow fades out)")]
    public float glowTime = 3f;

    static readonly Color GlowColor = new Color(1f, 0.85f, 0.45f);
    const float GlowIntensity = 2.5f;

    [Header("Messages")]
    public string noCombinationMessage = "No possible combination left!";
    public string changedMessage = "The notes were changed to match the lockers!";

    [Header("Search")]
    [Tooltip("The search for a combination stops after this many steps and counts as 'a combination exists' so the notes are never changed by mistake")]
    public int searchBudget = 300000;

    // the notes the level knows (value -> prefab), biggest first
    static readonly int[] StandardValues = { 100, 50, 20, 10, 5, 1 };

    LockerManager manager;
    float nextCheck;
    float stuckTime;
    bool running;
    int budget;
    bool budgetExceeded;
    WashingMachine[] washingMachines;
    CashCountingMachine[] countingMachines;

    void Awake()
    {
        manager = GetComponent<LockerManager>();
    }

    void Update()
    {
        if (running || Time.time < nextCheck)
            return;
        nextCheck = Time.time + checkInterval;

        if (!CanCheck())
        {
            stuckTime = 0f;
            return;
        }
        CollectState(out var _open, out var _loose);
        if (_open.Count == 0 || IsSolvable(_open, _loose))
        {
            stuckTime = 0f;
            return;
        }

        stuckTime += checkInterval;
        if (stuckTime >= stuckDelay)
        {
            StartCoroutine(RescueRoutine());
        }
    }

    bool CanCheck()
    {
        var _heap = NoteHeapManager.instance;
        var _controller = GameController.instance;
        if (_heap == null || !_heap.IsReady || _controller == null || !_controller.gameStarted)
            return false;

        // notes inside a machine are not on the floor yet, wait until the machines are empty
        if (washingMachines == null)
        {
            washingMachines = FindObjectsByType<WashingMachine>(FindObjectsSortMode.None);
            countingMachines = FindObjectsByType<CashCountingMachine>(FindObjectsSortMode.None);
        }
        for (int i = 0; i < washingMachines.Length; i++)
        {
            if (washingMachines[i] != null && (washingMachines[i].CurrentState != WashingMachine.State.Idle || washingMachines[i].NotesInside > 0))
                return false;
        }
        for (int i = 0; i < countingMachines.Length; i++)
        {
            if (countingMachines[i] != null && (countingMachines[i].CurrentState != CashCountingMachine.State.Idle || countingMachines[i].NotesInside > 0))
                return false;
        }
        return true;
    }

    #region State
    /// <summary>What the lockers that are not complete still need, and every note that is not in a locker.</summary>
    void CollectState(out List<int> _open, out List<PickableNote> _loose)
    {
        _open = new List<int>();
        var _lockerSurfaces = new HashSet<NotePlaceSurface>();
        for (int i = 0; i < manager.lockers.Count; i++)
        {
            var _locker = manager.lockers[i];
            if (_locker.surface != null)
            {
                _lockerSurfaces.Add(_locker.surface);
            }
            int _need = manager.GetRemainingNeed(_locker);
            if (_need > 0)
            {
                _open.Add(_need);
            }
        }

        _loose = new List<PickableNote>();
        for (int i = 0; i < PickableNote.All.Count; i++)
        {
            var _note = PickableNote.All[i];
            if (_note == null || _note.Value <= 0)
                continue;
            var _placed = _note.GetComponent<PlacedNote>();
            if (_placed != null && !_placed.pickedUp && _placed.surface != null && _lockerSurfaces.Contains(_placed.surface))
                continue; // lies in a locker, it is already counted in what the locker still needs
            _loose.Add(_note);
        }
    }

    /// <summary>True when the notes can be split so that every locker gets exactly what it needs.</summary>
    bool IsSolvable(List<int> _needs, List<PickableNote> _loose)
    {
        // distinct values, biggest first, and how many notes there are of each
        var _counts = new SortedDictionary<int, int>(Comparer<int>.Create((a, b) => b.CompareTo(a)));
        for (int i = 0; i < _loose.Count; i++)
        {
            int _value = _loose[i].Value;
            _counts.TryGetValue(_value, out int _count);
            _counts[_value] = _count + 1;
        }
        var _values = new int[_counts.Count];
        var _available = new int[_counts.Count];
        int _index = 0;
        foreach (var _pair in _counts)
        {
            _values[_index] = _pair.Key;
            _available[_index] = _pair.Value;
            _index++;
        }

        // the small needs first, they fail fastest
        var _sorted = new List<int>(_needs);
        _sorted.Sort();
        budget = searchBudget;
        budgetExceeded = false;
        return SolveLocker(0, _sorted, _values, _available);
    }

    bool SolveLocker(int _locker, List<int> _needs, int[] _values, int[] _available)
    {
        if (_locker >= _needs.Count)
            return true;
        return Fill(_locker, 0, _needs[_locker], _needs, _values, _available);
    }

    /// <summary>Fills the locker with the notes of denomination _d and the smaller ones, then goes on with the next locker.</summary>
    bool Fill(int _locker, int _d, int _remaining, List<int> _needs, int[] _values, int[] _available)
    {
        if (--budget <= 0)
        {
            budgetExceeded = true;
            return true; // too much to search: assume it works, the notes are only changed when it is sure it does not
        }
        if (_remaining == 0)
            return SolveLocker(_locker + 1, _needs, _values, _available);
        if (_d >= _values.Length)
            return false;

        // the notes that are left must be enough for the rest
        int _worth = 0;
        for (int i = _d; i < _values.Length; i++)
        {
            _worth += _values[i] * _available[i];
        }
        if (_worth < _remaining)
            return false;

        int _max = Mathf.Min(_available[_d], _remaining / _values[_d]);
        for (int _take = _max; _take >= 0; _take--)
        {
            _available[_d] -= _take;
            bool _ok = Fill(_locker, _d + 1, _remaining - _take * _values[_d], _needs, _values, _available);
            _available[_d] += _take;
            if (_ok || budgetExceeded)
                return true;
        }
        return false;
    }
    #endregion

    #region Rescue
    IEnumerator RescueRoutine()
    {
        running = true;

        // 1. tell the player
        if (UIController.instance != null)
        {
            UIController.instance.DisplayInstructions(noCombinationMessage);
        }
        yield return new WaitForSeconds(messageTime);

        // the player may have fixed it in the meantime
        if (!StillStuck())
        {
            FinishRescue();
            yield break;
        }

        // 2. the notes change, only the changed and the new notes glow
        var _glowing = new List<PickableNote>();
        if (StillStuck())
        {
            CollectState(out var _open, out var _loose);
            Rebalance(_open, _loose, _glowing);
            if (UIController.instance != null)
            {
                UIController.instance.DisplayInstructions(changedMessage);
            }
            StartCoroutine(SaveAfterFrame());
        }

        // 3. the glow fades out
        SetNotesGlow(_glowing, 1f);
        float _time = 0f;
        while (_time < glowTime)
        {
            _time += Time.deltaTime;
            SetNotesGlow(_glowing, 1f - _time / glowTime);
            yield return null;
        }
        SetNotesGlow(_glowing, 0f);
        FinishRescue();
    }

    static void SetNotesGlow(List<PickableNote> _notes, float _level)
    {
        for (int i = 0; i < _notes.Count; i++)
        {
            if (_notes[i] != null)
            {
                _notes[i].SetGlow(_level, GlowColor, GlowIntensity);
            }
        }
    }

    bool StillStuck()
    {
        if (!CanCheck())
            return false;
        CollectState(out var _open, out var _loose);
        return _open.Count > 0 && !IsSolvable(_open, _loose);
    }

    void FinishRescue()
    {
        stuckTime = 0f;
        running = false;
    }

    IEnumerator SaveAfterFrame()
    {
        yield return null; // the removed notes are destroyed at the end of the frame
        if (NoteHeapManager.instance != null)
        {
            NoteHeapManager.instance.SaveHeap();
        }
    }

    /// <summary>
    /// Makes the notes outside the lockers add up to exactly what the lockers need: every need is split into notes,
    /// existing notes that already fit stay as they are, the other notes are changed to the missing denominations,
    /// extra notes are removed and missing ones are spawned in the heap.
    /// </summary>
    void Rebalance(List<int> _needs, List<PickableNote> _loose, List<PickableNote> _glowing)
    {
        var _heap = NoteHeapManager.instance;
        var _prefabs = new Dictionary<int, PickableNote>();
        if (_heap != null && _heap.CurrentLevel != null)
        {
            foreach (var _entry in _heap.CurrentLevel.noteTypes)
            {
                if (_entry.prefab != null && _entry.prefab.Value > 0)
                {
                    _prefabs[_entry.prefab.Value] = _entry.prefab;
                }
            }
        }
        var _values = new List<int>();
        for (int i = 0; i < StandardValues.Length; i++)
        {
            if (_prefabs.ContainsKey(StandardValues[i]))
            {
                _values.Add(StandardValues[i]);
            }
        }
        if (_values.Count == 0)
            return;

        // 1. what the lockers need, as a list of notes (the biggest notes first, so there are as few as possible)
        var _wanted = new Dictionary<int, int>();
        for (int i = 0; i < _needs.Count; i++)
        {
            int _left = _needs[i];
            for (int d = 0; d < _values.Count; d++)
            {
                int _count = _left / _values[d];
                if (_count > 0)
                {
                    _wanted.TryGetValue(_values[d], out int _have);
                    _wanted[_values[d]] = _have + _count;
                    _left -= _count * _values[d];
                }
            }
        }

        // 2. the notes in the hands and the nearest ones are kept first
        var _held = new HashSet<PickableNote>();
        if (NotePickupManager.instance != null)
        {
            foreach (var _note in NotePickupManager.instance.HeldNotes)
            {
                _held.Add(_note);
            }
        }
        var _camera = GameController.instance != null ? GameController.instance.gameMainCamera : null;
        Vector3 _player = _camera != null ? _camera.transform.position : Vector3.zero;
        _loose.Sort((a, b) =>
        {
            int _heldA = _held.Contains(a) ? 0 : 1;
            int _heldB = _held.Contains(b) ? 0 : 1;
            if (_heldA != _heldB)
                return _heldA.CompareTo(_heldB);
            return (a.transform.position - _player).sqrMagnitude.CompareTo((b.transform.position - _player).sqrMagnitude);
        });

        // notes that already are one of the wanted denominations stay as they are
        var _toChange = new List<PickableNote>();
        for (int i = 0; i < _loose.Count; i++)
        {
            int _value = _loose[i].Value;
            if (_wanted.TryGetValue(_value, out int _stillWanted) && _stillWanted > 0)
            {
                _wanted[_value] = _stillWanted - 1;
            }
            else
            {
                _toChange.Add(_loose[i]);
            }
        }
        var _missing = new List<int>();
        foreach (var _pair in _wanted)
        {
            for (int i = 0; i < _pair.Value; i++)
            {
                _missing.Add(_pair.Key);
            }
        }

        // 3. change, remove or spawn
        int _changed = Mathf.Min(_toChange.Count, _missing.Count);
        for (int i = 0; i < _changed; i++)
        {
            _toChange[i].ChangeDenomination(_prefabs[_missing[i]]);
            _glowing.Add(_toChange[i]);
        }
        for (int i = _changed; i < _toChange.Count; i++)
        {
            RemoveNote(_toChange[i]);
        }
        int _spawned = 0;
        for (int i = _changed; i < _missing.Count; i++)
        {
            var _new = SpawnNote(_prefabs[_missing[i]], _spawned++);
            if (_new != null)
            {
                _glowing.Add(_new);
            }
        }
    }

    /// <summary>Removes a note for good (it leaves the hands and the surface it lies on first).</summary>
    static void RemoveNote(PickableNote _note)
    {
        if (NotePickupManager.instance != null)
        {
            NotePickupManager.instance.RemoveNote(_note);
        }
        _note.OnPickedUp();
        if (Application.isPlaying)
        {
            Destroy(_note.gameObject);
        }
        else
        {
            DestroyImmediate(_note.gameObject);
        }
    }

    /// <summary>A missing note drops into the heap.</summary>
    static PickableNote SpawnNote(PickableNote _prefab, int _index)
    {
        var _heap = NoteHeapManager.instance;
        Transform _root = _heap != null ? _heap.heapRoot : null;
        Vector2 _flat = Random.insideUnitCircle * 0.5f;
        Vector3 _center = _root != null ? _root.position : Vector3.zero;

        // The note is laid on the heap without a fall (a thin note dropped onto a pile can get pushed through the
        // floor): find what is under the spot, the floor or the notes already there, and put the note on top of it.
        Physics.SyncTransforms();
        var _origin = new Vector3(_center.x + _flat.x, _center.y + 1.5f, _center.z + _flat.y);
        float _y = _center.y;
        if (Physics.Raycast(_origin, Vector3.down, out RaycastHit _hit, 4f, ~0, QueryTriggerInteraction.Ignore))
        {
            _y = _hit.point.y;
        }
        var _box = _prefab.GetComponent<BoxCollider>();
        float _thickness = Mathf.Max(0.01f, _box != null ? _box.size.y * _prefab.normalScale : 0.016f);

        var _note = Instantiate(_prefab, _root);
        _note.noteId = _prefab.noteId;
        _note.SetWashed(false);
        _note.transform.SetPositionAndRotation(
            new Vector3(_origin.x, _y + _thickness * 0.5f + 0.002f, _origin.z),
            Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        var _body = _note.GetComponent<Rigidbody>();
        if (_body != null)
        {
            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.Sleep();
        }
        return _note;
    }
    #endregion
}
