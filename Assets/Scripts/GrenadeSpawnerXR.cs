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
    public float respawnDelay = 2f;

    [Header("Spawning Behavior")]
    [Tooltip("Should the grenade respawn automatically after being destroyed?")]
    public bool autoRespawn = true;

    [Tooltip("Spawn the first grenade on Start?")]
    public bool spawnOnStart = true;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("Optional socket interactor to show preview/snap point.")]
    public XRSocketInteractor socket;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // Runtime
    private GameObject currentGrenade;
    private bool isWaitingForRespawn;
    private Coroutine respawnCoroutine;
    private int grenadeCounter = 0; // Track how many grenades we've spawned

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

        // Clean up any existing grenade reference
        if (currentGrenade != null)
        {
            if (showDebugLogs)
                Debug.Log("[GrenadeSpawnerXR] Current grenade reference still exists. Clearing it.");
            currentGrenade = null;
        }

        // Instantiate at spawn point
        currentGrenade = Instantiate(grenadePrefab, spawnPoint.position, spawnPoint.rotation);
        grenadeCounter++;
        currentGrenade.name = $"Grenade_{grenadeCounter}"; // Give it a unique name for debugging
        
        // Ensure the grenade's rigidbody is awake and ready
        var rb = currentGrenade.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.WakeUp();
        }

        // Register this spawner with the grenade
        var explosionScript = currentGrenade.GetComponent<GrenadeExplosiveXR>();
        if (explosionScript != null)
        {
            explosionScript.SetSpawner(this);
            if (showDebugLogs)
                Debug.Log($"[GrenadeSpawnerXR] Registered spawner with {currentGrenade.name}.");
        }
        else
        {
            Debug.LogWarning($"[GrenadeSpawnerXR] {currentGrenade.name} missing GrenadeExplosiveXR component!");
        }

        isWaitingForRespawn = false;
        
        if (showDebugLogs)
            Debug.Log($"[GrenadeSpawnerXR] Spawned grenade '{currentGrenade.name}' at {spawnPoint.position}");
    }

    IEnumerator RespawnAfterDelay()
    {
        isWaitingForRespawn = true;
        
        if (showDebugLogs)
            Debug.Log($"[GrenadeSpawnerXR] Grenade destroyed. Respawning in {respawnDelay}s...");
        
        yield return new WaitForSeconds(respawnDelay);
        
        SpawnGrenade();
        
        respawnCoroutine = null;
    }

    // Public method that can be called by the grenade when it explodes
    public void NotifyGrenadeDestroyed(GameObject grenadeGO)
    {
        if (currentGrenade == grenadeGO)
        {
            if (showDebugLogs)
                Debug.Log($"[GrenadeSpawnerXR] Grenade '{grenadeGO.name}' notified spawner of destruction.");
            
            currentGrenade = null;
            
            // Start respawn if auto-respawn is enabled and not already waiting
            if (autoRespawn && !isWaitingForRespawn && respawnCoroutine == null)
            {
                respawnCoroutine = StartCoroutine(RespawnAfterDelay());
            }
        }
    }

    // Optional: Manual respawn trigger (useful for testing in editor)
    [ContextMenu("Force Respawn Grenade")]
    public void ForceRespawn()
    {
        if (showDebugLogs)
            Debug.Log("[GrenadeSpawnerXR] Force respawn triggered.");

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
        
        if (showDebugLogs)
            Debug.Log("[GrenadeSpawnerXR] Auto respawn stopped.");
    }

    // Optional: Resume auto-respawning
    [ContextMenu("Resume Auto Respawn")]
    public void ResumeAutoRespawn()
    {
        autoRespawn = true;
        
        if (showDebugLogs)
            Debug.Log("[GrenadeSpawnerXR] Auto respawn resumed.");
    }
}
