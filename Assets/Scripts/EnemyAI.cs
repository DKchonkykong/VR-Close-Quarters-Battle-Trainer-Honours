using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Investigate, Chase, Attack }

    [Header("Refs")]
    public NavMeshAgent agent;
    public Transform player;
    public EnemyPerception perception;

    [Header("Patrol")]
    public List<Transform> patrolPoints = new();
    public float patrolWait = 1f;

    [Header("Caution / Search")]
    public float investigateTime = 3f;

    [Header("Danger / Attack")]
    public float attackRange = 2.5f;
    public float fireCooldown = 0.4f;

    [Header("Tuning")]
    public float patrolSpeed = 1.6f;
    public float chaseSpeed = 3.2f;

    [Header("Debug")]
    public State currentState = State.Patrol;

    int patrolIndex = 0;
    float stateTimer = 0f;
    float fireTimer = 0f;

    Vector3 lastSeenPos;
    bool hasLastSeen;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!perception) perception = GetComponent<EnemyPerception>();

        if (!player)
        {
            var xrOrigin = FindAnyObjectByType<XROrigin>();
            if (xrOrigin && xrOrigin.Camera) player = xrOrigin.Camera.transform;
            else if (Camera.main) player = Camera.main.transform;
        }
    }

    void OnEnable()
    {
        stateTimer = 0f;
        fireTimer = 0f;
        hasLastSeen = false;
    }

    void Update()
    {
        if (!agent || !agent.isOnNavMesh)
            return;

        bool canSee = perception && player && perception.CanSeePlayer(player);

        if (canSee)
        {
            lastSeenPos = player.position;
            hasLastSeen = true;
        }

        float distToPlayer = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        // State transitions
        switch (currentState)
        {
            case State.Patrol:
                agent.speed = patrolSpeed;

                if (canSee) { SetState(State.Chase); break; }
                DoPatrol();
                break;

            case State.Investigate:
                agent.speed = chaseSpeed;

                if (canSee) { SetState(State.Chase); break; }
                DoInvestigate();
                break;

            case State.Chase:
                agent.speed = chaseSpeed;

                if (!canSee)
                {
                    SetState(State.Investigate);
                    break;
                }

                if (distToPlayer <= attackRange)
                {
                    SetState(State.Attack);
                    break;
                }

                agent.SetDestination(player.position);
                break;

            case State.Attack:
                agent.speed = chaseSpeed;

                if (!canSee)
                {
                    SetState(State.Investigate);
                    break;
                }

                if (distToPlayer > attackRange * 1.15f)
                {
                    SetState(State.Chase);
                    break;
                }

                DoAttack();
                break;
        }
    }

    void SetState(State s)
    {
        currentState = s;
        stateTimer = 0f;

        if (s == State.Investigate && hasLastSeen)
            agent.SetDestination(lastSeenPos);
    }

    void DoPatrol()
    {
        if (patrolPoints == null || patrolPoints.Count == 0)
            return;

        Transform target = patrolPoints[patrolIndex];
        if (!target) return;

        if (!agent.hasPath)
            agent.SetDestination(target.position);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            stateTimer += Time.deltaTime;
            if (stateTimer >= patrolWait)
            {
                stateTimer = 0f;
                patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
                agent.SetDestination(patrolPoints[patrolIndex].position);
            }
        }
    }

    void DoInvestigate()
    {
        stateTimer += Time.deltaTime;

        // Wait around last seen position for a bit
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            if (stateTimer >= investigateTime)
            {
                SetState(State.Patrol);
            }
        }
    }

    void DoAttack()
    {
        // Face player
        if (player)
        {
            Vector3 flatDir = player.position - transform.position;
            flatDir.y = 0;
            if (flatDir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flatDir), 8f * Time.deltaTime);
        }

        // “Shoot” timer (hook your own damage later)
        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            fireTimer = fireCooldown;

            // TODO: implement enemy firing/hitscan at player
            // e.g. Debug.DrawRay(perception.eyePoint.position, (player.position - perception.eyePoint.position).normalized * 10f, Color.red, 0.1f);
        }
    }
}
