using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The wall of lockers. Every locker has a door and a NotePlaceSurface (the floor of the locker) and an amount.
/// - When the game starts every door is open, except the lockers that were completed before (saved).
/// - Notes are put into a locker like on any table (the surface refuses a note that would go over the amount).
/// - When the notes in a locker add up to its amount the door closes, the money is removed (it is not kept or saved)
///   and the locker is marked complete. The completed lockers are saved and stay closed.
/// </summary>
public class LockerManager : MonoBehaviour
{
    [Serializable]
    public class LockerEntry
    {
        [Tooltip("Unique name, used by the save file")]
        public string id;
        public Transform door;
        [Tooltip("Worth in dollars the notes in the locker must add up to for the locker to close")]
        [Min(1)] public int amount = 100;
        [Tooltip("The floor of the locker, where the notes are put (NotePlaceSurface with a BoxCollider)")]
        public NotePlaceSurface surface;
        [Tooltip("Text that shows the amount the locker still needs (on the locker board)")]
        public TMP_Text label;
        [Tooltip("Text with the name of the locker on the board (set by the setup, for example A01)")]
        public TMP_Text nameText;
        [Tooltip("The locker picture on the board, an empty white image until the sprite is assigned")]
        public UnityEngine.UI.Image icon;

        [NonSerialized] public bool completed;
        [NonSerialized] public Quaternion closedRotation;
        [NonSerialized] public float doorAngle;
        [NonSerialized] public int lastShownRemaining = -2;
        [NonSerialized] public Vector3 outward = Vector3.zero;
    }

    const string SaveFileName = "LockerData";

    public List<LockerEntry> lockers = new List<LockerEntry>();

    [Header("Door")]
    [Tooltip("Degrees around the door's hinge (local Y) when the door is open, the door is closed at 0")]
    public float doorOpenAngle = -100f;
    public float doorMoveTime = 0.6f;
    public AudioClip doorCloseSound;

    [Header("Labels")]
    [Tooltip("Turn the label texts to the player (for floating labels). The texts of the locker board stay flat on the board.")]
    public bool billboardLabels = false;

    [Header("Messages")]
    public string alreadyCompleteMessage = "This locker is already full!";
    public string dirtyNoteMessage = "First wash the notes, then place them in the locker!";
    public string tooMuchMessage = "Too much money! This locker needs only ${0} more.";

    [Header("Timing")]
    [Tooltip("Seconds between two checks of the notes in the lockers")]
    public float checkInterval = 0.2f;

    float nextCheck;
    Coroutine registerRoutine;

    void Start()
    {
        var _save = SerializationManager.LoadFile(SaveFileName) as LockerSaveData;
        for (int i = 0; i < lockers.Count; i++)
        {
            var _locker = lockers[i];
            if (_locker.door == null)
                continue;

            _locker.closedRotation = _locker.door.localRotation;
            _locker.completed = _save != null && _save.completedIds != null && _save.completedIds.Contains(_locker.id);
            SetDoorAngle(_locker, _locker.completed ? 0f : doorOpenAngle);
            SetSurfaceEnabled(_locker, !_locker.completed);
            // never shown yet: the first update always writes the text (a locker that was completed in an earlier
            // session must show DONE and not the text of the scene)
            _locker.lastShownRemaining = -2;
            UpdateLabel(_locker, _locker.amount);
            ComputeOutward(_locker);

            var _captured = _locker;
            if (_locker.surface != null)
            {
                _locker.surface.rejectReason = _note => RejectReason(_captured, _note);
            }
        }
        registerRoutine = StartCoroutine(RegisterSaving());

        // when the notes left cannot complete the lockers anymore, the balancer changes them
        if (GetComponent<LockerNoteBalancer>() == null)
        {
            gameObject.AddComponent<LockerNoteBalancer>();
        }
    }

    #region Need
    /// <summary>The direction the locker door faces (notes that are ejected fly this way).</summary>
    static void ComputeOutward(LockerEntry _locker)
    {
        var _body = _locker.door.parent != null ? _locker.door.parent.GetComponent<MeshRenderer>() : null;
        var _doorRenderer = _locker.door.GetComponent<MeshRenderer>();
        Vector3 _outward = Vector3.zero;
        if (_body != null && _doorRenderer != null)
        {
            _outward = _doorRenderer.bounds.center - _body.bounds.center;
            _outward.y = 0f;
            _outward = _outward.normalized;
        }
        _locker.outward = _outward;
    }

    /// <summary>What the locker still needs: its amount minus the notes in it.</summary>
    public int GetRemainingNeed(LockerEntry _locker)
    {
        if (_locker.completed || _locker.surface == null)
            return 0;
        return Mathf.Max(0, _locker.amount - GetStoredWorth(_locker, out _));
    }
    #endregion

    /// <summary>The GameController is spawned at runtime, wait for it and then add the locker saving to its saving actions.</summary>
    IEnumerator RegisterSaving()
    {
        while (GameController.instance == null)
        {
            yield return null;
        }
        GameController.instance.AddSavingAction(SaveLockers);
        registerRoutine = null;
    }

    void Update()
    {
        // the labels always face the player
        var _camera = GameController.instance != null ? GameController.instance.gameMainCamera : null;
        if (billboardLabels && _camera != null)
        {
            for (int i = 0; i < lockers.Count; i++)
            {
                var _label = lockers[i].label;
                if (_label != null)
                {
                    _label.transform.rotation = Quaternion.LookRotation(_label.transform.position - _camera.transform.position, Vector3.up);
                }
            }
        }

        if (Time.time < nextCheck)
            return;
        nextCheck = Time.time + checkInterval;
        CheckLockers();
    }

    #region Notes in the lockers
    /// <summary>Worth of the notes lying in the locker and whether they all settled (the last note may still be gliding in).</summary>
    int GetStoredWorth(LockerEntry _locker, out bool _settled, List<PlacedNote> _notes = null)
    {
        int _worth = 0;
        _settled = true;
        for (int i = 0; i < PlacedNote.All.Count; i++)
        {
            var _placed = PlacedNote.All[i];
            if (_placed.surface != _locker.surface || _placed.pickedUp)
                continue;
            var _note = _placed.GetComponent<PickableNote>();
            if (_note == null)
                continue;
            _worth += _note.Value;
            if (_note.itemCondition == ItemPickandPlace.ItemState.placing)
            {
                _settled = false;
            }
            _notes?.Add(_placed);
        }
        return _worth;
    }

    string RejectReason(LockerEntry _locker, PickableNote _note)
    {
        if (_locker.completed)
            return alreadyCompleteMessage;
        // only washed notes go into a locker
        if (_note != null && !_note.IsWashed)
            return dirtyNoteMessage;
        int _stored = GetStoredWorth(_locker, out _);
        int _left = _locker.amount - _stored;
        if (_note != null && _note.Value > _left)
            return string.Format(tooMuchMessage, _left);
        return null;
    }

    void CheckLockers()
    {
        for (int i = 0; i < lockers.Count; i++)
        {
            var _locker = lockers[i];
            if (_locker.door == null || _locker.surface == null)
                continue;

            if (_locker.completed)
            {
                // a note saved on a completed locker (saved before it closed) is removed again
                var _leftovers = new List<PlacedNote>();
                GetStoredWorth(_locker, out _, _leftovers);
                RemoveNotes(_leftovers);
                continue;
            }

            int _stored = GetStoredWorth(_locker, out bool _settled);
            if (_stored > _locker.amount)
            {
                // a locker never holds even one dollar over its limit (this only happens when the amount was lowered
                // or an old save holds more): the notes on top are taken out again
                EjectExcess(_locker);
                continue;
            }
            UpdateLabel(_locker, Mathf.Max(0, _locker.amount - _stored));
            if (_stored >= _locker.amount && _settled)
            {
                Complete(_locker);
            }
        }
    }

    /// <summary>Takes the top notes out of a locker that holds more than its amount and lets them drop in front of it.</summary>
    void EjectExcess(LockerEntry _locker)
    {
        var _placedNotes = new List<PlacedNote>();
        int _stored = GetStoredWorth(_locker, out _, _placedNotes);
        // the highest notes first
        _placedNotes.Sort((a, b) => b.transform.position.y.CompareTo(a.transform.position.y));
        for (int i = 0; i < _placedNotes.Count && _stored > _locker.amount; i++)
        {
            var _note = _placedNotes[i].GetComponent<PickableNote>();
            if (_note == null)
                continue;
            _stored -= _note.Value;
            _note.OnPickedUp(); // leaves the locker (and its stack)
            _note.Release(_locker.outward * 3f + Vector3.up * 2f);
        }
    }

    static void RemoveNotes(List<PlacedNote> _placedNotes)
    {
        for (int i = 0; i < _placedNotes.Count; i++)
        {
            if (_placedNotes[i] != null)
            {
                // the note is gone for good: it is not kept in the locker and not saved anywhere
                Destroy(_placedNotes[i].gameObject);
            }
        }
    }
    #endregion

    #region Complete
    void Complete(LockerEntry _locker)
    {
        _locker.completed = true;
        var _notes = new List<PlacedNote>();
        GetStoredWorth(_locker, out _, _notes);
        RemoveNotes(_notes);
        SetSurfaceEnabled(_locker, false);
        UpdateLabel(_locker, 0);
        StartCoroutine(CloseDoor(_locker));
        StartCoroutine(SaveAfterFrame());
    }

    IEnumerator CloseDoor(LockerEntry _locker)
    {
        if (doorCloseSound != null && SoundController.instance != null)
        {
            SoundController.instance.OnPlayInteractionSound(doorCloseSound);
        }
        float _from = _locker.doorAngle;
        float _time = 0f;
        while (_time < doorMoveTime)
        {
            _time += Time.deltaTime;
            SetDoorAngle(_locker, Mathf.Lerp(_from, 0f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_time / doorMoveTime))));
            yield return null;
        }
        SetDoorAngle(_locker, 0f);
    }

    /// <summary>Saves the lockers and the notes once the removed notes are really gone (they are destroyed at the end of the frame).</summary>
    IEnumerator SaveAfterFrame()
    {
        yield return null;
        SaveLockers();
        if (NoteHeapManager.instance != null)
        {
            NoteHeapManager.instance.SaveHeap();
        }
    }

    void SetDoorAngle(LockerEntry _locker, float _angle)
    {
        _locker.doorAngle = _angle;
        _locker.door.localRotation = _locker.closedRotation * Quaternion.Euler(0f, _angle, 0f);
    }

    static void SetSurfaceEnabled(LockerEntry _locker, bool _enabled)
    {
        if (_locker.surface == null)
            return;
        // the floor and the place zone of the locker
        var _colliders = _locker.surface.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < _colliders.Length; i++)
        {
            _colliders[i].enabled = _enabled;
        }
    }

    void UpdateLabel(LockerEntry _locker, int _remaining)
    {
        if (_locker.label == null)
            return;
        int _shown = _locker.completed ? -1 : _remaining;
        if (_locker.lastShownRemaining == _shown)
            return;
        _locker.lastShownRemaining = _shown;
        // the amount the locker still needs, like "$1900"
        _locker.label.text = _locker.completed
            ? "<color=#7CFF8A>DONE</color>"
            : "$" + _remaining;
    }
    #endregion

    #region Save
    /// <summary>Writes which lockers are complete. Registered as a GameController saving action.</summary>
    public void SaveLockers()
    {
        var _data = new LockerSaveData();
        for (int i = 0; i < lockers.Count; i++)
        {
            if (lockers[i].completed)
            {
                _data.completedIds.Add(lockers[i].id);
            }
        }
        SerializationManager.Save(_data, SaveFileName);
    }

    /// <summary>Deletes the saved lockers, the next start opens all of them again.</summary>
    public void ResetSave()
    {
        SerializationManager.DeleteFile(SaveFileName);
    }
    #endregion
}

[Serializable]
public class LockerSaveData
{
    /// <summary>Ids of the lockers that were filled and closed.</summary>
    public List<string> completedIds = new List<string>();
}
