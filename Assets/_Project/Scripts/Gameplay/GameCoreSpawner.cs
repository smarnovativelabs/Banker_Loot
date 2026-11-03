using UnityEngine;

/// <summary>
/// Spawns the "Game Core" prefab (controls, HUD, tutorial, game controller and the note managers) when the
/// scene loads, so every gameplay scene only holds its own environment and the core is the same everywhere.
/// It runs before every other script so the core's singletons exist when the scene objects start.
/// </summary>
[DefaultExecutionOrder(-10000)]
public class GameCoreSpawner : MonoBehaviour
{
    [Tooltip("The Game Core prefab, spawned once when this scene loads")]
    public GameObject corePrefab;

    void Awake()
    {
        // a core that is already in the scene (e.g. while testing) is used as it is
        if (corePrefab == null || GameController.instance != null)
            return;

        var _core = Instantiate(corePrefab);
        _core.name = corePrefab.name;
    }
}
