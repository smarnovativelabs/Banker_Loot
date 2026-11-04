using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks the upper face of a box (a table, counter, crate...) as a place where carried notes can be put down.
/// Put it on an object with a BoxCollider that is only rotated around Y.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class NotePlaceSurface : MonoBehaviour
{
    /// <summary>Every surface in the scene, used to find the surface of a saved note.</summary>
    public static readonly List<NotePlaceSurface> All = new List<NotePlaceSurface>();

    [Tooltip("Notes stay at least this far (meters) from the edge of the surface")]
    public float edgeMargin = 0.02f;
    [Tooltip("Unique name used by the note saving. Leave empty to use the hierarchy path (breaks when the object is renamed or moved)")]
    public string surfaceId;

    [Header("Grid")]
    [Tooltip("Divide the surface into cells the size of a note: the note snaps to the nearest cell and always lies the same way")]
    public bool snapToGrid = true;
    [Tooltip("Free space (meters) between two neighbouring cells")]
    public float gridGap = 0.04f;

    /// <summary>
    /// Optional rule set by the owner of the surface (a locker): returns why the note cannot be put here, or an
    /// empty string / null when it is fine.
    /// </summary>
    public System.Func<PickableNote, string> rejectReason;

    BoxCollider box;

    /// <summary>Stable key of this surface in the save file.</summary>
    public string Id
    {
        get
        {
            if (!string.IsNullOrEmpty(surfaceId))
                return surfaceId;
            string _path = name;
            for (var _parent = transform.parent; _parent != null; _parent = _parent.parent)
            {
                _path = _parent.name + "/" + _path;
            }
            return _path;
        }
    }

    void OnEnable()
    {
        if (!All.Contains(this))
        {
            All.Add(this);
        }
    }

    void OnDisable()
    {
        All.Remove(this);
    }

    public static NotePlaceSurface Find(string _id)
    {
        for (int i = 0; i < All.Count; i++)
        {
            if (All[i].Id == _id)
                return All[i];
        }
        return null;
    }

    BoxCollider Box
    {
        get
        {
            if (box == null)
            {
                box = GetComponent<BoxCollider>();
            }
            return box;
        }
    }

    /// <summary>World height of the upper face.</summary>
    public float TopY => transform.TransformPoint(Box.center + Vector3.up * (Box.size.y * 0.5f)).y;

    /// <summary>
    /// Finds the grid cell nearest to the point. The surface is divided into cells of (note size + gridGap), the grid is
    /// centred on the surface, the note's long edge runs along the longer side of the surface. Returns false when not
    /// even one note fits. axisYaw is the world yaw that turns a model's X axis along the long side of the surface.
    /// </summary>
    public bool TryGetGridCell(Vector3 _worldPoint, float _noteLong, float _noteShort, out Vector3 _cellCenter, out float _axisYaw, out Vector2 _cellSize)
    {
        _cellCenter = _worldPoint;
        _axisYaw = transform.eulerAngles.y;
        _cellSize = Vector2.zero;

        Vector3 _scale = transform.lossyScale;
        float _scaleX = Mathf.Abs(_scale.x);
        float _scaleZ = Mathf.Abs(_scale.z);
        float _sizeX = Box.size.x * _scaleX;
        float _sizeZ = Box.size.z * _scaleZ;
        bool _longAlongX = _sizeX >= _sizeZ;
        float _cellX = (_longAlongX ? _noteLong : _noteShort) + gridGap;
        float _cellZ = (_longAlongX ? _noteShort : _noteLong) + gridGap;
        int _countX = Mathf.FloorToInt((_sizeX - 2f * edgeMargin + gridGap) / _cellX);
        int _countZ = Mathf.FloorToInt((_sizeZ - 2f * edgeMargin + gridGap) / _cellZ);
        if (_countX < 1 || _countZ < 1)
            return false;

        Vector3 _local = transform.InverseTransformPoint(_worldPoint);
        float _metersX = (_local.x - Box.center.x) * _scaleX;
        float _metersZ = (_local.z - Box.center.z) * _scaleZ;
        int _indexX = Mathf.Clamp(Mathf.RoundToInt(_metersX / _cellX + (_countX - 1) * 0.5f), 0, _countX - 1);
        int _indexZ = Mathf.Clamp(Mathf.RoundToInt(_metersZ / _cellZ + (_countZ - 1) * 0.5f), 0, _countZ - 1);
        float _centerX = (_indexX - (_countX - 1) * 0.5f) * _cellX;
        float _centerZ = (_indexZ - (_countZ - 1) * 0.5f) * _cellZ;

        _local.x = Box.center.x + _centerX / _scaleX;
        _local.z = Box.center.z + _centerZ / _scaleZ;
        _cellCenter = transform.TransformPoint(_local);
        _axisYaw = transform.eulerAngles.y + (_longAlongX ? 0f : 90f);
        _cellSize = new Vector2(_cellX, _cellZ);
        return true;
    }

    /// <summary>
    /// Moves the point so a note footprint (half extents in meters along the note's own X and Z, turned by
    /// yawDegrees around Y) stays completely on the upper face. Returns false when the footprint cannot fit at all.
    /// </summary>
    public bool ClampToFootprint(ref Vector3 _worldPoint, Vector3 _halfExtents, float _yawDegrees)
    {
        float _relative = Mathf.DeltaAngle(transform.eulerAngles.y, _yawDegrees) * Mathf.Deg2Rad;
        float _cos = Mathf.Abs(Mathf.Cos(_relative));
        float _sin = Mathf.Abs(Mathf.Sin(_relative));
        // size of the footprint's bounding box in the surface's own axes, in meters
        float _extentX = _cos * _halfExtents.x + _sin * _halfExtents.z;
        float _extentZ = _sin * _halfExtents.x + _cos * _halfExtents.z;

        Vector3 _scale = transform.lossyScale;
        float _halfX = Box.size.x * 0.5f - edgeMargin / Mathf.Abs(_scale.x) - _extentX / Mathf.Abs(_scale.x);
        float _halfZ = Box.size.z * 0.5f - edgeMargin / Mathf.Abs(_scale.z) - _extentZ / Mathf.Abs(_scale.z);
        if (_halfX < 0f || _halfZ < 0f)
            return false;

        Vector3 _local = transform.InverseTransformPoint(_worldPoint);
        _local.x = Mathf.Clamp(_local.x, Box.center.x - _halfX, Box.center.x + _halfX);
        _local.z = Mathf.Clamp(_local.z, Box.center.z - _halfZ, Box.center.z + _halfZ);
        _worldPoint = transform.TransformPoint(_local);
        return true;
    }
}
