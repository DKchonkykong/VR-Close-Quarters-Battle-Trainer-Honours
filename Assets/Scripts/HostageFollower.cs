using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

public class HostageFollower : MonoBehaviour
{
    public enum State { Waiting, Following, MovingToSafeZone, Secured, Dead }

    [Header("Refs")]
    public NavMeshAgent agent;
    public Transform player;

    [Header("Follow")]
    public float followDistance = 1.3f;
    public float sideOffset = 0.35f;
    public float stopDistance = 0.4f;
    public float repathRate = 0.15f;

    [Header("Debug")]
    public State state = State.Waiting;

    float repathTimer;
    Vector3 safeZoneTarget;
    bool hasSafeZoneTarget;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();

        if (!player)
        {
            var xrOrigin = FindAnyObjectByType<XROrigin>();
            if (xrOrigin && xrOrigin.Camera) player = xrOrigin.Camera.transform;
            else if (Camera.main) player = Camera.main.transform;
        }

        if (agent)
        {
            agent.stoppingDistance = stopDistance;
        }
    }

    void Update()
    {
        if (!agent || !agent.isOnNavMesh) return;

        switch (state)
        {
            case State.Following:
                UpdateFollow();
                break;

            case State.MovingToSafeZone:
                UpdateMoveToSafeZone();
                break;

            case State.Secured:
            case State.Waiting:
            case State.Dead:
                break;
        }
    }

    void UpdateFollow()
    {
        if (!player) return;

        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f) return;
        repathTimer = repathRate;

        Vector3 behind = -player.forward;
        behind.y = 0f;
        behind.Normalize();

        Vector3 right = player.right;
        right.y = 0f;
        right.Normalize();

        Vector3 targetPos = player.position
                            + behind * followDistance
                            + right * sideOffset;

        if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
    }

    void UpdateMoveToSafeZone()
    {
        if (!hasSafeZoneTarget) return;

        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f) return;
        repathTimer = repathRate;

        if (NavMesh.SamplePosition(safeZoneTarget, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }

        // If close enough, lock hostage in secured state
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            Secure();
        }
    }

    public void StartFollowing()
    {
        if (state == State.Secured || state == State.Dead) return;

        state = State.Following;
        hasSafeZoneTarget = false;

        if (agent)
            agent.isStopped = false;
    }

    public void MoveToSafeZone(Vector3 target)
    {
        if (state == State.Secured || state == State.Dead) return;

        state = State.MovingToSafeZone;
        safeZoneTarget = target;
        hasSafeZoneTarget = true;
        repathTimer = 0f;

        if (agent)
            agent.isStopped = false;
    }

    public void Secure()
    {
        if (state == State.Secured || state == State.Dead) return;

        state = State.Secured;
        hasSafeZoneTarget = false;

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    public void KillHostage()
    {
        state = State.Dead;
        hasSafeZoneTarget = false;

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }
}