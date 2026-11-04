using UnityEngine;

/// <summary>
/// A trigger collider under the environment. Anything that falls through the floor lands in it and is respawned:
/// a note goes back to the heap. Put it (with a trigger BoxCollider) below the lowest floor of the level.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FallRespawnZone : MonoBehaviour
{
    void Reset()
    {
        var _collider = GetComponent<Collider>();
        if (_collider != null)
        {
            _collider.isTrigger = true;
        }
    }

    void OnTriggerEnter(Collider _other)
    {
        Respawn(_other);
    }

    // a note that was already inside when the zone was enabled is caught too
    void OnTriggerStay(Collider _other)
    {
        Respawn(_other);
    }

    static void Respawn(Collider _other)
    {
        var _note = _other.GetComponentInParent<PickableNote>();
        if (_note != null && !_note.isPlayerPicked)
        {
            _note.RespawnAtHeap();
        }
    }
}
