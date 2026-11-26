#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class SkullTorchFireBuilder
{
    const string Dir = "Assets/_Project/Prefabs/FX";
    const string TexPath = Dir + "/FireSoft.png";
    const string FireMatPath = Dir + "/FireFlame.mat";
    const string SmokeMatPath = Dir + "/FireSmoke.mat";
    const string PrefabPath = Dir + "/SkullTorchFire.prefab";

    [MenuItem("Tools/Build Skull Torch Fire")]
    public static void Build()
    {
        Directory.CreateDirectory(Dir);
        AssetDatabase.Refresh();

        // Soft, slightly noisy radial texture
        if (!File.Exists(TexPath))
        {
            int s = 128;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = (x - s / 2f) / (s / 2f), dy = (y - s / 2f) / (s / 2f);
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float pn = Mathf.PerlinNoise(x * 0.07f, y * 0.07f);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    a *= Mathf.Lerp(0.6f, 1f, pn);
                    t.SetPixel(x, y, new Color(1, 1, 1, a));
                }
            File.WriteAllBytes(TexPath, t.EncodeToPNG());
            AssetDatabase.ImportAsset(TexPath);
            var imp = (TextureImporter)AssetImporter.GetAtPath(TexPath);
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TexPath);

        var fireMat = MakeMat(FireMatPath, tex, true);
        var smokeMat = MakeMat(SmokeMatPath, tex, false);

        var root = new GameObject("SkullTorchFire");

        // Flame core
        var flame = NewPS("Flame", root.transform, fireMat);
        {
            var ps = flame.GetComponent<ParticleSystem>();
            var m = ps.main;
            m.duration = 1f; m.loop = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles = 80;
            m.gravityModifier = -0.05f;
            var e = ps.emission; e.rateOverTime = 35;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6; sh.radius = 0.04f;
            sh.rotation = new Vector3(-90, 0, 0);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[]{
                new GradientColorKey(new Color(1f,0.95f,0.6f),0f),
                new GradientColorKey(new Color(1f,0.55f,0.1f),0.35f),
                new GradientColorKey(new Color(0.8f,0.15f,0.02f),0.75f),
                new GradientColorKey(new Color(0.2f,0.03f,0f),1f)},
                new[]{ new GradientAlphaKey(0f,0f), new GradientAlphaKey(0.9f,0.12f), new GradientAlphaKey(0.6f,0.6f), new GradientAlphaKey(0f,1f)});
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0, 0.6f), new Keyframe(0.25f, 1f), new Keyframe(1, 0.1f)));
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.25f; noise.frequency = 1.2f; noise.scrollSpeed = 0.8f; noise.quality = ParticleSystemNoiseQuality.Medium;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
        }

        // Inner hot core (smaller, brighter)
        var core = NewPS("Core", root.transform, fireMat);
        {
            var ps = core.GetComponent<ParticleSystem>();
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            m.startSpeed = 0.2f; m.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.18f);
            m.startColor = new Color(1f, 0.9f, 0.5f, 0.9f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles = 40;
            var e = ps.emission; e.rateOverTime = 20;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 3; sh.radius = 0.02f; sh.rotation = new Vector3(-90, 0, 0);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[]{ new GradientColorKey(Color.white,0), new GradientColorKey(new Color(1f,0.6f,0.1f),1) },
                new[]{ new GradientAlphaKey(0,0), new GradientAlphaKey(1,0.15f), new GradientAlphaKey(0,1) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0.2f)));
        }

        // Embers / sparks
        var sparks = NewPS("Embers", root.transform, fireMat);
        {
            var ps = sparks.GetComponent<ParticleSystem>();
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.015f, 0.035f);
            m.startColor = new Color(1f, 0.6f, 0.15f, 1f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles = 30;
            var e = ps.emission; e.rateOverTime = 4;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 20; sh.radius = 0.05f; sh.rotation = new Vector3(-90, 0, 0);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[]{ new GradientColorKey(new Color(1f,0.8f,0.3f),0), new GradientColorKey(new Color(0.9f,0.2f,0f),1) },
                new[]{ new GradientAlphaKey(1,0), new GradientAlphaKey(1,0.6f), new GradientAlphaKey(0,1) });
            col.color = g;
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.5f; noise.frequency = 0.8f;
        }

        // Smoke
        var smoke = NewPS("Smoke", root.transform, smokeMat);
        {
            var ps = smoke.GetComponent<ParticleSystem>();
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.4f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.35f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0, 6.28f);
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.maxParticles = 30;
            var e = ps.emission; e.rateOverTime = 6;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 10; sh.radius = 0.05f; sh.rotation = new Vector3(-90, 0, 0);
            sh.position = new Vector3(0, 0.15f, 0);
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[]{ new GradientColorKey(new Color(0.15f,0.13f,0.12f),0), new GradientColorKey(new Color(0.3f,0.3f,0.3f),1) },
                new[]{ new GradientAlphaKey(0,0), new GradientAlphaKey(0.25f,0.25f), new GradientAlphaKey(0,1) });
            col.color = g;
            var sz = ps.sizeOverLifetime; sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0, 0.5f), new Keyframe(1, 2f)));
            var noise = ps.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.6f;
            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
        }

        // Flickering light
        var lg = new GameObject("FireLight");
        lg.transform.SetParent(root.transform, false);
        lg.transform.localPosition = new Vector3(0, 0.25f, 0);
        var light = lg.AddComponent<Light>();
        light.type = LightType.Point; light.color = new Color(1f, 0.55f, 0.2f);
        light.intensity = 2.5f; light.range = 6f; light.shadows = LightShadows.None;
        lg.AddComponent<SkullFireFlicker>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        // Attach to every TorchSkullWall in open scene
        int n = 0;
        foreach (var go in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!go.name.StartsWith("TorchSkullWall")) continue;
            if (go.Find("SkullTorchFire") != null) continue;
            var mr = go.GetComponent<Renderer>();
            if (mr == null) continue;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go);
            Vector3 top = mr.bounds.center + Vector3.up * (mr.bounds.extents.y * 0.9f);
            inst.transform.position = top;
            inst.transform.rotation = Quaternion.identity;
            Undo.RegisterCreatedObjectUndo(inst, "Add skull fire");
            n++;
        }
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
        Debug.Log("Skull torch fire added to " + n + " torches");
    }

    static GameObject NewPS(string name, Transform parent, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        return go;
    }

    static Material MakeMat(string path, Texture2D tex, bool additive)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        var sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var mat = new Material(sh);
        mat.SetTexture("_BaseMap", tex);
        mat.SetFloat("_Surface", 1);
        mat.SetFloat("_Blend", additive ? 2 : 0); // 2 = Additive, 0 = Alpha
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
        mat.SetFloat("_ZWrite", 0);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.renderQueue = (int)RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        if (additive) mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }
}

#endif
