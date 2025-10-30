using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GunXR : MonoBehaviour
{
    [Header("References")]
    public XRSocketInteractor magSocket;     // socket child on the grip
    public Transform muzzle;                 // optional: where bullets would spawn

    [Header("Eject")]
    public Vector3 ejectImpulseLocal = new Vector3(0f, 0.8f, 0.2f);

    [Header("Fire")]
    public float fireCooldown = 0.12f;       // seconds between shots
    public AudioSource audioSource;
    public AudioClip fireClip;
    public AudioClip dryClip;
    public AudioClip insertClip;
    public AudioClip ejectClip;

    [Header("State (read-only)")]
    public MagazineXR currentMag;
    float _nextFireTime;

    void OnEnable()
    {
        if (magSocket)
        {
            magSocket.selectEntered.AddListener(OnSocketSelected);
            magSocket.selectExited.AddListener(OnSocketDeselected);
        }
    }

    void OnDisable()
    {
        if (magSocket)
        {
            magSocket.selectEntered.RemoveListener(OnSocketSelected);
            magSocket.selectExited.RemoveListener(OnSocketDeselected);
        }
    }

    // === Socket callbacks (magazine inserted/removed) ===
    void OnSocketSelected(SelectEnterEventArgs args)
    {
        var mag = (args.interactableObject as XRBaseInteractable)?.transform.GetComponent<MagazineXR>();
        if (!mag) return;
        OnMagazineInserted(mag);
    }

    void OnSocketDeselected(SelectExitEventArgs args)
    {
        var mag = (args.interactableObject as XRBaseInteractable)?.transform.GetComponent<MagazineXR>();
        if (!mag) return;
        OnMagazineRemoved(mag);
    }

    // Called by socket OR by MagazineXR when it detects socket selection
    public void OnMagazineInserted(MagazineXR mag)
    {
        currentMag = mag;
        if (audioSource && insertClip) audioSource.PlayOneShot(insertClip);
        // You could chamber a round here if you simulate that separately.
        // e.g., roundsInChamber = Mathf.Min(1, currentMag.currentRounds--);
    }

    public void OnMagazineRemoved(MagazineXR mag)
    {
        if (currentMag == mag) currentMag = null;
        if (audioSource && ejectClip) audioSource.PlayOneShot(ejectClip);
    }

    // === Public API ===

    /// <summary>Try to fire one shot. Returns true if a round was consumed.</summary>
    public bool TryFire()
    {
        if (Time.time < _nextFireTime) return false;

        if (currentMag == null || currentMag.currentRounds <= 0)
        {
            if (audioSource && dryClip) audioSource.PlayOneShot(dryClip);
            _nextFireTime = Time.time + 0.12f;
            return false;
        }

        currentMag.currentRounds--;
        _nextFireTime = Time.time + fireCooldown;

        // TODO: spawn projectile / hitscan, play VFX/SFX, recoil, haptics, slide animation
        if (audioSource && fireClip) audioSource.PlayOneShot(fireClip);

        return true;
    }

    /// <summary>Eject the currently inserted magazine (if any).</summary>
    public void EjectMag()
    {
        if (!currentMag) return;
        Vector3 worldImpulse = transform.TransformDirection(ejectImpulseLocal);
        var mag = currentMag;
        currentMag = null;
        mag.Eject(worldImpulse);
    }

    internal void EjectPressed()
    {
        EjectMag();
    }
}
