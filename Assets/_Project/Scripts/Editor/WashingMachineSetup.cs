#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Makes the washing machine of the bank room work: adds the WashingMachine (collider, drum point, timer text, outlines)
/// next to the "Washing Machine Body" / "Washing Machine Door" meshes of the open scene. Safe to run again.
/// </summary>
public static class WashingMachineSetup
{
    const string BodyName = "Washing Machine Body";
    const string DoorName = "Washing Machine Door";
    const string RootName = "Washing Machine";
    const string OpenSoundPath = "Assets/_Project/Audio/SFX/open farm door.mp3";
    const string CloseSoundPath = "Assets/_Project/Audio/SFX/close farm door.mp3";

    const string GameCorePath = "Assets/_Project/Prefabs/Game Core.prefab";

    /// <summary>Does every step of the dirty / washed notes setup in one go. Safe to run again.</summary>
    [MenuItem("Banker Loot/Setup Dirty Notes And Washing Machine")]
    public static void SetupAll()
    {
        NotePrefabSetup.BuildNotePrefabs();
        NotePrefabSetup.RelinkLevelData();
        FixHandPile();
        Setup();
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
    }

    /// <summary>
    /// The carried pile was tuned for the old note mesh (long along Z), the new fbx note is long along X: turn the
    /// pile a quarter and shrink the notes to the size they had in the hand before. Absolute values, so re-running is fine.
    /// </summary>
    static void FixHandPile()
    {
        var root = PrefabUtility.LoadPrefabContents(GameCorePath);
        var pickup = root.GetComponentInChildren<NotePickupManager>(true);
        if (pickup != null)
        {
            pickup.pileAngle = (Quaternion.Euler(5.259f, 91.92f, -69.912f) * Quaternion.Euler(0f, -90f, 0f)).eulerAngles;
            pickup.noteScale = 0.3f;
            PrefabUtility.SaveAsPrefabAsset(root, GameCorePath);
        }
        PrefabUtility.UnloadPrefabContents(root);
    }

    [MenuItem("Banker Loot/Setup Washing Machine")]
    public static void Setup()
    {
        var body = FindInScene(BodyName);
        var door = FindInScene(DoorName);
        if (body == null || door == null)
        {
            Debug.LogWarning("[BankerLoot] '" + BodyName + "' / '" + DoorName + "' not found in the open scene.");
            return;
        }

        var root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Washing Machine");
        }

        // collider around the whole machine (a little bigger than the mesh so it is hit before any collider behind it)
        var bodyBounds = body.GetComponent<MeshRenderer>().bounds;
        var doorBounds = door.GetComponent<MeshRenderer>().bounds;
        bodyBounds.Encapsulate(doorBounds);
        root.transform.position = bodyBounds.center;
        root.transform.rotation = Quaternion.identity;
        var box = root.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = root.AddComponent<BoxCollider>();
        }
        box.center = Vector3.zero;
        box.size = bodyBounds.size + Vector3.one * 0.03f;

        var machine = root.GetComponent<WashingMachine>();
        if (machine == null)
        {
            machine = root.AddComponent<WashingMachine>();
        }
        machine.door = door.transform;

        // the notes disappear into the middle of the door
        var drum = root.transform.Find("Drum Point");
        if (drum == null)
        {
            drum = new GameObject("Drum Point").transform;
            drum.SetParent(root.transform, false);
        }
        drum.position = new Vector3(doorBounds.center.x + 0.2f, doorBounds.center.y, doorBounds.center.z);
        machine.drumPoint = drum;

        machine.outlines = new[] { GetOutline(body), GetOutline(door) };
        machine.doorOpenSound = AssetDatabase.LoadAssetAtPath<AudioClip>(OpenSoundPath);
        machine.doorCloseSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CloseSoundPath);

        // timer above the machine
        var timer = root.transform.Find("Timer Text");
        if (timer == null)
        {
            var go = new GameObject("Timer Text");
            go.transform.SetParent(root.transform, false);
            timer = go.transform;
            var text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 4f;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 0.9f, 0.3f);
            text.rectTransform.sizeDelta = new Vector2(3f, 1f);
            text.text = "00:20";
            try
            {
                text.outlineWidth = 0.2f;
                text.outlineColor = new Color32(0, 0, 0, 255);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[BankerLoot] Timer text outline skipped: " + e.Message);
            }
            if (text.font == null)
            {
                Debug.LogWarning("[BankerLoot] TextMesh Pro has no default font, import Window > TextMeshPro > Import TMP Essential Resources.");
            }
        }
        timer.position = new Vector3(bodyBounds.center.x, bodyBounds.max.y + 0.25f, bodyBounds.center.z);
        machine.timerText = timer.GetComponent<TMP_Text>();
        machine.timerText.gameObject.SetActive(false);

        EditorUtility.SetDirty(machine);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[BankerLoot] Washing machine set up.");
    }

    static GameObject FindInScene(string _name)
    {
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform.name == _name && transform.gameObject.scene.IsValid())
                return transform.gameObject;
        }
        return null;
    }

    static Outline GetOutline(GameObject _target)
    {
        var outline = _target.GetComponent<Outline>();
        if (outline == null)
        {
            outline = _target.AddComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = new Color(1f, 0.85f, 0.1f);
            outline.OutlineWidth = 4f;
        }
        outline.enabled = false;
        return outline;
    }
}
#endif
