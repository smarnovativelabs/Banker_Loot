using UnityEngine;

[RequireComponent(typeof(Light))]
public class SkullFireFlicker : MonoBehaviour
{
    Light l; float baseI; float seed;
    void Awake() { l = GetComponent<Light>(); baseI = l.intensity; seed = Random.value * 100f; }
    void Update()
    {
        float n = Mathf.PerlinNoise(Time.time * 7f, seed);
        l.intensity = baseI * Mathf.Lerp(0.7f, 1.2f, n);
    }
}
