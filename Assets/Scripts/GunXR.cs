using System;
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
    public SlideRail slide;

    [Header("State")]
    public MagazineXR currentMag;
    bool slideLocked = false;
    float nextFire;

    void OnEnable()
    {
        if (!magSocket) return;

        magSocket.selectEntered.AddListener(a =>
        {
            currentMag = a.interactableObject.transform.GetComponent<MagazineXR>();
            if (audioSource && insertClip) audioSource.PlayOneShot(insertClip);
        });
        magSocket.selectExited.AddListener(a =>
        {
            if (currentMag && a.interactableObject.transform.GetComponent<MagazineXR>() == currentMag)
                currentMag = null;
            if (audioSource && ejectClip) audioSource.PlayOneShot(ejectClip);
        });
    }

    void OnDisable()
    {
        if (!magSocket) return;
        magSocket.selectEntered.RemoveAllListeners();
        magSocket.selectExited.RemoveAllListeners();
    }

    // Called externally (e.g. by GunInputXR) when trigger pressed
    public void FirePressed()
    {
        if (Time.time < nextFire) return;
        if (slideLocked)
        {
            nextFire = Time.time + 0.1f;
            return;
        }

        if (currentMag == null || currentMag.currentRounds <= 0)
        {
            if (audioSource && dryClip) audioSource.PlayOneShot(dryClip);
            nextFire = Time.time + 0.1f;
            return;
        }

        // Consume one round
        currentMag.currentRounds--;
        nextFire = Time.time + fireCooldown;

        if (audioSource && fireClip) audioSource.PlayOneShot(fireClip);

        // Hitscan
        Vector3 o = muzzle ? muzzle.position : transform.position;
        Vector3 d = muzzle ? muzzle.forward : transform.forward;
        if (Physics.Raycast(o, d, out RaycastHit hit, maxRange, hitMask, QueryTriggerInteraction.Ignore))
        {
            Debug.DrawLine(o, hit.point, Color.red, 0.15f);
            // You could apply damage here if you have a target component.
        }

        // Slide blowback
        slide?.DoBlowback();

        // Lock slide if mag now empty
        if (currentMag.currentRounds <= 0)
        {
            slideLocked = true;
            slide?.SetLocked(true);
        }
    }

    // Called by SlideRail when the user pulled far enough then released
    public void OnSlideCharged()
    {
        if (currentMag && currentMag.currentRounds > 0)
        {
            slideLocked = false;
            slide?.SetLocked(false);
        }
    }

    public void EjectPressed()
    {
        if (!magSocket || !magSocket.hasSelection) return;
        var io = magSocket.firstInteractableSelected as XRBaseInteractable;
        if (!io) return;

        magSocket.interactionManager.SelectExit(magSocket, io);

        var mag = io.transform.GetComponent<MagazineXR>();
        var rb = mag ? mag.GetComponent<Rigidbody>() : null;

        mag.transform.SetParent(null, true);
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(transform.TransformDirection(new Vector3(0, -0.5f, 0.8f)), ForceMode.Impulse);
        }
    }

    internal void OnMagazineInserted(MagazineXR magazineXR)
    {
        if (!magazineXR) return;

        currentMag = magazineXR;
        magazineXR.currentGun = this;
        magazineXR.isInserted = true;
        magazineXR.currentRounds = Mathf.Clamp(magazineXR.currentRounds, 0, Mathf.Max(0, magazineXR.maxRounds));

        // If slide was locked but now there is ammo, allow user to rack it
        if (currentMag.currentRounds > 0 && slideLocked)
            slide?.SetLocked(true); // keep locked until racked
    }

    internal void OnMagazineRemoved(MagazineXR magazineXR)
    {
        if (!magazineXR) return;
        if (currentMag == magazineXR) currentMag = null;

        magazineXR.currentGun = null;
        magazineXR.isInserted = false;
    }
}
