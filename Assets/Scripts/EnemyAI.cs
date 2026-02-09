using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Investigate, Chase, Attack }
    
    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public EnemyPerception perception;
    public Transform eyePoint; // if null, will calculate from transform

    [Header("Patrol")]
    public List<Transform> patrolPoints = new();
    public float patrolWait = 1f;
    public float patrolSpeed = 1.6f;

    [Header("Investigation")]
    public float investigateTime = 3f;
    public float chaseSpeed = 3.2f;

    [Header("Attack")]
    public float attackRange = 2.5f;
    public float attackStopDistance = 2f;
    public int damagePerShot = 1;
    public float fireCooldown = 0.4f;

    [Header("Raycasting")]
    [Tooltip("What blocks shots (Walls, etc.)")]
    public LayerMask shotBlockMask = ~0;
    
    [Tooltip("Optional: What counts as player hit (Player layer)")]
    public LayerMask playerHitMask = ~0;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fireClip;
    public AudioClip blockedClip; // optional "thud" if hits wall

    [Header("Visual Feedback")]
    public LineRenderer shotLine;
    public float shotLineDuration = 0.05f;
    
    [Header("Debug")]
    public bool drawAttackDebugRay = true;
    public float debugLineTime = 0.05f;
    public State currentState = State.Patrol;

    // Runtime state
    int patrolIndex = 0;
    float stateTimer = 0f;
    float nextFireTime = 0f;

    Vector3 lastSeenPos;
    bool hasLastSeen;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!perception) perception = GetComponent<EnemyPerception>();

        if (!player)
        {
            var xrOrigin = FindAnyObjectByType<XROrigin>();
            if (xrOrigin && xrOrigin.Camera) 
                player = xrOrigin.Camera.transform;
            else if (Camera.main) 
                player = Camera.main.transform;
        }

        // Ensure shotLine is disabled at start
        if (shotLine) 
            shotLine.enabled = false;
    }

    void OnEnable()
    {
        stateTimer = 0f;
        nextFireTime = 0f;
        hasLastSeen = false;
    }

    void Update()
    {
        if (!agent || !agent.isOnNavMesh)
            return;

        bool canSee = perception && player && perception.CanSee(player, out _);

        if (canSee)
        {
            lastSeenPos = player.position;
            hasLastSeen = true;
        }

        float distToPlayer = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        // State machine
        switch (currentState)
        {
            case State.Patrol:
                agent.speed = patrolSpeed;
                agent.stoppingDistance = 0.5f;

                if (canSee)
                {
                    SetState(State.Chase);
                    break;
                }

                DoPatrol();
                break;

            case State.Investigate:
                agent.speed = chaseSpeed;
                agent.stoppingDistance = 0.5f;

                if (canSee)
                {
                    SetState(State.Chase);
                    break;
                }

                DoInvestigate();
                break;

            case State.Chase:
                agent.speed = chaseSpeed;
                agent.stoppingDistance = 0.5f;

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
                FaceTarget(player.position);
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

                DoAttack(canSee, distToPlayer);
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

    void DoAttack(bool canSee, float distToPlayer)
    {
        // Set stopping distance for CQB
        agent.stoppingDistance = Mathf.Max(attackStopDistance, attackRange * 0.8f);

        // If close enough and can see, stop and shoot
        if (canSee && distToPlayer <= attackRange)
        {
            // Stop moving while firing
            agent.SetDestination(transform.position);

            // Face player
            FaceTarget(player.position);

            // Fire at player
            TryAttackPlayer();
        }
        else
        {
            // Keep moving toward player
            agent.SetDestination(player.position);

            // Still face player while moving
            FaceTarget(player.position);
        }
    }

    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 flatDir = targetPosition - transform.position;
        flatDir.y = 0;

        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8f * Time.deltaTime);
        }
    }

    void TryAttackPlayer()
    {
        if (!player) return;

        // Cooldown check
        if (Time.time < nextFireTime) return;

        // Get origin point (eye or estimated head position)
        Vector3 origin = eyePoint 
            ? eyePoint.position 
            : transform.position + Vector3.up * 1.5f;

        // Target player's center mass (slightly above origin)
        Vector3 target = player.position + Vector3.up * 0.2f;
        Vector3 dir = (target - origin).normalized;
        float dist = Vector3.Distance(origin, target);

        // Optional: Add slight inaccuracy for realism
        dir = Quaternion.Euler(
            Random.Range(-1f, 1f), 
            Random.Range(-2f, 2f), 
            0f
        ) * dir;

        // Raycast: check if something blocks the shot
        bool blocked = Physics.Raycast(
            origin, 
            dir, 
            out RaycastHit hit, 
            dist, 
            shotBlockMask, 
            QueryTriggerInteraction.Ignore
        );

        // Visual feedback
        Vector3 hitPoint = blocked ? hit.point : target;
        
        if (drawAttackDebugRay)
        {
            Debug.DrawLine(
                origin, 
                hitPoint, 
                blocked ? Color.red : Color.green, 
                debugLineTime
            );
        }

        if (shotLine)
        {
            ShowShotLine(origin, hitPoint);
        }

        // Apply damage if shot is clear
        if (!blocked)
        {
            var ph = player.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damagePerShot);
                Debug.Log($"Enemy hit player for {damagePerShot} damage! HP: {ph.currentHealth}/{ph.maxHealth}");
            }

            if (audioSource && fireClip) 
                audioSource.PlayOneShot(fireClip);
        }
        else
        {
            // Shot was blocked by wall/obstacle
            Debug.Log($"Enemy shot blocked by {hit.collider.name}");
            
            if (audioSource && blockedClip) 
                audioSource.PlayOneShot(blockedClip);
        }

        nextFireTime = Time.time + fireCooldown;
    }

    void ShowShotLine(Vector3 a, Vector3 b)
    {
        if (!shotLine) return;

        shotLine.enabled = true;
        shotLine.SetPosition(0, a);
        shotLine.SetPosition(1, b);
        
        CancelInvoke(nameof(HideShotLine));
        Invoke(nameof(HideShotLine), shotLineDuration);
    }

    void HideShotLine()
    {
        if (shotLine) shotLine.enabled = false;
    }

    // Editor gizmos for debugging
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw current state
        Vector3 textPos = transform.position + Vector3.up * 2.5f;
        UnityEditor.Handles.Label(textPos, $"State: {currentState}");

        // Draw patrol path
        if (patrolPoints != null && patrolPoints.Count > 1)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < patrolPoints.Count; i++)
            {
                if (patrolPoints[i] == null) continue;
                
                int next = (i + 1) % patrolPoints.Count;
                if (patrolPoints[next] == null) continue;

                Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[next].position);
            }
        }

        // Draw line to player if visible
        if (player && perception && perception.CanSee(player, out _))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position + Vector3.up * 1.5f, player.position);
        }
    }
}
