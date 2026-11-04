#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Makes the lockers of the "Locker_Manager" object work: adds the LockerManager and for every locker its floor
/// (NotePlaceSurface), the amount label and the entry in the manager's list. Safe to run again: existing amounts are kept.
/// </summary>
public static class LockerManagerSetup
{
    const string ManagerName = "Locker_Manager";
    const string DoorSoundPath = "Assets/_Project/Audio/SFX/close farm door.mp3";

    [MenuItem("Banker Loot/Setup Lockers")]
    public static void Setup()
    {
        var manager = GameObject.Find(ManagerName);
        if (manager == null)
        {
            Debug.LogWarning("[BankerLoot] '" + ManagerName + "' not found in the open scene.");
            return;
        }

        var component = manager.GetComponent<LockerManager>();
        if (component == null)
        {
            component = manager.AddComponent<LockerManager>();
        }
        component.doorCloseSound = AssetDatabase.LoadAssetAtPath<AudioClip>(DoorSoundPath);

        // keep the amounts that were already set
        // and the texts of the locker board
        var oldEntries = new System.Collections.Generic.Dictionary<string, LockerManager.LockerEntry>();
        foreach (var entry in component.lockers)
        {
            if (!string.IsNullOrEmpty(entry.id))
            {
                oldEntries[entry.id] = entry;
            }
        }
        component.lockers.Clear();

        // the lockers are the children with a door as their own child (the floor surfaces and labels added below are
        // skipped), collected first because the loop adds children to the manager
        var lockerList = new System.Collections.Generic.List<Transform>();
        foreach (Transform child in manager.transform)
        {
            if (child.childCount > 0 && child.GetComponent<MeshRenderer>() != null)
            {
                lockerList.Add(child);
            }
        }

        int index = 0;
        foreach (var locker in lockerList)
        {
            var door = locker.GetChild(0);
            oldEntries.TryGetValue(locker.name, out var old);
            var entry = new LockerManager.LockerEntry
            {
                id = locker.name,
                door = door,
                amount = old != null ? old.amount : 100 + 50 * index,
                nameText = old != null ? old.nameText : null,
                icon = old != null ? old.icon : null,
                label = old != null ? old.label : null
            };
            entry.surface = GetOrCreateSurface(manager.transform, locker, door);
            // a floating label is only made when the locker has no text yet (run 'Setup Locker Board' to use the board)
            if (entry.label == null)
            {
                entry.label = GetOrCreateLabel(manager.transform, locker, door);
            }
            component.lockers.Add(entry);
            index++;
        }

        EditorUtility.SetDirty(component);
        EditorSceneManager.MarkSceneDirty(manager.scene);
        Debug.Log("[BankerLoot] " + component.lockers.Count + " lockers set up. Set each amount in the Locker_Manager list.");
    }

    /// <summary>The floor of the locker: a thin box inside it, the notes lie on its top face.</summary>
    static NotePlaceSurface GetOrCreateSurface(Transform _manager, Transform _locker, Transform _door)
    {
        string name = _locker.name + " Place Surface";
        var existing = _manager.Find(name);
        GameObject go = existing != null ? existing.gameObject : new GameObject(name);
        go.transform.SetParent(_manager, false);
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var box = go.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = go.AddComponent<BoxCollider>();
        }
        FitFloor(_locker, _door, go.transform, box);

        var surface = go.GetComponent<NotePlaceSurface>();
        if (surface == null)
        {
            surface = go.AddComponent<NotePlaceSurface>();
        }
        surface.surfaceId = "Locker_" + _locker.name;
        surface.snapToGrid = true;
        EditorUtility.SetDirty(surface);
        return surface;
    }

    /// <summary>Re-fits the floor of every locker to its real inside (use it when a locker model changes).</summary>
    [MenuItem("Banker Loot/Fit Locker Floors")]
    public static void FitFloors()
    {
        var manager = GameObject.Find(ManagerName);
        var component = manager != null ? manager.GetComponent<LockerManager>() : null;
        if (component == null)
            return;
        foreach (var entry in component.lockers)
        {
            if (entry.surface == null || entry.door == null)
                continue;
            var box = entry.surface.GetComponent<BoxCollider>();
            FitFloor(entry.door.parent, entry.door, entry.surface.transform, box);
            EditorUtility.SetDirty(entry.surface);
            EditorUtility.SetDirty(box);
        }
        EditorSceneManager.MarkSceneDirty(manager.scene);
        Debug.Log("[BankerLoot] Locker floors fitted.");
    }

    /// <summary>
    /// Measures the inside of the locker with temporary mesh colliders and fits the floor box to it. The locker has a
    /// raised floor (a bump) behind a lower front lip, the box covers only the raised part so the notes lie on it and
    /// not inside it. The box also stays inside the left, right and back walls.
    /// </summary>
    static void FitFloor(Transform _locker, Transform _door, Transform _floor, BoxCollider _box)
    {
        var bounds = _locker.GetComponent<MeshRenderer>().bounds;
        // the side the door is on is the front of the locker
        Vector3 front = _door.GetComponent<MeshRenderer>().bounds.center - bounds.center;
        front.y = 0f;
        front = Mathf.Abs(front.x) > Mathf.Abs(front.z) ? new Vector3(Mathf.Sign(front.x), 0f, 0f) : new Vector3(0f, 0f, Mathf.Sign(front.z));
        Vector3 side = new Vector3(front.z, 0f, front.x); // across the locker

        var temp = _locker.gameObject.AddComponent<MeshCollider>();
        Physics.SyncTransforms();
        try
        {
            // a point inside the locker, floor height straight below it
            Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + 0.4f, bounds.center.z);
            if (!temp.Raycast(new Ray(origin, Vector3.down), out RaycastHit floorHit, 1f))
                throw new System.Exception("no floor found inside " + _locker.name);
            float floorY = floorHit.point.y;
            float rayY = floorY + 0.2f;
            Vector3 inside = new Vector3(origin.x, rayY, origin.z);

            float left = Wall(temp, inside, -side, bounds.size.x + bounds.size.z);
            float right = Wall(temp, inside, side, bounds.size.x + bounds.size.z);
            float back = Wall(temp, inside, -front, bounds.size.x + bounds.size.z);

            // walk to the front until the floor changes height (the lower lip)
            float flat = 0f;
            for (float s = 0.01f; s < 1f; s += 0.01f)
            {
                var from = new Vector3(origin.x, floorY + 0.3f, origin.z) + front * s;
                if (!temp.Raycast(new Ray(from, Vector3.down), out RaycastHit h, 1f) || Mathf.Abs(h.point.y - floorY) > 0.008f)
                    break;
                flat = s;
            }

            const float margin = 0.012f;
            float minAcross = -left + margin;
            float maxAcross = right - margin;
            float minDepth = -back + margin;
            float maxDepth = flat - margin;
            Vector3 center = new Vector3(origin.x, floorY, origin.z)
                             + side * (minAcross + maxAcross) * 0.5f
                             + front * (minDepth + maxDepth) * 0.5f;
            float across = maxAcross - minAcross;
            float depth = maxDepth - minDepth;

            // the surface is 2 cm thick, its top face is the floor
            _floor.position = new Vector3(center.x, floorY - 0.01f, center.z);
            _box.center = Vector3.zero;
            _box.size = Mathf.Abs(front.z) > 0.5f
                ? new Vector3(across, 0.02f, depth)
                : new Vector3(depth, 0.02f, across);

            // the place zone fills the whole inside of the locker (also the lower lip in front and the opening), so the
            // note can be put down wherever the player aims in the locker, it is only found by the placing ray
            var zone = _floor.Find("Place Zone");
            if (zone == null)
            {
                zone = new GameObject("Place Zone").transform;
                zone.SetParent(_floor, false);
            }
            zone.rotation = Quaternion.identity;
            zone.localScale = Vector3.one;
            zone.position = new Vector3(center.x, floorY + 0.35f, center.z) + front * 0.075f;
            var zoneBox = zone.GetComponent<BoxCollider>();
            if (zoneBox == null)
            {
                zoneBox = zone.gameObject.AddComponent<BoxCollider>();
            }
            zoneBox.isTrigger = true;
            zoneBox.center = Vector3.zero;
            zoneBox.size = Mathf.Abs(front.z) > 0.5f
                ? new Vector3(across + 0.04f, 0.7f, depth + 0.15f)
                : new Vector3(depth + 0.15f, 0.7f, across + 0.04f);
            zone.gameObject.layer = LockerColliderLayerSetup.EnsureLayer(AimLayers.PlaceZoneLayerName);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[BankerLoot] Floor of " + _locker.name + " not fitted: " + e.Message);
        }
        finally
        {
            Object.DestroyImmediate(temp);
        }
    }

    /// <summary>Distance from the point to the wall in the given direction.</summary>
    static float Wall(MeshCollider _collider, Vector3 _from, Vector3 _direction, float _max)
    {
        return _collider.Raycast(new Ray(_from, _direction), out RaycastHit hit, _max) ? hit.distance : 0.3f;
    }

    /// <summary>World text above the locker door that shows what the locker still needs.</summary>
    static TMP_Text GetOrCreateLabel(Transform _manager, Transform _locker, Transform _door)
    {
        string name = _locker.name + " Label";
        var existing = _manager.Find(name);
        TextMeshPro text;
        if (existing != null)
        {
            text = existing.GetComponent<TextMeshPro>();
        }
        else
        {
            var go = new GameObject(name);
            go.transform.SetParent(_manager, false);
            text = go.AddComponent<TextMeshPro>();
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 1.1f;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(1f, 0.9f, 0.3f);
            text.rectTransform.sizeDelta = new Vector2(1f, 0.4f);
            text.text = "";
        }

        // in front of the locker, just above the door (the lockers face away from their back, towards the door)
        var body = _locker.GetComponent<MeshRenderer>().bounds;
        var doorBounds = _door.GetComponent<MeshRenderer>().bounds;
        Vector3 outward = doorBounds.center - body.center;
        outward.y = 0f;
        outward = outward.normalized;
        text.transform.position = new Vector3(doorBounds.center.x, doorBounds.max.y + 0.07f, doorBounds.center.z) + outward * 0.25f;
        return text;
    }
}
#endif
