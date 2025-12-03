using UnityEngine;
using System.Collections;

public class TortugaController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float walkSpeed = 3.0f;
    public float runMultiplier = 1.25f;
    public float jumpForce = 5.5f;

    [Header("Jump Feel (Juice)")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.12f;
    public float maxFallSpeed = 18f;
    // Gravity multipliers for better jump feel
    public float fallGravityMultiplier = 2.0f;
    public float lowJumpGravityMultiplier = 2.8f;

    [Header("Ladder Stats")]
    private bool isClimbing;
    private float ladderClimbSpeed;
    private float ladderSlideSpeed;

    [Header("Water Stats")]
    public float swimSpeed = 5f;
    public float swimAcceleration = 3f;
    public float rotationSpeed = 5f;
    public float waterDashForce = 12f;
    public float waterDashCooldown = 1f;

    [Header("Growth Stats")]
    public float maxSize = 2.5f;
    public float growthSmoothness = 2f;
    private Vector3 baseScale;
    private float currentSizeMultiplier = 1f;
    private float targetSizeMultiplier = 1f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundSkinWidth = 0.06f;
    public float groundCheckDistance = 0.1f;

    // --- REFERENCES ---
    private Rigidbody2D rb;
    private Collider2D col;
    private Animator anim;
    private CharacterStats stats;
    private PlayerCombat combat;

    // --- STATE VARIABLES ---
    private bool isGrounded;
    private bool isSwimming;
    private bool canWaterDash = true;
    private bool isAbilityOverridingMovement = false;
    private int facing = 1;

    // --- TIMERS ---
    private float coyoteCounter;
    private float jumpBufferCounter;

    // --- INPUTS ---
    private Vector2 inputDir; // X and Y
    private bool jumpPressed;
    private bool jumpHeld;
    private bool runHeld;
    private bool dashPressed;
    private bool attackPressed;
    
    private float baseGravity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        anim = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();
        combat = GetComponent<PlayerCombat>();

        rb.freezeRotation = true;
        baseGravity = Mathf.Max(0.0001f, rb.gravityScale);
        
        // Save initial scale
        baseScale = transform.localScale;
        baseScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, baseScale.z);
    }

    void Update()
    {
        // 1. If Ability (Dash on land) is active, pause logic
        if (isAbilityOverridingMovement) return;

        // 2. Process Inputs
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        inputDir = new Vector2(x, y).normalized; // Vector normalized for swimming
        
        // Keep raw X for ground movement to avoid slowdown on diagonals
        float rawX = x; 

        jumpPressed = Input.GetButtonDown("Jump");
        jumpHeld = Input.GetButton("Jump");
        runHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        dashPressed = Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.Space);
        attackPressed = Input.GetButtonDown("Fire1");

        // 3. Logic Checkers
        CheckGround();
        
        // Timers for Jump Feel
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        if (jumpPressed) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        // 4. Growth Logic (Lerp)
        if (Mathf.Abs(currentSizeMultiplier - targetSizeMultiplier) > 0.01f)
        {
            currentSizeMultiplier = Mathf.Lerp(currentSizeMultiplier, targetSizeMultiplier, Time.deltaTime * growthSmoothness);
            UpdateScale();
        }

        // 5. Jump Off Ladder
        if (isClimbing && jumpPressed)
        {
            SetLadderState(false, 0, 0);
            PerformJump();
        }

        // 6. Water Dash
        if (isSwimming && dashPressed && canWaterDash)
        {
            StartCoroutine(WaterDashRoutine());
        }

        // 7. Ground Jump Logic
        if (!isSwimming && !isClimbing)
        {
            if (jumpBufferCounter > 0f && coyoteCounter > 0f)
            {
                PerformJump();
                jumpBufferCounter = 0f;
            }
        }

        // 8. Attack
        if (attackPressed && combat != null) combat.PerformAttack();
    }

    void FixedUpdate()
    {
        if (isAbilityOverridingMovement) return;

        // PRIORITY: Swimming > Climbing > Ground
        if (isSwimming)
        {
            HandleSwimmingPhysics();
        }
        else if (isClimbing)
        {
            HandleLadderPhysics();
        }
        else
        {
            HandleGroundPhysics();
            ApplyBetterGravity();
        }
    }

    // ============================================
    //              LOGIC: SWIMMING
    // ============================================
   void HandleSwimmingPhysics()
    {
        // 1. Mover
        if (inputDir.sqrMagnitude > 0.1f)
        {
            // --- CORRECCIÓN AQUÍ ---
            // Calculamos la velocidad final basada en si corres o no
            float finalSpeed = swimSpeed * (runHeld ? runMultiplier : 1f);
            
            // Aplicamos esa velocidad a la dirección
            Vector2 targetVel = inputDir * finalSpeed;

            // Aplicamos el movimiento suave (Lerp)
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVel, swimAcceleration * Time.fixedDeltaTime);
            // -----------------------
            
            // 2. Lógica Visual (Rotación)
            
            // A. FACE LEFT/RIGHT (Flip)
            if (Mathf.Abs(inputDir.x) > 0.1f)
            {
                facing = inputDir.x > 0 ? 1 : -1;
                UpdateScale(); 
            }

            // B. TILT UP/DOWN
            float tiltAngle = 0f;
            if (Mathf.Abs(inputDir.y) > 0.1f)
            {
                // Inclinación suave al subir o bajar
                tiltAngle = inputDir.y > 0 ? 55f * facing : -55f * facing;
            }

            // Rotar suavemente
            Quaternion targetRot = Quaternion.Euler(0, 0, tiltAngle);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            // Desacelerar si no hay input
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, swimAcceleration * Time.fixedDeltaTime);
            
            // Enderezar la tortuga horizontalmente
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    // ============================================
    //              LOGIC: LADDER
    // ============================================
    void HandleLadderPhysics()
    {
        float xInput = Input.GetAxisRaw("Horizontal");
        float yInput = Input.GetAxisRaw("Vertical");

        // Allow slow horizontal movement
        float targetVX = xInput * (walkSpeed * 0.5f);
        float targetVY = 0f;

        if (Mathf.Abs(yInput) > 0.1f)
        {
            targetVY = yInput * ladderClimbSpeed;
            // anim.SetFloat("climbSpeed", 1);
        }
        else
        {
            targetVY = -ladderSlideSpeed; // Slide down slowly
            // anim.SetFloat("climbSpeed", 0);
        }

        rb.linearVelocity = new Vector2(targetVX, targetVY);
    }

    // ============================================
    //              LOGIC: GROUND
    // ============================================
    void HandleGroundPhysics()
    {
        // Reset rotation if we came from water
        if (transform.rotation != Quaternion.identity) 
            transform.rotation = Quaternion.identity;

        float xInput = Input.GetAxisRaw("Horizontal");
        float currentSpeed = walkSpeed * (runHeld ? runMultiplier : 1f);
        
        rb.linearVelocity = new Vector2(xInput * currentSpeed, rb.linearVelocity.y);

        // Clamp Fall Speed
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);

        // Facing
        if (Mathf.Abs(xInput) > 0.01f)
        {
            facing = xInput > 0 ? 1 : -1;
            UpdateScale();
        }
    }

    void ApplyBetterGravity()
    {
        float vy = rb.linearVelocity.y;
        float g = baseGravity;

        if (vy < -0.01f) g = baseGravity * fallGravityMultiplier;
        else if (vy > 0.01f && !jumpHeld) g = baseGravity * lowJumpGravityMultiplier;

        rb.gravityScale = g;
    }

    void PerformJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        coyoteCounter = 0f;
    }

    // ============================================
    //              STATE MANAGEMENT
    // ============================================

    // --- WATER ---
    public void SetSwimming(bool state)
    {
        isSwimming = state;
        if (isSwimming)
        {
            rb.gravityScale = 0;
            rb.linearDamping = 1f;
            // anim.SetBool("isSwimming", true);
            isClimbing = false; // Cannot climb and swim
        }
        else
        {
            rb.gravityScale = baseGravity;
            rb.linearDamping = 0f;
            transform.rotation = Quaternion.identity;
            // anim.SetBool("isSwimming", false);
        }
    }

    // --- LADDER ---
    public void SetLadderState(bool active, float climbSpeed, float slideSpeed)
    {
        // Don't climb if swimming
        if (isSwimming && active) return;

        isClimbing = active;
        ladderClimbSpeed = climbSpeed;
        ladderSlideSpeed = slideSpeed;

        if (active)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
            // anim.SetBool("isClimbing", true);
        }
        else
        {
            rb.gravityScale = baseGravity;
            // anim.SetBool("isClimbing", false);
        }
    }

    // --- ABILITY ---
    public void SetAbilityOverride(bool active) => isAbilityOverridingMovement = active;

    // --- GROWTH ---
    public void EatFish(float growthAmount, int healAmount)
    {
        if (stats != null) stats.Heal(healAmount);

        if (targetSizeMultiplier < maxSize)
        {
            targetSizeMultiplier += growthAmount;
            swimSpeed += 0.5f; 
            waterDashForce += 1f;
        }
    }

    void UpdateScale()
    {
        transform.localScale = new Vector3(baseScale.x * currentSizeMultiplier * facing, baseScale.y * currentSizeMultiplier, baseScale.z);
    }

    // ============================================
    //              UTILITIES
    // ============================================
    void CheckGround()
    {
        Bounds b = col.bounds;
        Vector2 size = new Vector2(b.size.x - groundSkinWidth * 2f, b.size.y);
        Vector2 origin = new Vector2(b.center.x, b.min.y + size.y * 0.5f);
        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
    }

    IEnumerator WaterDashRoutine()
    {
        canWaterDash = false;
        Vector2 dir = inputDir.sqrMagnitude > 0.1f ? inputDir : (Vector2)transform.right * facing;
        rb.AddForce(dir * waterDashForce, ForceMode2D.Impulse);
        yield return new WaitForSeconds(waterDashCooldown);
        canWaterDash = true;
    }
}