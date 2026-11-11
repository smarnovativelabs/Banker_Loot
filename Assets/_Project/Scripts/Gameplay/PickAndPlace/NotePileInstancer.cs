using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws the notes that lie still in the heap with GPU instancing instead of one draw call per note.
/// Every note stays a normal object (collider, rigidbody, PickableNote), so aiming, picking, saving and the locker
/// balancing work as before. A note that lies asleep in the heap only has its MeshRenderer switched off here and is
/// drawn together with all the notes of the same look in one instanced call (Graphics.RenderMeshInstanced).
/// A note gets its own renderer back the moment it is aimed at (outline), carried, thrown, awake, glowing or placed.
/// </summary>
[DefaultExecutionOrder(100)]
public class NotePileInstancer : MonoBehaviour
{
    [Tooltip("Seconds between two checks for notes that came to rest or started moving")]
    public float scanInterval = 0.15f;
    [Tooltip("A falling or thrown note slower than this (m/s) counts as calm")]
    public float calmSpeed = 0.08f;
    [Tooltip("Spin slower than this (rad/s) counts as calm")]
    public float calmAngularSpeed = 1.2f;
    [Tooltip("Seconds a note must stay calm before it is frozen and drawn with the pile again")]
    public float calmTime = 0.4f;
    [Tooltip("Instances per draw call")]
    public int chunkSize = 500;

    class Tracked
    {
        public PickableNote note;
        public MeshRenderer renderer;
        public MeshFilter filter;
        public Rigidbody body;
        public Outline outline;
        public bool instanced;
        public Group group;
        public Matrix4x4 matrix;
        public float calmTime;
    }

    class Group
    {
        public Mesh mesh;
        public Material material;       // runtime copy with GPU instancing on
        public readonly List<Tracked> members = new List<Tracked>();
        public Matrix4x4[] matrices = new Matrix4x4[0];
        public int count;
        public bool dirty = true;
    }

    readonly Dictionary<PickableNote, Tracked> tracked = new Dictionary<PickableNote, Tracked>();
    readonly List<PickableNote> removeList = new List<PickableNote>();
    readonly Dictionary<(Mesh, Material), Group> groups = new Dictionary<(Mesh, Material), Group>();
    readonly List<Material> copies = new List<Material>();
    Transform heapRoot;
    float nextScan;
    int layer;
    Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 50f);

    /// <summary>Starts instancing the notes of the heap that lies under the given root.</summary>
    public void Begin(Transform _heapRoot)
    {
        heapRoot = _heapRoot;
        nextScan = 0f;
        if (heapRoot != null)
        {
            bounds = new Bounds(heapRoot.position, Vector3.one * 60f);
        }
    }

    void OnDisable()
    {
        ReleaseAll();
    }

    void OnDestroy()
    {
        ReleaseAll();
        for (int i = 0; i < copies.Count; i++)
        {
            if (copies[i] != null)
                Destroy(copies[i]);
        }
        copies.Clear();
    }

    /// <summary>Gives every note its renderer back (used when the instancing stops).</summary>
    void ReleaseAll()
    {
        foreach (var _t in tracked.Values)
        {
            if (_t.renderer != null)
                _t.renderer.enabled = true;
        }
        tracked.Clear();
        groups.Clear();
    }

    void LateUpdate()
    {
        if (heapRoot == null)
            return;

        // the note that is aimed at must be drawn by its own renderer (the outline needs it), also between two scans
        var _pickup = NotePickupManager.instance;
        var _hovered = _pickup != null ? _pickup.HoveredNote : null;
        if (_hovered != null)
        {
            ActivateNow(_hovered);
            // aiming at any note of a tower highlights the top note of that tower
            ActivateNow(NotePickupManager.ResolvePickTarget(_hovered));
        }

        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + scanInterval;
            Scan();
        }
        RebuildDirty();
        Draw();
    }

    void ActivateNow(PickableNote _note)
    {
        if (_note != null && tracked.TryGetValue(_note, out Tracked _tracked) && _tracked.instanced)
        {
            Activate(_tracked);
        }
    }

    #region Scan
    void Scan()
    {
        // new notes
        var _all = PickableNote.All;
        for (int i = 0; i < _all.Count; i++)
        {
            var _note = _all[i];
            if (_note != null && !tracked.ContainsKey(_note))
            {
                Track(_note);
            }
        }
        // notes that are gone
        removeList.Clear();
        foreach (var _pair in tracked)
        {
            if (_pair.Key == null)
            {
                removeList.Add(_pair.Key);
            }
        }
        for (int i = 0; i < removeList.Count; i++)
        {
            tracked.Remove(removeList[i]);
        }
        if (removeList.Count > 0)
        {
            foreach (var _group in groups.Values)
            {
                _group.members.RemoveAll(_m => _m.note == null);
                _group.dirty = true;
            }
        }

        foreach (var _t in tracked.Values)
        {
            if (_t.note == null || _t.renderer == null)
                continue;
            FreezeWhenCalm(_t);
            bool _rest = CanInstance(_t);
            if (_rest && !_t.instanced)
            {
                Instance(_t);
            }
            else if (!_rest && _t.instanced)
            {
                Activate(_t);
            }
        }
    }

    /// <summary>
    /// A note that fell (its support was picked) or was thrown and now hardly moves is frozen again (kinematic), like the
    /// notes of the pile. Touching notes never fall fully asleep, so waiting for sleep would keep them out of the batches.
    /// </summary>
    void FreezeWhenCalm(Tracked _t)
    {
        var _body = _t.body;
        if (_body == null || _body.isKinematic || _t.note.isPlayerPicked || _t.note.transform.parent != heapRoot)
        {
            _t.calmTime = 0f;
            return;
        }
        if (_body.linearVelocity.sqrMagnitude < calmSpeed * calmSpeed && _body.angularVelocity.sqrMagnitude < calmAngularSpeed * calmAngularSpeed)
        {
            _t.calmTime += scanInterval;
            if (_t.calmTime >= calmTime && _t.note.GetComponent<PlacedNote>() == null)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                _body.isKinematic = true;
                _t.calmTime = 0f;
            }
        }
        else
        {
            _t.calmTime = 0f;
        }
    }

    void Track(PickableNote _note)
    {
        var _t = new Tracked
        {
            note = _note,
            renderer = _note.GetComponent<MeshRenderer>(),
            filter = _note.GetComponent<MeshFilter>(),
            body = _note.GetComponent<Rigidbody>(),
            outline = _note.GetComponent<Outline>()
        };
        tracked.Add(_note, _t);
        layer = _note.gameObject.layer;
        if (_t.renderer != null)
        {
            // the same lighting as the instanced draw: no per object probes and no shadow casting
            _t.renderer.lightProbeUsage = LightProbeUsage.Off;
            _t.renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _t.renderer.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    bool CanInstance(Tracked _t)
    {
        var _note = _t.note;
        if (_t.filter == null || _t.filter.sharedMesh == null || _t.body == null)
            return false;
        if (_note.isPlayerPicked || _note.IsGlowing)
            return false;
        if (_t.outline != null && _t.outline.enabled)
            return false;

        var _placed = _note.GetComponent<PlacedNote>();
        if (_placed != null)
        {
            // a note lying on a table or in a locker: drawn with the others once it has settled on its spot
            if (_placed.pickedUp || _placed.surface == null || !_t.body.isKinematic)
                return false;
            if ((_note.transform.position - _placed.placedPosition).sqrMagnitude > 0.000009f)
                return false;
            return _t.renderer.sharedMaterial != null;
        }

        // a heap note: resting means kinematic, or a falling body that came to sleep
        if (!_t.body.isKinematic && !_t.body.IsSleeping())
            return false;
        // notes in a machine or anywhere else are handled by their own renderers
        if (_note.transform.parent != heapRoot)
            return false;
        return _t.renderer.sharedMaterial != null;
    }
    #endregion

    #region State changes
    void Instance(Tracked _t)
    {
        var _mesh = _t.filter.sharedMesh;
        var _source = _t.renderer.sharedMaterial;
        if (!groups.TryGetValue((_mesh, _source), out Group _group))
        {
            var _copy = new Material(_source) { enableInstancing = true };
            copies.Add(_copy);
            _group = new Group { mesh = _mesh, material = _copy };
            groups.Add((_mesh, _source), _group);
        }
        _t.matrix = _t.note.transform.localToWorldMatrix;
        _t.group = _group;
        _t.instanced = true;
        _t.renderer.enabled = false;
        _group.members.Add(_t);
        _group.dirty = true;
    }

    void Activate(Tracked _t)
    {
        if (!_t.instanced)
            return;
        _t.instanced = false;
        if (_t.renderer != null)
            _t.renderer.enabled = true;
        if (_t.group != null)
        {
            _t.group.members.Remove(_t);
            _t.group.dirty = true;
        }
        _t.group = null;
    }

    void RebuildDirty()
    {
        foreach (var _group in groups.Values)
        {
            if (!_group.dirty)
                continue;
            _group.dirty = false;
            int _n = _group.members.Count;
            if (_group.matrices.Length < _n)
            {
                _group.matrices = new Matrix4x4[Mathf.NextPowerOfTwo(Mathf.Max(_n, 16))];
            }
            for (int i = 0; i < _n; i++)
            {
                _group.matrices[i] = _group.members[i].matrix;
            }
            _group.count = _n;
        }
    }
    #endregion

    void Draw()
    {
        foreach (var _group in groups.Values)
        {
            if (_group.count <= 0 || _group.mesh == null || _group.material == null)
                continue;
            var _params = new RenderParams(_group.material)
            {
                layer = layer,
                worldBounds = bounds,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = true,
                lightProbeUsage = LightProbeUsage.Off,
                reflectionProbeUsage = ReflectionProbeUsage.Off,
                motionVectorMode = MotionVectorGenerationMode.ForceNoMotion
            };
            for (int _start = 0; _start < _group.count; _start += chunkSize)
            {
                int _len = Mathf.Min(chunkSize, _group.count - _start);
                Graphics.RenderMeshInstanced(_params, _group.mesh, 0, _group.matrices, _len, _start);
            }
        }
    }
}
