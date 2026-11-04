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

        machine.body = body.transform;
        // the old drop particles are replaced by the water surfaces
        var oldDrops = drum.Find("Water Effect");
        if (oldDrops != null)
        {
            Object.DestroyImmediate(oldDrops.gameObject);
        }
        machine.waterEffect = null;
        machine.waterSurfaces = CreateWaterSurfaces(drum);
        machine.outlines = new[] { GetOutline(body), GetOutline(door) };
        machine.doorOpenSound = AssetDatabase.LoadAssetAtPath<AudioClip>(OpenSoundPath);
        machine.doorCloseSound = AssetDatabase.LoadAssetAtPath<AudioClip>(CloseSoundPath);

        // timer above the machine
        var timer = root.transform.Find("Timer Text");
        if (timer == null)
        {
            var go = new GameObject("Timer Text");
            go.transform.SetParent(root.transform, false);
            var text = go.AddComponent<TextMeshPro>();
            // adding the text swaps the Transform for a RectTransform, so the transform is read after it
            timer = go.transform;
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

    const string WaterMaterialPath = "Assets/_Project/Art/Materials/Water Drop.mat";
    const string WaterBackMaterialPath = "Assets/_Project/Art/Materials/Washing Water.mat";
    const string WaterFrontMaterialPath = "Assets/_Project/Art/Materials/Washing Water Front.mat";

    /// <summary>
    /// Two round water sheets facing the door: one behind the circling notes (the main water) and a faint one in
    /// front of them, so the notes tumble inside the water. They are shown and filled by the WashingMachine.
    /// </summary>
    static Renderer[] CreateWaterSurfaces(Transform _drum)
    {
        var back = GetOrCreateSheet(_drum, "Water Back", 0.14f, GetWaterMaterial(WaterBackMaterialPath, 0.85f));
        var front = GetOrCreateSheet(_drum, "Water Front", -0.1f, GetWaterMaterial(WaterFrontMaterialPath, 0.3f));
        return new[] { back, front };
    }

    static Renderer GetOrCreateSheet(Transform _drum, string _name, float _depth, Material _material)
    {
        var existing = _drum.Find(_name);
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = _name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(_drum, false);
        }
        // the quad's face looks out of the door (towards -X), its right side is the player's right
        go.transform.localPosition = new Vector3(_depth, 0f, 0f);
        go.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
        go.transform.localScale = Vector3.one * 0.52f;
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.enabled = false;
        return renderer;
    }

    static Material GetWaterMaterial(string _path, float _alpha)
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(_path);
        if (material == null)
        {
            var shader = Shader.Find("BankerLoot/Washing Water");
            if (shader == null)
            {
                Debug.LogWarning("[BankerLoot] Shader 'BankerLoot/Washing Water' not found.");
                return null;
            }
            material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(_path) };
            AssetDatabase.CreateAsset(material, _path);
        }
        material.SetFloat("_Alpha", _alpha);
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>Soft light blue drops that spray around inside the drum while the machine washes.</summary>
    static ParticleSystem GetOrCreateWaterEffect(Transform _drum)
    {
        var existing = _drum.Find("Water Effect");
        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject("Water Effect");
            go.transform.SetParent(_drum, false);
        }
        var particles = go.GetComponent<ParticleSystem>();
        if (particles == null)
        {
            particles = go.AddComponent<ParticleSystem>();
        }
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        // the water stays in the drum: it moves with the machine, has no gravity and swirls around the drum axis
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.09f);
        main.startColor = new Color(0.55f, 0.85f, 1f, 0.8f);
        main.gravityModifier = 0f;
        main.maxParticles = 400;

        var emission = particles.emission;
        emission.rateOverTime = 140f;

        // a ring just inside the drum wall (the drum axis points out of the door, along X)
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = 0.1f;
        shape.donutRadius = 0.1f;
        shape.rotation = new Vector3(0f, 90f, 0f);

        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        // the same speed as the notes (WashingMachine.orbitSpeed 220 degrees per second), set again by the machine at start
        velocity.orbitalX = 220f * Mathf.Deg2Rad;

        // a little turbulence so the water looks like a flowing mass and not a ring of dots
        var noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.8f;
        noise.scrollSpeed = 0.6f;

        var size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.5f), new Keyframe(0.25f, 1f), new Keyframe(1f, 0.6f)));

        // the drops fade out
        var color = particles.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.sharedMaterial = GetOrCreateWaterMaterial();
        return particles;
    }

    static Material GetOrCreateWaterMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterialPath);
        if (material != null)
            return material;

        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }
        material = new Material(shader) { name = "Water Drop" };
        // the soft round particle texture that comes with Unity
        var texture = AssetDatabase.GetBuiltinExtraResource<Texture2D>("Default-Particle.psd");
        if (texture != null)
        {
            material.SetTexture("_BaseMap", texture);
        }
        material.SetColor("_BaseColor", Color.white);
        NotePlacementManager.ConfigureTransparent(material);
        AssetDatabase.CreateAsset(material, WaterMaterialPath);
        return material;
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
