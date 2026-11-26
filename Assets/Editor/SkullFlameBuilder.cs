#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds the SkullFlame prefab (package flame + smoke + embers + wind + flickering light)
/// and puts it on every TorchSkullWall in the open scene, centred on the skull.
/// Menu: Tools > Build Skull Flame (Package Fire)
/// </summary>
public static class SkullFlameBuilder
{
    const string Pack = "Assets/UnityTechnologies/ParticlePack/EffectExamples/";
    const string Dir = "Assets/_Project/Prefabs/FX";
    const string PrefabPath = Dir + "/SkullFlame.prefab";

    // How far above the skull's centre the fire starts, as a fraction of the skull's half-height.
    const float HeightFactor = 0.55f;

    [MenuItem("Tools/Build Skull Flame (Package Fire)")]
    public static void Build()
    {
        Directory.CreateDirectory(Dir);
        AssetDatabase.Refresh();

        var flameSrc = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "Fire & Explosion Effects/Prefabs/MediumFlames.prefab");
        var smokeSrc = AssetDatabase.LoadAssetAtPath<GameObject>(Pack + "Smoke & Steam Effects/Prefabs/SmokeEffect.prefab");
        var embersMat = AssetDatabase.LoadAssetAtPath<Material>(Pack + "Fire & Explosion Effects/Materials/Embers.mat");
        if (flameSrc == null || smokeSrc == null)
        {
            Debug.LogError("SkullFlameBuilder: package prefabs not found under " + Pack);
            return;
        }

        var root = new GameObject("SkullFlame");

        // ---- Flame (package MediumFlames, shrunk to fit a skull) ----
        var flame = Object.Instantiate(flameSrc, root.transform);
        flame.name = "Flame";
        flame.transform.localPosition = Vector3.zero;
        flame.transform.localRotation = Quaternion.Euler(270, 0, 0);
        flame.transform.localScale = Vector3.one * 0.25f;
        {
            var ps = flame.GetComponent<ParticleSystem>();
            var sh = ps.shape; sh.radius = 0.06f;
            var em = ps.emission; em.rateOverTime = 18f;
            var life = ps.main; life.startLifetime = new ParticleSystem.MinMaxCurve(0.9f, 1.5f);
            // narrow + tall flame (non-uniform billboard size)
            var m = ps.main;
            m.startSize3D = true;
            m.startSizeX = new ParticleSystem.MinMaxCurve(0.65f, 1.0f);
            m.startSizeY = new ParticleSystem.MinMaxCurve(2.0f, 3.2f);
            m.startSizeZ = new ParticleSystem.MinMaxCurve(1f, 1f);
        }

        // ---- Smoke (package SmokeEffect, small and subtle) ----
        var smoke = Object.Instantiate(smokeSrc, root.transform);
        smoke.name = "Smoke";
        smoke.transform.localPosition = new Vector3(0, 0.14f, 0);
        smoke.transform.localRotation = Quaternion.Euler(270, 0, 0);
        smoke.transform.localScale = Vector3.one;
        foreach (var ps in smoke.GetComponentsInChildren<ParticleSystem>())
        {
            var m = ps.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3.5f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.3f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
            m.maxParticles = 40;
            m.startColor = new Color(0.22f, 0.2f, 0.19f, 0.5f);
            var em = ps.emission; em.rateOverTime = 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 8f; sh.radius = 0.04f;
            if (ps.noise.enabled) { var n = ps.noise; n.strength = 0.25f; }
        }

        // ---- Embers (package Embers material) ----
        var emb = new GameObject("Embers");
        emb.transform.SetParent(root.transform, false);
        emb.transform.localRotation = Quaternion.Euler(270, 0, 0);
        {
            var ps = emb.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var m = ps.main;
            m.loop = true; m.duration = 2f;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            m.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.6f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.012f, 0.03f);
            m.startColor = new Color(1f, 0.7f, 0.25f, 1f);
            m.maxParticles = 30;
            var em = ps.emission; em.rateOverTime = 5f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 15f; sh.radius = 0.04f;
            var col = ps.colorOverLifetime; col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.85f, 0.4f), 0f), new GradientColorKey(new Color(0.9f, 0.25f, 0.02f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var n = ps.noise; n.enabled = true; n.strength = 0.3f; n.frequency = 0.8f;
            var r = emb.GetComponent<ParticleSystemRenderer>();
            if (embersMat != null) r.sharedMaterial = embersMat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        // ---- Light ----
        var lg = new GameObject("FireLight");
        lg.transform.SetParent(root.transform, false);
        lg.transform.localPosition = new Vector3(0, 0.2f, 0);
        var light = lg.AddComponent<Light>();
        light.type = LightType.Point; light.color = new Color(1f, 0.55f, 0.2f);
        light.intensity = 3f; light.range = 7f; light.shadows = LightShadows.None;
        lg.AddComponent<SkullFireFlicker>();

        // ---- Wind ----
        var wind = root.AddComponent<SkullFireWind>();
        wind.direction = new Vector3(1f, 0f, 0.3f);
        wind.strength = 0.5f;

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        // ---- Place on every skull, centred on its mesh ----
        int count = 0;
        var all = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var torches = new System.Collections.Generic.List<Transform>();
        foreach (var t in all)
            if (t != null && t.name.StartsWith("TorchSkullWall") && t.GetComponent<Renderer>() != null) torches.Add(t);

        foreach (var t in torches)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i);
                if (c.name == "SkullFlame" || c.name == "SkullTorchFire") Object.DestroyImmediate(c.gameObject);
            }
            var b = t.GetComponent<Renderer>().bounds;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, t);
            inst.transform.position = new Vector3(b.center.x, b.center.y + b.extents.y * HeightFactor, b.center.z);
            inst.transform.rotation = Quaternion.identity;
            count++;
        }
        EditorSceneManager.MarkAllScenesDirty();
        AssetDatabase.SaveAssets();
        Debug.Log("Skull flame added to " + count + " torches");
    }
}
#endif
