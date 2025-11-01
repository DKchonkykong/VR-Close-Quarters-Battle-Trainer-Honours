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
    [Header("Slide / joint")]
    public SlideJointXR slide;      // assign the slide object here
    public bool slideLocked;        // for debug

    // call this from input: fire
    public void FirePressed()
    {
        if (slideLocked)
        {
            Debug.Log("[GunXR] Slide locked, can't fire");
            return;
        }

        if (currentMag == null || currentMag.currentRounds <= 0)
        {
            Debug.Log("[GunXR] Dry");
            // play dry sound
            return;
        }

        currentMag.currentRounds--;
        Debug.Log("[GunXR] Bang. Ammo left: " + currentMag.currentRounds);

        // do hitscan etc...

        // if now empty → lock slide
        if (currentMag.currentRounds <= 0 && slide != null)
        {
            slideLocked = true;
            slide.LockSlideOpen();
            Debug.Log("[GunXR] Slide locked OPEN (empty)");
        }
    }

    // this is called BY THE SLIDE when user pulled it far and released
    public void OnSlideCharged()
    {
        // called by SlideJointXR when player pulled it far and released
        if (currentMag != null && currentMag.currentRounds > 0)
        {
            slideLocked = false;
            if (slide != null)
                slide.UnlockSlide();
            Debug.Log("Slide charged, gun ready.");
        }
        else
        {
            Debug.Log("Slide charged but no ammo in mag.");
        }

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
    }

    // Called by socket OR by MagazineXR when it detects socket selection
    // when mag is inserted
    public void OnMagazineInserted(MagazineXR mag)
    {
        currentMag = mag;
        Debug.Log("[GunXR] Mag inserted: " + mag.currentRounds);
        // NOTE: we do NOT auto-unlock slide here — user must rack it
    }

    public void OnMagazineRemoved(MagazineXR mag)
    {
        if (currentMag == mag)
            currentMag = null;
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
