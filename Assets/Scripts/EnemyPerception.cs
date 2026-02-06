using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    [Header("Vision")]
    public float viewDistance = 15f;
    [Range(1, 179)] public float viewAngle = 90f;
    public Transform eyePoint; // optional (head height)
    public LayerMask occlusionMask; // walls, props, etc.

    public bool CanSeePlayer(Transform player)
    {
        if (!player) return false;

        Vector3 origin = eyePoint ? eyePoint.position : (transform.position + Vector3.up * 1.6f);
        Vector3 toPlayer = player.position - origin;
        float dist = toPlayer.magnitude;

        if (dist > viewDistance) return false;

        Vector3 dir = toPlayer / dist;

        // FOV check
        float angle = Vector3.Angle(transform.forward, dir);
        if (angle > viewAngle * 0.5f) return false;

        // LOS check
        if (Physics.Raycast(origin, dir, out RaycastHit hit, viewDistance, occlusionMask, QueryTriggerInteraction.Ignore))
        {
            // If we hit something before player, no LOS
            if (hit.transform != player && !hit.transform.IsChildOf(player))
                return false;
        }

        return true;
    }
}
