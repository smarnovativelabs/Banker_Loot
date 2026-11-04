#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Puts the "Locker_Colliders" object and all its children on the "LockerColliders" layer (created when missing).
/// The player's hover and placing rays ignore that layer (see AimLayers), thrown notes still collide with it.
/// Safe to run again.
/// </summary>
public static class LockerColliderLayerSetup
{
    const string RootName = "Locker_Colliders";

    [MenuItem("Banker Loot/Setup Locker Collider Layer")]
    public static void Setup()
    {
        int layer = GetOrCreateLayer(AimLayers.LockerCollidersLayerName);
        if (layer < 0)
        {
            Debug.LogWarning("[BankerLoot] No free layer left for '" + AimLayers.LockerCollidersLayerName + "'.");
            return;
        }

        var root = GameObject.Find(RootName);
        if (root == null)
        {
            Debug.LogWarning("[BankerLoot] '" + RootName + "' not found in the open scene.");
            return;
        }

        int count = 0;
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            Undo.RecordObject(child.gameObject, "Locker colliders layer");
            child.gameObject.layer = layer;
            EditorUtility.SetDirty(child.gameObject);
            count++;
        }
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[BankerLoot] " + count + " objects of " + RootName + " are on the layer " + AimLayers.LockerCollidersLayerName + ".");
    }

    /// <summary>Layer index of the name, the first free layer from 8 on is named when it does not exist yet.</summary>
    public static int EnsureLayer(string _name)
    {
        return GetOrCreateLayer(_name);
    }

    static int GetOrCreateLayer(string _name)
    {
        int existing = LayerMask.NameToLayer(_name);
        if (existing >= 0)
            return existing;

        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            var element = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(element.stringValue))
            {
                element.stringValue = _name;
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return i;
            }
        }
        return -1;
    }
}
#endif
