using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI_FSM : MonoBehaviour
{
    public enum State { Patrol, Chase, Attack, Idle, Dead }
    public State current = State.Patrol;

    [Header("Refs")]
    public EnemyStats stats;
    public Sensor2D sensor;           // <- Usa el sensor multi-objetivo
    public Transform[] patrolPoints;
    public Transform attackPoint;
    public LayerMask damageableMask;        // capa(s) de objetivos dañables (guerrero + tortuga)
    public EnemyThreat threat;              // opcional, pero recomendado (taunt/amenaza)

    [Header("Tunables")]
    public float lostSightToPatrolTime = 2.0f;

    // Internos
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private EnemyHealth hp;

    private Transform currentTarget;        // objetivo actual (guerrero o tortuga)
    private int patrolIndex = 0;
    private float lastSeenTimer = 0f;
    private float attackCooldownTimer = 0f;

    void Awake()
    {
        rb  = GetComponent<Rigidbody2D>();
        sr  = GetComponentInChildren<SpriteRenderer>();
        hp  = GetComponent<EnemyHealth>();
        if (hp) hp.OnDeath += HandleDeath;
    }

    void Start()
    {
        if (sensor != null && stats != null)
        {
            sensor.sightRange     = stats.sightRange;
            sensor.sightAngle     = stats.sightAngle;
            sensor.sightObstacles = stats.sightObstacles;
        }
    }

    void Update()
    {
        if (current == State.Dead) return;

        if (attackCooldownTimer > 0f) attackCooldownTimer -= Time.deltaTime;

        bool hasTarget = ResolveTarget();  // decide a quién perseguir/atacar

        switch (current)
        {
            case State.Patrol:
                if (hasTarget) { current = State.Chase; lastSeenTimer = 0f; }
                break;

            case State.Chase:
                if (hasTarget)
                {
                    lastSeenTimer = 0f;
                    if (InAttackRange()) current = State.Attack;
                }
                else
                {
                    lastSeenTimer += Time.deltaTime;
                    if (lastSeenTimer >= lostSightToPatrolTime) current = State.Patrol;
                }
                break;

            case State.Attack:
                if (!InAttackRange())
                {
                    current = hasTarget ? State.Chase : State.Patrol;
                }
                else
                {
                    TryAttack();
                }
                break;
        }

        // Girar visualmente hacia el target actual
        if (currentTarget != null)
        {
            int facing = (currentTarget.position.x >= transform.position.x) ? 1 : -1;
            var ls = transform.localScale;
            transform.localScale = new Vector3(facing * Mathf.Abs(ls.x), ls.y, ls.z);
        }
    }

    void FixedUpdate()
    {
        if (current == State.Dead) { rb.linearVelocity = Vector2.zero; return; }

        switch (current)
        {
            case State.Patrol: DoPatrol(); break;
            case State.Chase:  DoChase();  break;
            case State.Attack: rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); break;
            default:           rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y); break;
        }
    }

    // ----------------- Resolución de objetivo -----------------
    bool ResolveTarget()
    {
        // 1) Taunt/Threat tiene prioridad absoluta si está activo
        if (threat != null && threat.isTaunted && threat.tauntTarget != null)
        {
            currentTarget = threat.tauntTarget; // fuerza target (la tortuga)
            return true;
        }

        // 2) Si ya tengo target y todavía lo veo, lo mantengo (Anti-flicker)
        if (currentTarget != null && sensor != null && sensor.CanSee(currentTarget))
            return true;

        // 3) Buscar mejor visible con el sensor (guerrero o tortuga)
        if (sensor != null && sensor.TryGetBestTarget(out Transform best))
        {
            // Si hay EnemyThreat, puede preferir otro objetivo por amenaza acumulada
            if (threat != null)
            {
                Transform preferred = threat.GetPreferredTarget(best);
                currentTarget = preferred != null ? preferred : best;
            }
            else
            {
                currentTarget = best;
            }
            return true;
        }

        currentTarget = null;
        return false;
    }

    // ----------------- Estados -----------------
    void DoPatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        Transform target = patrolPoints[patrolIndex];
        float dx = target.position.x - transform.position.x;
        float dir = Mathf.Sign(dx);
        float speed = stats.walkSpeed;

        int facing = (target.position.x >= transform.position.x) ? 1 : -1;
        var ls = transform.localScale;
        transform.localScale = new Vector3(facing * Mathf.Abs(ls.x), ls.y, ls.z);

        if (Mathf.Abs(dx) > 0.1f)
            rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
        else
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    void DoChase()
    {
        if (currentTarget == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float dx = currentTarget.position.x - transform.position.x;
        float dir = Mathf.Sign(dx);
        float speed = stats.chaseSpeed;

        int facing = (currentTarget.position.x >= transform.position.x) ? 1 : -1;
        var ls = transform.localScale;
        transform.localScale = new Vector3(facing * Mathf.Abs(ls.x), ls.y, ls.z);

        rb.linearVelocity = new Vector2(dir * speed, rb.linearVelocity.y);
    }

    // ----------------- Combate -----------------
    bool InAttackRange()
    {
        if (currentTarget == null || attackPoint == null) return false;
        return Vector2.Distance(attackPoint.position, currentTarget.position) <= stats.attackRange;
    }

    void TryAttack()
    {
        if (attackCooldownTimer > 0f) return;

        // Golpea a cualquier IDamageable en rango (guerrero o tortuga)
        Collider2D c = Physics2D.OverlapCircle(attackPoint.position, stats.attackRange, damageableMask);
        if (c != null)
        {
            var damageable = c.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int facing = transform.localScale.x >= 0f ? 1 : -1;
                Vector2 hitNormal = new Vector2(facing, 0f);
                damageable.TakeDamage(stats.damage, attackPoint.position, hitNormal);
            }
        }

        attackCooldownTimer = stats.attackCooldown;
    }

    // ----------------- Muerte -----------------
    void HandleDeath()
    {
        current = State.Dead;
        rb.linearVelocity = Vector2.zero;
        enabled = false;
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        float r = (stats != null ? stats.attackRange : 0.7f);
        Gizmos.DrawWireSphere(attackPoint.position, r);
    }
}
