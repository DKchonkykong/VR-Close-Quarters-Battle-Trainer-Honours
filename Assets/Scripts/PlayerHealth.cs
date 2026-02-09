using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 5;
    public int currentHealth;

    [Header("On Death")]
    public float resetDelay = 1.0f;
    public bool reloadSceneOnDeath = true;

    void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log($"Player hit! HP: {currentHealth}/{maxHealth}");

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        Debug.Log("Player died.");

        if (reloadSceneOnDeath)
            StartCoroutine(ReloadAfterDelay());
    }

    IEnumerator ReloadAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
