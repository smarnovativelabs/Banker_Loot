#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Makes the "Cash Counting Machine" mesh of the bank room work: adds the CashCountingMachine (collider, intake point,
/// screen text, outline) next to it. Safe to run again.
/// </summary>
public static class CashCountingMachineSetup
{
    const string MeshName = "Cash Counting Machine";
    const string RootName = "Cash Counter";

    [MenuItem("Banker Loot/Setup Cash Counting Machine")]
    public static void Setup() { Setup(false); }

    /// <summary>Same, but the feed, roller and output points are put back to their default place for the mesh (use after the mesh was moved or turned).</summary>
    [MenuItem("Banker Loot/Setup Cash Counting Machine (Reset Points)")]
    public static void SetupResetPoints() { Setup(true); }

    static void Setup(bool _resetPoints)
    {
        var mesh = FindInScene(MeshName);
        if (mesh == null)
        {
            Debug.LogWarning("[BankerLoot] '" + MeshName + "' not found in the open scene.");
            return;
        }

        var root = GameObject.Find(RootName);
        if (root == null)
        {
            root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Cash Counter");
        }

        // collider around the machine, a little bigger than the mesh so it is hit before any collider behind it
        var bounds = mesh.GetComponent<MeshRenderer>().bounds;
        root.transform.position = bounds.center;
        root.transform.rotation = Quaternion.identity;
        var box = root.GetComponent<BoxCollider>();
        if (box == null)
        {
            box = root.AddComponent<BoxCollider>();
        }
        box.center = Vector3.zero;
        box.size = bounds.size + Vector3.one * 0.03f;

        var machine = root.GetComponent<CashCountingMachine>();
        if (machine == null)
        {
            machine = root.AddComponent<CashCountingMachine>();
        }

        machine.body = mesh.transform;

        // the old single intake point is replaced by the path the notes take through the machine
        var oldIntake = root.transform.Find("Intake Point");
        if (oldIntake != null)
        {
            Object.DestroyImmediate(oldIntake.gameObject);
        }
        // feed tray on top at the back, the rollers in the middle, the output stack at the front (the mesh faces its local -Z, so the
        // points follow the way the mesh is turned). The points only get their position when they are created (or on reset).
        Vector3 back = mesh.transform.forward; back.y = 0f; back.Normalize();
        float half = Mathf.Abs(back.x) * bounds.extents.x + Mathf.Abs(back.z) * bounds.extents.z;
        Vector3 front = bounds.center - back * half;
        machine.hopperPoint = GetOrCreatePoint(root.transform, "Hopper Point", new Vector3(front.x, bounds.max.y - 0.01f, front.z) + back * 0.26f, _resetPoints);
        machine.rollerPoint = GetOrCreatePoint(root.transform, "Roller Point", new Vector3(front.x, bounds.min.y + 0.14f, front.z) + back * 0.17f, _resetPoints);
        machine.outputPoint = GetOrCreatePoint(root.transform, "Output Point", new Vector3(front.x, bounds.min.y + 0.06f, front.z) + back * 0.07f, _resetPoints);

        // the screen floats above the machine and shows the notes inside and their worth
        var screen = root.transform.Find("Screen Text");
        if (screen == null)
        {
            var go = new GameObject("Screen Text");
            go.transform.SetParent(root.transform, false);
            var text = go.AddComponent<TextMeshPro>();
            // adding the text swaps the Transform for a RectTransform, so the transform is read after it
            screen = go.transform;
            text.alignment = TextAlignmentOptions.Center;
            text.fontSize = 2.2f;
            text.fontStyle = FontStyles.Bold;
            text.color = new Color(0.45f, 1f, 0.55f);
            text.rectTransform.sizeDelta = new Vector2(2f, 0.8f);
            text.text = "$0";
            try
            {
                text.outlineWidth = 0.2f;
                text.outlineColor = new Color32(0, 30, 0, 255);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[BankerLoot] Screen text outline skipped: " + e.Message);
            }
        }
        screen.position = new Vector3(bounds.center.x, bounds.max.y + 0.2f, bounds.center.z);
        machine.screenText = screen.GetComponent<TMP_Text>();
        machine.screenText.gameObject.SetActive(false);

        var outline = mesh.GetComponent<Outline>();
        if (outline == null)
        {
            outline = mesh.AddComponent<Outline>();
            outline.OutlineMode = Outline.Mode.OutlineAll;
            outline.OutlineColor = new Color(1f, 0.85f, 0.1f);
            outline.OutlineWidth = 4f;
        }
        outline.enabled = false;
        machine.outlines = new[] { outline };

        EditorUtility.SetDirty(machine);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("[BankerLoot] Cash counting machine set up.");
    }

    static Transform GetOrCreatePoint(Transform _parent, string _name, Vector3 _worldPosition, bool _reset)
    {
        var point = _parent.Find(_name);
        if (point == null)
        {
            point = new GameObject(_name).transform;
            point.SetParent(_parent, false);
            point.position = _worldPosition;
        }
        else if (_reset)
        {
            point.position = _worldPosition;
        }
        return point;
    }

    static GameObject FindInScene(string _name)
    {
        foreach (var transform in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (transform.name == _name && transform.gameObject.scene.IsValid() && transform.GetComponent<MeshRenderer>() != null)
                return transform.gameObject;
        }
        return null;
    }
}
#endif
