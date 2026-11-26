#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Sets up level 3 (Haunted Mansion): the Locker_Manager for the locker wall (one body "Locker_1" with 29 loose doors:
/// floor surface, place zone, amount label and amount per locker), the blocking colliders of the wall, and the
/// LevelData (Level_3) with its heap point / player spawn prefabs. Safe to run again: the amounts that were already
/// set are kept. Menu: Banker Loot > Setup Level 3 (Haunted Mansion).
/// </summary>
public static class HauntedMansionSetup
{
    const string ScenePath = "Assets/_Project/Scenes/Level_3 Haunted Mansion.unity";
    const string ManagerName = "Locker_Manager";
    const string BodyName = "Locker_1";
    const string DoorPrefix = "Locker_Door";
    const string DoorSoundPath = "Assets/_Project/Audio/SFX/close farm door.mp3";
    const string DataFolder = "Assets/_Project/Data/Level_3";
    const string Level2Path = "Assets/_Project/Data/Level_2/Level_2.asset";

    // where the heap of notes and the player start (the cash room, in front of the table and the machines)
    static readonly Vector3 HeapPosition = new Vector3(2.0f, -0.105f, 2.8f);
    static readonly Vector3 PlayerPosition = new Vector3(4.0f, -0.1f, 1.5f);
    const float PlayerYaw = -57f; // looks at the heap
    // the machines of the mansion are turned -90 degrees around Y compared to the casino cash room they were copied from
    const float MachineYaw = -90f;

    [MenuItem("Banker Loot/Setup Level 3 (Haunted Mansion)")]
    public static void SetupAll()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            Debug.LogError("[BankerLoot] Open '" + ScenePath + "' first.");
            return;
        }
        SplitMergedDoors();
        SetupMachines();
        SetupLockers();
        SetupLockerColliders();
        SetupLevelData();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[BankerLoot] Level 3 set up. Save the scene.");
    }

    #region Machines
    /// <summary>
    /// The washing machine, the cash counting machine and the table of the scene were copied from the casino level and
    /// still point at the meshes of the casino cash room (which has no floor here). They are moved onto the machines and
    /// the desk of the mansion: the objects with the logic (drum, hopper, texts, collider) are carried along with the
    /// same offset and rotation, the references and the hover outlines are set to the mansion meshes. Runs only once.
    /// </summary>
    static void SetupMachines()
    {
        var body = FindBody();
        if (body == null)
            return;
        var props = body.parent;

        var washing = Object.FindFirstObjectByType<WashingMachine>();
        var washingBody = props.Find("Washing Machine Body");
        var washingDoor = props.Find("Washing Machine Door");
        if (washing != null && washingBody != null && washingDoor != null && washing.body != null && washing.body.parent != props)
        {
            var oldBody = washing.body;
            var oldDoor = washing.door;
            MoveWithMachine(washing.transform, oldBody, washingBody);
            washing.outlines = MoveOutlines(washing.outlines, new[] { oldBody, oldDoor }, new[] { washingBody, washingDoor });
            washing.body = washingBody;
            washing.door = washingDoor;
            EditorUtility.SetDirty(washing);
            Debug.Log("[BankerLoot] Washing machine moved onto the mansion machine.");
        }

        var counter = Object.FindFirstObjectByType<CashCountingMachine>();
        var counterBody = props.Find("Cash Counting Machine");
        if (counter != null && counterBody != null && counter.body != null && counter.body.parent != props)
        {
            var oldBody = counter.body;
            MoveWithMachine(counter.transform, oldBody, counterBody);
            counter.outlines = MoveOutlines(counter.outlines, new[] { oldBody }, new[] { counterBody });
            counter.body = counterBody;
            EditorUtility.SetDirty(counter);
            Debug.Log("[BankerLoot] Cash counting machine moved onto the mansion machine.");
        }

        // the note table: the top of the desk, right of the counting machine
        var desk = props.Find("Desk");
        var table = GameObject.Find("Table");
        if (desk != null && table != null && table.GetComponent<NotePlaceSurface>() != null)
        {
            var deskBounds = desk.GetComponent<MeshRenderer>().bounds;
            var box = table.GetComponent<BoxCollider>();
            table.transform.localScale = new Vector3(1.37f, 0.89f, 1f);
            // the collider is centred on the desk (right of the counting machine), its top face at the top of the desk
            Vector3 scale = table.transform.lossyScale;
            float centerX = deskBounds.center.x + 0.55f;
            float top = deskBounds.max.y;
            table.transform.position = new Vector3(
                centerX - box.center.x * scale.x,
                top - (box.center.y + box.size.y * 0.5f) * scale.y,
                deskBounds.center.z - box.center.z * scale.z);
            EditorUtility.SetDirty(table);
            Debug.Log("[BankerLoot] Table surface placed on the desk.");
        }
    }

    /// <summary>Moves the logic object like its machine moved: from the old body to the new one (the new one is turned by MachineYaw).</summary>
    static void MoveWithMachine(Transform _logic, Transform _oldBody, Transform _newBody)
    {
        var turn = Quaternion.Euler(0f, MachineYaw, 0f);
        Vector3 offset = turn * (_logic.position - _oldBody.position);
        _logic.SetPositionAndRotation(_newBody.position + offset, turn * _logic.rotation);
    }

    static Outline[] MoveOutlines(Outline[] _old, Transform[] _from, Transform[] _to)
    {
        var result = new List<Outline>();
        foreach (var outline in _old)
        {
            if (outline == null)
                continue;
            for (int i = 0; i < _from.Length; i++)
            {
                if (outline.transform != _from[i])
                    continue;
                var target = _to[i].GetComponent<Outline>();
                if (target == null)
                {
                    target = _to[i].gameObject.AddComponent<Outline>();
                    EditorUtility.CopySerialized(outline, target);
                }
                result.Add(target);
            }
        }
        return result.ToArray();
    }
    #endregion

    #region Merged doors
    /// <summary>
    /// One door mesh of the wall (Locker_Door_007_1) holds two doors in one mesh (the door of the cell A05 is part of it,
    /// so that cell has no door of its own). A door that wide cannot swing, so every separate piece of such a mesh becomes
    /// its own door object with its hinge at the left edge (like the other doors). The new meshes are saved next to the level data.
    /// </summary>
    static void SplitMergedDoors()
    {
        var body = FindBody();
        if (body == null)
            return;
        var wide = new List<Transform>();
        foreach (Transform t in body.parent)
        {
            var renderer = t.GetComponent<MeshRenderer>();
            if (t.name.StartsWith(DoorPrefix) && renderer != null && renderer.bounds.size.x > 1.5f)
            {
                wide.Add(t);
            }
        }
        foreach (var door in wide)
        {
            SplitDoor(door);
        }
    }

    static void SplitDoor(Transform _door)
    {
        var filter = _door.GetComponent<MeshFilter>();
        var renderer = _door.GetComponent<MeshRenderer>();
        var mesh = filter.sharedMesh;
        var vertices = mesh.vertices;

        // weld the vertices by position, the pieces are the triangle-connected groups
        var parent = new int[vertices.Length];
        for (int i = 0; i < parent.Length; i++)
            parent[i] = i;
        var weld = new Dictionary<Vector3Int, int>();
        for (int i = 0; i < vertices.Length; i++)
        {
            var key = Vector3Int.RoundToInt(vertices[i] * 1000f);
            if (weld.TryGetValue(key, out int other))
                Union(parent, i, other);
            else
                weld[key] = i;
        }
        var triangles = new List<int[]>();
        for (int s = 0; s < mesh.subMeshCount; s++)
        {
            var t = mesh.GetTriangles(s);
            triangles.Add(t);
            for (int i = 0; i < t.Length; i += 3)
            {
                Union(parent, t[i], t[i + 1]);
                Union(parent, t[i], t[i + 2]);
            }
        }

        var pieces = new Dictionary<int, List<int>>();
        for (int i = 0; i < vertices.Length; i++)
        {
            int root = Find(parent, i);
            if (!pieces.TryGetValue(root, out var list))
            {
                list = new List<int>();
                pieces[root] = list;
            }
            list.Add(i);
        }

        // pieces that touch or overlap (door and handle) belong to the same door
        var groups = new List<List<int>>();
        var groupBounds = new List<Bounds>();
        foreach (var piece in pieces.Values)
        {
            var b = new Bounds(_door.TransformPoint(vertices[piece[0]]), Vector3.zero);
            foreach (int v in piece)
                b.Encapsulate(_door.TransformPoint(vertices[v]));
            int target = -1;
            for (int g = 0; g < groups.Count; g++)
            {
                var expanded = groupBounds[g];
                expanded.Expand(0.05f);
                if (expanded.Intersects(b))
                {
                    target = g;
                    break;
                }
            }
            if (target < 0)
            {
                groups.Add(new List<int>(piece));
                groupBounds.Add(b);
            }
            else
            {
                groups[target].AddRange(piece);
                var merged = groupBounds[target];
                merged.Encapsulate(b);
                groupBounds[target] = merged;
            }
        }

        if (groups.Count < 2)
        {
            Debug.LogWarning("[BankerLoot] " + _door.name + " is wide but has only one piece, not split.");
            return;
        }

        // the group at the pivot of the door stays the door itself, the others become new doors
        int keep = 0;
        float best = float.MaxValue;
        for (int g = 0; g < groups.Count; g++)
        {
            float d = Mathf.Abs(groupBounds[g].min.x - _door.position.x);
            if (d < best)
            {
                best = d;
                keep = g;
            }
        }

        System.IO.Directory.CreateDirectory(DataFolder + "/Meshes");
        AssetDatabase.Refresh();

        string[] freeNames = { "Locker_Door_015_1" };
        int extra = 0;
        var created = new List<string>();
        Vector3 originalPosition = _door.position;
        for (int g = 0; g < groups.Count; g++)
        {
            var bounds = groupBounds[g];
            // new pivot: the left edge of the door, at its vertical centre and the z of the original pivot
            Vector3 pivot = g == keep ? originalPosition : new Vector3(bounds.min.x, bounds.center.y, originalPosition.z);
            string name = g == keep ? _door.name : (extra < freeNames.Length ? freeNames[extra] : _door.name + "_Part" + extra);
            if (g != keep)
                extra++;

            var part = BuildPart(mesh, vertices, triangles, groups[g], _door, pivot, originalPosition, name);
            string assetPath = DataFolder + "/Meshes/" + name + ".asset";
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(part, assetPath);

            GameObject go;
            if (g == keep)
            {
                go = _door.gameObject;
            }
            else
            {
                go = new GameObject(name);
                go.transform.SetParent(_door.parent, false);
                go.transform.SetPositionAndRotation(pivot, _door.rotation);
                go.transform.localScale = _door.localScale;
                go.layer = _door.gameObject.layer;
                GameObjectUtility.SetStaticEditorFlags(go, GameObjectUtility.GetStaticEditorFlags(_door.gameObject));
                go.AddComponent<MeshFilter>();
                var newRenderer = go.AddComponent<MeshRenderer>();
                EditorUtility.CopySerialized(renderer, newRenderer);
                Undo.RegisterCreatedObjectUndo(go, name);
            }
            go.GetComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(assetPath);
            created.Add(name);
        }
        Debug.Log("[BankerLoot] " + _door.name + " was one mesh with " + groups.Count + " doors, split into: " + string.Join(", ", created));
    }

    static Mesh BuildPart(Mesh _source, Vector3[] _vertices, List<int[]> _triangles, List<int> _group, Transform _door, Vector3 _pivotWorld, Vector3 _originalPivotWorld, string _name)
    {
        var remap = new Dictionary<int, int>();
        var positions = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var sourceNormals = _source.normals;
        var sourceUvs = _source.uv;
        _group.Sort();
        // the door has no rotation and scale 1 (checked by the caller's data), so a world offset is a local offset
        Vector3 shift = _originalPivotWorld - _pivotWorld;
        foreach (int v in _group)
        {
            remap[v] = positions.Count;
            positions.Add(_vertices[v] + shift);
            if (sourceNormals.Length == _vertices.Length)
                normals.Add(sourceNormals[v]);
            if (sourceUvs.Length == _vertices.Length)
                uvs.Add(sourceUvs[v]);
        }

        var mesh = new Mesh { name = _name };
        mesh.indexFormat = positions.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(positions);
        if (normals.Count == positions.Count)
            mesh.SetNormals(normals);
        if (uvs.Count == positions.Count)
            mesh.SetUVs(0, uvs);
        mesh.subMeshCount = _triangles.Count;
        for (int s = 0; s < _triangles.Count; s++)
        {
            var list = new List<int>();
            var t = _triangles[s];
            for (int i = 0; i < t.Length; i += 3)
            {
                if (remap.ContainsKey(t[i]))
                {
                    list.Add(remap[t[i]]);
                    list.Add(remap[t[i + 1]]);
                    list.Add(remap[t[i + 2]]);
                }
            }
            mesh.SetTriangles(list, s);
        }
        if (normals.Count != positions.Count)
            mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static int Find(int[] _parent, int _i)
    {
        while (_parent[_i] != _i)
        {
            _parent[_i] = _parent[_parent[_i]];
            _i = _parent[_i];
        }
        return _i;
    }

    static void Union(int[] _parent, int _a, int _b)
    {
        int ra = Find(_parent, _a);
        int rb = Find(_parent, _b);
        if (ra != rb)
            _parent[ra] = rb;
    }
    #endregion

    #region Lockers
    struct Cell
    {
        public Transform door;
        public int row;      // 0 = top
        public int column;   // 0 = left
        public string id;
    }

    static List<Cell> FindCells(Transform _root)
    {
        // the doors are loose in the mansion at first, once grouped they are "<id>_Door" under the Locker_Manager
        var doors = new List<Transform>();
        var searchRoots = new List<Transform> { _root };
        var managerObject = GameObject.Find(ManagerName);
        if (managerObject != null)
        {
            searchRoots.Add(managerObject.transform);
        }
        foreach (var searchRoot in searchRoots)
        {
            foreach (var t in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if ((t.name.StartsWith(DoorPrefix) || t.name.EndsWith("_Door")) && t.GetComponent<MeshRenderer>() != null && !doors.Contains(t))
                {
                    doors.Add(t);
                }
            }
        }

        var xs = new List<float>();
        var ys = new List<float>();
        foreach (var d in doors)
        {
            AddDistinct(xs, d.GetComponent<MeshRenderer>().bounds.center.x);
            AddDistinct(ys, d.GetComponent<MeshRenderer>().bounds.center.y);
        }
        xs.Sort();
        ys.Sort();
        ys.Reverse(); // top row first

        var cells = new List<Cell>();
        foreach (var d in doors)
        {
            var b = d.GetComponent<MeshRenderer>().bounds;
            int column = IndexOf(xs, b.center.x);
            int row = IndexOf(ys, b.center.y);
            // the names printed on the doors: H01 to H30, row by row from the top, from the left as the player sees the
            // wall (the doors face +Z, so the left is the highest X)
            int number = row * xs.Count + (xs.Count - column);
            cells.Add(new Cell { door = d, row = row, column = column, id = "H" + number.ToString("00") });
        }
        cells.Sort((a, b) => string.CompareOrdinal(a.id, b.id));
        return cells;
    }

    static void AddDistinct(List<float> _list, float _value)
    {
        foreach (var v in _list)
        {
            if (Mathf.Abs(v - _value) < 0.2f)
                return;
        }
        _list.Add(_value);
    }

    static int IndexOf(List<float> _list, float _value)
    {
        for (int i = 0; i < _list.Count; i++)
        {
            if (Mathf.Abs(_list[i] - _value) < 0.2f)
                return i;
        }
        return 0;
    }

    static void SetupLockers()
    {
        var body = FindBody();
        if (body == null)
        {
            Debug.LogError("[BankerLoot] '" + BodyName + "' not found.");
            return;
        }
        var cells = FindCells(body.parent);

        var managerObject = GameObject.Find(ManagerName);
        if (managerObject == null)
        {
            managerObject = new GameObject(ManagerName);
            Undo.RegisterCreatedObjectUndo(managerObject, ManagerName);
        }
        var manager = managerObject.GetComponent<LockerManager>();
        if (manager == null)
        {
            manager = managerObject.AddComponent<LockerManager>();
        }
        manager.doorCloseSound = AssetDatabase.LoadAssetAtPath<AudioClip>(DoorSoundPath);
        manager.doorOpenAngle = -100f;
        manager.billboardLabels = true;
        manager.fallbackOutward = Vector3.forward; // the doors face +Z

        var old = new Dictionary<string, LockerManager.LockerEntry>();
        foreach (var entry in manager.lockers)
        {
            if (!string.IsNullOrEmpty(entry.id))
            {
                old[entry.id] = entry;
            }
        }
        manager.lockers.Clear();

        var meshCollider = body.gameObject.AddComponent<MeshCollider>();
        Physics.SyncTransforms();
        try
        {
            for (int i = 0; i < cells.Count; i++)
            {
                var cell = cells[i];
                old.TryGetValue(cell.id, out var previous);
                var entry = new LockerManager.LockerEntry
                {
                    id = cell.id,
                    door = cell.door,
                    amount = previous != null ? previous.amount : 1500 + 50 * i
                };
                entry.surface = GetOrCreateSurface(managerObject.transform, cell, meshCollider);
                // the texts of the locker board (run 'Setup Locker Board (Haunted Mansion 30)') are kept, else a floating label is made
                entry.nameText = previous != null ? previous.nameText : null;
                entry.icon = previous != null ? previous.icon : null;
                entry.label = previous != null && previous.label != null ? previous.label : GetOrCreateLabel(managerObject.transform, cell, body);
                manager.lockers.Add(entry);
            }
        }
        finally
        {
            Object.DestroyImmediate(meshCollider);
        }

        // like the other levels: every door is a child of its locker object under the Locker_Manager, not static (it swings)
        foreach (var entry in manager.lockers)
        {
            GroupDoor(managerObject.transform, entry);
        }

        // objects of cells that do not exist (anymore) from an earlier run
        var valid = new HashSet<string>();
        foreach (var cell in cells)
        {
            valid.Add(cell.id + " Place Surface");
            valid.Add(cell.id + " Label");
        }
        for (int i = managerObject.transform.childCount - 1; i >= 0; i--)
        {
            var child = managerObject.transform.GetChild(i);
            bool emptyOldGroup = child.childCount == 0 && child.name.Length == 3 && child.name[0] == 'H' && !valid.Contains(child.name + " Place Surface");
            if (emptyOldGroup || ((child.name.EndsWith(" Place Surface") || child.name.EndsWith(" Label")) && !valid.Contains(child.name)))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        EditorUtility.SetDirty(manager);
        Debug.Log("[BankerLoot] " + manager.lockers.Count + " lockers set up in " + ManagerName + ", total amount " + TotalAmount(manager));
    }

    /// <summary>Moves the door under an empty locker object (named by the id, at the hinge of the door) of the manager and names it "A01_Door".</summary>
    static void GroupDoor(Transform _manager, LockerManager.LockerEntry _entry)
    {
        var door = _entry.door;
        // the doors are children of the imported mansion model: Unity does not allow moving a part of a model instance,
        // so the model is unpacked in the scene (its meshes and materials stay the same assets)
        if (PrefabUtility.IsPartOfPrefabInstance(door))
        {
            var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(door.gameObject);
            Debug.Log("[BankerLoot] Unpacking '" + instanceRoot.name + "' so the doors can be moved under the Locker_Manager.");
            PrefabUtility.UnpackPrefabInstance(instanceRoot, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        var group = _manager.Find(_entry.id);
        if (group == null)
        {
            var go = new GameObject(_entry.id);
            Undo.RegisterCreatedObjectUndo(go, _entry.id);
            go.transform.SetParent(_manager, false);
            go.transform.SetPositionAndRotation(door.position, Quaternion.identity);
            group = go.transform;
        }
        if (door.parent != group)
        {
            door.SetParent(group, true); // keeps the place of the door in the world
        }
        door.name = _entry.id + "_Door";
        GameObjectUtility.SetStaticEditorFlags(door.gameObject, 0);
        EditorUtility.SetDirty(door.gameObject);
    }

    public static int TotalAmount(LockerManager _manager)
    {
        int total = 0;
        foreach (var l in _manager.lockers)
        {
            total += l.amount;
        }
        return total;
    }

    static Transform FindBody()
    {
        foreach (var root in EditorSceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == BodyName)
                    return t;
            }
        }
        return null;
    }

    static NotePlaceSurface GetOrCreateSurface(Transform _manager, Cell _cell, MeshCollider _body)
    {
        string name = _cell.id + " Place Surface";
        var existing = _manager.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(_manager, false);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var box = go.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = go.AddComponent<BoxCollider>();
        }
        FitFloor(_body, _cell.door, go.transform, box);

        var surface = go.GetComponent<NotePlaceSurface>();
        if (surface == null)
        {
            surface = go.AddComponent<NotePlaceSurface>();
        }
        surface.surfaceId = "Locker_" + _cell.id;
        surface.snapToGrid = true;
        EditorUtility.SetDirty(surface);
        return surface;
    }

    /// <summary>
    /// Measures the inside of the locker cell behind the door (the doors face +Z) with the temporary mesh collider of the
    /// wall body and fits the floor box and the place zone to it, like LockerManagerSetup does for single lockers.
    /// </summary>
    static void FitFloor(MeshCollider _body, Transform _door, Transform _floor, BoxCollider _box)
    {
        var doorBounds = _door.GetComponent<MeshRenderer>().bounds;
        float bodyZ = _body.bounds.center.z;
        Vector3 origin = new Vector3(doorBounds.center.x, doorBounds.center.y, bodyZ);
        Vector3 front = Vector3.forward;

        if (!_body.Raycast(new Ray(origin, Vector3.down), out RaycastHit floorHit, 1f))
        {
            Debug.LogWarning("[BankerLoot] No floor found inside the locker of " + _door.name);
            return;
        }
        float floorY = floorHit.point.y;
        float ceilingY = _body.Raycast(new Ray(origin, Vector3.up), out RaycastHit ceilingHit, 1f) ? ceilingHit.point.y : floorY + 0.5f;
        Vector3 inside = new Vector3(origin.x, floorY + 0.2f, origin.z);

        float left = Wall(_body, inside, Vector3.left);
        float right = Wall(_body, inside, Vector3.right);
        float back = Wall(_body, inside, Vector3.back);

        // walk to the front until the floor changes height (the lip of the cell)
        float flat = 0f;
        for (float s = 0.01f; s < 1f; s += 0.01f)
        {
            var from = new Vector3(origin.x, floorY + 0.3f, origin.z) + front * s;
            if (!_body.Raycast(new Ray(from, Vector3.down), out RaycastHit h, 1f) || Mathf.Abs(h.point.y - floorY) > 0.008f)
                break;
            flat = s;
        }

        const float margin = 0.012f;
        float minX = origin.x - left + margin;
        float maxX = origin.x + right - margin;
        float minZ = origin.z - back + margin;
        float maxZ = origin.z + flat - margin;
        float across = maxX - minX;
        float depth = maxZ - minZ;
        float centerX = (minX + maxX) * 0.5f;
        float centerZ = (minZ + maxZ) * 0.5f;

        // the surface is 2 cm thick, its top face is the floor
        _floor.position = new Vector3(centerX, floorY - 0.01f, centerZ);
        _box.center = Vector3.zero;
        _box.size = new Vector3(across, 0.02f, depth);

        // the place zone fills the cell (and a bit of the opening in front), only the height of this cell so it never
        // reaches the cell above
        float height = Mathf.Max(0.3f, ceilingY - floorY - 0.02f);
        var zone = _floor.Find("Place Zone");
        if (zone == null)
        {
            zone = new GameObject("Place Zone").transform;
            zone.SetParent(_floor, false);
        }
        zone.rotation = Quaternion.identity;
        zone.localScale = Vector3.one;
        zone.position = new Vector3(centerX, floorY + height * 0.5f, centerZ + 0.075f);
        var zoneBox = zone.GetComponent<BoxCollider>();
        if (zoneBox == null)
        {
            zoneBox = zone.gameObject.AddComponent<BoxCollider>();
        }
        zoneBox.isTrigger = true;
        zoneBox.center = Vector3.zero;
        zoneBox.size = new Vector3(across + 0.04f, height, depth + 0.15f);
        zone.gameObject.layer = LockerColliderLayerSetup.EnsureLayer(AimLayers.PlaceZoneLayerName);
    }

    static float Wall(MeshCollider _collider, Vector3 _from, Vector3 _direction)
    {
        return _collider.Raycast(new Ray(_from, _direction), out RaycastHit hit, 2f) ? hit.distance : 0.3f;
    }

    /// <summary>World text in front of the top edge of the door that shows what the locker still needs.</summary>
    static TMP_Text GetOrCreateLabel(Transform _manager, Cell _cell, Transform _body)
    {
        string name = _cell.id + " Label";
        var existing = _manager.Find(name);
        TextMeshPro text;
        if (existing != null)
        {
            text = existing.GetComponent<TextMeshPro>();
        }
        else
        {
            var go = new GameObject(name);
            go.transform.SetParent(_manager, false);
            text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 1.1f;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 0.9f, 0.3f);
            text.rectTransform.sizeDelta = new Vector2(1f, 0.4f);
            text.text = "";
        }
        var doorBounds = _cell.door.GetComponent<MeshRenderer>().bounds;
        text.transform.position = new Vector3(doorBounds.center.x, doorBounds.max.y - 0.06f, doorBounds.center.z) + Vector3.forward * 0.2f;
        return text;
    }
    #endregion

    #region Colliders
    /// <summary>One invisible collider per column over the whole wall (thrown notes and the player cannot get inside).</summary>
    static void SetupLockerColliders()
    {
        var body = FindBody();
        if (body == null)
            return;
        var cells = FindCells(body.parent);
        // the "Locker_Colliders" of the scene are the ones of the casino level (copied with the scene, they stand across
        // the mansion at the desk): they are switched off, the colliders of this wall get their own root
        var stale = GameObject.Find("Locker_Colliders");
        if (stale != null)
        {
            for (int i = stale.transform.childCount - 1; i >= 0; i--)
            {
                if (stale.transform.GetChild(i).name.StartsWith("Haunted Locker Collider "))
                {
                    Object.DestroyImmediate(stale.transform.GetChild(i).gameObject);
                }
            }
            stale.SetActive(false);
        }
        var root = GameObject.Find("Haunted_Locker_Colliders");
        if (root == null)
        {
            root = new GameObject("Haunted_Locker_Colliders");
        }
        int layer = LockerColliderLayerSetup.EnsureLayer(AimLayers.LockerCollidersLayerName);
        var bodyBounds = body.GetComponent<MeshRenderer>().bounds;

        var columns = new Dictionary<int, Bounds>();
        foreach (var c in cells)
        {
            var b = c.door.GetComponent<MeshRenderer>().bounds;
            if (columns.TryGetValue(c.column, out var all))
            {
                all.Encapsulate(b);
                columns[c.column] = all;
            }
            else
            {
                columns[c.column] = b;
            }
        }
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            var child = root.transform.GetChild(i);
            if (child.name.StartsWith("Haunted Locker Collider ") && int.TryParse(child.name.Substring(24), out int number) && !columns.ContainsKey(number - 1))
            {
                Object.DestroyImmediate(child.gameObject);
            }
        }
        foreach (var pair in columns)
        {
            string name = "Haunted Locker Collider " + (pair.Key + 1).ToString("00");
            var existing = root.transform.Find(name);
            var go = existing != null ? existing.gameObject : new GameObject(name);
            go.transform.SetParent(root.transform, false);
            go.layer = layer;
            var box = go.GetComponent<BoxCollider>();
            if (box == null)
            {
                box = go.AddComponent<BoxCollider>();
            }
            // from the floor of the lowest cell to the roof of the highest, as deep as the body
            float bottom = pair.Value.min.y - 0.05f;
            float top = pair.Value.max.y + 0.05f;
            go.transform.position = new Vector3(pair.Value.center.x, (bottom + top) * 0.5f, bodyBounds.center.z);
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            box.center = Vector3.zero;
            box.size = new Vector3(0.92f, top - bottom, bodyBounds.size.z);
        }
        Debug.Log("[BankerLoot] " + columns.Count + " locker colliders set up.");
    }
    #endregion

    #region Level data
    static void SetupLevelData()
    {
        if (!AssetDatabase.IsValidFolder(DataFolder))
        {
            AssetDatabase.CreateFolder("Assets/_Project/Data", "Level_3");
        }
        var level2 = AssetDatabase.LoadAssetAtPath<LevelData>(Level2Path);

        string assetPath = DataFolder + "/Level_3.asset";
        var data = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<LevelData>();
            AssetDatabase.CreateAsset(data, assetPath);
        }

        var serialized = new SerializedObject(data);
        serialized.FindProperty("level").intValue = 3;
        serialized.FindProperty("displayName").stringValue = "Haunted Mansion";
        serialized.FindProperty("rewardCash").intValue = 600;
        serialized.FindProperty("scene").objectReferenceValue = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        data.heapPointPrefab = MakePointPrefab(DataFolder + "/Level_3_Note Heap Point.prefab", "Level_3_Note Heap Point", HeapPosition, 0f);
        data.playerSpawnPointPrefab = MakePointPrefab(DataFolder + "/Level_3_Player Spawn Point.prefab", "Level_3_Player Spawn Point", PlayerPosition, PlayerYaw);

        // the heap holds exactly what the lockers need (see the other levels): 1 x counts add up to the locker total
        data.noteTypes.Clear();
        int[] counts = { 410, 200, 500, 500, 130, 100 }; // 100, 50, 20, 10, 5, 1 dollar notes = 66750 (30 lockers: 1500 + 50 * index)
        for (int i = 0; i < level2.noteTypes.Count && i < counts.Length; i++)
        {
            var source = level2.noteTypes[i];
            data.noteTypes.Add(new LevelData.NoteTypeEntry
            {
                noteId = source.noteId,
                prefab = source.prefab,
                spawnCount = counts[i],
                maxCount = counts[i]
            });
        }
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log("[BankerLoot] Level_3 data ready: " + assetPath);
    }

    static GameObject MakePointPrefab(string _path, string _name, Vector3 _position, float _yaw)
    {
        var point = new GameObject(_name);
        point.transform.SetPositionAndRotation(_position, Quaternion.Euler(0f, _yaw, 0f));
        var prefab = PrefabUtility.SaveAsPrefabAsset(point, _path);
        Object.DestroyImmediate(point);
        return prefab;
    }
    #endregion
}
#endif
