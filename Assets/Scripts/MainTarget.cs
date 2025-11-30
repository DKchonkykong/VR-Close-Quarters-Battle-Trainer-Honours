using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TargetType { Enemy, Hostage }

public class MainTarget : MonoBehaviour, IDamageable
{
    [Header("Target Type")]
    public TargetType type = TargetType.Enemy;
  
    [Header("Visual")]
    public Renderer rend;
    public Color enemyHitColor = Color.green;
    public Color hostageHitColor = Color.red;
    public float resetTime = 0.5f;

    Color originalColor;
    Material mat;

    void Awake()
    {
        if (!rend)
            rend = GetComponentInChildren<Renderer>();

        if (rend)
        {
            mat = rend.material;              // instance material
            originalColor = mat.color;
        }
    }

    public void TakeDamage(int amount)
    {
        OnHit(default);
    }

    public void OnHit(RaycastHit hit)
    {
        if (mat == null) return;

        // apply color depending on type
        switch (type)
        {
            case TargetType.Enemy:
                mat.color = enemyHitColor;    // good to shoot ✔
                break;

            case TargetType.Hostage:
                mat.color = hostageHitColor;  // bad to shoot ✘
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
}