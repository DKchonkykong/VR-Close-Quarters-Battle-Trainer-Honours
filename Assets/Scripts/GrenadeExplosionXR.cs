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
    public GameObject explosionVfxPrefab;
    [Tooltip("How long the particle effect lasts before being destroyed")]
    public float vfxDuration = 2f;

    AudioSource audioSource;
    bool beepPlayed;

    [Header("Debug")]
    public bool drawDebug = true;

    Rigidbody rb;
    XRGrabInteractable grab;
    float fuseTimer = -1f;
    bool startedBeeps;
    private float sfxVolume = 1f;

    // Reference to the spawner (set by spawner when grenade is created)
    private GrenadeSpawnerXR spawner;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (!audioSource) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;  // 3D sound
        audioSource.playOnAwake = false;

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

    // Called by the spawner to set itself as the parent spawner
    public void SetSpawner(GrenadeSpawnerXR grenadeSpawner)
    {
        spawner = grenadeSpawner;
    }

    void OnGrab(SelectEnterEventArgs args)
    {
        state = GrenadeState.Held;
        fuseTimer = -1f;
        startedBeeps = false;
        beepPlayed = false;
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

        if (!beepPlayed && beepClip != null && fuseTimer <= beepStartSecondsRemaining)
        {
            beepPlayed = true;
            AudioSource.PlayClipAtPoint(beepClip, transform.position, sfxVolume);
        }
        fuseTimer -= Time.deltaTime;

        if (state == GrenadeState.Thrown && fuseTimer > 0f)
        {
            if (!startedBeeps && fuseTimer <= beepStartSecondsRemaining && beepClip)
            {
                startedBeeps = true;
                audioSource.clip = beepClip;
                audioSource.loop = true;
                audioSource.Play();
            }
        }

        if (fuseTimer <= 0f)
            Explode();
    }

    void Explode()
    {
        if (state == GrenadeState.Exploded) return;
        state = GrenadeState.Exploded;

        Vector3 center = transform.position;

        // Freeze it so it doesn't "sink" after collider off
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        // Audio + VFX
        if (audioSource && audioSource.isPlaying) audioSource.Stop();

        // Spawn VFX and auto-destroy after duration
        if (explosionVfxPrefab)
        {
            GameObject vfx = Instantiate(explosionVfxPrefab, center, Quaternion.identity);
            
            // Stop particle emission after a short time, then destroy
            ParticleSystem[] particles = vfx.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles)
            {
                // Set particle system to stop emitting new particles
                var main = ps.main;
                main.loop = false;
                
                // Optional: reduce lifetime if particles last too long
                if (main.duration > vfxDuration)
                {
                    main.duration = vfxDuration * 0.5f;
                }
            }
            
            // Destroy the VFX GameObject after the duration
            Destroy(vfx, vfxDuration);
        }

        if (explosionClip)
            AudioSource.PlayClipAtPoint(explosionClip, center, 1f);

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

        // **NOTIFY SPAWNER BEFORE DESTROYING**
        if (spawner != null)
        {
            spawner.NotifyGrenadeDestroyed(gameObject);
        }

        // Disable grenade body visuals
        DisableGrenadeBody();
        
        // Destroy grenade GameObject immediately (VFX is separate now)
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