using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitDirection, float force);
    void TakeDamage(int damagePerShot);

    bool IsDead { get; }
}