using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GrenadeSpawnerXR : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("The grenade prefab to spawn (must have GrenadeThrowXR and GrenadeExplosionXR).")]
    public GameObject grenadePrefab;

    [Tooltip("Where the grenade appears (this transform's position/rotation).")]
    public Transform spawnPoint;

    [Tooltip("Time after explosion before respawning a new grenade.")]
    public float respawnDelay = 4f;

    [Header("Spawning Behavior")]
    [Tooltip("Should the grenade respawn automatically after being destroyed?")]
    public bool autoRespawn = true;

    [Tooltip("Spawn the first grenade on Start?")]
    public bool spawnOnStart = true;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("Optional socket interactor to show preview/snap point.")]
    public XRSocketInteractor socket;

    // Runtime
    private GameObject currentGrenade;
    private bool isWaitingForRespawn;
    private Coroutine respawnCoroutine;

    void Start()
    {
        // Use this transform if no spawn point assigned
        if (!spawnPoint) spawnPoint = transform;

        // Spawn the first grenade
        if (spawnOnStart)
        {
            SpawnGrenade();
        }
    }

    void Update()
    {
        if (!autoRespawn) return;

        // Check if current grenade was destroyed (exploded) and we're not already waiting
        if (!isWaitingForRespawn && currentGrenade == null)
        {
            respawnCoroutine = StartCoroutine(RespawnAfterDelay());
        }
    }

    void OnDisable()
    {
        // Clean up coroutine if disabled
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }
    }

    public void SpawnGrenade()
    {
        if (!grenadePrefab)
        {
            Debug.LogError("[GrenadeSpawnerXR] No grenade prefab assigned!", this);
            return;
        }

        // Clean up any existing grenade
        if (currentGrenade != null)
        {
            Destroy(currentGrenade);
        }

        // Instantiate at spawn point
        currentGrenade = Instantiate(grenadePrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Ensure the grenade's rigidbody is awake and ready
        var rb = currentGrenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.WakeUp();
        }

        isWaitingForRespawn = false;
        
        Debug.Log($"[GrenadeSpawnerXR] Spawned grenade at {spawnPoint.position}");
    }

    IEnumerator RespawnAfterDelay()
    {
        isWaitingForRespawn = true;
        
        Debug.Log($"[GrenadeSpawnerXR] Grenade destroyed. Respawning in {respawnDelay}s...");
        
        yield return new WaitForSeconds(respawnDelay);
        
        SpawnGrenade();
        
        respawnCoroutine = null;
    }

    // Optional: Manual respawn trigger (useful for testing in editor)
    [ContextMenu("Force Respawn Grenade")]
    public void ForceRespawn()
    {
        if (currentGrenade != null)
            Destroy(currentGrenade);
        
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }
        
        isWaitingForRespawn = false;
        SpawnGrenade();
    }

    // Optional: Stop auto-respawning
    [ContextMenu("Stop Auto Respawn")]
    public void StopAutoRespawn()
    {
        autoRespawn = false;
        if (respawnCoroutine != null)
        {
            StopCoroutine(respawnCoroutine);
            respawnCoroutine = null;
        }
    }

    // Optional: Resume auto-respawning
    [ContextMenu("Resume Auto Respawn")]
    public void ResumeAutoRespawn()
    {
        autoRespawn = true;
    }
}
