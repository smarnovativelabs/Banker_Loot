#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the pickable currency note prefabs from Assets/_Project/Art/FBX/Dollars.fbx into Assets/_Project/Prefabs/Notes
/// and points the Level Data note types at them. Safe to run again: existing prefabs are rebuilt in place.
/// </summary>
public static class NotePrefabSetup
{
    const string ModelPath = "Assets/_Project/Art/FBX/Dollars.fbx";
    public const string PrefabFolder = "Assets/_Project/Prefabs/Notes";
    const string MaterialFolder = "Assets/_Project/Art/Materials";
    const string PhysicsMaterialPath = MaterialFolder + "/Currency Note Physics.asset";
    const string PickSoundPath = "Assets/_Project/Audio/SFX/Notes/NotePickup.wav";
    const string ThrowSoundPath = "Assets/_Project/Audio/SFX/Notes/NoteThrow.wav";

    // prefab / note id, mesh name inside the fbx, display name
    static readonly string[][] Notes =
    {
        new[] { "1-Dollar", "Dollar_1", "$1 Note" },
        new[] { "2-Dollar", "Dollar_2", "$2 Note" },
        new[] { "5-Dollar", "Dollar_5", "$5 Note" },
        new[] { "10-Dollar", "Dollar_10", "$10 Note" },
        new[] { "20-Dollar", "Dollar_20", "$20 Note" },
        new[] { "50-Dollar", "Dollar_50", "$50 Note" },
        new[] { "100-Dollar", "Dollar_100", "$100 Note" },
    };

    // the fbx note is 0.22 x 0.10 m, scaled so its long edge matches the size the notes had before
    const float NoteScale = 2.3f;

    [MenuItem("Banker Loot/Build Note Prefabs")]
    public static void BuildNotePrefabs()
    {
        Directory.CreateDirectory(PrefabFolder);
        var material = FindMaterial();
        if (material == null)
        {
            Debug.LogWarning("[BankerLoot] No material found in " + ModelPath);
            return;
        }

        foreach (var note in Notes)
        {
            var mesh = FindMesh(note[1]);
            if (mesh == null)
            {
                Debug.LogWarning("[BankerLoot] Mesh " + note[1] + " not found in " + ModelPath);
                continue;
            }

            var go = new GameObject(note[0]);
            go.layer = 6;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;

            // the notes are flat, give the collider some thickness so rays and physics catch them
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
            pickable.noteId = note[0];
            pickable.itemPickSound = AssetDatabase.LoadAssetAtPath<AudioClip>(PickSoundPath);
            pickable.itemThrowSound = AssetDatabase.LoadAssetAtPath<AudioClip>(ThrowSoundPath);
            pickable.normalScale = NoteScale;
            pickable.camChildScale = 2f;
            pickable.throwForceMul = 1f;
            pickable.pickedPosition = new Vector3(0.22f, -0.18f, 0.6f);
            // the fbx note lies flat with its long edge along X: stand it up so the face looks at the camera
            pickable.pickedAngle = Quaternion.Euler(-90f, 0f, 0f).eulerAngles;
            go.transform.localScale = Vector3.one * pickable.normalScale;

            PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/" + note[0] + ".prefab");
            Object.DestroyImmediate(go);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BankerLoot] Note prefabs built in " + PrefabFolder);
    }

    /// <summary>Points every note type of the level data at the prefab with the same note id.</summary>
    [MenuItem("Banker Loot/Relink Level Data Note Prefabs")]
    public static void RelinkLevelData()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:LevelData"))
        {
            var data = AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
            foreach (var entry in data.noteTypes)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<PickableNote>(PrefabFolder + "/" + entry.noteId + ".prefab");
                if (prefab != null)
                {
                    entry.prefab = prefab;
                }
                else
                {
                    Debug.LogWarning("[BankerLoot] No note prefab for " + entry.noteId + " in " + data.name);
                }
            }
            EditorUtility.SetDirty(data);
        }
        AssetDatabase.SaveAssets();
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

    static Material FindMaterial()
    {
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(ModelPath))
        {
            if (asset is Material material)
                return material;
        }
        return null;
    }

    /// <summary>Grippy, non bouncy surface so the flat notes stay piled up instead of sliding apart.</summary>
    static PhysicsMaterial GetOrCreatePhysicsMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(PhysicsMaterialPath);
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
        AssetDatabase.CreateAsset(material, PhysicsMaterialPath);
        return material;
    }
}
#endif
