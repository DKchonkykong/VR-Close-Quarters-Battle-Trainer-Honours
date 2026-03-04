using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;


//grenade spawner similar to magazine spawner it will respawn the grenade after it explodes this is for testing to make sure everything works correctly
public class GrenadeSpawnerXR : MonoBehaviour
{
    [Header("Spawning")]
    [Tooltip("The grenade prefab to spawn (must have GrenadeThrowXR and GrenadeExplosionXR).")]
    public GameObject grenadePrefab;

    [Tooltip("Where the grenade appears (this transform's position/rotation).")]
    public Transform spawnPoint;

    [Tooltip("Time after explosion before respawning a new grenade.")]
    public float respawnDelay = 4f;

    [Header("Visual Feedback (Optional)")]
    [Tooltip("Optional socket interactor to show preview/snap point.")]
    public XRSocketInteractor socket;

    // Runtime
    GameObject currentGrenade;
    bool isWaitingForRespawn;

    void Start()
    {
        // Use this transform if no spawn point assigned
        if (!spawnPoint) spawnPoint = transform;

        // Spawn the first grenade
        SpawnGrenade();
    }

    void Update()
    {
        // Check if current grenade was destroyed (exploded)
        if (!isWaitingForRespawn && currentGrenade == null)
        {
            StartCoroutine(RespawnAfterDelay());
        }
    }

    void SpawnGrenade()
    {
        if (!grenadePrefab)
        {
            Debug.LogError("[GrenadeSpawnerXR] No grenade prefab assigned!", this);
            return;
        }

        // Instantiate at spawn point
        currentGrenade = Instantiate(grenadePrefab, spawnPoint.position, spawnPoint.rotation);

        // Optional: Subscribe to explosion event for immediate respawn trigger
        var explosionScript = currentGrenade.GetComponent<GrenadeExplosiveXR>();
        if (explosionScript != null)
        {
            // If your GrenadeExplosionXR has an event, hook it here
            // For now we rely on the grenade being destroyed
        }

        // Optional: If using a socket interactor for visual feedback
        //don't need this though
        //if (socket && !socket.hasSelection)
        //{
        //    var grabInteractable = currentGrenade.GetComponent<XRGrabInteractable>();
        //    if (grabInteractable)
        //    {
        //        // Try to snap it into the socket (visual only, not grabbed)
        //        socket.interactionManager.SelectEnter(socket, grabInteractable);
        //    }
        //}

        isWaitingForRespawn = false;
    }

    IEnumerator RespawnAfterDelay()
    {
        isWaitingForRespawn = true;
        
        Debug.Log($"[GrenadeSpawnerXR] Grenade exploded. Respawning in {respawnDelay}s...");
        
        yield return new WaitForSeconds(respawnDelay);
        
        SpawnGrenade();
    }

    // Optional: Manual respawn trigger (useful for testing in editor)
    [ContextMenu("Force Respawn Grenade")]
    void ForceRespawn()
    {
        if (currentGrenade != null)
            Destroy(currentGrenade);
        
        StopAllCoroutines();
        isWaitingForRespawn = false;
        SpawnGrenade();
    }
}
