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

    [Header("Dash (Ctrl)")]
    public float dashSpeed = 14f;          // velocidad horizontal del dash
    public float dashDuration = 0.18f;     // duración del dash en segundos
    public float dashCooldown = 0.35f;     // cooldown entre dashes
    public int maxAirDashes = 1;           // dashes permitidos en aire
    public bool resetDashOnGround = true;  // reponer dashes al tocar suelo

    [Header("Ground Check (BoxCast)")]
    public LayerMask groundLayer;
    [Tooltip("Extra grosor lateral del boxcast (reduce falsos negativos en bordes)")]
    public float groundSkinWidth = 0.06f;
    [Tooltip("Distancia hacia abajo para el boxcast")]
    public float groundCheckDistance = 0.08f;

    [Header("Combate")]
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;

    [Header("Gestión externa")]
    public bool canControl = true;

    // ---------- FEEL DEL SALTO ----------
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

    // Timers
    private float dashTimer;
    private float dashCooldownTimer;
    private float coyoteCounter;
    private float jumpBufferCounter;

    // Input cache
    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool dashPressed;
    private bool attackPressed;

    // Otros
    private float baseGravityScale;
    private int facing = 1; // 1 derecha, -1 izquierda

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
        // --- Leer inputs solo si puedo controlar ---
        if (canControl)
        {
            moveInput     = Input.GetAxisRaw("Horizontal");
            jumpPressed   = Input.GetButtonDown("Jump");
            jumpHeld      = Input.GetButton("Jump");
            dashPressed   = Input.GetKeyDown(KeyCode.LeftControl);
            attackPressed = Input.GetButtonDown("Fire1");
        }
        else
        {
            moveInput = 0f;
            jumpPressed = jumpHeld = dashPressed = attackPressed = false;
        }

        // --- Ground Check robusto (BoxCast a partir del collider) ---
        isGrounded = CheckGrounded();

        // Reset de dashes al tocar suelo
        if (isGrounded && resetDashOnGround) airDashesUsed = 0;

        // --- Coyote y Jump Buffer ---
        if (isGrounded) coyoteCounter = coyoteTime;
        else            coyoteCounter -= Time.deltaTime;

        if (jumpPressed) jumpBufferCounter = jumpBufferTime;
        else             jumpBufferCounter -= Time.deltaTime;

        // Intento de salto usando buffer + coyote (si no estoy en dash)
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !isDashing)
        {
            DoJump();
            jumpBufferCounter = 0f;
        }

        // --- Ataque (si aplica a tu lógica actual) ---
        if (attackPressed && combat != null && manager != null)
        {
            combat.PerformAttack();
            manager.RegisterAttack();
        }

        // --- Cooldown de dash ---
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;

        // --- Activación de dash (en aire o tierra) ---
        if (dashPressed && !isDashing && dashCooldownTimer <= 0f)
        {
            bool canDashNow = isGrounded || (airDashesUsed < maxAirDashes);
            if (canDashNow)
            {
                StartDash();
                if (!isGrounded) airDashesUsed++;
            }
        }

        // --- Fin de dash por tiempo ---
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0f) EndDash();
        }

        // --- Gravedad dinámica (no aplicar mientras dashing) ---
        if (!isDashing) ApplyBetterGravity();
    }

    void FixedUpdate()
    {
        // --- Movimiento horizontal (no cuando dashing) ---
        if (!isDashing)
        {
            float finalSpeed = (Input.GetKey(KeyCode.LeftShift) && canControl) ? speed * runMultiplier : speed;
            float targetVX = moveInput * finalSpeed;
            rb.linearVelocity = new Vector2(targetVX, rb.linearVelocity.y);

            // Voltear (por escala, para que hijos sigan mirando al frente)
            if (moveInput > 0.01f)      facing = 1;
            else if (moveInput < -0.01f) facing = -1;

            var ls = transform.localScale;
            transform.localScale = new Vector3(facing * Mathf.Abs(ls.x), ls.y, ls.z);
        }

        // --- Clamp de caída ---
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    // ----------------- Acciones -----------------
    void DoJump()
    {
        // Reinicia vertical, aplica impulso
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        // Consumir coyote y buffer
        coyoteCounter = 0f;
        jumpBufferCounter = 0f;

        OnJump?.Invoke();
    }

    void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;

        // Direccion del dash:
        // Si hay input, úsalo; si no, usa el facing actual
        int dashDir = facing;
        if (Mathf.Abs(moveInput) > 0.01f) dashDir = moveInput > 0 ? 1 : -1;

        // Congelar gravedad durante dash (para un “slice” limpio)
        rb.gravityScale = 0f;

        // Dar velocidad horizontal fuerte, limpias Y para un dash recto
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
    }

    void EndDash()
    {
        isDashing = false;
        rb.gravityScale = baseGravityScale;

        // Mantener el momentum horizontal, pero no infinito
        rb.linearVelocity = new Vector2(Mathf.Clamp(rb.linearVelocity.x, -dashSpeed, dashSpeed), rb.linearVelocity.y);
    }

    // ----------------- Utilidades -----------------
    bool CheckGrounded()
    {
        // BoxCast usando el tamaño del collider, con un “skin” horizontal
        Bounds b = col.bounds;
        Vector2 size = new Vector2(b.size.x , b.size.y);
        Vector2 origin = new Vector2(b.center.x, b.min.y + size.y * 0.5f);

        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    void ApplyBetterGravity()
    {
        float vy = rb.linearVelocity.y;
        bool nearApex = Mathf.Abs(vy) < apexThreshold && !isGrounded;

        if (vy < -0.01f)
        {
            rb.gravityScale = baseGravityScale * fallGravityMultiplier;
        }
        else if (vy > 0.01f && !jumpHeld)
        {
            rb.gravityScale = baseGravityScale * lowJumpGravityMultiplier;
        }
        else if (nearApex)
        {
            rb.gravityScale = baseGravityScale * apexGravityMultiplier;
        }
        else
        {
            rb.gravityScale = baseGravityScale;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (col == null) return;

        // Gizmo del BoxCast de suelo
        Bounds b = col.bounds;
        Vector2 size = new Vector2(b.size.x - groundSkinWidth * 2f, b.size.y);
        Vector2 origin = new Vector2(b.center.x, b.min.y + size.y * 0.5f);
        Vector2 down = Vector2.down * groundCheckDistance;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(origin, size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(origin + down, size);
    }

    // Métodos auxiliares para el manager
    public void SetControl(bool enabledControl)
    {
        canControl = enabledControl;
        if (!enabledControl)
        {
            moveInput = 0f; jumpPressed = false; jumpHeld = false; dashPressed = false; attackPressed = false;
        }
    }
}
