using UnityEngine;

public class EnemyPerception : MonoBehaviour
{
    [Header("Vision")]
    public float viewDistance = 15f;
    [Range(1f, 180f)] public float viewAngle = 90f;
    public Transform eyePoint;

    [Header("Occlusion")]
    public LayerMask occlusionMask; // set to Walls
    public bool drawDebug = true;

    public bool CanSee(Transform target, out RaycastHit hit)
    {
        hit = default;
        if (!eyePoint || !target) return false;

        Vector3 toTarget = (target.position - eyePoint.position);
        float dist = toTarget.magnitude;

        if (dist > viewDistance) return false;

        Vector3 dir = toTarget / dist;

        // Angle check
        float angle = Vector3.Angle(eyePoint.forward, dir);
        if (angle > viewAngle * 0.5f) return false;

        // Occlusion check (only walls should block)
        // If you want *anything* to block, use Physics.Raycast without mask filtering.
        bool blocked = Physics.Raycast(
            eyePoint.position,
            dir,
            out hit,
            dist,
            occlusionMask,
            QueryTriggerInteraction.Ignore
        );

        if (drawDebug)
        {
            Color c = blocked ? Color.red : Color.green;
            Debug.DrawLine(eyePoint.position, target.position, c, 0f, false);
        }

        // If ray hits a wall before reaching target, blocked = true.
        return !blocked;
    }

    private void OnDrawGizmosSelected()
    {
        if (!eyePoint) return;

        // Draw view distance
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(eyePoint.position, viewDistance);

        // Draw FOV lines
        Vector3 left = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * eyePoint.forward;
        Vector3 right = Quaternion.Euler(0, viewAngle * 0.5f, 0) * eyePoint.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(eyePoint.position, eyePoint.position + left * viewDistance);
        Gizmos.DrawLine(eyePoint.position, eyePoint.position + right * viewDistance);
    }
}
