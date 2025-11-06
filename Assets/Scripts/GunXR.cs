using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class GunXR : MonoBehaviour
{
    [Header("Refs")]
    public XRSocketInteractor magSocket;
    public Transform muzzle;
    public LayerMask hitMask = ~0;
    public AudioSource audioSource;
    public AudioClip fireClip, dryClip, insertClip, ejectClip;

    [Header("Fire")]
    public float fireCooldown = 0.12f;
    public float maxRange = 100f;

    [Header("Slide (no joint)")]
    public SlideConstrainedZ slide;
    public float blowbackDist = 0.01f;   // small back kick
    public float blowbackBackTime = 0.05f;
    public float blowbackForwardTime = 0.08f;

    [Header("State")]
    public MagazineXR currentMag;
    bool slideLocked = false;
    float nextFire;

    void OnEnable()
    {
        if (magSocket)
        {
            magSocket.selectEntered.AddListener(a => {
                currentMag = a.interactableObject.transform.GetComponent<MagazineXR>();
                if (audioSource && insertClip) audioSource.PlayOneShot(insertClip);
            });
            magSocket.selectExited.AddListener(a => {
                if (currentMag && a.interactableObject.transform.GetComponent<MagazineXR>() == currentMag)
                    currentMag = null;
                if (audioSource && ejectClip) audioSource.PlayOneShot(ejectClip);
            });
        }
    }

    public void FirePressed()
    {
        if (Time.time < nextFire) return;
        if (slideLocked) return;

        if (currentMag == null || currentMag.currentRounds <= 0)
        {
            if (audioSource && dryClip) audioSource.PlayOneShot(dryClip);
            nextFire = Time.time + 0.1f;
            return;
        }

        currentMag.currentRounds--;
        nextFire = Time.time + fireCooldown;

        if (audioSource && fireClip) audioSource.PlayOneShot(fireClip);

        // hitscan
        Vector3 o = muzzle ? muzzle.position : transform.position;
        Vector3 d = muzzle ? muzzle.forward : transform.forward;
        if (Physics.Raycast(o, d, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
            Debug.DrawLine(o, hit.point, Color.red, 0.15f);

        // blowback animation (moves slide along local Z briefly)
        if (slide) StartCoroutine(BlowbackConstrained());

        // empty → lock
        if (currentMag.currentRounds <= 0)
            slideLocked = true;
    }

    IEnumerator BlowbackConstrained()
    {
        var tr = slide.transform;
        var parent = slide.parentSpace ? slide.parentSpace : tr.parent;
        var lp0 = parent.InverseTransformPoint(tr.position);
        float backZ = lp0.z - blowbackDist; // assume back is negative Z

        // go back
        float t = 0f;
        while (t < blowbackBackTime)
        {
            t += Time.deltaTime;
            var lp = lp0; lp.z = Mathf.Lerp(lp0.z, backZ, t / blowbackBackTime);
            tr.position = parent.TransformPoint(lp);
            yield return null;
        }
        // return
        t = 0f;
        while (t < blowbackForwardTime)
        {
            t += Time.deltaTime;
            var lp = lp0; lp.z = Mathf.Lerp(backZ, lp0.z, t / blowbackForwardTime);
            tr.position = parent.TransformPoint(lp);
            yield return null;
        }
        tr.position = parent.TransformPoint(lp0);
    }

    // called by SlideConstrainedZ when player pulled far enough & released
    public void OnSlideCharged()
    {
        if (currentMag && currentMag.currentRounds > 0)
            slideLocked = false; // gun armed again
    }

    public void EjectPressed()
    {
        if (!magSocket || !magSocket.hasSelection) return;
        var io = magSocket.firstInteractableSelected;
        magSocket.interactionManager.SelectExit(magSocket, io);
    }

    internal void OnMagazineInserted(MagazineXR magazineXR)
    {
        if (magazineXR == null) return;

        // Link gun <-> mag
        currentMag = magazineXR;
        magazineXR.currentGun = this;
        magazineXR.isInserted = true;

        // Ensure ammo counts are sane
        magazineXR.currentRounds = Mathf.Clamp(magazineXR.currentRounds, 0, Mathf.Max(0, magazineXR.maxRounds));
    }

    internal void OnMagazineRemoved(MagazineXR magazineXR)
    {
        if (magazineXR == null) return;

        // If this mag was our active mag, clear it
        if (currentMag == magazineXR)
            currentMag = null;

        // Unlink gun <-> mag
        magazineXR.currentGun = null;
        magazineXR.isInserted = false;
    }
}
