using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public interface IDamageable
{
    void TakeDamage(int amount);
}

public class MainTarget : MonoBehaviour, IDamageable
{
    //basically this handles the target's health and what it doesn when it is hit by a raycast
    public int health = 50;
    public Renderer visual;
    public Color hitColor = Color.red;
    public float resetTime = 2f;


    Color _originalColor;
    Material _mat;

    void Awake()
    {
        if (visual == null)
        {
            visual = GetComponentInChildren<Renderer>();
        }
        if (visual != null)
        {
            // make a unique material instance for this target
            _mat = visual.material;
            _originalColor = _mat.color;
        }

    }

    public void TakeDamage(int amount)
    {
        OnHit(default);
    }

    public void OnHit(RaycastHit hit)
    {
        if (_mat == null) return;

        _mat.color = hitColor;

        // if we get hit again, restart the timer
        CancelInvoke(nameof(ResetColor));
        Invoke(nameof(ResetColor), resetTime);
    }
    void ResetColor()
    {
        if (_mat == null) return;
        _mat.color = _originalColor;
    }
}