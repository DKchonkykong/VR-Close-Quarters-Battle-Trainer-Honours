using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(XRGrabInteractable))]
public class GrenadeExplosiveXR : MonoBehaviour
{
    public enum GrenadeState { Idle, Held, Thrown, Exploded }
    public GrenadeState state = GrenadeState.Idle;

    [Header("Fuse")]
    public float fuseSeconds = 2.0f;
    public bool startFuseOnRelease = true;

    [Header("Explosion")]
    public float radius = 4.5f;
    public float maxDamage = 100f;
    public float minDamage = 10f;
    public float lethalInnerRadius = 1.0f;
    public float impulse = 12f;
    public LayerMask damageMask = ~0;
    public LayerMask occlusionMask;
    public bool useLineOfSight = true;

    [Header("Audio / VFX")]
    public AudioClip explosionClip;
    public AudioClip beepClip;
    public float beepStartSecondsRemaining = 0.6f;
    public GameObject explosionVfxPrefab; // optional (particle prefab)

    [Header("Debug")]
    public bool drawDebug = true;

    Rigidbody rb;
    XRGrabInteractable grab;
    float fuseTimer = -1f;
    bool beeped;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grab = GetComponent<XRGrabInteractable>();

        grab.selectEntered.AddListener(OnGrab);
        grab.selectExited.AddListener(OnRelease);
    }

    void OnDestroy()
    {
        grab.selectEntered.RemoveListener(OnGrab);
        grab.selectExited.RemoveListener(OnRelease);
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        state = GrenadeState.Held;
        fuseTimer = -1f;
        beeped = false;
    }

    void OnRelease(SelectExitEventArgs args)
    {
        state = GrenadeState.Thrown;
        if (startFuseOnRelease)
            fuseTimer = fuseSeconds;
    }

    void Update()
    {
        if (state != GrenadeState.Thrown) return;
        if (fuseTimer < 0f) return;

        fuseTimer -= Time.deltaTime;

        // optional beep near the end
        if (!beeped && beepClip && fuseTimer <= beepStartSecondsRemaining)
        {
            AudioSource.PlayClipAtPoint(beepClip, transform.position);
            beeped = true;
        }

        if (fuseTimer <= 0f)
            Explode();
    }

    void Explode()
    {
        if (state == GrenadeState.Exploded) return;
        state = GrenadeState.Exploded;

        Vector3 center = transform.position;

        // Freeze it so it doesn’t “sink” after collider off
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Audio + VFX
        if (explosionClip)
            AudioSource.PlayClipAtPoint(explosionClip, center);

        if (explosionVfxPrefab)
            Instantiate(explosionVfxPrefab, center, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(center, radius, damageMask, QueryTriggerInteraction.Ignore);

        foreach (var col in hits)
        {
            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead) continue;

            Vector3 closest = col.ClosestPoint(center);
            float dist = Vector3.Distance(center, closest);

            if (useLineOfSight)
            {
                Vector3 dir = (closest - center);
                float len = dir.magnitude;
                if (len > 0.001f)
                {
                    if (Physics.Raycast(center, dir.normalized, out _, len, occlusionMask, QueryTriggerInteraction.Ignore))
                        continue; // blocked
                }
            }

            float dmg = ComputeDamage(dist);
            Vector3 hitDir = (closest - center).normalized;
            damageable.TakeDamage(dmg, closest, hitDir, impulse);

            var hitRb = col.attachedRigidbody;
            if (hitRb != null && !hitRb.isKinematic)
                hitRb.AddExplosionForce(impulse * 50f, center, radius, 0.3f, ForceMode.Impulse);
        }

        DisableGrenadeBody();
        Destroy(gameObject, 0.05f);
    }

    float ComputeDamage(float distance)
    {
        if (distance <= lethalInnerRadius) return maxDamage;
        if (distance >= radius) return 0f;

        float t = Mathf.InverseLerp(radius, lethalInnerRadius, distance);
        float smooth = t * t * (3f - 2f * t); // SmoothStep falloff
        return Mathf.Lerp(minDamage, maxDamage, smooth);
    }

    void DisableGrenadeBody()
    {
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        var mr = GetComponentInChildren<MeshRenderer>();
        if (mr) mr.enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebug) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, lethalInnerRadius);
    }
}