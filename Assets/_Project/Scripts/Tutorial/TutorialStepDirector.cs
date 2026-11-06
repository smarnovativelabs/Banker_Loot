using UnityEngine;

/// <summary>
/// Runs the gameplay steps of the tutorial (the "Gameplay" area of the GameTutorialManager). For every step it
///  - points the 3D arrow at the thing to go to (note stack, washing machine, counting machine, table, locker screen, locker A01),
///  - puts the hand on the HUD button to press (the button is forced on screen so it can be pressed),
///  - and watches the game to complete the step when the player really did it (no events are needed in the gameplay code).
/// It also completes the "Move Around" step of the controls tutorial by watching the player move.
/// Steps are matched by their sub task id, the texts and order are edited on the Tutorial Manager.
/// </summary>
public class TutorialStepDirector : MonoBehaviour
{
    public const string GameplayTask = "Gameplay_T0";
    public const string ControlsTask = "Control_T0";

    [Header("Hand")]
    [Tooltip("The hand object (Anim Hand Obj), it is moved over the button to press")]
    public RectTransform handRoot;
    public Animator handAnimator;
    [Tooltip("Moves the hand from the middle of the button (the original Food game hand sits on the button as it is)")]
    public Vector2 handOffset = Vector2.zero;

    [Header("Rules")]
    [Tooltip("Meters the player has to walk to finish the Move Around step")]
    public float moveDistance = 1.5f;
    [Tooltip("Notes the player must hold to finish the Hold to pick fast step")]
    public int fastPickCount = 5;
    [Tooltip("The Walk to the locker screen step finishes at this distance (meters)")]
    public float boardDistance = 6f;
    [Tooltip("Size of the pointer (ring and arrow), 1 = the size of the prefab")]
    public float pointerScale = 0.3f;
    [Tooltip("The arrow is hidden when the player is closer than this (meters) to the target, so it never blocks the view")]
    public float arrowHideDistance = 1.8f;
    [Tooltip("The tip of the arrow stays this far (meters) above the top of its target")]
    public float pointerHeight = 0.2f;
    [Tooltip("The tip of the arrow never goes above this height (meters), so it stays under the roof")]
    public float maxArrowTop = 2.7f;

    GameTutorialManager tutorial;
    TutorialSubTask step;
    string stepId;
    string taskId;

    // targets of the current step
    Transform pointerTarget;
    RectTransform handTarget;
    bool handFollowsHand;           // the locker step: pick button until notes are in hand, then the Place button
    bool handShown;
    string handAnimPlaying;

    // things found for the current step
    WashingMachine washer;
    CashCountingMachine counter;
    NotePlaceSurface table;
    NotePlaceSurface lockerSurface;
    Transform board;

    // baselines of the current step
    Vector3 startPosition;
    bool sawBusy;
    int placedBaseline;

    void OnEnable()
    {
        GameTutorialManager.SubTaskDisplayed += OnStepDisplayed;
    }

    void OnDisable()
    {
        GameTutorialManager.SubTaskDisplayed -= OnStepDisplayed;
        ClearHelpers();
    }

    void ClearHelpers()
    {
        CurrencyInteractButton.ForceVisible = false;
        NotePlacementHud.ForceVisible = false;
        if (handRoot != null)
            handRoot.gameObject.SetActive(false);
        handShown = false;
        handAnimPlaying = null;
        handTarget = null;
        handFollowsHand = false;
        pointerTarget = null;
    }

    #region Step start
    void OnStepDisplayed(TutorialSubTask _step)
    {
        tutorial = GameTutorialManager.instance;
        step = _step;
        stepId = _step != null ? _step.subTaskId : null;
        taskId = stepId != null && stepId.StartsWith("Gameplay_") ? GameplayTask : ControlsTask;
        sawBusy = false;
        ClearHelpers();
        if (string.IsNullOrEmpty(stepId))
            return;

        FindTargets();
        startPosition = PlayerPosition();
        placedBaseline = CountPlaced();
        StartHelpers();
    }

    void FindTargets()
    {
        washer = FindFirstObjectByType<WashingMachine>();
        counter = FindFirstObjectByType<CashCountingMachine>();
        table = null;
        for (int i = 0; i < NotePlaceSurface.All.Count; i++)
        {
            if (NotePlaceSurface.All[i] != null && NotePlaceSurface.All[i].name == "Table")
            {
                table = NotePlaceSurface.All[i];
                break;
            }
        }
        var _lockers = FindFirstObjectByType<LockerManager>();
        lockerSurface = _lockers != null && _lockers.lockers.Count > 0 ? _lockers.lockers[0].surface : null;
        var _board = GameObject.Find("RequireAmount");
        board = _board != null ? _board.transform : null;
    }

    void StartHelpers()
    {
        switch (stepId)
        {
            case "Gameplay_Pick":
            case "Gameplay_PickHold":
                pointerTarget = NoteHeapManager.instance != null ? NoteHeapManager.instance.heapRoot : null;
                SetHand(CurrencyButtonRect(), true);
                break;
            case "Gameplay_Wash":
                pointerTarget = washer != null ? washer.transform : null;
                SetHand(CurrencyButtonRect(), true);
                break;
            case "Gameplay_WashWait":
                pointerTarget = washer != null ? washer.transform : null;
                break;
            case "Gameplay_WashTake":
                pointerTarget = washer != null ? washer.transform : null;
                SetHand(PickButtonRect(), false);
                break;
            case "Gameplay_Count":
                pointerTarget = counter != null ? counter.transform : null;
                SetHand(CurrencyButtonRect(), true);
                break;
            case "Gameplay_CountTake":
                pointerTarget = counter != null ? counter.transform : null;
                SetHand(PickButtonRect(), false);
                break;
            case "Gameplay_Place":
                pointerTarget = table != null ? table.transform : null;
                SetHand(PlaceButtonRect(), true);
                break;
            case "Gameplay_Board":
                pointerTarget = board;
                break;
            case "Gameplay_Locker":
                pointerTarget = lockerSurface != null ? lockerSurface.transform : null;
                handFollowsHand = true;
                break;
        }
        UpdatePointerPosition(true);
    }
    #endregion

    #region Per frame
    void Update()
    {
        tutorial = GameTutorialManager.instance;
        if (tutorial == null || tutorial.CurrentSubTask == null || tutorial.CurrentSubTask != step)
        {
            if (step != null && (tutorial == null || tutorial.CurrentSubTask == null))
            {
                step = null;
                stepId = null;
                ClearHelpers();
            }
            return;
        }
        if (CheckDone())
        {
            string _taskId = taskId;
            string _stepId = stepId;
            tutorial.OnSubTaskComplete(_taskId, _stepId);
        }
    }

    void LateUpdate()
    {
        if (step == null)
            return;
        UpdatePointerPosition(false);
        UpdateHand();
    }

    bool CheckDone()
    {
        var _pick = NotePickupManager.instance;
        switch (stepId)
        {
            case "Controls_T0_Move":
                return Vector3.Distance(PlayerPosition(), startPosition) >= moveDistance;
            case "Gameplay_Pick":
                return _pick != null && _pick.HeldCount >= 1;
            case "Gameplay_PickHold":
                return _pick != null && _pick.HeldCount >= Mathf.Min(fastPickCount, Mathf.Max(1, _pick.MaxNotes));
            case "Gameplay_Wash":
                return washer == null || washer.CurrentState == WashingMachine.State.Washing
                    || washer.CurrentState == WashingMachine.State.Done || washer.CurrentState == WashingMachine.State.Unloading;
            case "Gameplay_WashWait":
                return washer == null || washer.CurrentState == WashingMachine.State.Done
                    || washer.CurrentState == WashingMachine.State.Unloading;
            case "Gameplay_WashTake":
            {
                if (washer == null)
                    return true;
                if (washer.CurrentState != WashingMachine.State.Idle)
                    sawBusy = true;
                bool _washedInHand = _pick != null && _pick.TopNote != null && _pick.TopNote.IsWashed;
                return _washedInHand || (sawBusy && washer.CurrentState == WashingMachine.State.Idle);
            }
            case "Gameplay_Count":
                return counter == null || counter.CurrentState != CashCountingMachine.State.Idle;
            case "Gameplay_CountTake":
            {
                if (counter == null)
                    return true;
                if (counter.CurrentState != CashCountingMachine.State.Idle)
                    sawBusy = true;
                return sawBusy && counter.CurrentState == CashCountingMachine.State.Idle;
            }
            case "Gameplay_Place":
                return CountPlaced() > placedBaseline;
            case "Gameplay_Board":
                return board == null || CameraDistance(board.position) <= boardDistance;
            case "Gameplay_Locker":
                return lockerSurface == null || CountPlaced() > placedBaseline;
        }
        return false;
    }

    // notes put on the table (table step) or in the first locker (locker step)
    int CountPlaced()
    {
        NotePlaceSurface _surface = stepId == "Gameplay_Locker" ? lockerSurface : table;
        int _count = 0;
        for (int i = 0; i < PlacedNote.All.Count; i++)
        {
            var _placed = PlacedNote.All[i];
            if (_placed == null)
                continue;
            if (_surface == null || _placed.surface == _surface)
                _count++;
        }
        return _count;
    }
    #endregion

    #region Pointer and hand
    void UpdatePointerPosition(bool _force)
    {
        if (tutorial == null || tutorial.targetPointer == null)
            return;
        if (pointerTarget == null || !pointerTarget.gameObject.activeInHierarchy)
        {
            if (tutorial.targetPointer.activeSelf && stepId != null && stepId.StartsWith("Gameplay_"))
                tutorial.targetPointer.SetActive(false);
            return;
        }
        var _pointer = tutorial.targetPointer;
        if (!_pointer.activeSelf)
        {
            _pointer.SetActive(true);
            if (tutorial.targetArrow != null)
            {
                var _renderer = tutorial.targetArrow.GetComponent<MeshRenderer>();
                if (_renderer != null)
                    _renderer.enabled = true;
                var _outline = tutorial.targetArrow.GetComponent<Outline>();
                if (_outline != null)
                    _outline.enabled = true;
            }
        }
        // the pointer (ring on the floor + arrow above it) keeps a fixed size, only its place changes
        if (!Mathf.Approximately(_pointer.transform.localScale.x, pointerScale))
            _pointer.transform.localScale = Vector3.one * pointerScale;

        Bounds _bounds = BoundsOf(pointerTarget);
        // how far the tip of the arrow reaches above the pointer's own position
        float _rise = 1f;
        if (tutorial.targetArrow != null)
        {
            var _arrowRenderer = tutorial.targetArrow.GetComponent<MeshRenderer>();
            if (_arrowRenderer != null)
                _rise = Mathf.Max(0.2f, _arrowRenderer.bounds.max.y - _pointer.transform.position.y);
        }
        // the arrow just clears the top of the target, never goes below its base and never through the ceiling
        float _y = _bounds.max.y + pointerHeight - _rise;
        _y = Mathf.Max(_y, _bounds.min.y);
        _y = Mathf.Min(_y, maxArrowTop - _rise);
        _pointer.transform.position = new Vector3(_bounds.center.x, _y, _bounds.center.z);

        // close to the target the arrow would only block the view, the ring on the floor stays as the location sign
        if (tutorial.targetArrow != null)
        {
            var _arrowRenderer = tutorial.targetArrow.GetComponent<MeshRenderer>();
            if (_arrowRenderer != null)
            {
                var _camera = PlayerCamera();
                float _distance = _camera != null
                    ? Vector2.Distance(new Vector2(_camera.transform.position.x, _camera.transform.position.z), new Vector2(_pointer.transform.position.x, _pointer.transform.position.z))
                    : 99f;
                _arrowRenderer.enabled = _distance > arrowHideDistance;
            }
        }
    }

    void UpdateHand()
    {
        if (handRoot == null)
            return;
        RectTransform _target = handTarget;
        if (handFollowsHand)
        {
            // pick the notes up first, then press Place
            var _pick = NotePickupManager.instance;
            bool _holding = _pick != null && _pick.HeldCount > 0;
            _target = _holding ? PlaceButtonRect() : CurrencyButtonRect();
            CurrencyInteractButton.ForceVisible = !_holding;
            NotePlacementHud.ForceVisible = _holding;
        }
        if (_target == null)
        {
            if (handShown)
            {
                handRoot.gameObject.SetActive(false);
                handShown = false;
            }
            return;
        }
        if (!handShown)
        {
            handRoot.gameObject.SetActive(true);
            handShown = true;
            handAnimPlaying = null;
        }
        // the middle of the button (its position is only its pivot, the corner for the buttons in the screen corner)
        handRoot.position = _target.TransformPoint(_target.rect.center) + (Vector3)handOffset;
        handRoot.localEulerAngles = Vector3.zero;
        if (handAnimator != null && handAnimPlaying != "Tap")
        {
            handAnimator.Play("Tap");
            handAnimPlaying = "Tap";
        }
    }

    void SetHand(RectTransform _button, bool _forceButtonVisible)
    {
        handTarget = _button;
        if (_button == null)
            return;
        var _currency = CurrencyButtonRect();
        var _place = PlaceButtonRect();
        CurrencyInteractButton.ForceVisible = _forceButtonVisible && _button == _currency;
        NotePlacementHud.ForceVisible = _forceButtonVisible && _button == _place;
    }

    static RectTransform CurrencyButtonRect()
    {
        var _button = FindFirstObjectByType<CurrencyInteractButton>(FindObjectsInactive.Include);
        return _button != null ? (RectTransform)_button.transform : null;
    }

    static RectTransform PlaceButtonRect()
    {
        var _hud = FindFirstObjectByType<NotePlacementHud>(FindObjectsInactive.Include);
        return _hud != null && _hud.placeButton != null ? (RectTransform)_hud.placeButton.transform : null;
    }

    static RectTransform PickButtonRect()
    {
        return UIController.instance != null && UIController.instance.pickButton != null
            ? (RectTransform)UIController.instance.pickButton.transform : null;
    }
    #endregion

    #region Helpers
    static Camera PlayerCamera()
    {
        if (GameController.instance != null && GameController.instance.gameMainCamera != null)
            return GameController.instance.gameMainCamera;
        return Camera.main;
    }

    static Vector3 PlayerPosition()
    {
        var _camera = PlayerCamera();
        return _camera != null ? _camera.transform.position : Vector3.zero;
    }

    static float CameraDistance(Vector3 _point)
    {
        var _camera = PlayerCamera();
        return _camera != null ? Vector3.Distance(_camera.transform.position, _point) : 0f;
    }

    // box around everything under the transform (the arrow stands at its centre and hovers over its top)
    static Bounds BoundsOf(Transform _target)
    {
        var _bounds = new Bounds(_target.position, Vector3.zero);
        bool _has = false;
        foreach (var _renderer in _target.GetComponentsInChildren<Renderer>())
        {
            if (!_renderer.enabled || _renderer is ParticleSystemRenderer)
                continue;
            if (!_has)
            {
                _bounds = _renderer.bounds;
                _has = true;
            }
            else
            {
                _bounds.Encapsulate(_renderer.bounds);
            }
        }
        foreach (var _collider in _target.GetComponentsInChildren<Collider>())
        {
            if (!_has)
            {
                _bounds = _collider.bounds;
                _has = true;
            }
            else
            {
                _bounds.Encapsulate(_collider.bounds);
            }
        }
        return _has ? _bounds : new Bounds(_target.position, Vector3.zero);
    }
    #endregion
}
