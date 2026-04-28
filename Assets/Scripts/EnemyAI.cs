using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Investigate, Attack, Dead }
    public State currentState = State.Idle;

    [Header("Patrulha")]
    public float patrolRadius   = 10f;
    public float patrolWaitTime = 2f;
    private float   patrolTimer;
    private Vector3 patrolTarget;

    [Header("Detecção — Cone de Visão")]
    public float visionRange = 15f;
    public float visionAngle = 90f;
    public LayerMask obstacleMask;

    [Header("Detecção — Proximidade")]
    public float proximityRange = 2.5f;

    [Header("Combate")]
    public float attackRange    = 1.8f;
    public float attackDamage   = 10f;
    public float attackCooldown     = 2.7f;


    [Header("Velocidade")]
    public float patrolSpeed      = 2f;
    public float chaseSpeed       = 5f;
    public float investigateSpeed = 3f;

    [Header("Investigação")]
    public float investigateWaitTime = 4f;
    private Vector3 lastKnownPosition;
    private float   investigateTimer;

    [Header("Referências")]
    public Transform player;

    [Header("SFX")]
    public AudioSource footstepSource;
    public AudioSource patrolGrowl;
    public AudioSource chaseGrowl;
    public AudioSource swing;

    public float cooldown = 2.6f;
    private float lastPlayTime = -Mathf.Infinity;

    [Header("Perseguição")]
    public float pathUpdateInterval = 0.15f;
    private float pathUpdateTimer   = 0f;

    private float nextPatrolSoundTime = 0f;
    private float nextChaseGrowlTime  = 0f;

    private NavMeshAgent agent;
    private Animator     animator;
    private PlayerHealth playerHealth;

    private float attackTimer = 0f;
    public  bool  isAttacking = false;
    public  bool  isHit       = false;
    private float attackingTimeout = 0f;
    private float hittingTimeout   = 0f;

    public float attackAnimDuration = 2.633f;
    public float hitAnimDuration    = 2f;

    void Awake()
    {
        agent    = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (currentState == State.Dead) return;

        attackTimer -= Time.deltaTime;
        float dist = Vector3.Distance(transform.position, player.position);

        TickTimeouts(); // estava faltando essa linha
        UpdateState(dist);
        ExecuteState(dist);
        UpdateAnimator();
        HandleMovementSFX();
    }
    // ─── Detecção ─────────────────────────────────────────────────────────────

    bool CanSeePlayer()
    {
        Vector3 dirToPlayer = (player.position - transform.position).normalized;
        float   angle       = Vector3.Angle(transform.forward, dirToPlayer);
        float   dist        = Vector3.Distance(transform.position, player.position);

        if (dist > visionRange) return false;
        if (angle > visionAngle) return false;

        Vector3 origin = transform.position + Vector3.up * 1f;
        Vector3 target = player.position    + Vector3.up * 1f;

        if (Physics.Raycast(origin, (target - origin).normalized, dist, obstacleMask))
            return false;

        return true;
    }

    bool CanSensePlayer()
    {
        return Vector3.Distance(transform.position, player.position) <= proximityRange;
    }

    bool PlayerDetected() => CanSeePlayer() || CanSensePlayer();

    // ─── State Machine ────────────────────────────────────────────────────────

    void UpdateState(float dist)
    {
        if (isAttacking || isHit) return;

        if (dist <= attackRange && PlayerDetected())
        {
            currentState = State.Attack;
            return;
        }

        if (PlayerDetected())
        {
            lastKnownPosition = player.position;
            if (currentState != State.Chase)
                pathUpdateTimer = 0f;
            currentState = State.Chase;
            return;
        }

        if (currentState == State.Chase)
        {
            investigateTimer = investigateWaitTime;
            currentState     = State.Investigate;
            return;
        }

        if (currentState != State.Investigate)
            currentState = State.Patrol;
    }

    void ExecuteState(float dist)
    {
        switch (currentState)
        {
            case State.Idle:
                agent.isStopped = true;
                break;

            case State.Patrol:
                agent.isStopped = false;
                agent.speed     = patrolSpeed;
                patrolTimer    -= Time.deltaTime;

                if (patrolTimer <= 0f || agent.remainingDistance < 0.5f)
                {
                    patrolTarget = GetRandomPatrolPoint();
                    agent.SetDestination(patrolTarget);
                    patrolTimer  = patrolWaitTime;
                }

                if (Time.time >= nextPatrolSoundTime)
                {
                    patrolGrowl.PlayOneShot(patrolGrowl.clip);
                    nextPatrolSoundTime = Time.time + Random.Range(4f, 10f);
                }
                break;

            case State.Chase:
                if (isHit) break;

                if (Time.time >= nextChaseGrowlTime)
                {
                    chaseGrowl.PlayOneShot(chaseGrowl.clip);
                    nextChaseGrowlTime = Time.time + Random.Range(3f, 5f);
                }

                animator.ResetTrigger("Attack");
                agent.isStopped        = false;
                agent.speed            = chaseSpeed;
                agent.stoppingDistance = attackRange;

                pathUpdateTimer -= Time.deltaTime;
                if (pathUpdateTimer <= 0f)
                {
                    agent.SetDestination(player.position);
                    pathUpdateTimer = pathUpdateInterval;
                }
                break;

            case State.Investigate:
                if (isHit) break;
                agent.isStopped        = false;
                agent.speed            = investigateSpeed;
                agent.stoppingDistance = 0.5f;
                agent.SetDestination(lastKnownPosition);

                if (agent.remainingDistance < 0.6f)
                {
                    agent.isStopped   = true;
                    investigateTimer -= Time.deltaTime;
                    if (investigateTimer <= 0f)
                        currentState = State.Patrol;
                }
                break;

            case State.Attack:
                agent.isStopped = true;
                agent.velocity  = Vector3.zero;

                if (isHit) break;

                Vector3 dir = (player.position - transform.position).normalized;
                dir.y = 0;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, Quaternion.LookRotation(dir), 10f * Time.deltaTime);

                if (attackTimer <= 0f && !isAttacking && attackingTimeout <= 0f)
                {
                    // isAttacking = true;
                    attackTimer = attackCooldown;
                    // swing.PlayDelayed(0.6f); COMENTAR ESSA LINHA RESOLVE BUG QUE TRAVAVA O MAYNARD
                    // patrolGrowl.PlayDelayed(0.0f);
                    HandleAttackSFX();
                    animator.ResetTrigger("Attack");
                    animator.SetTrigger("Attack");
                }
                break;
        }
    }

    void UpdateAnimator()
    {
        animator.SetFloat("Speed", agent.isStopped ? 0f : agent.velocity.magnitude);
    }

    // ─── Animation Events ─────────────────────────────────────────────────────

    // Chamar no primeiro frame do clipe Attack:  SetAttacking(1)
    // Chamar no último frame do clipe Attack:    SetAttacking(0)
    // Chamar no primeiro frame do clipe Hit:     SetHit(1)
    // Chamar no último frame do clipe Hit:       SetHit(0)
    public void SetAttacking(int value)
    {
        isAttacking = value == 1;
        if (isAttacking)
            attackingTimeout = attackAnimDuration;
    }

    public void SetHit(int value)
    {
        isHit = value == 1;
        if (isHit)
            hittingTimeout = hitAnimDuration;
    }

    void TickTimeouts()
    {
        if (isAttacking)
        {
            attackingTimeout -= Time.deltaTime;
            if (attackingTimeout <= 0f)
            {
                isAttacking = false;
                Debug.LogWarning("[EnemyAI] attackingTimeout estourou — flag resetada forçadamente");
            }
        }

        if (isHit)
        {
            hittingTimeout -= Time.deltaTime;
            if (hittingTimeout <= 0f)
            {
                isHit = false;
                Debug.LogWarning("[EnemyAI] hittingTimeout estourou — flag resetada forçadamente");
            }
        }
    }

    // ─── Callbacks ────────────────────────────────────────────────────────────

    public void ApplyAttackDamage()
    {
        if (!isAttacking) return;
        if (Vector3.Distance(transform.position, player.position) <= attackRange + 0.5f)
            playerHealth?.TakeDamage(attackDamage);
    }

    public void OnHit()
    {
        isHit        = true;
        isAttacking  = false;      // garante que ataque anterior é cancelado
        attackingTimeout = 0f;     // cancela timeout pendente do ataque
        hittingTimeout   = hitAnimDuration; // inicia timeout do hit
        currentState = State.Idle;
        agent.velocity  = Vector3.zero;
        agent.isStopped = true;
        agent.ResetPath();
        animator.ResetTrigger("Attack");
        animator.ResetTrigger("Hit");
        animator.SetTrigger("Hit");
    }

    // ─── SFX ──────────────────────────────────────────────────────────────────

    void HandleMovementSFX()
    {
        bool isMoving = agent.velocity.magnitude > 0.1f;

        if (isMoving)
        {
            if (!footstepSource.isPlaying)
                footstepSource.Play();
            footstepSource.pitch = currentState == State.Chase ? 1.67f : 1.1f;
        }
        else
        {
            if (footstepSource.isPlaying)
                footstepSource.Stop();
        }
    }

    public void HandleAttackSFX()
    {
        // If still on cooldown, ignore the call
        if (Time.time < lastPlayTime + cooldown)
            return;

        lastPlayTime = Time.time;

        if (patrolGrowl != null && patrolGrowl.clip != null)
            patrolGrowl.PlayOneShot(patrolGrowl.clip);

        if (swing != null && swing.clip != null)
            swing.PlayDelayed(0.6f);
    }

    // ─── Utilitários ──────────────────────────────────────────────────────────

    Vector3 GetRandomPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere * patrolRadius;
        randomDir += transform.position;
        NavMesh.SamplePosition(randomDir, out NavMeshHit hit, patrolRadius, 1);
        return hit.position;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);
        Vector3 left  = Quaternion.Euler(0, -visionAngle, 0) * transform.forward * visionRange;
        Vector3 right = Quaternion.Euler(0,  visionAngle, 0) * transform.forward * visionRange;
        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, proximityRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (currentState == State.Investigate)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(lastKnownPosition, 0.3f);
        }
    }

    void LateUpdate()
    {
        if (isHit || isAttacking)
        {
            agent.isStopped = true;
            agent.velocity  = Vector3.zero;
        }
    }
}