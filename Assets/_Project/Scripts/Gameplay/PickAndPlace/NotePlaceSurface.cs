using UnityEngine;

/// <summary>
/// Marks the upper face of a box (a table, counter, crate...) as a place where carried notes can be put down.
/// Put it on an object with a BoxCollider that is only rotated around Y.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class NotePlaceSurface : MonoBehaviour
{
    [Tooltip("Notes stay at least this far (meters) from the edge of the surface")]
    public float edgeMargin = 0.02f;

    BoxCollider box;

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
