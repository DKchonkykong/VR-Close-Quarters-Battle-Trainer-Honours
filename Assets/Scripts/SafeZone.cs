using System.Collections.Generic;
using UnityEngine;
using Unity.XR.CoreUtils;

public class SafeZone : MonoBehaviour
{
    public int securedCount;

    private readonly HashSet<HostageFollower> alreadySecured = new();

    private void OnTriggerEnter(Collider other)
    {
        // If player enters, tell all following hostages to come into safe zone
        var xrOrigin = other.GetComponentInParent<XROrigin>();
        if (xrOrigin != null)
        {
            HostageFollower[] hostages = FindObjectsByType<HostageFollower>(FindObjectsSortMode.None);

            foreach (var h in hostages)
            {
                if (h == null) continue;
                if (h.state != HostageFollower.State.Following) continue;

                h.MoveToSafeZone(transform.position);
            }

            return;
        }

        // If hostage enters, secure them immediately
        var hostage = other.GetComponentInParent<HostageFollower>();
        if (hostage != null &&
            hostage.state != HostageFollower.State.Secured &&
            hostage.state != HostageFollower.State.Dead &&
            !alreadySecured.Contains(hostage))
        {
            hostage.Secure();
            alreadySecured.Add(hostage);
            securedCount++;

            Debug.Log($"Hostage secured! Total: {securedCount}");
        }
    }
}