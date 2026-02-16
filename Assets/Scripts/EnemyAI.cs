using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.XR.CoreUtils;

public class EnemyAI : MonoBehaviour
{
    public enum State { Patrol, Investigate, Chase, Attack, Dead }

    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    public EnemyPerception perception;
    public Transform eyePoint;          // where raycasts originate
    public MainTarget targetHealth;     // optional: used for death detection
    public Animator animator;           // optional: animations

    [Header("Patrol")]
    public List<Transform> patrolPoints = new();
    public float patrolWait = 1f;
    public float patrolSpeed = 1.6f;

    [Header("Investigate/Chase")]
    public float investigateTime = 3f;
    public float chaseSpeed = 3.2f;

    [Header("Attack")]
    public float attackRange = 8f;
    public float attackStopDistance = 2f;
    public int damagePerShot = 1;
    public float fireCooldown = 0.4f;

    [Header("Attack Movement (Strafe)")]
    [Tooltip("How far to offset left/right while attacking.")]
    public float strafeDistance = 1.25f;

    [Tooltip("How often the enemy picks a new strafe direction/point.")]
    public float strafeChangeInterval = 0.75f;

    [Tooltip("Chance to switch strafe direction each interval (0..1).")]
    [Range(0f, 1f)] public float strafeSwitchChance = 0.6f;

    [Tooltip("How far to keep from the player while attacking.")]
    public float idealAttackDistance = 3.0f;

    [Header("Raycasting")]
    [Tooltip("What blocks shots (Walls, Doors, Props etc).")]
    public LayerMask shotBlockMask = ~0;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip fireClip;
    public AudioClip blockedClip;

    [Header("Visual Feedback")]
    public LineRenderer shotLine;
    public float shotLineDuration = 0.05f;

    [Header("Debug")]
    public bool drawAttackDebugRay = true;
    public float debugLineTime = 0.05f;
    public State currentState = State.Patrol;

    // Runtime
    int patrolIndex = 0;
    float stateTimer = 0f;
    float nextFireTime = 0f;

    Vector3 lastSeenPos;
    bool hasLastSeen;

    float strafeTimer = 0f;
    int strafeSign = 1; // -1 left, +1 right

    bool isDead = false;

    // Animator param names (change if your controller uses different names)
    static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
    static readonly int AnimIsShooting = Animator.StringToHash("IsShooting");
    static readonly int AnimSpeed = Animator.StringToHash("Speed");
    static readonly int AnimDie = Animator.StringToHash("Die");

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!perception) perception = GetComponent<EnemyPerception>();
        if (!targetHealth) targetHealth = GetComponent<MainTarget>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (!player)
        {
            var xrOrigin = FindAnyObjectByType<XROrigin>();
            if (xrOrigin && xrOrigin.Camera) player = xrOrigin.Camera.transform;
            else if (Camera.main) player = Camera.main.transform;
        }

        if (!eyePoint && perception && perception.eyePoint) eyePoint = perception.eyePoint;

        if (shotLine) shotLine.enabled = false;
    }

    void OnEnable()
    {
        ResetAI();
    }

    void ResetAI()
    {
        stateTimer = 0f;
        nextFireTime = 0f;
        hasLastSeen = false;

        strafeTimer = 0f;
        strafeSign = 1;

        // If we respawn by resetting health without disabling the GO,
        // detect that and “revive” cleanly.
        if (targetHealth && targetHealth.currentHealth > 0)
        {
            isDead = false;
            currentState = State.Patrol;
            if (agent)
            {
                agent.isStopped = false;
                agent.enabled = true;
            }
        }

        SetAnimShooting(false);
    }

    void Update()
    {
        if (!agent || !agent.isOnNavMesh) return;

        // Death check (works even if MainTarget just sets HP to 0 and hides visuals)
        if (!isDead && targetHealth && targetHealth.currentHealth <= 0)
        {
            Die();
        }

        // If dead, do nothing
        if (isDead || currentState == State.Dead)
        {
            UpdateAnimatorMovement();
            return;
        }

        bool canSee = perception && player && perception.CanSee(player, out _);

        if (canSee)
        {
            lastSeenPos = player.position;
            hasLastSeen = true;
        }

        float distToPlayer = player ? Vector3.Distance(transform.position, player.position) : Mathf.Infinity;

        switch (currentState)
        {
            case State.Patrol:
                agent.speed = patrolSpeed;
                agent.stoppingDistance = 0.5f;
                SetAnimShooting(false);

                if (canSee) { SetState(State.Chase); break; }
                DoPatrol();
                break;

            case State.Investigate:
                agent.speed = chaseSpeed;
                agent.stoppingDistance = 0.5f;
                SetAnimShooting(false);

                if (canSee) { SetState(State.Chase); break; }
                DoInvestigate();
                break;

            case State.Chase:
                agent.speed = chaseSpeed;
                agent.stoppingDistance = 0.5f;
                SetAnimShooting(false);

                if (!canSee) { SetState(State.Investigate); break; }
                if (distToPlayer <= attackRange) { SetState(State.Attack); break; }

                agent.SetDestination(player.position);
                FaceTarget(player.position);
                break;

            case State.Attack:
                agent.speed = chaseSpeed;
                agent.stoppingDistance = Mathf.Max(attackStopDistance, attackRange * 0.35f);

                if (!canSee) { SetState(State.Investigate); break; }
                if (distToPlayer > attackRange * 1.2f) { SetState(State.Chase); break; }

                DoAttack(distToPlayer);
                break;
        }

        UpdateAnimatorMovement();
    }

    void SetState(State s)
    {
        currentState = s;
        stateTimer = 0f;

        if (s == State.Investigate && hasLastSeen)
            agent.SetDestination(lastSeenPos);

        if (s == State.Attack)
        {
            strafeTimer = 0f;
            // Randomize first strafe direction
            strafeSign = (Random.value < 0.5f) ? -1 : 1;
        }
    }

    void DoPatrol()
    {
        if (patrolPoints == null || patrolPoints.Count == 0) return;

        Transform target = patrolPoints[patrolIndex];
        if (!target) return;

        if (!agent.hasPath) agent.SetDestination(target.position);

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

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.05f)
        {
            if (stateTimer >= investigateTime)
            {
                SetState(State.Patrol);
            }
        }
    }

    void DoAttack(float distToPlayer)
    {
        if (!player) return;

        // Always face player in CQB
        FaceTarget(player.position);

        // If too close/far, adjust forward/back to maintain "ideal" range
        Vector3 toPlayer = (player.position - transform.position);
        toPlayer.y = 0f;

        float planarDist = toPlayer.magnitude;
        Vector3 forward = (planarDist > 0.001f) ? (toPlayer / planarDist) : transform.forward;

        Vector3 desired = transform.position;

        // Maintain an ideal distance band
        if (planarDist < idealAttackDistance * 0.85f)
        {
            desired = transform.position - forward * 0.75f; // back up a bit
        }
        else if (planarDist > idealAttackDistance * 1.15f)
        {
            desired = transform.position + forward * 0.75f; // step in a bit
        }

        // Strafe left/right around the player
        strafeTimer += Time.deltaTime;
        if (strafeTimer >= strafeChangeInterval)
        {
            strafeTimer = 0f;
            if (Random.value < strafeSwitchChance) strafeSign *= -1;
        }

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized; // right relative to player direction
        desired += right * (strafeSign * strafeDistance);

        // Snap desired point to navmesh so agent doesn't try to walk through walls
        if (NavMesh.SamplePosition(desired, out NavMeshHit navHit, 1.0f, agent.areaMask))
        {
            agent.SetDestination(navHit.position);
        }
        else
        {
            // Fallback: just stop moving
            agent.SetDestination(transform.position);
        }

        // Shoot when in range (you can also require “steady aim” here)
        if (distToPlayer <= attackRange)
        {
            SetAnimShooting(true);
            TryAttackPlayer();
        }
        else
        {
            SetAnimShooting(false);
        }
    }

    void TryAttackPlayer()
    {
        if (!player) return;
        if (Time.time < nextFireTime) return;

        Vector3 origin = (eyePoint ? eyePoint.position : transform.position + Vector3.up * 1.5f);
        Vector3 target = player.position + Vector3.up * 0.2f;

        Vector3 dir = (target - origin).normalized;
        float dist = Vector3.Distance(origin, target);

        // Tiny inaccuracy (optional)
        dir = Quaternion.Euler(Random.Range(-1f, 1f), Random.Range(-2f, 2f), 0f) * dir;

        // IMPORTANT:
        // This raycast returns TRUE if it hits *something* in shotBlockMask.
        // So shotBlockMask must include walls/doors/props etc. (things that block bullets).
        bool hitSomething = Physics.Raycast(
            origin,
            dir,
            out RaycastHit hit,
            dist,
            shotBlockMask,
            QueryTriggerInteraction.Ignore
        );

        Vector3 hitPoint = hitSomething ? hit.point : target;

        if (drawAttackDebugRay)
        {
            Debug.DrawLine(origin, hitPoint, hitSomething ? Color.red : Color.green, debugLineTime);
        }

        if (shotLine) ShowShotLine(origin, hitPoint);

        // If we hit something before reaching player, we assume the shot is blocked.
        // If you want head/body hitboxes later: raycast ALL, then pick the first hit.
        if (!hitSomething)
        {
            var ph = player.GetComponentInParent<PlayerHealth>();
            if (ph) ph.TakeDamage(damagePerShot);

            if (audioSource && fireClip) audioSource.PlayOneShot(fireClip);
        }
        else
        {
            if (audioSource && blockedClip) audioSource.PlayOneShot(blockedClip);
        }

        nextFireTime = Time.time + fireCooldown;
    }

    void FaceTarget(Vector3 targetPosition)
    {
        Vector3 flatDir = targetPosition - transform.position;
        flatDir.y = 0f;

        if (flatDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 8f * Time.deltaTime);
        }
    }

    void Die()
    {
        isDead = true;
        currentState = State.Dead;

        SetAnimShooting(false);

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        if (animator)
        {
            animator.SetTrigger(AnimDie);
        }
    }

    void UpdateAnimatorMovement()
    {
        if (!animator || !agent) return;

        float speed = agent.velocity.magnitude;
        animator.SetFloat(AnimSpeed, speed);
        animator.SetBool(AnimIsMoving, speed > 0.05f);
    }

    void SetAnimShooting(bool shooting)
    {
        if (!animator) return;
        animator.SetBool(AnimIsShooting, shooting);
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
}
