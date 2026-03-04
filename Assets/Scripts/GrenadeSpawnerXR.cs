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

    [Tooltip("Only allow one grenade to exist at a time?")]
    public bool onlyOneAtATime = true;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("Optional socket interactor to show preview/snap point.")]
    public XRSocketInteractor socket;

    [Header("Debug")]
    public bool showDebugLogs = true;

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
            if (showDebugLogs)
                Debug.Log($"[GrenadeSpawnerXR] Detected grenade is null. Starting respawn...");
            
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

        // Check if we should only have one at a time
        if (onlyOneAtATime && currentGrenade != null)
        {
            if (showDebugLogs)
                Debug.Log("[GrenadeSpawnerXR] Grenade already exists. Skipping spawn.");
            return;
        }

        // Clean up any existing grenade
        if (currentGrenade != null)
        {
            if (showDebugLogs)
                Debug.Log("[GrenadeSpawnerXR] Destroying existing grenade before spawning new one.");
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
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.WakeUp();
        }

        // Subscribe to grenade destruction (optional enhancement)
        var explosionScript = currentGrenade.GetComponent<GrenadeExplosiveXR>();
        if (explosionScript != null)
        {
            // You could add an event here if you modify GrenadeExplosiveXR
            if (showDebugLogs)
                Debug.Log("[GrenadeSpawnerXR] Found GrenadeExplosiveXR component on spawned grenade.");
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
