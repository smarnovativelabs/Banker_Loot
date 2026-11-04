using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// The cash counting machine. It only takes washed notes, up to capacity (30) at a time:
/// - Interact while carrying notes: the washed ones fly into the machine one by one, and the screen counts up their
///   worth. Dirty notes stay in the hand.
/// - Interact with empty hands: the counted notes come back to the hand (as many as the hand holds) and the total
///   goes down by what was taken out.
/// The notes stay normal PickableNotes (hidden while inside), so the note saving keeps their washed state.
/// </summary>
public class CashCountingMachine : MonoBehaviour, ICarryInteractable
{
    public enum State
    {
        Idle,
        Counting,
        Ready,
        Unloading
    }

    [Header("Parts")]
    [Tooltip("The machine mesh, it trembles a little while it counts")]
    public Transform body;
    [Tooltip("Where a note lands in the feed tray on top of the machine, before it is pulled in")]
    public Transform hopperPoint;
    [Tooltip("Where the note passes the rollers, the screen counts it here")]
    public Transform rollerPoint;
    [Tooltip("Bottom of the output stack at the front: counted notes come out here and pile up")]
    public Transform outputPoint;
    [Tooltip("World text on the machine that shows the notes inside and their total worth")]
    public TMP_Text screenText;
    [Tooltip("Outlines shown while the machine is aimed at")]
    public Outline[] outlines;

    [Header("Counting")]
    [Tooltip("Notes the machine takes at a time")]
    public int capacity = 30;
    [Tooltip("Seconds between two notes entering the machine, like the real machine feeding one note after the other")]
    public float noteInterval = 0.22f;
    [Tooltip("Seconds a note takes to fly from the hand to the feed tray (and back to the hand when taken out)")]
    public float flyTime = 0.35f;
    [Tooltip("Seconds a note takes to be pulled through the rollers to the output stack")]
    public float feedTime = 0.45f;
    [Tooltip("Size of a note on the machine, as a fraction of its normal size (a full size note is wider than the machine)")]
    [Range(0.1f, 1f)] public float noteScaleOnMachine = 0.35f;
    [Tooltip("Height between two notes of the output stack")]
    public float stackStep = 0.0032f;

    [Header("Vibration")]
    [Tooltip("How far (meters) the machine trembles while it counts")]
    public float shakeAmplitude = 0.0035f;
    [Tooltip("Shakes per second")]
    public float shakeFrequency = 45f;
    [Tooltip("Seconds the trembling takes to build up and to calm down")]
    public float shakeRamp = 0.25f;
    [Tooltip("Extra strong shake when counting starts and ends")]
    public float kickStrength = 2f;

    [Header("Messages")]
    public string hoverCount = "Count Notes";
    public string hoverNoNotes = "Pick up washed notes to count them";
    public string hoverTake = "Take Counted Notes";
    public string handsBusyMessage = "Throw the item in your hand first!";
    public string needNotesMessage = "Pick up some washed notes to count them!";
    public string dirtyNotesMessage = "Only washed notes can be counted!";
    public string skippedDirtyMessage = "Dirty notes were not taken, wash them first!";
    public string fullMessage = "The machine is full!";

    readonly List<PickableNote> inside = new List<PickableNote>();
    State state = State.Idle;
    int total;
    int flying;
    int stackCount;
    Vector3 bodyBasePosition;
    bool shaking;
    float shakeLevel;
    float kick;
    AudioClip tickClip;
    AudioClip doneClip;
    AudioClip startClip;

    public State CurrentState => state;
    public int TotalWorth => total;
    public int NotesInside => inside.Count;

    void Start()
    {
        if (body != null)
        {
            bodyBasePosition = body.localPosition;
        }
        UpdateScreen();
    }

    void LateUpdate()
    {
        UpdateShake();

        // the screen always faces the player
        if (screenText != null && screenText.gameObject.activeSelf)
        {
            var _camera = GameController.instance != null ? GameController.instance.gameMainCamera : null;
            if (_camera != null)
            {
                screenText.transform.rotation = Quaternion.LookRotation(screenText.transform.position - _camera.transform.position, Vector3.up);
            }
        }
    }

    /// <summary>
    /// The machine trembles while it counts like the washing machine does: it builds up, with a stronger kick when
    /// counting starts and ends. The notes lying in the output tray tremble with it.
    /// </summary>
    void UpdateShake()
    {
        if (body == null)
            return;

        float _target = state == State.Counting ? 1f : 0f;
        shakeLevel = Mathf.MoveTowards(shakeLevel, _target, Time.deltaTime / Mathf.Max(0.01f, shakeRamp));
        kick = Mathf.MoveTowards(kick, 0f, Time.deltaTime / 0.4f);
        float _level = shakeLevel + kick * kickStrength;

        if (_level <= 0.001f)
        {
            if (shaking)
            {
                shaking = false;
                ApplyShake(Vector3.zero);
            }
            return;
        }

        shaking = true;
        float _time = Time.time * shakeFrequency;
        var _offset = new Vector3(
            Mathf.PerlinNoise(_time, 0.3f) - 0.5f,
            Mathf.PerlinNoise(_time, 4.1f) - 0.5f,
            Mathf.PerlinNoise(_time, 8.9f) - 0.5f) * (2f * shakeAmplitude * _level);
        ApplyShake(_offset);
    }

    void ApplyShake(Vector3 _offset)
    {
        body.localPosition = bodyBasePosition + _offset;
        // the stacked notes ride on the machine
        for (int i = 0; i < inside.Count; i++)
        {
            if (inside[i] != null)
            {
                inside[i].transform.position = outputPoint.position + Vector3.up * (i * stackStep) + _offset;
            }
        }
    }

    #region Interaction
    public void OnHoverItems()
    {
        var _pickup = NotePickupManager.instance;
        int _held = _pickup != null ? _pickup.HeldCount : 0;
        string _text;
        if (state == State.Counting || state == State.Unloading)
        {
            _text = "Counting...";
        }
        else if (_held > 0)
        {
            int _washed = CountWashed(_pickup);
            _text = _washed > 0 ? hoverCount + " (" + _washed + ")" : dirtyNotesMessage;
        }
        else
        {
            _text = inside.Count > 0 ? hoverTake + " (" + inside.Count + ")" : hoverNoNotes;
        }

        SetOutline(true);
        if (UIController.instance == null)
            return;
        UIController.instance.DisplayHoverObjectName(_text, true, HoverInstructionType.General);
        // with notes in the hands the currency button is the interact button, otherwise the generic one is used
        UIController.instance.OnChangeInteraction(0, _held == 0 && inside.Count > 0 && state == State.Ready);
    }

    public void TurnOffOutline()
    {
        SetOutline(false);
    }

    public void OnInteract()
    {
        if (state == State.Counting || state == State.Unloading)
            return;

        var _controller = GameController.instance;
        if (_controller != null && _controller.currentPicketItem != null)
        {
            Message(handsBusyMessage);
            return;
        }

        var _pickup = NotePickupManager.instance;
        if (_pickup == null)
            return;

        if (_pickup.HeldCount > 0)
        {
            // only the washed notes are taken, newest first, as many as the machine has room for
            var _take = new List<PickableNote>();
            var _held = _pickup.HeldNotes;
            int _dirty = 0;
            int _room = capacity - inside.Count;
            for (int i = _held.Count - 1; i >= 0; i--)
            {
                if (!_held[i].IsWashed)
                {
                    _dirty++;
                }
                else if (_take.Count < _room)
                {
                    _take.Add(_held[i]);
                }
            }

            if (_take.Count == 0)
            {
                Message(_room <= 0 && _dirty < _held.Count ? fullMessage : dirtyNotesMessage);
                return;
            }
            StartCoroutine(CountRoutine(_take, _dirty));
        }
        else if (inside.Count > 0)
        {
            StartCoroutine(UnloadRoutine());
        }
        else
        {
            Message(needNotesMessage);
        }
    }

    static int CountWashed(NotePickupManager _pickup)
    {
        int _washed = 0;
        var _held = _pickup.HeldNotes;
        for (int i = 0; i < _held.Count; i++)
        {
            if (_held[i].IsWashed)
            {
                _washed++;
            }
        }
        return _washed;
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

    #region Counting
    IEnumerator CountRoutine(List<PickableNote> _notes, int _dirtyLeft)
    {
        state = State.Counting;
        SetOutline(false);
        PlayClip(ref startClip, 330f, 0.3f, true);
        kick = 1f;
        var _pickup = NotePickupManager.instance;
        for (int i = 0; i < _notes.Count; i++)
        {
            if (_pickup == null || !_pickup.RemoveNote(_notes[i]))
                continue;
            StartCoroutine(FlyIn(_notes[i]));
            yield return new WaitForSeconds(noteInterval);
        }
        while (flying > 0)
        {
            yield return null;
        }

        state = inside.Count > 0 ? State.Ready : State.Idle;
        kick = 0.6f;
        PlayClip(ref doneClip, 1760f, 0.35f, true);
        UpdateScreen();
        if (_dirtyLeft > 0)
        {
            Message(skippedDirtyMessage);
        }
    }

    IEnumerator FlyIn(PickableNote _note)
    {
        flying++;
        // the note leaves the hand pile and is moved by this routine only
        _note.itemCondition = ItemPickandPlace.ItemState.ready;
        _note.isPlayerPicked = false;
        _note.TurnOffOutline();
        var _transform = _note.transform;
        _transform.SetParent(null, true);

        // every note gets its own place in the output stack, in the order the notes are fed
        int _slot = stackCount++;
        Vector3 _stackPosition = outputPoint.position + Vector3.up * (_slot * stackStep);
        // the notes lie flat with their long edge across the machine (a little crooked, like a real stack)
        Quaternion _flat = Quaternion.Euler(0f, body != null ? body.eulerAngles.y + Random.Range(-3f, 3f) : Random.Range(-3f, 3f), 0f);
        Vector3 _scale = Vector3.one * (_note.normalScale * noteScaleOnMachine);

        // 1. from the hand to the feed tray on top of the machine
        Vector3 _from = _transform.position;
        Quaternion _fromRotation = _transform.rotation;
        Vector3 _fromScale = _transform.localScale;
        float _time = 0f;
        while (_time < flyTime)
        {
            _time += Time.deltaTime;
            float _t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_time / flyTime));
            _transform.position = Vector3.Lerp(_from, hopperPoint.position, _t);
            _transform.rotation = Quaternion.Slerp(_fromRotation, _flat, _t);
            _transform.localScale = Vector3.Lerp(_fromScale, _scale, _t);
            yield return null;
        }

        // 2. the rollers pull it in and push it out at the front: tray -> rollers -> output stack
        _time = 0f;
        bool _counted = false;
        while (_time < feedTime)
        {
            _time += Time.deltaTime;
            float _t = Mathf.Clamp01(_time / feedTime);
            Vector3 _position = _t < 0.5f
                ? Vector3.Lerp(hopperPoint.position, rollerPoint.position, Mathf.SmoothStep(0f, 1f, _t * 2f))
                : Vector3.Lerp(rollerPoint.position, _stackPosition, Mathf.SmoothStep(0f, 1f, (_t - 0.5f) * 2f));
            _transform.position = _position;
            if (!_counted && _t >= 0.5f)
            {
                // the note passes the rollers: the screen counts it
                _counted = true;
                total += _note.Value;
                PlayClip(ref tickClip, 2400f, 0.05f, false);
                UpdateScreen();
            }
            yield return null;
        }
        _transform.position = _stackPosition;
        inside.Add(_note);
        flying--;
    }

    IEnumerator UnloadRoutine()
    {
        state = State.Unloading;
        SetOutline(false);
        var _pickup = NotePickupManager.instance;
        while (inside.Count > 0 && _pickup != null && _pickup.HeldCount < _pickup.MaxNotes)
        {
            int _last = inside.Count - 1;
            var _note = inside[_last];
            if (_note == null)
            {
                inside.RemoveAt(_last);
                continue;
            }

            // the top note of the output stack flies to the hand
            if (!_pickup.TryAddToPile(_note))
                break;
            inside.RemoveAt(_last);
            stackCount = inside.Count;
            total -= _note.Value;
            PlayClip(ref tickClip, 2400f, 0.05f, false);
            UpdateScreen();
            yield return new WaitForSeconds(noteInterval);
        }

        state = inside.Count > 0 ? State.Ready : State.Idle;
        UpdateScreen();
    }
    #endregion

    #region Screen and sounds
    void UpdateScreen()
    {
        if (screenText == null)
            return;
        bool _show = inside.Count > 0 || state == State.Counting;
        screenText.gameObject.SetActive(_show);
        if (_show)
        {
            screenText.text = "<size=45%>NOTES " + inside.Count + "/" + capacity + "</size>\n$" + total.ToString("N0");
        }
    }

    void PlayClip(ref AudioClip _clip, float _frequency, float _length, bool _decay)
    {
        if (_clip == null)
        {
            _clip = CreateTone(_frequency, _length, _decay);
        }
        if (SoundController.instance != null)
        {
            SoundController.instance.OnPlayInteractionSound(_clip);
        }
        else
        {
            AudioSource.PlayClipAtPoint(_clip, transform.position);
        }
    }

    /// <summary>A short electronic tone (no audio asset needed): a click for every counted note, a ding when done.</summary>
    static AudioClip CreateTone(float _frequency, float _length, bool _decay)
    {
        const int rate = 44100;
        int _samples = Mathf.RoundToInt(rate * _length);
        var _data = new float[_samples];
        for (int i = 0; i < _samples; i++)
        {
            float _envelope = _decay
                ? 1f - (float)i / _samples
                : Mathf.Min(1f, Mathf.Min(i, _samples - i) / (rate * 0.005f));
            _data[i] = Mathf.Sin(2f * Mathf.PI * _frequency * i / rate) * 0.4f * _envelope;
        }
        var _clip = AudioClip.Create("Counting Machine Tone", _samples, 1, rate, false);
        _clip.SetData(_data, 0);
        return _clip;
    }
    #endregion
}
