using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Rigidbody), typeof(Collider), typeof(XRGrabInteractable))]
public class MagazineXR : XRGrabInteractable
{
    [Header("Ammo")]
    public int maxRounds = 15;
    public int currentRounds = 15;

    [Header("State (read-only)")]
    public bool isInserted;                 // true while in a socket
    public GunXR currentGun;                // gun we are inserted into (if any)

    Rigidbody _rb;
    Collider _col;

    protected override void Awake()
    {
        base.Awake();
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        // If selected by a socket, consider it attached to the gun
        if (args.interactorObject is XRSocketInteractor socket)
        {
            // Parent under the socket so pose stays aligned (XRI also aligns using attach points)
            transform.SetParent(socket.transform, worldPositionStays: false);

            // Disable physics while docked
            _rb.isKinematic = true;
            if (_col) _col.enabled = false;

            // Find gun above the socket
            currentGun = GetComponentInParent<GunXR>();
            isInserted = true;

            if (currentGun) currentGun.OnMagazineInserted(this);
        }
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        // If we were removed from a socket, re-enable physics
        if (args.interactorObject is XRSocketInteractor)
        {
            transform.SetParent(null, true);

            _rb.isKinematic = false;
            if (_col) _col.enabled = true;

            if (currentGun) currentGun.OnMagazineRemoved(this);
            currentGun = null;
            isInserted = false;
        }
    }

    /// <summary>Eject this mag programmatically (gun calls this).</summary>
    public void Eject(Vector3 worldImpulse)
    {
        // If still parented under socket, unparent
        if (transform.parent) transform.SetParent(null, true);

        _rb.isKinematic = false;
        if (_col) _col.enabled = true;

        _rb.AddForce(worldImpulse, ForceMode.Impulse);

        if (currentGun) currentGun.OnMagazineRemoved(this);
        currentGun = null;
        isInserted = false;
    }
}
