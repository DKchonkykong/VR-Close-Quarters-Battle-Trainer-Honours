using UnityEngine;

public class SafeZone : MonoBehaviour
{
    public int securedCount;

    private void OnTriggerEnter(Collider other)
    {
        var hostage = other.GetComponentInParent<HostageFollower>();
        if (hostage && hostage.state == HostageFollower.State.Following)
        {
            hostage.Secure();
            securedCount++;
            Debug.Log($"Hostage secured! Total: {securedCount}");
        }
    }
}