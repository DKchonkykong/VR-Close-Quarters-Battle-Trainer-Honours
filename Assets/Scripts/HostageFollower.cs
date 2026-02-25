using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

public class HostageFollower : MonoBehaviour
{
    public enum State { Waiting, Following, Secured, Dead }

    [Header("Refs")]
    public NavMeshAgent agent;
    public Transform player; // XR camera or rig target

    [Header("Follow")]
    public float followDistance = 1.3f;     // how far behind player
    public float sideOffset = 0.35f;        // slight side offset so they don't collide
    public float stopDistance = 0.9f;       // when close enough, stop
    public float repathRate = 0.15f;        // how often to update destination

    [Header("Debug")]
    public State state = State.Waiting;

    float repathTimer;

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
        if (state != State.Following) return;
        if (!agent || !agent.isOnNavMesh || !player) return;

        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f) return;
        repathTimer = repathRate;

        // Follow point behind the player
        Vector3 behind = -player.forward;
        behind.y = 0f;
        behind.Normalize();

        Vector3 right = player.right;
        right.y = 0f;
        right.Normalize();

        Vector3 targetPos = player.position
                            + behind * followDistance
                            + right * sideOffset;

        agent.SetDestination(targetPos);
    }

    public void StartFollowing()
    {
        if (state == State.Secured || state == State.Dead) return;
        state = State.Following;
        if (agent) agent.isStopped = false;
    }

    public void Secure()
    {
        if (state == State.Secured || state == State.Dead) return;
        state = State.Secured;

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    public void KillHostage()
    {
        state = State.Dead;
        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }
        // optional: play animation / disable visuals
    }
}