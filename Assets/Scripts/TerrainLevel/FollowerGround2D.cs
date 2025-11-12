using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FollowerGround2D : MonoBehaviour
{
    [Header("Referencias")]
    public Transform leader;                // Asignado por el manager
    public Rigidbody2D leaderRb;            // Recomendado: Rigidbody del líder
    public Transform leaderGroundCheck;     // Opcional: groundCheck del líder (mejor)
    public Transform myGroundCheck;         // Recomendado: groundCheck del seguidor
    public LayerMask groundLayer;           // Capa del piso

    [Header("Follow (solo X)")]
    public float followDistance = 3.0f;     // Distancia detrás del líder
    public float stopBuffer = 0.5f;         // Zona muerta para no "bailar"
    public float maxSpeed = 4.0f;           // Velocidad máx X
    public float accel = 20f;               // Acelerar hacia vX objetivo
    public float decel = 30f;               // Desacelerar al frenar

    [Header("Catchup (si se quedó muy atrás)")]
    public float leashDistance = 12f;       // Si supera esto, warp detrás del líder
    public float rayDown = 3f;              // Raycast hacia abajo para pegar al piso al warpear

    [Header("Detección de frente del líder")]
    public bool useLeaderSpriteFacing = true;
    public bool fallbackUseLeaderVelocity = true;

    [Header("Salto espejo (estricto)")]
    public bool mirrorJump = true;
    public float jumpForce = 7f;            // Fuerza de salto del seguidor
    public float groundRadius = 0.14f;      // Radio ground del seguidor
    public float leaderGroundRadius = 0.14f;// Radio ground del líder (si se usa groundCheck)
    public float minLeaderJumpVy = 1.0f;    // Mínimo Vy para considerar "salto" (evita caídas/bordes)
    public float mirrorWindow = 0.12f;      // Ventana tras salto del líder para replicar

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer leaderSR;
    private Rigidbody2D cachedLeaderRb;

    private bool myGrounded;
    private bool leaderGrounded;
    private bool leaderWasGrounded;         // frame anterior
    private float timeSinceLeaderJump = 999f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;

        if (leader != null)
        {
            leaderSR = leader.GetComponentInChildren<SpriteRenderer>();
            cachedLeaderRb = leaderRb != null ? leaderRb : leader.GetComponent<Rigidbody2D>();
        }
    }

    void OnEnable()
    {
        if (leader != null)
        {
            leaderSR = leader.GetComponentInChildren<SpriteRenderer>();
            cachedLeaderRb = leaderRb != null ? leaderRb : leader.GetComponent<Rigidbody2D>();
        }
    }

    void Update()
    {
        if (leader == null) return;

        // Ground states
        myGrounded = IsGrounded(myGroundCheck ? myGroundCheck.position : (Vector2)transform.position, groundRadius);

        if (leaderGroundCheck != null)
            leaderGrounded = IsGrounded(leaderGroundCheck.position, leaderGroundRadius);
        else if (cachedLeaderRb != null)
            leaderGrounded = Mathf.Abs(cachedLeaderRb.linearVelocity.y) < 0.01f; // fallback aproximado
        else
            leaderGrounded = false;

        // Detectar salto del líder por transición suelo->aire con Vy positiva
        if (mirrorJump && cachedLeaderRb != null)
        {
            bool leaderJustJumped = (leaderWasGrounded && !leaderGrounded && cachedLeaderRb.linearVelocity.y > minLeaderJumpVy);

            if (leaderJustJumped)
            {
                timeSinceLeaderJump = 0f;
            }

            // Ventana para replicar el salto (solo si el follower está en el suelo)
            timeSinceLeaderJump += Time.deltaTime;
            if (timeSinceLeaderJump <= mirrorWindow && myGrounded)
            {
                DoJump(jumpForce);
                timeSinceLeaderJump = 999f; // cerrar ventana
            }
        }

        leaderWasGrounded = leaderGrounded;
    }

    void FixedUpdate()
    {
        if (leader == null) return;

        // Dirección del líder (por sprite o por velocidad)
        int facingDir = 1;
        if (useLeaderSpriteFacing && leaderSR != null)
            facingDir = leaderSR.flipX ? -1 : 1;
        else if (fallbackUseLeaderVelocity && (cachedLeaderRb != null))
            facingDir = (cachedLeaderRb.linearVelocity.x >= 0f) ? 1 : -1;

        // Objetivo X (detrás del líder)
        float targetX = leader.position.x - facingDir * followDistance;

        // Warp si está muy lejos
        if (Mathf.Abs(leader.position.x - rb.position.x) > leashDistance)
        {
            WarpBehindLeader(facingDir);
            return;
        }

        // Control P simple con histéresis
        float deltaX = targetX - rb.position.x;
        float desiredVX = 0f;

        if (Mathf.Abs(deltaX) > stopBuffer)
        {
            float pOut = deltaX * 5f;
            desiredVX = Mathf.Clamp(pOut, -maxSpeed, maxSpeed);
        }

        // Aceleración/desaceleración en X, preservando Y
        float currentVX = rb.linearVelocity.x;
        float rate = (Mathf.Abs(desiredVX) > Mathf.Abs(currentVX)) ? accel : decel;
        float newVX = Mathf.MoveTowards(currentVX, desiredVX, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVX, rb.linearVelocity.y);
    }

    // ---------- Utilidades ----------
    bool IsGrounded(Vector2 pos, float radius)
    {
        return Physics2D.OverlapCircle(pos, radius, groundLayer);
    }

    void DoJump(float force)
    {
        if (!myGrounded) return;                 // jamás saltar si no está en el suelo
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
    }

    void WarpBehindLeader(int facingDir)
    {
        Vector2 desired = new Vector2(leader.position.x - facingDir * followDistance, leader.position.y + 1f);
        RaycastHit2D hit = Physics2D.Raycast(desired, Vector2.down, rayDown, groundLayer);
        float y = (hit.collider != null) ? hit.point.y + GetColliderHalfHeight() : rb.position.y;

        rb.position = new Vector2(desired.x, y);
        rb.linearVelocity = Vector2.zero;
    }

    float GetColliderHalfHeight()
    {
        if (col is CapsuleCollider2D cap) return cap.size.y * 0.5f;
        if (col is BoxCollider2D box)     return box.size.y * 0.5f;
        if (col is CircleCollider2D cir)  return cir.radius;
        return 0.5f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (myGroundCheck != null) Gizmos.DrawWireSphere(myGroundCheck.position, groundRadius);
        if (leaderGroundCheck != null) Gizmos.DrawWireSphere(leaderGroundCheck.position, leaderGroundRadius);
    }
}
