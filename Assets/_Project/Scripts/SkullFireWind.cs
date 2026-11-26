using UnityEngine;

/// <summary>
/// Natural torch-fire wind. A slow gust (smooth noise) pushes the fire sideways:
/// the flame itself only leans a little, while smoke and embers get blown clearly,
/// so the fire stays compact on the skull and the smoke trails off like real wind.
/// </summary>
public class SkullFireWind : MonoBehaviour
{
    [Tooltip("Wind direction (world space, horizontal).")]
    public Vector3 direction = new Vector3(1f, 0f, 0.3f);
    [Tooltip("Base wind speed. Smoke and embers feel this fully.")]
    public float strength = 0.5f;
    [Tooltip("How fast gusts change.")]
    public float gustSpeed = 0.5f;
    [Tooltip("0 = constant wind, 1 = gusts that fully die down.")]
    [Range(0f, 1f)] public float gustiness = 0.6f;

    [Header("How much each layer is pushed")]
    [Range(0f, 1f)] public float flameInfluence = 0.12f;
    public float smokeInfluence = 1.2f;
    public float emberInfluence = 1.5f;

    [Header("Flame lean (degrees)")]
    public float flameLean = 6f;

    ParticleSystem[] systems;
    float[] influence;
    Transform flame;
    float seed;

    void Awake()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
        influence = new float[systems.Length];
        seed = Random.value * 100f;
        var f = transform.Find("Flame");
        if (f != null) flame = f;
        for (int i = 0; i < systems.Length; i++)
        {
            string n = systems[i].name.ToLowerInvariant();
            influence[i] = n.Contains("smoke") ? smokeInfluence : n.Contains("ember") ? emberInfluence : flameInfluence;
            var fo = systems[i].forceOverLifetime;
            fo.enabled = true;
            fo.space = ParticleSystemSimulationSpace.World;
        }
    }

    void Update()
    {
        float t = Time.time * gustSpeed;
        float gust = Mathf.PerlinNoise(t, seed);                       // 0..1, slow
        float flutter = Mathf.PerlinNoise(t * 4f + 31f, seed) - 0.5f;  // small fast jitter
        float k = strength * Mathf.Lerp(1f, gust * 2f, gustiness) + flutter * strength * 0.3f;
        float side = (Mathf.PerlinNoise(seed + 17f, t * 1.3f) - 0.5f) * strength;

        Vector3 d = direction.normalized;
        Vector3 w = d * k + new Vector3(-d.z, 0f, d.x) * side;

        for (int i = 0; i < systems.Length; i++)
        {
            var ps = systems[i];
            if (ps == null) continue;
            var fo = ps.forceOverLifetime;
            fo.x = w.x * influence[i];
            fo.y = 0f;
            fo.z = w.z * influence[i];
        }

        // Lean the whole flame a few degrees with the gust (looks like it bends in the wind).
        if (flame != null)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, d);
            float lean = (gust - 0.35f) * flameLean * 2f + flutter * flameLean;
            Quaternion world = Quaternion.AngleAxis(lean, axis) * Quaternion.Euler(270f, 0f, 0f);
            flame.localRotation = Quaternion.Inverse(transform.rotation) * world;
        }
    }
}
