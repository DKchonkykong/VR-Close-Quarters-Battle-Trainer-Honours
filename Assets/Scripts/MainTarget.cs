using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TargetType { Enemy, Hostage }

public class MainTarget : MonoBehaviour, IDamageable
{
    [Header("Target Type")]
    public TargetType type = TargetType.Enemy;
  
    [Header("Health")]
    public float maxHealth = 50f;
    public float currentHealth;
    public float respawnDelay = 3f;

    [Header("Visual")]
    public Renderer rend;
    public Color enemyHitColor = Color.green;
    public Color hostageHitColor = Color.red;
    public float resetTime = 0.5f;

    Color originalColor;
    Material mat;
    Vector3 originalPosition;
    Quaternion originalRotation;

    void Awake()
    {
        if (!rend)
            rend = GetComponentInChildren<Renderer>();

        if (rend)
        {
            mat = rend.material;              // instance material
            originalColor = mat.color;
        }

        // Store original transform for respawning
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Initialize health
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        
        OnHit(default);

        // Check if target is destroyed
        if (currentHealth <= 0)
        {
            DestroyTarget();
        }
    }

    public void OnHit(RaycastHit hit)
    {
        if (mat == null) return;

        // apply color depending on type
        switch (type)
        {
            case TargetType.Enemy:
                mat.color = enemyHitColor;    // good to shoot 
                break;

            case TargetType.Hostage:
                mat.color = hostageHitColor;  // bad to shoot 
                break;
        }

        CancelInvoke(nameof(ResetColor));
        Invoke(nameof(ResetColor), resetTime);
    }

    void ResetColor()
    {
        if (mat != null)
            mat.color = originalColor;
    }

    void DestroyTarget()
    {
        // Disable visuals + collider - NOT the GameObject!
        if (rend != null) rend.enabled = false;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // Start respawn timer
        StartCoroutine(RespawnAfterDelay());
    }

    IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);

        // Reset transform
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        // Reset health
        currentHealth = maxHealth;

        // Reset color
        if (mat != null)
            mat.color = originalColor;

        // Re-enable visuals + collider
        if (rend != null) rend.enabled = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

}