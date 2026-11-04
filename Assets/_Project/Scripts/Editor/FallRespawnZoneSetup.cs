#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Adds the "Respawn_Collider" under the environment of the open scene: a big trigger box below the floor. A note that
/// falls through the floor lands in it and goes back to the heap (see FallRespawnZone). Safe to run again.
/// </summary>
public static class FallRespawnZoneSetup
{
    const string ObjectName = "Respawn_Collider";

    [MenuItem("Banker Loot/Setup Fall Respawn Zone")]
    public static void Setup()
    {
        var go = GameObject.Find(ObjectName);
        if (go == null)
        {
            go = new GameObject(ObjectName);
            Undo.RegisterCreatedObjectUndo(go, ObjectName);
        }

        // centred on the level, 2 m below the lowest point of the floor (the floor top is around y -0.1)
        float lowest = LowestFloorY();
        go.transform.position = new Vector3(-1.5f, lowest - 3f, -1.5f);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var box = go.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = go.AddComponent<BoxCollider>();
        }
        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = new Vector3(80f, 4f, 80f);

        if (go.GetComponent<FallRespawnZone>() == null)
        {
            go.AddComponent<FallRespawnZone>();
        }
        EditorUtility.SetDirty(go);
        EditorSceneManager.MarkSceneDirty(go.scene);
        Debug.Log("[BankerLoot] Respawn_Collider at y " + go.transform.position.y.ToString("F1") + " (floor at about y " + lowest.ToString("F1") + ").");
    }

    /// <summary>The lowest top of the colliders in the room that are big enough to be a floor.</summary>
    static float LowestFloorY()
    {
        var floor = GameObject.Find("Floor");
        if (floor != null)
        {
            var collider = floor.GetComponent<Collider>();
            if (collider != null)
            {
                return collider.bounds.min.y;
            }
        }
        return -0.2f;
    }
}
#endif
