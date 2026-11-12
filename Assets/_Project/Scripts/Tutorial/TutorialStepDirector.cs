using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runs the gameplay steps of the tutorial (the "Gameplay" area of the GameTutorialManager). For every step it
///  - marks the thing to go to with the location ring on the floor (note stack, washing machine, counting machine, table,
///    locker screen, locker A01); the pointer has a Target on it, so the off screen indicator shows the direction arrow
///    at the edge of the screen while the place is not in view (the same as in the Food and Motel games),
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
    [Tooltip("Size of the location ring on the floor, 1 = the size of the prefab")]
    public float pointerScale = 1f;
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

    static TutorialStepDirector active;
    static readonly string[] StepOrder =
    {
        "Controls_T0_Intro", "Gameplay_PickInfo", "Gameplay_Pick", "Gameplay_PickHold", "Gameplay_Wash", "Gameplay_WashWait",
        "Gameplay_WashTake", "Gameplay_CountInfo", "Gameplay_Count", "Gameplay_CountTake", "Gameplay_Board", "Gameplay_BoardInfo",
        "Gameplay_Locker", "Shop_Open", "Shop_Buy", "Shop_Bought", "Shop_Close", "Shop_Done"
    };

    public const string ShopTask = "Shop_T0";

    /// <summary>True while a step of the shop tutorial is on screen.</summary>
    public static bool IsRunning
    {
        get { return active != null && active.stepId != null && active.stepId.StartsWith("Shop_"); }
    }

    /// <summary>
    /// The shop cannot be opened until the tutorial of placing notes in the locker is finished. Returns true (and tells the
    /// player) when it is still locked; the caller must then not open the shop.
    /// </summary>
    public static bool BlocksShop()
    {
        var _tutorial = GameTutorialManager.instance;
        if (_tutorial == null || _tutorial.isTutorialComplete || _tutorial.IsAreaTutorialComplete("Gameplay"))
            return false;
        if (UIController.instance != null)
            UIController.instance.DisplayInstructions("Finish the tutorial first");
        return true;
    }

    /// <summary>
    /// While the tutorial runs, the washing / counting machine cannot be used before its step. Returns true (and tells
    /// the player to follow the tutorial) when the machine is not allowed yet; the caller must then skip the interaction.
    /// </summary>
    public static bool BlocksMachine(object _machine)
    {
        if (active == null || string.IsNullOrEmpty(active.stepId))
            return false;
        string _firstStep = _machine is WashingMachine ? "Gameplay_Wash" : _machine is CashCountingMachine ? "Gameplay_Count" : null;
        if (_firstStep == null)
            return false;
        int _now = System.Array.IndexOf(StepOrder, active.stepId);
        if (_now >= System.Array.IndexOf(StepOrder, _firstStep))
            return false;
        if (UIController.instance != null)
            UIController.instance.DisplayInstructions("Follow Tutorial instructions");
        return true;
    }

    void OnEnable()
    {
        active = this;
        // the store pauses the game, the hand must keep tapping
        if (handAnimator != null)
            handAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        GameTutorialManager.SubTaskDisplayed += OnStepDisplayed;
    }

    void OnDisable()
    {
        if (active == this)
            active = null;
        GameTutorialManager.SubTaskDisplayed -= OnStepDisplayed;
        ClearHelpers();
    }

    void ClearHelpers()
    {
        PickingUpgrade.TutorialFree = false;
        if (cutoutRefresh != null)
        {
            StopCoroutine(cutoutRefresh);
            cutoutRefresh = null;
        }
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
        taskId = stepId == null ? ControlsTask
            : stepId.StartsWith("Gameplay_") ? GameplayTask
            : stepId.StartsWith("Shop_") ? ShopTask : ControlsTask;
        sawBusy = false;
        ClearHelpers();
        if (string.IsNullOrEmpty(stepId))
            return;

        FindTargets();
        if (stepId.StartsWith("Shop_"))
            PrepareShopStep(_step);
        startPosition = PlayerPosition();
        placedBaseline = CountPlaced();
        StartHelpers();
    }

    #region Shop steps
    int upgradeBaseline;
    Coroutine cutoutRefresh;

    static ShopPanel Shop()
    {
        return UIController.instance != null ? UIController.instance.shopPanel : null;
    }

    /// <summary>
    /// Shop steps: the screen is dimmed with a hole over the one button to press (the dim blocks every other touch), and the
    /// hand taps it. The first upgrade is free so the player can always finish the step.
    /// </summary>
    void PrepareShopStep(TutorialSubTask _step)
    {
        var _shop = Shop();
        RectTransform _button = null;
        switch (stepId)
        {
            case "Shop_Open":
                _button = UIController.instance != null && UIController.instance.shopButton != null
                    ? (RectTransform)UIController.instance.shopButton.transform : null;
                break;
            case "Shop_Buy":
                upgradeBaseline = PickingUpgrade.Level;
                PickingUpgrade.TutorialFree = true;
                if (_shop != null && _shop.upgradeCard != null)
                {
                    _shop.upgradeCard.Refresh();
                    _button = _shop.upgradeCard.buyButton != null ? (RectTransform)_shop.upgradeCard.buyButton.transform : null;
                }
                break;
            case "Shop_Close":
                _button = _shop != null && _shop.closeButton != null ? (RectTransform)_shop.closeButton.transform : null;
                break;
        }
        if (_button == null)
            return;
        // the manager cuts the hole for this object (it reads the target right after the step is announced)
        _step.btnToBeCutOut = _button.gameObject;
        SetHand(_button, false);
        // the store has just opened: cut the hole again once its layout is in place
        if (stepId != "Shop_Open")
            cutoutRefresh = StartCoroutine(RefreshCutOut(_button));
    }

    System.Collections.IEnumerator RefreshCutOut(RectTransform _button)
    {
        yield return null;
        yield return null;
        Canvas.ForceUpdateCanvases();
        if (tutorial != null && _button != null && _button.gameObject.activeInHierarchy)
            tutorial.RefreshCutOut(_button);
        cutoutRefresh = null;
    }
    #endregion

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
            case "Shop_Open":
            {
                var _shop = Shop();
                return _shop == null || _shop.gameObject.activeInHierarchy;
            }
            case "Shop_Buy":
                return PickingUpgrade.IsMaxed || PickingUpgrade.Level > upgradeBaseline;
            case "Shop_Close":
            {
                var _shop = Shop();
                return _shop == null || !_shop.gameObject.activeInHierarchy;
            }
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
            _pointer.SetActive(true);
        // like in the Food game: the 3D arrow stays off, the ring on the floor is the location sign
        // and the direction arrows on the floor (below) lead the way
        if (tutorial.targetArrow != null)
        {
            var _arrowRenderer = tutorial.targetArrow.GetComponent<MeshRenderer>();
            if (_arrowRenderer != null && _arrowRenderer.enabled)
                _arrowRenderer.enabled = false;
        }
        if (!Mathf.Approximately(_pointer.transform.localScale.x, pointerScale))
            _pointer.transform.localScale = Vector3.one * pointerScale;

        // the ring lies on the floor at the target
        Bounds _bounds = BoundsOf(pointerTarget);
        _pointer.transform.position = new Vector3(_bounds.center.x, _bounds.min.y + 0.02f, _bounds.center.z);

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
        }
        // the Place button and its hand show only while aiming at a spot where a note can be placed
        if (_target != null && _target == PlaceButtonRect())
        {
            var _placer = NotePlacementManager.instance;
            NotePlacementHud.ForceVisible = false;
            // only while aiming at the surface of this step (locker A01), not the table or other lockers
            NotePlaceSurface _wanted = stepId == "Gameplay_Locker" ? lockerSurface : table;
            bool _onWanted = _placer != null && _wanted != null
                && (_placer.PreviewSurface == _wanted || (_placer.IsPlaceHeld && _placer.PreviewSurface == null && handWasOnWanted));
            handWasOnWanted = _onWanted || (_placer != null && _placer.IsPlaceHeld && handWasOnWanted);
            if (!_onWanted)
                _target = null;
        }
        // the grab button hand is shown only while a note is aimed at (the button itself shows then too)
        bool _isPick = _target != null && _target == PickButtonRect();
        if (_target != null && (_isPick || _target == CurrencyButtonRect()))
        {
            var _pickup = NotePickupManager.instance;
            // each step shows the hand only while the player aims at ITS object:
            // pick steps -> a note; wash steps -> the washing machine; count steps -> the counting machine
            // (putting notes in also needs notes in hand)
            var _player = FindFirstObjectByType<PlayerInteraction>();
            var _aimObj = _player != null ? _player.currentInteractingObj as MonoBehaviour : null;
            bool _holdingNotes = _pickup != null && _pickup.HeldCount > 0;
            bool _aimed = false;
            switch (stepId)
            {
                case "Gameplay_Pick":
                case "Gameplay_PickHold":
                    _aimed = _pickup != null && (_pickup.HoveredNote != null || _pickup.IsInteractHeld);
                    break;
                case "Gameplay_Wash":
                    _aimed = _holdingNotes && washer != null && _aimObj == washer;
                    break;
                case "Gameplay_WashTake":
                    _aimed = washer != null && _aimObj == washer;
                    break;
                case "Gameplay_Count":
                    _aimed = _holdingNotes && counter != null && _aimObj == counter;
                    break;
                case "Gameplay_CountTake":
                    _aimed = counter != null && _aimObj == counter;
                    break;
            }
            CurrencyInteractButton.ForceVisible = false;
            if (!_aimed)
                _target = null;
        }
        UpdateHoldEffect(_target);
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

    bool handWasOnWanted;   // the preview hides for a frame after each placement while Place is held
    Image holdRipple;
    Text holdLabel;

    /// <summary>On the hold step the grab button pulses outwards, the hand presses down and keeps holding, and a HOLD label shows.</summary>
    void UpdateHoldEffect(RectTransform _target)
    {
        bool _on = _target != null && stepId == "Gameplay_PickHold";
        if (_on && holdRipple == null)
        {
            var _src = _target.GetComponentInChildren<Image>();
            var _go = new GameObject("Hold Ripple", typeof(RectTransform), typeof(Image));
            _go.transform.SetParent(_target, false);
            var _rt = (RectTransform)_go.transform;
            _rt.sizeDelta = _target.rect.size;
            _rt.localPosition = _target.rect.center;
            holdRipple = _go.GetComponent<Image>();
            holdRipple.raycastTarget = false;
            if (_src != null) { holdRipple.sprite = _src.sprite; holdRipple.type = _src.type; }
            holdRipple.color = new Color(1f, 0.85f, 0.2f, 0f);

            var _lbl = new GameObject("Hold Label", typeof(RectTransform), typeof(Text));
            _lbl.transform.SetParent(_target, false);
            var _lrt = (RectTransform)_lbl.transform;
            _lrt.sizeDelta = new Vector2(260f, 60f);
            _lrt.localPosition = (Vector3)_target.rect.center + new Vector3(0f, _target.rect.height * 0.5f + 90f, 0f);
            holdLabel = _lbl.GetComponent<Text>();
            holdLabel.raycastTarget = false;
            holdLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            holdLabel.fontSize = 44;
            holdLabel.fontStyle = FontStyle.Bold;
            holdLabel.alignment = TextAnchor.MiddleCenter;
            holdLabel.text = "HOLD!";
            holdLabel.color = new Color(1f, 0.85f, 0.2f, 1f);
            var _ol = _lbl.AddComponent<UnityEngine.UI.Outline>();
            _ol.effectColor = Color.black;
            _ol.effectDistance = new Vector2(2f, -2f);
        }
        if (holdRipple != null)
        {
            holdRipple.gameObject.SetActive(_on);
            holdLabel.gameObject.SetActive(_on);
        }
        if (!_on)
        {
            if (handRoot != null) handRoot.localScale = Vector3.one;
            return;
        }
        float _t = Mathf.Repeat(Time.unscaledTime, 0.9f) / 0.9f;
        holdRipple.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.45f, _t);
        holdRipple.color = new Color(1f, 0.85f, 0.2f, (1f - _t) * 0.85f);
        // the hand presses down quickly and stays down, as if holding
        float _press = Mathf.Clamp01(Mathf.Repeat(Time.unscaledTime, 1.6f) / 0.2f);
        handRoot.localScale = Vector3.one * Mathf.Lerp(1f, 0.82f, _press);
        holdLabel.rectTransform.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(Time.unscaledTime * 10f));
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
