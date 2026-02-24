using UnityEngine;

[DisallowMultipleComponent]
public class SnowTrailController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private ParticleSystem[] trails;

    [Header("Movement")]
    [SerializeField] private float minSpeedToEmit = 0.06f;
    [SerializeField] private float movingGraceSeconds = 0.12f;
    [SerializeField] private bool alwaysEmit = false;

    [Header("Snow Detection")]
    [SerializeField] private bool requireSnowContact = true;
    [SerializeField] private string snowTag = "Snow";
    [SerializeField] private LayerMask snowLayer;

    [Header("Debug")]
    [SerializeField] private bool startOnSnow = false;

    private bool onSnow;
    private Vector3 lastPosition;
    private float lastMovingTime;
    private float[] baseDistanceRates;
    private float[] baseTimeRates;

    private void Awake()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = GetComponentInParent<Rigidbody2D>();
            }
        }

        if ((trails == null || trails.Length == 0))
        {
            trails = GetComponentsInChildren<ParticleSystem>(true);
        }

        CacheBaseRates();
        onSnow = startOnSnow;
        lastPosition = transform.position;
        lastMovingTime = -999f;
        EnsureEmissionEnabled();
        SetTrailEmissionActive(false);
    }

    private void Update()
    {
        float speed;
        if (rb != null)
        {
            speed = rb.linearVelocity.magnitude;
        }
        else
        {
            Vector3 delta = transform.position - lastPosition;
            speed = (delta / Mathf.Max(Time.deltaTime, 0.0001f)).magnitude;
        }

        lastPosition = transform.position;
        bool movingNow = speed > minSpeedToEmit;
        if (movingNow)
        {
            lastMovingTime = Time.time;
        }

        bool moving = (Time.time - lastMovingTime) <= movingGraceSeconds;
        bool canEmit = moving && (!requireSnowContact || onSnow);
        if (alwaysEmit)
        {
            canEmit = true;
        }

        SetTrailEmissionActive(canEmit);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsSnowCollider(other))
        {
            onSnow = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (IsSnowCollider(other))
        {
            onSnow = false;
        }
    }

    private bool IsSnowCollider(Collider2D other)
    {
        bool matchesTag = !string.IsNullOrWhiteSpace(snowTag) && other.CompareTag(snowTag);
        bool matchesLayer = (snowLayer.value & (1 << other.gameObject.layer)) != 0;
        return matchesTag || matchesLayer;
    }

    private void EnsureEmissionEnabled()
    {
        if (trails == null)
        {
            return;
        }

        for (int i = 0; i < trails.Length; i++)
        {
            ParticleSystem ps = trails[i];
            if (ps == null)
            {
                continue;
            }

            ParticleSystem.EmissionModule emission = ps.emission;
            if (!emission.enabled)
            {
                emission.enabled = true;
            }
        }
    }

    private void CacheBaseRates()
    {
        if (trails == null)
        {
            return;
        }

        baseDistanceRates = new float[trails.Length];
        baseTimeRates = new float[trails.Length];

        for (int i = 0; i < trails.Length; i++)
        {
            ParticleSystem ps = trails[i];
            if (ps == null)
            {
                continue;
            }

            ParticleSystem.EmissionModule emission = ps.emission;
            baseDistanceRates[i] = emission.rateOverDistanceMultiplier;
            baseTimeRates[i] = emission.rateOverTimeMultiplier;
        }
    }

    private void SetTrailEmissionActive(bool active)
    {
        if (trails == null)
        {
            return;
        }

        for (int i = 0; i < trails.Length; i++)
        {
            ParticleSystem ps = trails[i];
            if (ps == null)
            {
                continue;
            }

            ParticleSystem.EmissionModule emission = ps.emission;
            float distanceRate = baseDistanceRates != null && i < baseDistanceRates.Length ? baseDistanceRates[i] : emission.rateOverDistanceMultiplier;
            float timeRate = baseTimeRates != null && i < baseTimeRates.Length ? baseTimeRates[i] : emission.rateOverTimeMultiplier;

            emission.rateOverDistanceMultiplier = active ? distanceRate : 0f;
            emission.rateOverTimeMultiplier = active ? timeRate : 0f;

            if (!ps.isPlaying)
            {
                ps.Play(true);
            }
        }
    }
}
