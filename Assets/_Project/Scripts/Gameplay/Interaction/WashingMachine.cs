using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The washing machine for dirty notes.
/// - Interact while carrying notes: the door opens, every carried note flies into the drum, the door closes and
///   the wash starts (washDuration seconds, the timer shows on top of the machine).
/// - When the timer runs out the machine beeps and stops, the notes inside are washed (clean texture).
/// - Interact again with empty hands: the door opens and the washed notes fly back into the hand pile.
/// The notes stay normal PickableNotes (hidden while inside), so the note saving keeps their washed state.
/// </summary>
public class WashingMachine : MonoBehaviour, InteractableObjects
{
    public enum State
    {
        Idle,
        Loading,
        Washing,
        Done,
        Unloading
    }

    [Header("Parts")]
    [Tooltip("The glass door, its pivot must be the hinge")]
    public Transform door;
    [Tooltip("Door angle (degrees around Y) when open")]
    public float doorOpenAngle = 100f;
    public float doorMoveTime = 0.45f;
    [Tooltip("Where the notes disappear into / come out of the drum")]
    public Transform drumPoint;
    [Tooltip("World text on top of the machine that shows the timer")]
    public TMP_Text timerText;
    [Tooltip("Outlines shown while the machine is aimed at (QuickOutline on the body and the door)")]
    public Outline[] outlines;

    [Header("Washing")]
    public float washDuration = 20f;
    [Tooltip("Seconds between two notes flying in / out")]
    public float noteInterval = 0.12f;
    [Tooltip("Seconds a note takes to fly in / out")]
    public float flyTime = 0.35f;
    [Tooltip("Size of a note inside the drum, as a fraction of its normal size (the full size does not fit through the door)")]
    [Range(0.2f, 1f)] public float insideScale = 0.5f;
    [Tooltip("Distance from the drum centre the notes circle at while washing")]
    public float orbitRadius = 0.12f;
    [Tooltip("Degrees per second the notes turn around the drum while washing")]
    public float orbitSpeed = 220f;
    public int beepCount = 3;

    [Header("Sounds")]
    public AudioClip doorOpenSound;
    public AudioClip doorCloseSound;

    [Header("Messages")]
    public string hoverWash = "Wash Notes";
    public string hoverNoNotes = "Pick up notes to wash";
    public string hoverTake = "Take Washed Notes";
    public string handsBusyMessage = "Throw the item in your hand first!";
    public string needNotesMessage = "Pick up some notes to wash them!";
    public string emptyHandsMessage = "Empty your hands to take the washed notes!";

    /// <summary>A note in the drum and where it is on its circle.</summary>
    class InsideNote
    {
        public PickableNote note;
        public float phase;
        public float depth;
        public Quaternion baseRotation;
    }

    readonly List<InsideNote> inside = new List<InsideNote>();
    float orbitAngle;
    State state = State.Idle;
    float remaining;
    float doorAngle;
    int flying;
    AudioClip beepClip;

    public State CurrentState => state;
    public int NotesInside => inside.Count;

    void Start()
    {
        SetDoorAngle(0f);
        UpdateTimerText();
    }

    void Update()
    {
        if (state == State.Washing)
        {
            orbitAngle += orbitSpeed * Time.deltaTime;
            UpdateOrbit();
            remaining -= Time.deltaTime;
            if (remaining <= 0f)
            {
                FinishWash();
            }
            UpdateTimerText();
        }
    }

    void LateUpdate()
    {
        // the timer always faces the player
        if (timerText != null && timerText.gameObject.activeSelf)
        {
            var _camera = GameController.instance != null ? GameController.instance.gameMainCamera : null;
            if (_camera != null)
            {
                timerText.transform.rotation = Quaternion.LookRotation(timerText.transform.position - _camera.transform.position, Vector3.up);
            }
        }
    }

    #region Interaction
    public void OnHoverItems()
    {
        var _pickup = NotePickupManager.instance;
        int _held = _pickup != null ? _pickup.HeldCount : 0;
        string _text;
        switch (state)
        {
            case State.Idle:
                _text = _held > 0 ? hoverWash + " (" + _held + ")" : hoverNoNotes;
                break;
            case State.Washing:
                _text = "Washing... " + Mathf.CeilToInt(Mathf.Max(0f, remaining)) + "s";
                break;
            case State.Done:
                _text = hoverTake;
                break;
            default:
                _text = "";
                break;
        }

        SetOutline(true);
        if (UIController.instance == null)
            return;
        UIController.instance.DisplayHoverObjectName(_text, !string.IsNullOrEmpty(_text), HoverInstructionType.General);
        // with notes in the hands the currency button is the interact button, otherwise the generic one is used
        bool _genericButton = _held == 0 && state == State.Done;
        UIController.instance.OnChangeInteraction(0, _genericButton);
    }

    public void TurnOffOutline()
    {
        SetOutline(false);
    }

    public void OnInteract()
    {
        if (state == State.Loading || state == State.Unloading)
            return;

        var _controller = GameController.instance;
        if (_controller != null && _controller.currentPicketItem != null)
        {
            Message(handsBusyMessage);
            return;
        }

        var _pickup = NotePickupManager.instance;
        int _held = _pickup != null ? _pickup.HeldCount : 0;
        switch (state)
        {
            case State.Idle:
                if (_held > 0)
                {
                    StartCoroutine(LoadRoutine());
                }
                else
                {
                    Message(needNotesMessage);
                }
                break;
            case State.Washing:
                Message("Washing... " + Mathf.CeilToInt(Mathf.Max(0f, remaining)) + "s left");
                break;
            case State.Done:
                if (_held > 0)
                {
                    Message(emptyHandsMessage);
                }
                else
                {
                    StartCoroutine(UnloadRoutine());
                }
                break;
        }
    }

    static void Message(string _text)
    {
        if (UIController.instance != null)
        {
            UIController.instance.DisplayInstructions(_text);
        }
    }

    void SetOutline(bool _enable)
    {
        if (outlines == null)
            return;
        for (int i = 0; i < outlines.Length; i++)
        {
            if (outlines[i] != null && outlines[i].enabled != _enable)
            {
                outlines[i].enabled = _enable;
            }
        }
    }
    #endregion

    #region Wash
    IEnumerator LoadRoutine()
    {
        state = State.Loading;
        SetOutline(false);
        yield return MoveDoor(true);

        var _pickup = NotePickupManager.instance;
        while (_pickup != null && _pickup.HeldCount > 0)
        {
            var _note = _pickup.RemoveTop();
            if (_note == null)
                break;
            StartCoroutine(FlyIn(_note));
            yield return new WaitForSeconds(noteInterval);
        }
        while (flying > 0)
        {
            yield return null;
        }

        yield return MoveDoor(false);
        if (inside.Count == 0)
        {
            state = State.Idle;
            yield break;
        }
        remaining = washDuration;
        state = State.Washing;
        UpdateTimerText();
    }

    IEnumerator FlyIn(PickableNote _note)
    {
        flying++;
        // the note leaves the hand pile and is moved by this routine only
        _note.itemCondition = ItemPickandPlace.ItemState.ready;
        _note.isPlayerPicked = false;
        _note.TurnOffOutline();
        var _transform = _note.transform;
        _transform.SetParent(drumPoint, true);

        Vector3 _from = _transform.position;
        Quaternion _fromRotation = _transform.rotation;
        Vector3 _fromScale = _transform.localScale;
        Vector3 _toScale = Vector3.one * (_note.normalScale * insideScale);
        float _time = 0f;
        while (_time < flyTime)
        {
            _time += Time.deltaTime;
            float _t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_time / flyTime));
            _transform.position = Vector3.Lerp(_from, drumPoint.position, _t);
            _transform.rotation = Quaternion.Slerp(_fromRotation, drumPoint.rotation, _t);
            _transform.localScale = Vector3.Lerp(_fromScale, _toScale, _t);
            yield return null;
        }
        // the note stays visible inside the drum (seen through the glass), the wash turns it around the drum
        inside.Add(new InsideNote
        {
            note = _note,
            phase = Random.Range(0f, 360f),
            depth = Random.Range(-0.08f, 0.08f),
            baseRotation = Random.rotation
        });
        flying--;
    }

    /// <summary>Moves every note on a circle around the drum axis (the axis points out of the door) and tumbles it.</summary>
    void UpdateOrbit()
    {
        for (int i = 0; i < inside.Count; i++)
        {
            var _inside = inside[i];
            if (_inside.note == null)
                continue;
            float _angle = orbitAngle + _inside.phase;
            float _rad = _angle * Mathf.Deg2Rad;
            var _transform = _inside.note.transform;
            _transform.localPosition = new Vector3(_inside.depth, Mathf.Cos(_rad) * orbitRadius, Mathf.Sin(_rad) * orbitRadius);
            _transform.localRotation = Quaternion.AngleAxis(_angle, Vector3.right) * _inside.baseRotation;
        }
    }

    void FinishWash()
    {
        remaining = 0f;
        for (int i = 0; i < inside.Count; i++)
        {
            if (inside[i].note != null)
            {
                inside[i].note.SetWashed(true);
            }
        }
        state = State.Done;
        StartCoroutine(BeepRoutine());
    }

    IEnumerator BeepRoutine()
    {
        if (beepClip == null)
        {
            beepClip = CreateBeep();
        }
        for (int i = 0; i < beepCount; i++)
        {
            if (SoundController.instance != null)
            {
                SoundController.instance.OnPlayInteractionSound(beepClip);
            }
            else
            {
                AudioSource.PlayClipAtPoint(beepClip, transform.position);
            }
            yield return new WaitForSeconds(0.35f);
        }
    }

    /// <summary>A short electronic beep (no audio asset needed).</summary>
    static AudioClip CreateBeep()
    {
        const int rate = 44100;
        const float length = 0.2f;
        const float frequency = 1400f;
        int _samples = Mathf.RoundToInt(rate * length);
        var _data = new float[_samples];
        for (int i = 0; i < _samples; i++)
        {
            float _fade = Mathf.Min(1f, Mathf.Min(i, _samples - i) / (rate * 0.01f));
            _data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / rate) * 0.5f * _fade;
        }
        var _clip = AudioClip.Create("Washing Machine Beep", _samples, 1, rate, false);
        _clip.SetData(_data, 0);
        return _clip;
    }

    IEnumerator UnloadRoutine()
    {
        state = State.Unloading;
        SetOutline(false);
        UpdateTimerText();
        yield return MoveDoor(true);

        var _pickup = NotePickupManager.instance;
        while (inside.Count > 0 && _pickup != null)
        {
            var _note = inside[0].note;
            if (_note == null)
            {
                inside.RemoveAt(0);
                continue;
            }
            if (_pickup.HeldCount >= _pickup.MaxNotes)
                break;

            // out of the drum: it flies from where it stopped to the hand
            if (!_pickup.TryAddToPile(_note))
                break;
            inside.RemoveAt(0);
            yield return new WaitForSeconds(noteInterval);
        }

        yield return MoveDoor(false);
        state = inside.Count > 0 ? State.Done : State.Idle;
        UpdateTimerText();
    }
    #endregion

    #region Door and timer
    IEnumerator MoveDoor(bool _open)
    {
        var _sound = _open ? doorOpenSound : doorCloseSound;
        if (_sound != null && SoundController.instance != null)
        {
            SoundController.instance.OnPlayInteractionSound(_sound);
        }
        float _from = doorAngle;
        float _to = _open ? doorOpenAngle : 0f;
        float _time = 0f;
        while (_time < doorMoveTime)
        {
            _time += Time.deltaTime;
            SetDoorAngle(Mathf.Lerp(_from, _to, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_time / doorMoveTime))));
            yield return null;
        }
        SetDoorAngle(_to);
    }

    void SetDoorAngle(float _angle)
    {
        doorAngle = _angle;
        if (door != null)
        {
            door.localRotation = Quaternion.Euler(0f, _angle, 0f);
        }
    }

    void UpdateTimerText()
    {
        if (timerText == null)
            return;
        switch (state)
        {
            case State.Washing:
                int _seconds = Mathf.CeilToInt(Mathf.Max(0f, remaining));
                timerText.text = (_seconds / 60).ToString("00") + ":" + (_seconds % 60).ToString("00");
                timerText.gameObject.SetActive(true);
                break;
            case State.Done:
                timerText.text = "DONE!";
                timerText.gameObject.SetActive(true);
                break;
            default:
                timerText.gameObject.SetActive(false);
                break;
        }
    }
    #endregion
}
