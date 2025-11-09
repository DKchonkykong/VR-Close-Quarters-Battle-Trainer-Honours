using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SlideRail : MonoBehaviour
{
    [Header("Anchors & Visual")]
    public Transform slideVisual;      // the moving mesh
    public Transform slideClosed;      // anchor A (closed)
    public Transform slideOpen;        // anchor B (open)

    [Header("Behaviour")]
    [Range(0.1f, 1f)] public float chargeThreshold01 = 0.7f; // pull depth to count as rack
    public float blowbackBackTime = 0.05f;
    public float blowbackFwdTime = 0.08f;
    public float blowbackDepth01 = 0.65f; // how far back (0..1) a fired blowback goes

    [Header("Refs")]
    public GunXR gun;                  // set in Inspector

    // runtime
    XRBaseInteractor grabbingHand;
    float t;          // 0=closed .. 1=open
    bool locked;      // set by GunXR when empty
    bool pulledEnoughThisGrab;

    void Reset()
    {
        // simple auto-wiring if placed on the gun
        if (!gun) gun = GetComponentInParent<GunXR>();
    }

    void LateUpdate()
    {
        // if being grabbed, update t from hand; else just keep visual on the rail
        if (grabbingHand) UpdateTFromHand();
        ApplyTToVisual();
    }

    // --- Public API for GunXR ---
    public void SetLocked(bool isLocked)
    {
        locked = isLocked;
        if (locked) SetT(1f); // snap to open when locking
    }

    public void DoBlowback()
    {
        if (locked || grabbingHand) return; // don't fight the user
        StopAllCoroutines();
        StartCoroutine(BlowbackRoutine());
    }

    // --- Called by SlideHandleXR (below) ---
    public void OnGrab(XRBaseInteractor hand)
    {
        grabbingHand = hand;
        pulledEnoughThisGrab = false;
    }

    public void OnRelease()
    {
        grabbingHand = null;

        if (pulledEnoughThisGrab && gun != null)
            gun.OnSlideCharged();      // let the gun clear the lock if ammo present

        // snap closed visually (user released the handle)
        SetT(0f);
    }

    // --- Core math ---
    void UpdateTFromHand()
    {
        if (!slideClosed || !slideOpen || !grabbingHand) return;

        Vector3 a = slideClosed.position;
        Vector3 b = slideOpen.position;
        Vector3 p = grabbingHand.transform.position;

        Vector3 ab = b - a;
        float len = ab.magnitude;
        if (len < 1e-4f) return;

        float proj = Vector3.Dot(p - a, ab.normalized);
        float newT = Mathf.Clamp01(proj / len);
        SetT(newT);

        if (t >= chargeThreshold01)
            pulledEnoughThisGrab = true;
    }

    void ApplyTToVisual()
    {
        if (!slideVisual || !slideClosed || !slideOpen) return;
        slideVisual.position = Vector3.Lerp(slideClosed.position, slideOpen.position, t);
        // keep slide orientation aligned to closed anchor for stability
        slideVisual.rotation = slideClosed.rotation;
    }

    void SetT(float value) => t = Mathf.Clamp01(value);

    IEnumerator BlowbackRoutine()
    {
        float backT = Mathf.Clamp01(blowbackDepth01);
        float t0 = t;
        // go back
        float tmr = 0f;
        while (tmr < blowbackBackTime)
        {
            tmr += Time.deltaTime;
            SetT(Mathf.Lerp(t0, backT, tmr / blowbackBackTime));
            ApplyTToVisual();
            yield return null;
        }
        // return forward
        tmr = 0f;
        while (tmr < blowbackFwdTime)
        {
            tmr += Time.deltaTime;
            SetT(Mathf.Lerp(backT, 0f, tmr / blowbackFwdTime));
            ApplyTToVisual();
            yield return null;
        }
        SetT(0f);
        ApplyTToVisual();
    }
}
