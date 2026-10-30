#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the pickable currency note prefabs in Assets/Dummy Assets (meshes + texture copied from the bank game)
/// and scatters a randomly rotated heap of them into the open scene. Both steps can be re-run from the menu.
/// </summary>
public static class DummyNotesSetup
{
    const string DummyRoot = "Assets/Dummy Assets";
    const string ModelPath = DummyRoot + "/Models/Counter_POS.fbx";
    const string TexturePath = DummyRoot + "/Textures/New_Currency.png";
    const string MaterialPath = DummyRoot + "/Materials/Currency Note.mat";
    const string PrefabFolder = DummyRoot + "/Prefabs";
    const string HeapName = "Note Heap";
    const string PickSoundPath = "Assets/_Project/Audio/SFX/Notes/NotePickup.wav";
    const string ThrowSoundPath = "Assets/_Project/Audio/SFX/Notes/NoteThrow.wav";

    // prefab name, mesh name inside the fbx, display name. The fbx mesh names are shifted by one denomination:
    // the atlas texture only holds $1, $5, $10, $20 and $50 and the mesh UVs point at them in that order.
    static readonly string[][] Notes =
    {
        new[] { "1-Dollar", "_5_Dollar", "$1 Note" },
        new[] { "5-Dollar", "_10_Dollar", "$5 Note" },
        new[] { "10-Dollar", "_20_Dollar", "$10 Note" },
        new[] { "20-Dollar", "_50_Dollar", "$20 Note" },
        new[] { "50-Dollar", "_100_Dollar", "$50 Note" },
    };

    [MenuItem("Banker Loot/Build Dummy Note Prefabs")]
    public static void BuildNotePrefabs()
    {
        Directory.CreateDirectory(PrefabFolder);
        var material = GetOrCreateMaterial();

        foreach (var note in Notes)
        {
            var mesh = FindMesh(note[1]);
            if (mesh == null)
            {
                Debug.LogWarning("[BankerLoot] Mesh " + note[1] + " not found in " + ModelPath);
                continue;
            }

            var go = new GameObject(note[0]);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;

            // the notes are flat quads, give the collider some thickness so rays and physics catch them
            var box = go.AddComponent<BoxCollider>();
            var size = mesh.bounds.size;
            box.center = mesh.bounds.center;
            box.size = new Vector3(size.x, 0.004f, size.z);
            box.sharedMaterial = GetOrCreatePhysicsMaterial();

            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.3f;
            body.linearDamping = 0.4f;
            body.angularDamping = 0.6f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            var outline = go.AddComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = new Color(1f, 0.85f, 0.1f);
            outline.OutlineWidth = 4f;
            outline.enabled = false;

            var pickable = go.AddComponent<PickableNote>();
            pickable.itemName = note[2];
            pickable.itemPickSound = AssetDatabase.LoadAssetAtPath<AudioClip>(PickSoundPath);
            pickable.itemThrowSound = AssetDatabase.LoadAssetAtPath<AudioClip>(ThrowSoundPath);
            pickable.normalScale = 4f;
            pickable.camChildScale = 2f;
            pickable.throwForceMul = 1f;
            pickable.pickedPosition = new Vector3(0.22f, -0.18f, 0.6f);
            // quad normal faces the camera, long edge horizontal
            pickable.pickedAngle = (Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(0f, 90f, 0f)).eulerAngles;
            go.transform.localScale = Vector3.one * pickable.normalScale;

            PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/" + note[0] + ".prefab");
            Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BankerLoot] Dummy note prefabs built in " + PrefabFolder);
    }

    [MenuItem("Banker Loot/Spawn Note Heap")]
    public static void SpawnNoteHeap()
    {
        SpawnNoteHeap(new Vector3(-1.5f, 0f, 2.5f), 140, 0.3f, 12345);
    }

    public static void SpawnNoteHeap(Vector3 _center, int _count, float _radius, int _seed)
    {
        var prefabs = new GameObject[Notes.Length];
        for (int i = 0; i < Notes.Length; i++)
        {
            prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/" + Notes[i][0] + ".prefab");
            if (prefabs[i] == null)
            {
                Debug.LogWarning("[BankerLoot] Note prefabs missing, run 'Build Dummy Note Prefabs' first.");
                return;
            }
        }

        var old = GameObject.Find(HeapName);
        if (old != null)
        {
            Object.DestroyImmediate(old);
        }
        var heap = new GameObject(HeapName);
        heap.transform.position = _center;

        var oldState = Random.state;
        Random.InitState(_seed);
        var bodies = new Rigidbody[_count];
        for (int i = 0; i < _count; i++)
        {
            var prefab = prefabs[Random.Range(0, prefabs.Length)];
            var note = (GameObject)PrefabUtility.InstantiatePrefab(prefab, heap.transform);
            var offset = Random.insideUnitCircle * _radius;
            // staggered drop heights so the notes do not overlap (and get pushed apart) when physics starts
            note.transform.position = _center + new Vector3(offset.x, 0.2f + i * 0.06f, offset.y);
            note.transform.rotation = Quaternion.Euler(Random.Range(-25f, 25f), Random.Range(0f, 360f), Random.Range(-25f, 25f));
            bodies[i] = note.GetComponent<Rigidbody>();
        }
        Random.state = oldState;

        // let the notes fall and settle in edit mode, so the heap is already formed when the scene opens
        var oldMode = Physics.simulationMode;
        Physics.simulationMode = SimulationMode.Script;
        for (int step = 0; step < 900; step++)
        {
            Physics.Simulate(0.02f);
        }
        Physics.simulationMode = oldMode;
        foreach (var body in bodies)
        {
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
        }

        EditorSceneManager.MarkSceneDirty(heap.scene);
        Debug.Log("[BankerLoot] Spawned a heap of " + _count + " notes.");
    }

    static Mesh FindMesh(string _name)
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
        {
            if (asset is Mesh mesh && mesh.name == _name)
                return mesh;
        }
        return null;
    }

    /// <summary>Grippy, non bouncy surface so the flat notes stay piled up instead of sliding apart.</summary>
    static PhysicsMaterial GetOrCreatePhysicsMaterial()
    {
        const string path = DummyRoot + "/Materials/Currency Note Physics.asset";
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
        if (material != null)
            return material;

        material = new PhysicsMaterial("Currency Note Physics")
        {
            dynamicFriction = 1f,
            staticFriction = 1.2f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Maximum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static Material GetOrCreateMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null)
            return material;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        material = new Material(shader) { name = "Currency Note" };
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath));
        material.SetFloat("_Cull", 0f); // notes are single quads, visible from both sides
        material.SetFloat("_Smoothness", 0.2f);
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }
}
#endif
