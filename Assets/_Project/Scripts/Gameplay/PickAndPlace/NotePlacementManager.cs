using UnityEngine;

/// <summary>
/// Dynamic placement of carried notes on NotePlaceSurface faces.
/// - While notes are carried and the crosshair is over a surface or over a placed note, a see-through copy of the
///   top carried note shows where it will go. The Place button puts it there, the rotate buttons turn it.
/// - Notes towers build themselves: a note placed on, or close to, other placed notes is added on top of that
///   tower, so there is no separate "make pile" step.
/// The HUD buttons are driven by NotePlacementHud, which only calls the public methods below.
/// </summary>
public class NotePlacementManager : MonoBehaviour
{
    public static NotePlacementManager instance;

    [Header("Preview")]
    [Tooltip("Transparent material used for the preview, tinted green / red")]
    public Material ghostMaterial;
    public Color validColor = new Color(0.5f, 1f, 0.5f, 0.6f);
    public Color invalidColor = new Color(1f, 0.35f, 0.35f, 0.6f);
    [Tooltip("Degrees one press of a rotate button turns the note")]
    public float rotateStep = 45f;
    [Tooltip("Seconds after placing a note before the next preview appears")]
    public float placeCooldown = 0.35f;
    [Tooltip("Small gap between the note and the surface it lies on")]
    public float surfaceOffset = 0.002f;

    [Tooltip("Seconds the Place button must stay down before it starts repeating")]
    public float holdDelay = 0.5f;
    [Tooltip("A press released within this time is a tap: it places one note and the circle never shows")]
    public float tapTime = 0.15f;
    [Tooltip("Seconds between two notes while the Place button is held")]
    public float holdPlaceInterval = 0.12f;

    [Header("Tower")]
    [Tooltip("A note placed within this distance (meters) of a placed note or tower is added on top of it")]
    public float autoPileRadius = 0.4f;
    [Tooltip("Height between two notes of a tower")]
    public float pileStep = 0.008f;

    [Header("Messages")]
    public string cannotPlaceMessage = "Not enough space to place the note here!";

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    GameObject ghost;
    MeshFilter ghostFilter;
    Material ghostInstance;
    NotePlaceSurface previewSurface;
    PlacedNote previewAnchor;
    Vector3 previewPosition;
    float previewYaw;
    float userYaw;
    bool previewing;
    bool previewValid;
    float cooldownUntil;
    bool placeHeld;
    bool holdRepeating;
    float placeDownTime;
    float nextHoldPlaceTime;
    // spot of a new stack, frozen when the Place button goes down so the first note can't drift with the crosshair
    bool spotLocked;
    NotePlaceSurface lockedSurface;
    Vector3 lockedPosition;
    float lockedBaseYaw;
    bool lockedValid;
    // tower that receives every note of the current hold
    PlacedNote holdAnchor;

    public bool IsPlaceHeld => placeHeld;
    /// <summary>0..1 while the Place button is held and the repeat has not started yet, otherwise 0.</summary>
    public float HoldProgress => IsHoldCharging ? Mathf.Clamp01((Time.time - placeDownTime - tapTime) / Mathf.Max(0.01f, holdDelay - tapTime)) : 0f;
    /// <summary>True from the moment the press is no longer a tap until the repeat starts, this is when the circle shows.</summary>
    public bool IsHoldCharging => placeHeld && !holdRepeating && Time.time - placeDownTime >= tapTime;
    public bool IsPreviewing => previewing;
    public bool PreviewValid => previewValid;

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
        if (ghost != null)
        {
            Destroy(ghost);
        }
    }

    void Update()
    {
        UpdatePreview();
        UpdateHold();
    }

    #region Hold to place
    /// <summary>Button pressed: a tap places one note (on release), a hold shows the circle and then keeps placing.</summary>
    public void OnPlaceDown()
    {
        placeHeld = true;
        holdRepeating = false;
        placeDownTime = Time.time;
        nextHoldPlaceTime = Time.time + holdDelay;
        // starting a new stack: freeze the spot until the first note is down
        holdAnchor = previewing ? previewAnchor : null;
        spotLocked = previewing && previewAnchor == null && previewSurface != null;
        if (spotLocked)
        {
            lockedSurface = previewSurface;
            lockedPosition = previewPosition;
            lockedBaseYaw = previewYaw - userYaw;
            lockedValid = previewValid;
        }
    }

    public void OnPlaceUp()
    {
        // a quick tap (released before the circle shows) places a single note
        if (placeHeld && !holdRepeating && Time.time - placeDownTime < tapTime)
        {
            ConfirmPlacement();
        }
        placeHeld = false;
        holdRepeating = false;
        spotLocked = false;
        holdAnchor = null;
    }

    void UpdateHold()
    {
        if (!placeHeld)
            return;
        if (NotePickupManager.instance == null || NotePickupManager.instance.HeldCount == 0)
        {
            placeHeld = false;
            holdRepeating = false;
            spotLocked = false;
            holdAnchor = null;
            return;
        }
        if (Time.time < nextHoldPlaceTime)
            return;
        // the circle is complete, from here on the notes keep coming
        bool _first = !holdRepeating;
        holdRepeating = true;
        if (previewing && (previewValid || _first))
        {
            // the first note also shows the "no space" message when the spot is blocked
            ConfirmPlacement();
            nextHoldPlaceTime = Time.time + holdPlaceInterval;
        }
    }
    #endregion

    #region Preview
    void UpdatePreview()
    {
        var _pickup = NotePickupManager.instance;
        var _controller = GameController.instance;
        var _camera = _controller != null ? _controller.gameMainCamera : null;
        var _note = _pickup != null ? _pickup.TopNote : null;
        if (_note == null || _camera == null || !_controller.gameStarted || _controller.currentPicketItem != null
            || Time.time < cooldownUntil)
        {
            HidePreview();
            return;
        }

        if (spotLocked)
        {
            var _lockedFilter = _note.GetComponent<MeshFilter>();
            if (_lockedFilter == null || _lockedFilter.sharedMesh == null)
            {
                HidePreview();
                return;
            }
            previewSurface = lockedSurface;
            previewAnchor = null;
            previewPosition = lockedPosition;
            previewYaw = lockedBaseYaw + userYaw;
            previewValid = lockedValid;
            previewing = true;
            ShowGhost(_lockedFilter.sharedMesh, _note.normalScale);
            return;
        }

        float _distance = PlayerInteraction.instance != null ? PlayerInteraction.instance.envInteractionDistance : 4f;
        Transform _eye = _camera.transform;
        // while the Place button is held, every note goes onto the tower the hold started / began, wherever the crosshair is
        if (holdAnchor != null && holdAnchor.surface == null)
        {
            holdAnchor = null;
        }
        PlacedNote _held = holdAnchor != null ? BaseOf(holdAnchor) : null;

        RaycastHit _hit = default;
        if (_held == null && !Physics.Raycast(_eye.position, _eye.forward, out _hit, _distance, ~0, QueryTriggerInteraction.Ignore))
        {
            HidePreview();
            return;
        }

        var _filter = _note.GetComponent<MeshFilter>();
        var _box = _note.GetComponent<BoxCollider>();
        if (_filter == null || _filter.sharedMesh == null)
        {
            HidePreview();
            return;
        }
        float _scale = _note.normalScale;
        float _halfThickness = _box != null ? _box.size.y * _scale * 0.5f : 0.008f;
        Vector3 _extents = _filter.sharedMesh.bounds.extents;
        bool _meshLongX = _extents.x >= _extents.z;

        // what is aimed at: a placed note (add to its tower) or an empty spot of a surface
        NotePlaceSurface _surface;
        PlacedNote _anchor;
        bool _gridCell = false;
        Vector3 _cellCenter = Vector3.zero;
        float _cellYaw = 0f;
        var _hitNote = _held == null ? _hit.collider.GetComponentInParent<PlacedNote>() : null;
        if (_held != null)
        {
            _surface = _held.surface;
            _anchor = _held;
        }
        else if (_hitNote != null && _hitNote.surface != null)
        {
            _surface = _hitNote.surface;
            _anchor = BaseOf(_hitNote);
        }
        else
        {
            _surface = _hit.collider.GetComponentInParent<NotePlaceSurface>();
            if (_surface == null || _hit.normal.y < 0.9f)
            {
                HidePreview();
                return;
            }
            if (_surface.snapToGrid)
            {
                // the table is divided into cells the size of a note: aim at a cell, the note snaps to its centre
                float _long = Mathf.Max(_extents.x, _extents.z) * 2f * _scale;
                float _short = Mathf.Min(_extents.x, _extents.z) * 2f * _scale;
                _gridCell = _surface.TryGetGridCell(_hit.point, _long, _short, out _cellCenter, out float _axisYaw, out Vector2 _cellSize);
                if (_gridCell)
                {
                    _cellYaw = _axisYaw + (_meshLongX ? 0f : -90f);
                    // the note can lie either way round, take the side that reads the right way up from the player
                    float _faceYaw = _eye.eulerAngles.y + (_meshLongX ? 0f : 90f);
                    if (Mathf.Abs(Mathf.DeltaAngle(_cellYaw, _faceYaw)) > 90f)
                    {
                        _cellYaw += 180f;
                    }
                    _anchor = FindBaseAtCell(_surface, _cellCenter, Mathf.Min(_cellSize.x, _cellSize.y) * 0.5f);
                }
                else
                {
                    _anchor = null;
                }
            }
            else
            {
                _anchor = FindNearbyBase(_surface, _hit.point);
            }
        }

        if (_anchor != null)
        {
            // on top of the tower, lined up with it
            int _count = _anchor.pile != null ? _anchor.pile.notes.Count : 1;
            // the settled pose, not the transform: a note placed a moment ago is still gliding to its spot
            Vector3 _basePosition = _anchor.placedPosition;
            previewYaw = _anchor.placedRotation.eulerAngles.y + userYaw;
            previewPosition = new Vector3(
                _basePosition.x,
                _surface.TopY + _halfThickness + surfaceOffset + _count * pileStep,
                _basePosition.z);
            previewValid = true;
        }
        else if (_surface.snapToGrid)
        {
            // empty grid cell, the note lies in the middle of it (no free cell left under the crosshair = not valid)
            previewYaw = _cellYaw;
            previewValid = _gridCell;
            previewPosition = new Vector3(
                _gridCell ? _cellCenter.x : _hit.point.x,
                _surface.TopY + _halfThickness + surfaceOffset,
                _gridCell ? _cellCenter.z : _hit.point.z);
        }
        else
        {
            // free spot: long edge across the view, like holding a bill in front of you
            var _half = new Vector3(_extents.x * _scale, 0f, _extents.z * _scale);
            // the long edge of the note mesh (X or Z depending on the model) goes across the view
            float _alongX = _extents.x >= _extents.z ? 0f : 90f;
            previewYaw = _eye.eulerAngles.y + _alongX + userYaw;
            Vector3 _point = _hit.point;
            previewValid = _surface.ClampToFootprint(ref _point, _half, previewYaw);
            _point.y = _surface.TopY + _halfThickness + surfaceOffset;
            previewPosition = _point;
        }

        // a surface can refuse a note (a locker that only takes its amount)
        if (previewValid && !string.IsNullOrEmpty(RejectReason(_surface, _note)))
        {
            previewValid = false;
        }

        previewSurface = _surface;
        previewAnchor = _anchor;
        previewing = true;
        ShowGhost(_filter.sharedMesh, _scale);
    }

    static string RejectReason(NotePlaceSurface _surface, PickableNote _note)
    {
        return _surface != null && _surface.rejectReason != null ? _surface.rejectReason(_note) : null;
    }

    /// <summary>The bottom note of the tower the note belongs to (the note itself when it lies alone).</summary>
    static PlacedNote BaseOf(PlacedNote _note)
    {
        return _note.pile != null && _note.pile.notes.Count > 0 ? _note.pile.notes[0] : _note;
    }

    /// <summary>The tower (or single note) lying in the grid cell around the point, null when the cell is empty.</summary>
    PlacedNote FindBaseAtCell(NotePlaceSurface _surface, Vector3 _cellCenter, float _tolerance)
    {
        PlacedNote _best = null;
        float _bestDistance = _tolerance;
        foreach (var _other in PlacedNote.All)
        {
            if (_other.surface != _surface)
                continue;
            var _base = BaseOf(_other);
            Vector3 _position = _base.placedPosition;
            float _flat = new Vector2(_position.x - _cellCenter.x, _position.z - _cellCenter.z).magnitude;
            if (_flat <= _bestDistance)
            {
                _bestDistance = _flat;
                _best = _base;
            }
        }
        return _best;
    }

    /// <summary>The nearest placed note / tower on the surface within autoPileRadius, null when the spot is free.</summary>
    PlacedNote FindNearbyBase(NotePlaceSurface _surface, Vector3 _point)
    {
        PlacedNote _best = null;
        float _bestDistance = autoPileRadius;
        foreach (var _other in PlacedNote.All)
        {
            if (_other.surface != _surface)
                continue;
            var _base = BaseOf(_other);
            Vector3 _position = _base.placedPosition;
            float _flat = new Vector2(_position.x - _point.x, _position.z - _point.z).magnitude;
            if (_flat <= _bestDistance)
            {
                _bestDistance = _flat;
                _best = _base;
            }
        }
        return _best;
    }

    void ShowGhost(Mesh _mesh, float _scale)
    {
        if (ghost == null)
        {
            ghost = new GameObject("Note Placement Ghost");
            ghostFilter = ghost.AddComponent<MeshFilter>();
            var _renderer = ghost.AddComponent<MeshRenderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
            ghostInstance = ghostMaterial != null ? new Material(ghostMaterial) : CreateFallbackMaterial();
            _renderer.sharedMaterial = ghostInstance;
        }
        if (ghostFilter.sharedMesh != _mesh)
        {
            ghostFilter.sharedMesh = _mesh;
        }
        ghost.transform.SetPositionAndRotation(previewPosition, Quaternion.Euler(0f, previewYaw, 0f));
        ghost.transform.localScale = Vector3.one * _scale;
        ghostInstance.SetColor(BaseColorId, previewValid ? validColor : invalidColor);
        if (!ghost.activeSelf)
        {
            ghost.SetActive(true);
        }
    }

    void HidePreview()
    {
        previewing = false;
        if (ghost != null && ghost.activeSelf)
        {
            ghost.SetActive(false);
        }
    }

    static Material CreateFallbackMaterial()
    {
        var _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        ConfigureTransparent(_material);
        return _material;
    }

    /// <summary>Sets up a URP Unlit material for alpha blending, also used by the editor setup.</summary>
    public static void ConfigureTransparent(Material _material)
    {
        _material.SetFloat("_Surface", 1f);
        _material.SetFloat("_Blend", 0f);
        _material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _material.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
        _material.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _material.SetFloat("_ZWrite", 0f);
        _material.SetFloat("_Cull", 0f);
        _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        _material.SetOverrideTag("RenderType", "Transparent");
        _material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        _material.SetShaderPassEnabled("ShadowCaster", false);
    }
    #endregion

    #region Buttons
    /// <summary>Turns the preview: +1 clockwise, -1 anti clockwise (like the food game's rotate buttons).</summary>
    public void RotatePreview(float _direction)
    {
        // on a grid table the notes always lie the same way, turning them would break the cells
        if (previewSurface != null && previewSurface.snapToGrid)
            return;
        userYaw = Mathf.Repeat(userYaw + Mathf.Sign(_direction) * rotateStep, 360f);
    }

    /// <summary>Puts the top carried note down where the preview is; on or next to other notes it joins their tower.</summary>
    public void ConfirmPlacement()
    {
        if (!previewing)
            return;
        if (!previewValid)
        {
            if (UIController.instance != null)
            {
                // the surface may say why (e.g. a locker that needs less money), otherwise there is no space
                string _reason = NotePickupManager.instance != null ? RejectReason(previewSurface, NotePickupManager.instance.TopNote) : null;
                UIController.instance.DisplayInstructions(string.IsNullOrEmpty(_reason) ? cannotPlaceMessage : _reason);
            }
            return;
        }

        var _note = NotePickupManager.instance != null ? NotePickupManager.instance.RemoveTop() : null;
        if (_note == null)
            return;

        var _placed = _note.GetComponent<PlacedNote>();
        if (_placed == null)
        {
            _placed = _note.gameObject.AddComponent<PlacedNote>();
        }
        _placed.surface = previewSurface;
        _placed.surfaceY = previewSurface.TopY;
        _placed.pile = null;
        _placed.pickedUp = false;
        _placed.placedPosition = previewPosition;
        _placed.placedRotation = Quaternion.Euler(0f, previewYaw, 0f);
        if (previewAnchor != null)
        {
            var _pile = previewAnchor.pile;
            if (_pile == null)
            {
                _pile = new NotePile();
                _pile.Add(previewAnchor);
            }
            _pile.Add(_placed);
        }
        _note.PlaceAt(previewPosition, Quaternion.Euler(0f, previewYaw, 0f), _note.normalScale);

        var _sound = SoundController.instance;
        if (_sound != null && _sound.itemPlaceSound != null)
        {
            _sound.OnPlayInteractionSound(_sound.itemPlaceSound);
        }
        userYaw = 0f;
        spotLocked = false;
        if (placeHeld)
        {
            // the first note of a new stack becomes the anchor for the rest of the hold
            holdAnchor = previewAnchor != null ? previewAnchor : _placed;
        }
        // while the button is held the next note follows right away
        cooldownUntil = placeHeld ? 0f : Time.time + placeCooldown;
        HidePreview();
    }
    #endregion
}
