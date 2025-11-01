using UnityEngine;

public class MagazineSpawner : MonoBehaviour
{
    [Header("Spawn setup")]
    public GameObject magazinePrefab;
    public Transform spawnPoint;       // where the mag appears
    public bool spawnOnStart = true;

    [Header("Limit")]
    public bool onlyOneAtATime = true;

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
