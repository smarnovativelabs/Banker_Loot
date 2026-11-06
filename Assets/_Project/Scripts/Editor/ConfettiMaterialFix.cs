using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// The Confetti FX package (copied from the food game) uses built-in render pipeline shaders, which do not render
/// in URP. This switches its materials to the URP shaders (particles unlit for the particles, lit for the 3D pieces).
/// </summary>
public static class ConfettiMaterialFix
{
    const string Folder = "Assets/Confetti FX/Materials";

    [MenuItem("Banker Loot/Fix Confetti Materials For URP")]
    public static void Fix()
    {
        var _particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        var _litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (_particleShader == null || _litShader == null)
        {
            Debug.LogError("Confetti fix: the URP shaders were not found.");
            return;
        }

        int _count = 0;
        foreach (var _guid in AssetDatabase.FindAssets("t:Material", new[] { Folder }))
        {
            var _material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(_guid));
            string _shaderName = _material.shader.name;
            if (_shaderName.StartsWith("Universal Render Pipeline"))
                continue;

            Texture _texture = _material.HasProperty("_MainTex") ? _material.GetTexture("_MainTex") : null;
            Color _color = _material.HasProperty("_Color") ? _material.GetColor("_Color") : Color.white;
            Color _emission = _material.HasProperty("_EmissionColor") ? _material.GetColor("_EmissionColor") : Color.black;

            if (_shaderName == "Standard" || _shaderName.StartsWith("Standard"))
            {
                _material.shader = _litShader;
                _material.SetColor("_BaseColor", _color);
                if (_texture != null) _material.SetTexture("_BaseMap", _texture);
                if (_emission.maxColorComponent > 0.01f)
                {
                    _material.EnableKeyword("_EMISSION");
                    _material.SetColor("_EmissionColor", _emission);
                }
            }
            else
            {
                bool _additive = _shaderName.Contains("Additive");
                _material.shader = _particleShader;
                _material.SetColor("_BaseColor", _color.maxColorComponent > 0f ? _color : Color.white);
                if (_texture != null) _material.SetTexture("_BaseMap", _texture);
                // transparent: alpha blended (or additive for the glowing sparks)
                _material.SetFloat("_Surface", 1f);
                _material.SetFloat("_Blend", _additive ? 2f : 0f);
                _material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                _material.SetFloat("_DstBlend", _additive ? (float)BlendMode.One : (float)BlendMode.OneMinusSrcAlpha);
                _material.SetFloat("_ZWrite", 0f);
                _material.SetOverrideTag("RenderType", "Transparent");
                _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _material.renderQueue = (int)RenderQueue.Transparent;
            }
            EditorUtility.SetDirty(_material);
            _count++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Confetti fix: " + _count + " materials switched to URP shaders.");
    }
}
