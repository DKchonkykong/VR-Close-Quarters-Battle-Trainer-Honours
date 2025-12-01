using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform[] spawnPositions;
    [SerializeField] private float spawnDuration = 30f;
    
    [Header("Current State")]
    [SerializeField] private GameObject currentEnemy;
    private float elapsedTime = 0f;
    private bool isSpawning = false;

    void Start()
    {
        StartSpawning();
    }

    void Update()
    {
        if (isSpawning)
        {
            elapsedTime += Time.deltaTime;
            
            // Check if spawn duration has ended
            if (elapsedTime >= spawnDuration)
            {
                EndSpawning();
            }
            
            // Check if current enemy is destroyed, spawn a new one
            if (currentEnemy == null && elapsedTime < spawnDuration)
            {
                SpawnEnemy();
            }
        }
    }

    public void StartSpawning()
    {
        isSpawning = true;
        elapsedTime = 0f;
        SpawnEnemy();
    }

    private void SpawnEnemy()
    {
        if (spawnPositions.Length == 0)
        {
            Debug.LogWarning("No spawn positions assigned!");
            return;
        }

        if (enemyPrefab == null)
        {
            Debug.LogWarning("No enemy prefab assigned!");
            return;
        }

        // Get random spawn position
        Transform randomSpawnPoint = spawnPositions[Random.Range(0, spawnPositions.Length)];
        
        // Spawn enemy at random position
        currentEnemy = Instantiate(enemyPrefab, randomSpawnPoint.position, randomSpawnPoint.rotation);
        
        Debug.Log($"Enemy spawned at {randomSpawnPoint.name}");
    }

    private void EndSpawning()
    {
        isSpawning = false;
        Debug.Log("Spawning ended!");
        
        // Optional: Destroy remaining enemy
        if (currentEnemy != null)
        {
            Destroy(currentEnemy);
        }
    }

    public void ResetSpawner()
    {
        if (currentEnemy != null)
        {
            Destroy(currentEnemy);
        }
        elapsedTime = 0f;
        isSpawning = false;
    }
}
