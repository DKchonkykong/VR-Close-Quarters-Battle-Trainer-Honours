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
    public float lethalInnerRadius = 1.0f; // full damage inside this
    public float impulse = 12f;
    public LayerMask damageMask = ~0;      // what can be damaged
    public LayerMask occlusionMask;        // walls/props that block explosion
    public bool useLineOfSight = true;

    [Header("Debug")]
    public bool drawDebug = true;

    Rigidbody rb;
    XRGrabInteractable grab;
    float fuseTimer = -1f;

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
        if (fuseTimer <= 0f)
            Explode();
    }

    void Explode()
    {
        if (state == GrenadeState.Exploded) return;
        state = GrenadeState.Exploded;

        Vector3 center = transform.position;

        // Broad phase: who is inside radius?
        Collider[] hits = Physics.OverlapSphere(center, radius, damageMask, QueryTriggerInteraction.Ignore);

        foreach (var col in hits)
        {
            // Find a damageable on this object or its parents
            var damageable = col.GetComponentInParent<IDamageable>();
            if (damageable == null || damageable.IsDead) continue;

            Vector3 closest = col.ClosestPoint(center);
            float dist = Vector3.Distance(center, closest);

            // LOS check (blast blocked by walls)
            if (useLineOfSight)
            {
                Vector3 dir = (closest - center);
                float len = dir.magnitude;
                if (len > 0.001f)
                {
                    if (Physics.Raycast(center, dir.normalized, out RaycastHit blockHit, len, occlusionMask, QueryTriggerInteraction.Ignore))
                    {
                        // Something blocks the explosion -> no damage
                        continue;
                    }
                }
            }

            float dmg = ComputeDamage(dist);
            Vector3 hitDir = (closest - center).normalized;

            // Apply damage + optional force info
            damageable.TakeDamage(dmg, closest, hitDir, impulse);

            // Apply physics impulse if it has a rigidbody
            var hitRb = col.attachedRigidbody;
            if (hitRb != null && !hitRb.isKinematic)
                hitRb.AddExplosionForce(impulse * 50f, center, radius, 0.3f, ForceMode.Impulse);
        }

        // Optional: disable visuals/collider then destroy
        DisableGrenadeBody();
        Destroy(gameObject, 0.2f);
    }

    float ComputeDamage(float distance)
    {
        if (distance <= lethalInnerRadius) return maxDamage;
        if (distance >= radius) return 0f;

        // Smooth falloff (more “technical” than linear)
        float t = Mathf.InverseLerp(radius, lethalInnerRadius, distance);
        float smooth = t * t * (3f - 2f * t); // SmoothStep
        return Mathf.Lerp(minDamage, maxDamage, smooth);
    }

    void DisableGrenadeBody()
    {
        // Stop further collisions
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;

        // Hide mesh if needed
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