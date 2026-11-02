using UnityEngine;

/// <summary>Tuning for a thrown note, kept on NotePickupManager so every note flies the same way.</summary>
[System.Serializable]
public class PaperFlightSettings
{
    [Header("Air")]
    [Tooltip("Resistance when the note moves face first. Higher = slower, floatier fall (falling speed is about sqrt(9.81 / this))")]
    public float faceDrag = 5f;
    [Tooltip("Resistance when the note slices through the air edge first")]
    public float edgeDrag = 0.2f;
    [Tooltip("How strongly the note turns its face into the airflow (it stalls and drops sooner when high)")]
    public float alignTorque = 1f;
    [Tooltip("Random wobble in rad/s^2, makes the note flutter")]
    public float wobble = 6f;
    public float wobbleFrequency = 2.5f;
    public float airLinearDamping = 0.05f;
    public float airAngularDamping = 0.3f;

    [Header("Launch")]
    [Tooltip("Seconds the note takes to level out of the hand pose after it is released")]
    public float levelTime = 0.12f;

    [Header("Landing")]
    [Tooltip("Higher damping after the first touch makes the note settle softly instead of sliding and bouncing")]
    public float landedLinearDamping = 3f;
    public float landedAngularDamping = 6f;
    [Tooltip("Touches this soon after the throw (hand, other thrown notes) do not count as landing")]
    public float landingGrace = 0.25f;
    public float maxFlightTime = 8f;
}

/// <summary>
/// Paper aerodynamics for a thrown note. Resistance acts along the note's face, so a falling sheet pushes itself
/// sideways when tilted (it glides), is slowed hard when it falls flat, turns its face into the airflow and
/// wobbles. It is only active between the throw and the first landing, so notes in the heap cost nothing.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PaperFlight : MonoBehaviour
{
    PaperFlightSettings settings;
    Rigidbody body;
    Quaternion levelFrom;
    Quaternion levelTo;
    float spin;
    float age;
    float seed;
    bool spinApplied;
    bool landed;

    public bool IsFlying => enabled && !landed;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        enabled = false;
    }

    /// <summary>Starts the flight: the note levels out to the given rotation, then spins about its face.</summary>
    public void Begin(PaperFlightSettings _settings, Quaternion _levelTo, float _spin)
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody>();
        }
        settings = _settings;
        levelFrom = body.rotation;
        levelTo = _levelTo;
        spin = _spin;
        age = 0f;
        seed = Random.value * 100f;
        spinApplied = false;
        landed = false;
        body.linearDamping = settings.airLinearDamping;
        body.angularDamping = settings.airAngularDamping;
        body.angularVelocity = Vector3.zero;
        enabled = true;
    }

    void FixedUpdate()
    {
        Step(Time.fixedDeltaTime);
    }

    public void Step(float _dt)
    {
        if (landed || settings == null)
            return;

        age += _dt;
        if (age > settings.maxFlightTime)
        {
            EndFlight();
            return;
        }

        if (age < settings.levelTime)
        {
            float _t = Mathf.SmoothStep(0f, 1f, age / settings.levelTime);
            body.MoveRotation(Quaternion.Slerp(levelFrom, levelTo, _t));
            body.angularVelocity = Vector3.zero;
            return;
        }
        if (!spinApplied)
        {
            spinApplied = true;
            body.angularVelocity = levelTo * Vector3.up * spin;
        }

        Vector3 _velocity = body.linearVelocity;
        Vector3 _normal = transform.up;
        float _normalSpeed = Vector3.Dot(_velocity, _normal);
        Vector3 _sideways = _velocity - _normal * _normalSpeed;

        // resistance along the face (quadratic) and along the sheet (linear): this is what makes it glide
        Vector3 _drag = -_normal * (settings.faceDrag * _normalSpeed * Mathf.Abs(_normalSpeed)) - _sideways * settings.edgeDrag;
        body.AddForce(_drag, ForceMode.Acceleration);

        float _speed = _velocity.magnitude;
        if (_speed > 0.05f)
        {
            // turn the face into the airflow
            Vector3 _facing = _normal * (_normalSpeed >= 0f ? 1f : -1f);
            body.AddTorque(Vector3.Cross(_facing, _velocity / _speed) * (settings.alignTorque * Mathf.Min(_speed, 3f)), ForceMode.Acceleration);

            // flutter
            float _k = Mathf.Clamp01(_speed / 1.5f) * settings.wobble;
            float _time = seed + age * settings.wobbleFrequency;
            var _noise = new Vector3(
                Mathf.PerlinNoise(_time, 0f) * 2f - 1f,
                Mathf.PerlinNoise(_time, 7.3f) * 2f - 1f,
                Mathf.PerlinNoise(_time, 14.6f) * 2f - 1f);
            body.AddTorque(_noise * _k, ForceMode.Acceleration);
        }
    }

    void OnCollisionEnter(Collision _collision)
    {
        if (landed || settings == null || age < settings.landingGrace)
            return;
        // other notes thrown just before this one do not end the flight
        if (age < 0.6f && _collision.collider.GetComponentInParent<PickableNote>() != null)
            return;
        EndFlight();
    }

    void EndFlight()
    {
        landed = true;
        if (body != null && settings != null)
        {
            body.linearDamping = settings.landedLinearDamping;
            body.angularDamping = settings.landedAngularDamping;
        }
        enabled = false;
    }
}
