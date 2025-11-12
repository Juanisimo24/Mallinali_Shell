using UnityEngine;

public class TortugaController : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 3.0f;            // más lenta
    public float runMultiplier = 1.25f;       // corre poco
    public float jumpForce = 5.5f;            // salto más bajo

    [Header("Shell Charge (embestida)")]
    public float chargeSpeed = 10f;
    public float chargeDuration = 0.22f;
    public float chargeCooldown = 0.6f;

    [Header("Guardia (caparazón)")]
    public KeyCode guardKey = KeyCode.Q; // botón derecho por defecto
    public float guardMoveMultiplier = 0.35f; // se mueve mucho menos
    public float guardGravityMultiplier = 1.2f; // se “siente” más pesada
    public float guardKnockbackResist = 0.6f;   // reduce 60% del retroceso

    [Header("Suelo (BoxCast)")]
    public LayerMask groundLayer;
    public float groundSkinWidth = 0.06f;
    public float groundCheckDistance = 0.1f;

    [Header("Input")]
    public KeyCode chargeKey = KeyCode.LeftControl; // embestida
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode runKey = KeyCode.LeftShift;

    [Header("Jump Feel")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.12f;
    public float fallGravityMultiplier = 2.0f;
    public float lowJumpGravityMultiplier = 2.8f;
    public float apexGravityMultiplier = 0.75f;
    public float apexThreshold = 0.35f;
    public float maxFallSpeed = 18f;
    private bool attackPressed;

    // Refs
    Rigidbody2D rb;
    Collider2D col;
    SpriteRenderer sr;
    TurtleHealth health;
    private PlayerCombat combat;
    private PlayerManagerDual manager;

    // Estado
    bool isGrounded;
    bool isCharging;
    public bool isGuarding;

    // Timers
    float coyoteCounter;
    float jumpBufferCounter;
    float chargeTimer;
    float chargeCooldownTimer;

    // Input cache
    float moveInput;
    bool jumpPressed;
    bool jumpHeld;
    bool chargePressed;
    public bool guardHeld;
    bool runHeld;

    // Otros
    float baseGravity;
    int facing = 1;          // 1 derecha, -1 izquierda

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        health = GetComponent<TurtleHealth>();
        combat = GetComponent<PlayerCombat>();
        manager = FindObjectOfType<PlayerManagerDual>();

        rb.freezeRotation = true;
        baseGravity = Mathf.Max(0.0001f, rb.gravityScale);
    }

    void Update()
    {
        // ---- Input
        moveInput     = Input.GetAxisRaw("Horizontal");
        jumpPressed   = Input.GetKeyDown(jumpKey);
        jumpHeld      = Input.GetKey(jumpKey);
        chargePressed = Input.GetKeyDown(chargeKey);
        guardHeld     = Input.GetKey(guardKey);
        runHeld       = Input.GetKey(runKey);
        attackPressed = Input.GetButtonDown("Fire1");

        // ---- Ground Check
        isGrounded = CheckGrounded();

        // ---- Guardia (caparazón)
        isGuarding = guardHeld && !isCharging;
        health.SetGuarding(isGuarding, guardKnockbackResist);

        // ---- Coyote & Jump Buffer
        if (isGrounded) coyoteCounter = coyoteTime;
        else            coyoteCounter -= Time.deltaTime;

        if (jumpPressed) jumpBufferCounter = jumpBufferTime;
        else             jumpBufferCounter -= Time.deltaTime;

        // Saltar (permitido si no está en embestida)
        if (jumpBufferCounter > 0f && coyoteCounter > 0f && !isCharging)
        {
            DoJump();
            jumpBufferCounter = 0f;
        }

        // ---- Charge (embestida)
        if (chargeCooldownTimer > 0f) chargeCooldownTimer -= Time.deltaTime;

        if (chargePressed && !isCharging && chargeCooldownTimer <= 0f)
        {
            StartCharge();
        }

        if (isCharging)
        {
            chargeTimer -= Time.deltaTime;
            if (chargeTimer <= 0f) EndCharge();
        }

        // ---- Gravedad dinámica (no se aplica en charge)
        if (!isCharging) ApplyBetterGravity();

        // ---- Facing
        if (!isCharging) // en charge lo fijamos al iniciar
        {
            if (moveInput > 0.01f) facing = 1;
            else if (moveInput < -0.01f) facing = -1;

            var ls = transform.localScale;
            transform.localScale = new Vector3(facing * Mathf.Abs(ls.x), ls.y, ls.z);
        }

         // --- Ataque (si aplica a tu lógica actual) ---
        if (attackPressed && combat != null && manager != null)
        {
            combat.PerformAttack();
            manager.RegisterAttack();
        }
    }

    void FixedUpdate()
    {
        // Movimiento horizontal
        if (!isCharging)
        {
            float finalSpeed = walkSpeed * (runHeld ? runMultiplier : 1f);
            if (isGuarding) finalSpeed *= guardMoveMultiplier;

            float targetVX = moveInput * finalSpeed;
            rb.linearVelocity = new Vector2(targetVX, rb.linearVelocity.y);
        }

        // Clamp caída
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    // ------------ Acciones ----------
    void DoJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);

        coyoteCounter = 0f;
    }

    void StartCharge()
    {
        isCharging = true;
        isGuarding = false; // no guardia durante embestida
        health.SetGuarding(false, 0f);

        // Fijar dirección: si hay input, úsalo; si no, usa facing actual
        int dir = Mathf.Abs(moveInput) > 0.01f ? (moveInput > 0 ? 1 : -1) : facing;
        facing = dir;
        var ls = transform.localScale;
        transform.localScale = new Vector3(facing * Mathf.Abs(ls.x), ls.y, ls.z);

        // “Armadura” ligera durante charge (menos daño / menos knockback)
        health.SetCharging(true);

        // Quitar gravedad para un corte horizontal limpio
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dir * chargeSpeed, 0f);

        chargeTimer = chargeDuration;
        chargeCooldownTimer = chargeCooldown;
    }

    void EndCharge()
    {
        isCharging = false;
        health.SetCharging(false);
        rb.gravityScale = baseGravity;

        // Mantén algo de momentum pero vuelve a gravedad normal
        rb.linearVelocity = new Vector2(Mathf.Clamp(rb.linearVelocity.x, -chargeSpeed, chargeSpeed), rb.linearVelocity.y);
    }

    // ------------ Utilidades ----------
    bool CheckGrounded()
    {
        Bounds b = col.bounds;
        Vector2 size = new Vector2(b.size.x - groundSkinWidth * 2f, b.size.y);
        Vector2 origin = new Vector2(b.center.x, b.min.y + size.y * 0.5f);
        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);
        return hit.collider != null;
    }

    void ApplyBetterGravity()
    {
        float vy = rb.linearVelocity.y;
        bool nearApex = Mathf.Abs(vy) < apexThreshold && !isGrounded;

        float g = baseGravity;
        if (vy < -0.01f)                      g = baseGravity * fallGravityMultiplier;
        else if (vy > 0.01f && !jumpHeld)     g = baseGravity * lowJumpGravityMultiplier;
        else if (nearApex)                    g = baseGravity * apexGravityMultiplier;

        // En guardia, se “siente” más pesada
        if (isGuarding) g *= guardGravityMultiplier;

        rb.gravityScale = g;
    }

    void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (col == null) return;

        Bounds b = col.bounds;
        Vector2 size = new Vector2(b.size.x - groundSkinWidth * 2f, b.size.y);
        Vector2 origin = new Vector2(b.center.x, b.min.y + size.y * 0.5f);
        Vector2 down = Vector2.down * groundCheckDistance;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(origin, size);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(origin + down, size);
    }
}
