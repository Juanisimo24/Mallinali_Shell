using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    public event System.Action OnJump;

    [Header("Movimiento")]
    public float speed = 5f;
    public float runMultiplier = 1.5f;

    [Header("Salto")]
    public float jumpForce = 7f;
    public int maxJumps = 2; // 👈 Doble salto (2 saltos permitidos)

    [Header("Dash (Ctrl)")]
    public float dashSpeed = 14f;
    public float dashDuration = 0.18f;
    public float dashCooldown = 0.35f;
    public int maxAirDashes = 1;
    public bool resetDashOnGround = true;

    [Header("Ground Check (BoxCast)")]
    public LayerMask groundLayer;
    public float groundSkinWidth = 0.06f;
    public float groundCheckDistance = 0.08f;

    [Header("Combate")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;

    [Header("Gestión externa")]
    public bool canControl = true;

    [Header("Jump Feel")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    public float fallGravityMultiplier = 2.2f;
    public float lowJumpGravityMultiplier = 3.0f;
    public float apexGravityMultiplier = 0.65f;
    public float apexThreshold = 0.35f;
    public float maxFallSpeed = 20f;

    // Refs
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private PlayerManagerDual manager;
    private PlayerCombat combat;

    // Estados
    private bool isGrounded;
    private bool isDashing;
    private int airDashesUsed;
    private int jumpsUsed;

    // Timers
    private float dashTimer;
    private float dashCooldownTimer;
    private float coyoteCounter;
    private float jumpBufferCounter;

    // Input
    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool dashPressed;
    private bool attackPressed;

    // Otros
    private float baseGravityScale;
    private int facing = 1;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        combat = GetComponent<PlayerCombat>();
        manager = FindObjectOfType<PlayerManagerDual>();

        rb.freezeRotation = true;
        baseGravityScale = Mathf.Max(0.0001f, rb.gravityScale);
    }

    void Update()
    {
        if (canControl)
        {
            moveInput = Input.GetAxisRaw("Horizontal");
            jumpPressed = Input.GetButtonDown("Jump");
            jumpHeld = Input.GetButton("Jump");
            dashPressed = Input.GetKeyDown(KeyCode.LeftControl);
            attackPressed = Input.GetButtonDown("Fire1");
        }
        else
        {
            moveInput = 0f;
            jumpPressed = jumpHeld = dashPressed = attackPressed = false;
        }

        // --- Ground check ---
        isGrounded = CheckGrounded();

        // Reset de saltos y dashes al tocar suelo
        if (isGrounded)
        {
            jumpsUsed = 0;
            if (resetDashOnGround) airDashesUsed = 0;
        }

        // Coyote / buffer
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        if (jumpPressed) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        // Intentar salto (doble o normal)
        if (jumpBufferCounter > 0f && (coyoteCounter > 0f || jumpsUsed < maxJumps - 1) && !isDashing)
        {
            DoJump();
            jumpBufferCounter = 0f;
        }

        // Ataque
        if (attackPressed && combat != null && manager != null)
        {
            combat.PerformAttack();
            manager.RegisterAttack();
        }

        // Dash
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

        if (dashPressed && !isDashing && dashCooldownTimer <= 0f)
        {
            bool canDashNow = isGrounded || (airDashesUsed < maxAirDashes);
            if (canDashNow)
            {
                StartDash();
                if (!isGrounded) airDashesUsed++;
            }
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) EndDash();
        }
    }

    void FixedUpdate()
    {
        // --- Movimiento horizontal ---
        if (!isDashing)
        {
            // 👇 No correr en el aire
            float currentSpeed = isGrounded && Input.GetKey(KeyCode.LeftShift) ? speed * runMultiplier : speed;

            float targetVX = moveInput * currentSpeed;
            float smoothedVX = Mathf.MoveTowards(rb.linearVelocity.x, targetVX, 20f * Time.fixedDeltaTime); // aceleración suave
            rb.linearVelocity = new Vector2(smoothedVX, rb.linearVelocity.y);

            // Flip
            if (moveInput > 0.01f) facing = 1;
            else if (moveInput < -0.01f) facing = -1;

            var ls = transform.localScale;
            transform.localScale = new Vector3(facing * Mathf.Abs(ls.x), ls.y, ls.z);
        }

        // --- Aplicar mejor gravedad (AddForce) ---
        if (!isDashing)
            ApplyBetterGravity();

        // Clamp de caída
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    // ----------------- ACCIONES -----------------
    void DoJump()
    {
        // Reinicia velocidad vertical antes de saltar
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        coyoteCounter = 0f;
        jumpBufferCounter = 0f;
        jumpsUsed++;

        OnJump?.Invoke();
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        int dashDir = facing;
        if (Mathf.Abs(moveInput) > 0.01f) dashDir = moveInput > 0 ? 1 : -1;

        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
    }

    void EndDash()
    {
        isDashing = false;
    }

    // ----------------- UTILIDADES -----------------
    bool CheckGrounded()
    {
        Bounds b = col.bounds;
        Vector2 origin = b.center;
        Vector2 size = new Vector2(b.size.x - groundSkinWidth * 2f, b.size.y);
        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    void ApplyBetterGravity()
    {
        float vy = rb.linearVelocity.y;
        bool nearApex = Mathf.Abs(vy) < apexThreshold && !isGrounded;
        float gravity = Physics2D.gravity.y * rb.mass; // base gravity force
        float multiplier = 1f;

        if (vy < -0.01f)
            multiplier = fallGravityMultiplier;
        else if (vy > 0.01f && !jumpHeld)
            multiplier = lowJumpGravityMultiplier;
        else if (nearApex)
            multiplier = apexGravityMultiplier;

        rb.AddForce(Vector2.up * gravity * (multiplier - 1f)); // add extra gravity without touching gravityScale
    }

    void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (col == null) return;

        Bounds b = col.bounds;
        Vector2 size = new Vector2(b.size.x - groundSkinWidth * 2f, b.size.y);
        Vector2 origin = b.center;
        Vector2 down = Vector2.down * groundCheckDistance;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(origin, size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(origin + down, size);
    }

    public void SetControl(bool enabledControl)
    {
        canControl = enabledControl;
        if (!enabledControl)
        {
            moveInput = 0f; jumpPressed = false; jumpHeld = false; dashPressed = false; attackPressed = false;
        }
    }
}
