using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FollowerGround2D : MonoBehaviour
{
    [Header("Referencias")]
    public Transform leader;
    public Rigidbody2D leaderRb;
    public Transform leaderGroundCheck;
    public Transform myGroundCheck;
    public LayerMask groundLayer;

    [Header("Follow (solo X)")]
    public float followDistance = 3.0f;
    public float stopBuffer = 0.5f;
    public float maxSpeed = 4.0f;
    public float accel = 20f;
    public float decel = 30f;

    [Header("Catchup")]
    public float leashDistance = 5f;
    public float rayDown = 3f;

    [Header("Detección de frente del líder")]
    public bool useLeaderSpriteFacing = true;
    public bool fallbackUseLeaderVelocity = true;

    [Header("Salto espejo (ahora con doble salto)")]
    public bool mirrorJump = true;
    public float jumpForce = 7f;
    public int maxJumps = 2; // 👈 nuevo: doble salto
    public float groundRadius = 0.14f;
    public float leaderGroundRadius = 0.14f;
    public float minLeaderJumpVy = 1.0f;
    public float mirrorWindow = 0.12f;
    public float groundCheckDistance = 0.08f;

    private Rigidbody2D rb;
    private Collider2D col;
    private Collider2D leadercol;
    private SpriteRenderer leaderSR;
    private Rigidbody2D cachedLeaderRb;

    private bool myGrounded;
    private bool leaderGrounded;
    private bool leaderWasGrounded;
    private float timeSinceLeaderJump = 999f;

    // 👇 contador de saltos
    private int jumpsUsed = 0;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        leadercol = leader.gameObject.GetComponent<Collider2D>();
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

        // Ground checks
        myGrounded = IsGrounded(col);
        if (leaderGroundCheck != null)
            leaderGrounded = IsGrounded(leadercol);
        else if (cachedLeaderRb != null)
            leaderGrounded = Mathf.Abs(cachedLeaderRb.linearVelocity.y) < 0.01f;
        else
            leaderGrounded = false;

        // Reset salto al tocar suelo
        if (myGrounded)
            jumpsUsed = 0;

        // Detectar salto del líder
        if (mirrorJump && cachedLeaderRb != null)
        {
            bool leaderJustJumped = cachedLeaderRb.linearVelocity.y > minLeaderJumpVy;
            if (leaderJustJumped)
                timeSinceLeaderJump = 0f;

            timeSinceLeaderJump += Time.deltaTime;

            // doble salto: permite replicar salto si tiene saltos disponibles
            if (timeSinceLeaderJump <= mirrorWindow && jumpsUsed < maxJumps)
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

        // Dirección del líder
        int facingDir = 1;
        if (useLeaderSpriteFacing && leaderSR != null)
            facingDir = leaderSR.flipX ? -1 : 1;
        else if (fallbackUseLeaderVelocity && cachedLeaderRb != null)
            facingDir = (cachedLeaderRb.linearVelocity.x >= 0f) ? 1 : -1;

        // Objetivo detrás del líder
        float targetX = leader.position.x - facingDir * followDistance;

        // Warp si se aleja demasiado
        if (Mathf.Abs(leader.position.x - rb.position.x) > leashDistance)
        {
            WarpBehindLeader(facingDir);
            return;
        }

        float deltaX = targetX - rb.position.x;
        float desiredVX = 0f;

        if (Mathf.Abs(deltaX) > stopBuffer)
        {
            float pOut = deltaX * 5f;
            desiredVX = Mathf.Clamp(pOut, -maxSpeed, maxSpeed);
        }

        float currentVX = rb.linearVelocity.x;
        float rate = (Mathf.Abs(desiredVX) > Mathf.Abs(currentVX)) ? accel : decel;
        float newVX = Mathf.MoveTowards(currentVX, desiredVX, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVX, rb.linearVelocity.y);
    }

    // ---------- Utilidades ----------
    bool IsGrounded(Collider2D collider)
    {
        Bounds b = collider.bounds;
        Vector2 size = new Vector2(b.size.x, b.size.y);
        Vector2 origin = new Vector2(b.center.x, b.min.y + size.y * 0.5f);

        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    void DoJump(float force)
    {
        if (jumpsUsed >= maxJumps) return; // permite hasta doble salto
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * force, ForceMode2D.Impulse);
        jumpsUsed++;
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
        if (col is BoxCollider2D box) return box.size.y * 0.5f;
        if (col is CircleCollider2D cir) return cir.radius;
        return 0.5f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        if (myGroundCheck != null) Gizmos.DrawWireSphere(myGroundCheck.position, groundRadius);
        if (leaderGroundCheck != null) Gizmos.DrawWireSphere(leaderGroundCheck.position, leaderGroundRadius);
    }
}
