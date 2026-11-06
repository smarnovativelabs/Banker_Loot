using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Assigns the level complete sound and the confetti (copied from the food game) to the Level Complete Panel in
/// the Gameplay HUD prefab. Run it after the panel was created by "Setup Level Complete Panel".
/// </summary>
public static class LevelCompleteEffectsSetup
{
    const string HudPath = "Assets/_Project/Prefabs/UI/Gameplay HUD.prefab";
    const string SoundPath = "Assets/_Project/Audio/SFX/level complete sound.mp3";
    const string ConfettiPath = "Assets/Confetti FX/Prefabs/2D/Explosion/ConfettiExplosionRainbow.prefab";

    [MenuItem("Banker Loot/Setup Level Complete Effects")]
    public static void Setup()
    {
        var _sound = AssetDatabase.LoadAssetAtPath<AudioClip>(SoundPath);
        var _confetti = AssetDatabase.LoadAssetAtPath<GameObject>(ConfettiPath);
        if (_sound == null || _confetti == null)
        {
            Debug.LogError("Level complete effects: sound or confetti not found (" + SoundPath + ", " + ConfettiPath + ")");
            return;
        }
        var _root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            var _panel = _root.GetComponentInChildren<LevelCompletePanel>(true);
            if (_panel == null)
            {
                Debug.LogError("Level complete effects: run 'Setup Level Complete Panel' first.");
                return;
            }
            _panel.celebrationSound = _sound;
            _panel.confettiPrefab = _confetti;
            // a lighter backdrop, so the confetti behind the panel stays visible
            var _backdrop = _panel.GetComponent<Image>();
            if (_backdrop != null)
            {
                _backdrop.color = new Color(0f, 0f, 0f, 0.4f);
            }
            PrefabUtility.SaveAsPrefabAsset(_root, HudPath);
            Debug.Log("Level complete effects assigned (sound + confetti).");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(_root);
        }
    }
}
