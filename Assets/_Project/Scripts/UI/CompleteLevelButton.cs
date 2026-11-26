using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The "Complete Level" button of the gameplay HUD: completes the level that is open (all its lockers), which unlocks
/// the next level and opens the Level Completed panel.
/// </summary>
[RequireComponent(typeof(Button))]
public class CompleteLevelButton : MonoBehaviour
{
    void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnPress);
    }

    void OnPress()
    {
        var _lockers = FindFirstObjectByType<LockerManager>();
        if (_lockers == null)
        {
            Debug.LogWarning("CompleteLevelButton: there is no LockerManager in this scene.");
            return;
        }
        _lockers.CompleteAllLockers();
    }
}
