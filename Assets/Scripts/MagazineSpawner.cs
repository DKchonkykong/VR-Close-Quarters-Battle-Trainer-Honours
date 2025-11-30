using UnityEngine;

public class MagazineSpawner : MonoBehaviour
{
    [Header("Spawn setup")]
    public GameObject magazinePrefab;
    public Transform spawnPoint;       // where the mag appears
    public bool spawnOnStart = true;

    [Header("Magazine Config")]
    public int magazineMaxRounds = 30;     // ← Add this
    public int magazineStartRounds = 30;   // ← Add this

    [Header("Limit")]
    public bool onlyOneAtATime = true;
    public bool spawnOnEject = true;   // automatically spawn when mag is ejected

    GameObject currentMag;

    void Start()
    {
        if (spawnOnStart)
            SpawnMagazine();
    }

    // Call this from code or a UnityEvent
    public void SpawnMagazine()
    {
        if (magazinePrefab == null || spawnPoint == null)
        {
            Debug.LogWarning("[MagazineSpawner] Missing prefab or spawn point.", this);
            return;
        }

        if (onlyOneAtATime && currentMag != null)
            return;

        currentMag = Instantiate(magazinePrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Initialize the magazine's ammo count ← NEW CODE
        var magXR = currentMag.GetComponent<MagazineXR>();
        if (magXR != null)
        {
            magXR.maxRounds = magazineMaxRounds;
            magXR.currentRounds = magazineStartRounds;
            
            if (spawnOnEject)
            {
                magXR.onMagazineEjected += OnMagazineEjected;
            }
        }
    }

    // Called when a magazine is ejected from the gun
    void OnMagazineEjected(MagazineXR mag)
    {
        if (mag != null)
        {
            // Unsubscribe from the event
            mag.onMagazineEjected -= OnMagazineEjected;
        }

        currentMag = null;
        
        // Spawn a new magazine immediately
        if (spawnOnEject)
        {
            SpawnMagazine();
        }
    }

    // the magazine can tell the spawner if it's destroyed 
    public void NotifyMagazineDestroyed(GameObject magGO)
    {
        if (currentMag == magGO)
            currentMag = null;
    }

    void Update()
    {
        // debug: press M to spawn
        if (Input.GetKeyDown(KeyCode.M))
        {
            SpawnMagazine();
        }
    }
}
