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
    }

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

        float _distance = PlayerInteraction.instance != null ? PlayerInteraction.instance.envInteractionDistance : 4f;
        Transform _eye = _camera.transform;
        if (!Physics.Raycast(_eye.position, _eye.forward, out RaycastHit _hit, _distance, ~0, QueryTriggerInteraction.Ignore))
        {
            HidePreview();
            return;
        }

        // what is aimed at: a placed note (add to its tower) or an empty spot of a surface
        NotePlaceSurface _surface;
        PlacedNote _anchor;
        var _hitNote = _hit.collider.GetComponentInParent<PlacedNote>();
        if (_hitNote != null && _hitNote.surface != null)
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
            _anchor = FindNearbyBase(_surface, _hit.point);
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

        if (_anchor != null)
        {
            // on top of the tower, lined up with it
            int _count = _anchor.pile != null ? _anchor.pile.notes.Count : 1;
            Vector3 _basePosition = _anchor.transform.position;
            previewYaw = _anchor.transform.eulerAngles.y + userYaw;
            previewPosition = new Vector3(
                _basePosition.x,
                _surface.TopY + _halfThickness + surfaceOffset + _count * pileStep,
                _basePosition.z);
            previewValid = true;
        }
        else
        {
            // free spot: long edge across the view, like holding a bill in front of you
            Vector3 _extents = _filter.sharedMesh.bounds.extents;
            var _half = new Vector3(_extents.x * _scale, 0f, _extents.z * _scale);
            previewYaw = _eye.eulerAngles.y + 90f + userYaw;
            Vector3 _point = _hit.point;
            previewValid = _surface.ClampToFootprint(ref _point, _half, previewYaw);
            _point.y = _surface.TopY + _halfThickness + surfaceOffset;
            previewPosition = _point;
        }

        previewSurface = _surface;
        previewAnchor = _anchor;
        previewing = true;
        ShowGhost(_filter.sharedMesh, _scale);
    }

    /// <summary>The bottom note of the tower the note belongs to (the note itself when it lies alone).</summary>
    static PlacedNote BaseOf(PlacedNote _note)
    {
        return _note.pile != null && _note.pile.notes.Count > 0 ? _note.pile.notes[0] : _note;
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
            Vector3 _position = _base.transform.position;
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
                UIController.instance.DisplayInstructions(cannotPlaceMessage);
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
        cooldownUntil = Time.time + placeCooldown;
        HidePreview();
    }
    #endregion
}
