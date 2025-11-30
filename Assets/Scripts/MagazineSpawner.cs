using UnityEngine;

public class MagazineSpawner : MonoBehaviour
{
    [Header("Spawn setup")]
    public GameObject magazinePrefab;
    public Transform spawnPoint;       // where the mag appears
    public bool spawnOnStart = true;

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
        
        // Hook up the magazine to notify us when it's ejected
        var magXR = currentMag.GetComponent<MagazineXR>();
        if (magXR != null && spawnOnEject)
        {
            magXR.onMagazineEjected += OnMagazineEjected;
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
