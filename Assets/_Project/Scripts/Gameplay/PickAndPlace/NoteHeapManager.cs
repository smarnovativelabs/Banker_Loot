using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

/// <summary>
/// Owns the heap of notes and the saving of every note in the world, through SerializationManager.
/// - Each note type has an entry: its prefab, how many notes the heap starts with and the most the heap may hold.
/// - Notes that are not lying on a NotePlaceSurface count as heap notes: the ones still in the heap, the ones in
///   the player's hands and the ones thrown anywhere in the environment. Only their count per type is saved, they
///   are dropped into the heap again on load.
/// - Notes placed on a surface are saved one by one (type, surface, position, rotation, tower order), so they come
///   back exactly where they were put and are not part of the heap count.
/// - Picking a note off a surface removes it from the placed saving, placing a heap note removes it from the heap
///   count, throwing a note adds it to the heap count again and a destroyed note leaves both. This falls out of
///   saving the live notes (PickableNote.All), so there is no per note bookkeeping to keep in sync.
/// The data is written by GameController's saving actions (auto save, pause menu, leaving the game).
/// </summary>
public class NoteHeapManager : MonoBehaviour
{
    public static NoteHeapManager instance;

    const string SaveBaseName = "NoteHeapData";
    /// <summary>Each level has its own heap save (level 1 keeps the plain name so older saves still load).</summary>
    static string SaveFileName => SaveNameFor(SaveBaseName);
    const int SaveVersion = 1;

    [Header("Levels")]
    [Tooltip("One LevelData per level, the one matching GameData.currentLevel is used (the first one when none matches)")]
    public List<LevelData> levels = new List<LevelData>();

    [Header("Heap")]
    [Tooltip("Fallback only: the heap point normally comes from the LevelData's Heap Point Prefab, which is spawned at runtime. Heap notes are parented to the point (also the thrown ones).")]
    public Transform heapRoot;
    [Tooltip("Size of the spawn circle: radius = note size x sqrt(note count) x this. Smaller = a tighter, taller pile (keeps notes away from tables).")]
    [Range(0.1f, 0.6f)] public float spreadFactor = 0.2f;
    [Tooltip("Largest radius (meters) of the spawn circle, 0 = no limit")]
    public float maxSpread = 0f;
    [Tooltip("Draw the notes that lie still in the heap with GPU instancing (far fewer draw calls). The notes stay normal objects.")]
    public bool useInstancing = true;
    [Tooltip("The floor is searched for from this far above the heap root")]
    public float liftHeight = 0.5f;
    [Tooltip("Notes already under the heap root are removed when the game starts, the manager spawns the heap itself")]
    public bool clearExistingHeapNotes = true;

    [Header("Loading Screen")]
    public string loadingMessage = "Loading Notes";

    /// <summary>The level data in use, chosen when the heap is initialized.</summary>
    public LevelData CurrentLevel { get; private set; }
    List<LevelData.NoteTypeEntry> noteTypes = new List<LevelData.NoteTypeEntry>();

    NoteHeapSaveData saveData;
    bool isReady;

    /// <summary>True once the heap was loaded and spawned, saving is blocked before that so an empty scene never overwrites the file.</summary>
    public bool IsReady => isReady;

    public const string NotesLayerName = "Notes";
    public const string PlayerLayerName = "Player";

    /// <summary>Layer of the currency notes (-1 when the layer does not exist yet, run Banker Loot/Setup Note Heap Saving).</summary>
    public static int NotesLayer => LayerMask.NameToLayer(NotesLayerName);

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // the notes and the player's capsule never touch, so the player walks through them and cannot push them
            int _notes = NotesLayer;
            int _player = LayerMask.NameToLayer(PlayerLayerName);
            if (_notes >= 0 && _player >= 0)
            {
                Physics.IgnoreLayerCollision(_notes, _player, true);
            }
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
            isReady = false;
        }
    }

    #region Lookup
    LevelData.NoteTypeEntry GetEntry(string _noteId)
    {
        if (string.IsNullOrEmpty(_noteId))
            return null;
        for (int i = 0; i < noteTypes.Count; i++)
        {
            if (noteTypes[i].noteId == _noteId && noteTypes[i].prefab != null)
                return noteTypes[i];
        }
        return null;
    }

    /// <summary>Notes of the given type that count as heap notes right now (everything not lying on a surface).</summary>
    public int GetHeapCount(string _noteId)
    {
        int _count = 0;
        for (int i = 0; i < PickableNote.All.Count; i++)
        {
            var _note = PickableNote.All[i];
            if (_note.noteId == _noteId && !IsPlaced(_note))
            {
                _count++;
            }
        }
        return _count;
    }

    static bool IsPlaced(PickableNote _note)
    {
        var _placed = _note.GetComponent<PlacedNote>();
        return _placed != null && !_placed.pickedUp && _placed.surface != null;
    }
    #endregion

    #region Load
    /// <summary>Called by GameController while the loading screen is up: loads the save, spawns the heap and the placed notes.</summary>
    public IEnumerator InitializeHeap()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.EnableLoadingScreen(true, loadingMessage);
        }
        yield return null;

        CurrentLevel = PickLevel();
        noteTypes = CurrentLevel != null ? CurrentLevel.noteTypes : new List<LevelData.NoteTypeEntry>();

        saveData = SerializationManager.LoadFile(SaveFileName) as NoteHeapSaveData;
        if (saveData != null && saveData.version != SaveVersion)
        {
            saveData = null;
        }

        SpawnPlayerPoint();
        SpawnHeapPoint();
        if (heapRoot == null)
        {
            heapRoot = transform;
        }
        if (clearExistingHeapNotes)
        {
            ClearExistingNotes();
        }

        SpawnPlacedNotes();
        yield return SpawnHeap();

        isReady = true;
        if (GameController.instance != null)
        {
            GameController.instance.AddSavingAction(SaveHeap);
        }
    }

    /// <summary>Spawns the level's player spawn point prefab and puts the player there.</summary>
    void SpawnPlayerPoint()
    {
        if (CurrentLevel == null || CurrentLevel.playerSpawnPointPrefab == null || Controlsmanager.instance == null)
            return;
        var _point = Instantiate(CurrentLevel.playerSpawnPointPrefab);
        _point.name = CurrentLevel.playerSpawnPointPrefab.name;
        Controlsmanager.instance.ChangePlayerPosition(_point.transform);
    }

    /// <summary>Spawns the level's heap point prefab and uses it as the heap root (the scene's own heapRoot is only a fallback).</summary>
    void SpawnHeapPoint()
    {
        if (CurrentLevel == null || CurrentLevel.heapPointPrefab == null)
            return;
        var _point = Instantiate(CurrentLevel.heapPointPrefab);
        _point.name = CurrentLevel.heapPointPrefab.name;
        heapRoot = _point.transform;
    }

    void ClearExistingNotes()
    {
        var _existing = heapRoot.GetComponentsInChildren<PickableNote>(true);
        for (int i = 0; i < _existing.Length; i++)
        {
            // removed from the registry right away, Destroy only takes effect at the end of the frame
            PickableNote.All.Remove(_existing[i]);
            Destroy(_existing[i].gameObject);
        }
    }

    IEnumerator SpawnHeap()
    {
        var _queue = new List<(LevelData.NoteTypeEntry entry, bool washed)>();
        for (int i = 0; i < noteTypes.Count; i++)
        {
            var _entry = noteTypes[i];
            if (_entry.prefab == null || string.IsNullOrEmpty(_entry.noteId))
                continue;

            int _count = _entry.spawnCount;
            int _washed = 0;
            if (saveData != null)
            {
                _count = 0;
                for (int j = 0; j < saveData.heapCounts.Count; j++)
                {
                    if (saveData.heapCounts[j].noteId == _entry.noteId)
                    {
                        _count = saveData.heapCounts[j].count;
                        _washed = saveData.heapCounts[j].washedCount;
                        break;
                    }
                }
            }
            _count = Mathf.Clamp(_count, 0, _entry.maxCount);
            for (int j = 0; j < _count; j++)
            {
                // the first notes of the type are the washed ones, the order is shuffled right after
                _queue.Add((_entry, j < _washed));
            }
        }
        Shuffle(_queue);

        // The notes are placed already lying on the ground in a pile (no physics drop), so nothing falls or collides
        // in front of the player: each note goes to a random spot around the root and on top of the notes below it.
        float _groundY = FindGroundY();
        var _placedNotes = new List<Vector3>(); // x, z, top height of each placed note
        var _spawned = new List<PickableNote>();
        var _spawnRadius = new List<float>();
        float _maxTop = _groundY;
        for (int i = 0; i < _queue.Count; i++)
        {
            var _prefab = _queue[i].entry.prefab;
            GetFootprint(_prefab, out float _radius, out float _thickness);

            // the pile widens with the amount of notes, so it grows in layers instead of one tall column
            float _spread = _radius * Mathf.Sqrt(_queue.Count) * spreadFactor;
            if (maxSpread > 0f)
                _spread = Mathf.Min(_spread, maxSpread);
            Vector2 _flat = Random.insideUnitCircle * _spread;
            float _x = heapRoot.position.x + _flat.x;
            float _z = heapRoot.position.z + _flat.y;

            // Each note lands on what is under it: it is dropped (ray casts) from above the pile onto the floor
            // or the notes below its footprint, so no note hangs in the air.
            Quaternion _rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GetHalfSize(_prefab, _thickness, out Vector3 _half);
            float _startY = Mathf.Max(_groundY, _maxTop) + _thickness + 0.05f;
            Vector3 _start = new Vector3(_x, _startY, _z);

            var _note = Instantiate(_prefab, _start, _rotation, heapRoot);
            _note.noteId = _queue[i].entry.noteId;
            _note.SetWashed(_queue[i].washed);
            var _body = _note.GetComponent<Rigidbody>();
            if (_body != null)
            {
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                _body.Sleep();
            }

            // the notes placed before are at their final place, make their colliders known to the physics queries
            Physics.SyncTransforms();
            // The note lands at the AVERAGE height under five points of its footprint (centre and four corners). Landing on
            // the highest point instead would build thin towers that grow into the air.
            float _sum = 0f;
            for (int s = 0; s < 5; s++)
            {
                Vector3 _offset = s == 0 ? Vector3.zero : new Vector3((s % 2 == 0 ? -1f : 1f) * _half.x * 0.7f, 0f, (s < 3 ? 1f : -1f) * _half.z * 0.7f);
                Vector3 _from = _start + _rotation * _offset;
                _sum += Physics.Raycast(_from, Vector3.down, out RaycastHit _hit, _startY - _groundY + 1f, ~0, QueryTriggerInteraction.Ignore)
                    ? Mathf.Max(_hit.point.y, _groundY)
                    : _groundY;
            }
            float _landY = _sum / 5f;
            float _y = _landY + _thickness * 0.5f + 0.002f;
            _note.transform.SetPositionAndRotation(new Vector3(_x, _y, _z), _rotation);
            if (_body != null)
            {
                // The pile is built already at rest, so its notes stay kinematic (no physics cost for ~1600 touching
                // bodies). A note becomes a normal falling body when it is thrown, or when the note it rests on is
                // picked (PickableNote.WakeNeighbours).
                _body.linearVelocity = Vector3.zero;
                _body.angularVelocity = Vector3.zero;
                _body.isKinematic = true;
            }
            _maxTop = Mathf.Max(_maxTop, _y + _thickness * 0.5f);
            _placedNotes.Add(new Vector3(_x, _z, _y + _thickness * 0.5f));
            _spawned.Add(_note);
            _spawnRadius.Add(_radius);
            if (i % 200 == 199)
                yield return null;
        }
        // the notes that lie still are drawn with GPU instancing (a few draw calls instead of one per note)
        if (useInstancing)
        {
            var _instancer = GetComponent<NotePileInstancer>();
            if (_instancer == null)
            {
                _instancer = gameObject.AddComponent<NotePileInstancer>();
            }
            _instancer.Begin(heapRoot);
        }
        yield return null;
    }


    /// <summary>Height of the floor under the lifted heap root (the root's own height when nothing is hit).</summary>
    float FindGroundY()
    {
        Vector3 _origin = heapRoot.position + Vector3.up * liftHeight;
        var _hits = Physics.RaycastAll(_origin, Vector3.down, liftHeight + 20f, ~0, QueryTriggerInteraction.Ignore);
        float _best = float.MaxValue;
        float _y = heapRoot.position.y;
        for (int i = 0; i < _hits.Length; i++)
        {
            // the notes of the old heap are still alive this frame, they are not the floor
            if (_hits[i].collider.GetComponentInParent<PickableNote>() != null)
                continue;
            if (_hits[i].distance < _best)
            {
                _best = _hits[i].distance;
                _y = _hits[i].point.y;
            }
        }
        return _y;
    }

    /// <summary>Radius (meters) other notes must stay out of horizontally, and the thickness of the note at its normal scale.</summary>
    /// <summary>Half size of the note's footprint (x, z at its normal scale) and half its thickness, for the drop cast.</summary>
    static void GetHalfSize(PickableNote _prefab, float _thickness, out Vector3 _half)
    {
        var _filter = _prefab.GetComponent<MeshFilter>();
        Vector3 _extents = _filter != null && _filter.sharedMesh != null ? _filter.sharedMesh.bounds.extents : new Vector3(0.1f, 0f, 0.05f);
        float _scale = _prefab.normalScale;
        _half = new Vector3(_extents.x * _scale, Mathf.Max(0.004f, _thickness * 0.5f), _extents.z * _scale);
    }

    static void GetFootprint(PickableNote _prefab, out float _radius, out float _thickness)
    {
        float _scale = _prefab.normalScale;
        var _filter = _prefab.GetComponent<MeshFilter>();
        var _box = _prefab.GetComponent<BoxCollider>();
        Vector3 _extents = _filter != null && _filter.sharedMesh != null ? _filter.sharedMesh.bounds.extents : new Vector3(0.1f, 0f, 0.05f);
        _radius = Mathf.Max(_extents.x, _extents.z) * _scale;
        _thickness = Mathf.Max(0.01f, _box != null ? _box.size.y * _scale : 0.016f);
    }

    /// <summary>The level of the open scene: the LevelData whose scene is loaded, else GameData.currentLevel, else 1.</summary>
    public static int GetActiveLevel()
    {
        if (instance != null)
        {
            string _scene = SceneManager.GetActiveScene().name;
            for (int i = 0; i < instance.levels.Count; i++)
            {
                var _data = instance.levels[i];
                if (_data != null && _data.SceneName == _scene)
                    return _data.level;
            }
        }
        return GameController.instance != null && GameController.instance.gameData != null
            ? GameController.instance.gameData.currentLevel
            : 1;
    }

    /// <summary>The LevelData of the active level, null when no manager or no matching data exists.</summary>
    public static LevelData GetActiveLevelData()
    {
        if (instance == null)
            return null;
        int _level = GetActiveLevel();
        for (int i = 0; i < instance.levels.Count; i++)
        {
            if (instance.levels[i] != null && instance.levels[i].level == _level)
                return instance.levels[i];
        }
        return null;
    }

    /// <summary>Save file name of the active level: level 1 = the base name, other levels get "_Level<n>".</summary>
    public static string SaveNameFor(string _baseName)
    {
        return SaveNameFor(_baseName, GetActiveLevel());
    }

    /// <summary>Save file name of the given level.</summary>
    public static string SaveNameFor(string _baseName, int _level)
    {
        return _level <= 1 ? _baseName : _baseName + "_Level" + _level;
    }

    /// <summary>The LevelData of the current level (GameData.currentLevel), the first assigned one when none matches.</summary>
    LevelData PickLevel()
    {
        int _level = GetActiveLevel();
        LevelData _first = null;
        for (int i = 0; i < levels.Count; i++)
        {
            if (levels[i] == null)
                continue;
            if (levels[i].level == _level)
                return levels[i];
            if (_first == null)
            {
                _first = levels[i];
            }
        }
        if (_first == null)
        {
            Debug.LogWarning("[NoteHeap] No LevelData assigned, the heap stays empty.");
        }
        return _first;
    }

    static void Shuffle<T>(List<T> _list)
    {
        for (int i = _list.Count - 1; i > 0; i--)
        {
            int _j = Random.Range(0, i + 1);
            (_list[i], _list[_j]) = (_list[_j], _list[i]);
        }
    }

    void SpawnPlacedNotes()
    {
        if (saveData == null)
            return;

        var _piles = new Dictionary<int, NotePile>();
        for (int i = 0; i < saveData.placed.Count; i++)
        {
            var _saved = saveData.placed[i];
            var _entry = GetEntry(_saved.noteId);
            var _surface = NotePlaceSurface.Find(_saved.surfaceId);
            if (_entry == null || _surface == null)
            {
                Debug.LogWarning("[NoteHeap] Skipped a saved note (" + _saved.noteId + " on " + _saved.surfaceId + "): type or surface not found.");
                continue;
            }

            var _note = Instantiate(_entry.prefab, heapRoot);
            _note.noteId = _entry.noteId;
            _note.SetWashed(_saved.washed);
            _note.transform.SetPositionAndRotation(_saved.position, _saved.rotation);
            var _body = _note.GetComponent<Rigidbody>();
            if (_body != null)
            {
                _body.isKinematic = true;
            }

            var _placed = _note.gameObject.AddComponent<PlacedNote>();
            _placed.surface = _surface;
            _placed.surfaceY = _surface.TopY;
            _placed.placedPosition = _saved.position;
            _placed.placedRotation = _saved.rotation;
            if (_saved.pileId >= 0)
            {
                if (!_piles.TryGetValue(_saved.pileId, out NotePile _pile))
                {
                    _pile = new NotePile();
                    _piles.Add(_saved.pileId, _pile);
                }
                _pile.Add(_placed);
            }
        }

        // a tower that lost notes (missing prefab / surface) can end up with a single note, that is a lone note
        foreach (var _pile in _piles.Values)
        {
            if (_pile.notes.Count == 1)
            {
                _pile.notes[0].pile = null;
            }
        }
    }
    #endregion

    #region Save
    /// <summary>Writes the heap counts and every placed note. Registered as a GameController saving action.</summary>
    public void SaveHeap()
    {
        if (!isReady)
            return;

        var _data = new NoteHeapSaveData { version = SaveVersion };
        var _counts = new Dictionary<string, int>();
        var _washedCounts = new Dictionary<string, int>();
        var _pileIds = new Dictionary<NotePile, int>();

        // placed notes in tower order (bottom first) so the towers are rebuilt the same way
        for (int i = 0; i < PlacedNote.All.Count; i++)
        {
            var _placed = PlacedNote.All[i];
            if (_placed.pickedUp || _placed.surface == null)
                continue;
            var _note = _placed.GetComponent<PickableNote>();
            if (_note == null || GetEntry(_note.noteId) == null)
                continue;

            int _pileId = -1;
            if (_placed.pile != null && !_pileIds.TryGetValue(_placed.pile, out _pileId))
            {
                _pileId = _pileIds.Count;
                _pileIds.Add(_placed.pile, _pileId);
                // the pile list holds the real order, so save the whole tower when its first note shows up
                for (int j = 0; j < _placed.pile.notes.Count; j++)
                {
                    AddPlaced(_data, _placed.pile.notes[j], _pileId);
                }
                continue;
            }
            if (_placed.pile != null)
                continue; // already written together with its tower

            AddPlaced(_data, _placed, -1);
        }

        for (int i = 0; i < PickableNote.All.Count; i++)
        {
            var _note = PickableNote.All[i];
            if (GetEntry(_note.noteId) == null || IsPlaced(_note))
                continue;
            _counts.TryGetValue(_note.noteId, out int _count);
            _counts[_note.noteId] = _count + 1;
            if (_note.IsWashed)
            {
                _washedCounts.TryGetValue(_note.noteId, out int _washed);
                _washedCounts[_note.noteId] = _washed + 1;
            }
        }
        foreach (var _pair in _counts)
        {
            _washedCounts.TryGetValue(_pair.Key, out int _washed);
            _data.heapCounts.Add(new NoteCountSave { noteId = _pair.Key, count = _pair.Value, washedCount = _washed });
        }

        saveData = _data;
        SerializationManager.Save(_data, SaveFileName);
    }

    static void AddPlaced(NoteHeapSaveData _data, PlacedNote _placed, int _pileId)
    {
        var _note = _placed.GetComponent<PickableNote>();
        if (_note == null || _placed.surface == null)
            return;
        _data.placed.Add(new PlacedNoteSave
        {
            noteId = _note.noteId,
            surfaceId = _placed.surface.Id,
            position = _placed.placedPosition,
            rotation = _placed.placedRotation,
            pileId = _pileId,
            washed = _note.IsWashed
        });
    }

    /// <summary>Deletes the saved heap, the next start spawns the configured amounts again.</summary>
    public void ResetSave()
    {
        saveData = null;
        SerializationManager.DeleteFile(SaveFileName);
    }
    #endregion
}

[Serializable]
public class NoteHeapSaveData
{
    public int version;
    /// <summary>Notes per type that are not on a surface (heap, hands, thrown).</summary>
    public List<NoteCountSave> heapCounts = new List<NoteCountSave>();
    /// <summary>Notes lying on a surface, towers are listed bottom to top.</summary>
    public List<PlacedNoteSave> placed = new List<PlacedNoteSave>();
}

[Serializable]
public class NoteCountSave
{
    public string noteId;
    public int count;
    /// <summary>How many of them were washed. Optional so saves from before the washing machine still load.</summary>
    [OptionalField]
    public int washedCount;
}

[Serializable]
public class PlacedNoteSave
{
    public string noteId;
    public string surfaceId;
    public Vector3 position;
    public Quaternion rotation;
    /// <summary>Notes with the same id (>= 0) form one tower, -1 for a note lying alone.</summary>
    public int pileId = -1;
    /// <summary>Optional so saves from before the washing machine still load.</summary>
    [OptionalField]
    public bool washed;
}
