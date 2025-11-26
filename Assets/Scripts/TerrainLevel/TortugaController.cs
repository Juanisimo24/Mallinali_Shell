using UnityEngine;

public class TortugaController : MonoBehaviour
{
    [Header("Movement Stats")]
    public float walkSpeed = 3.0f;
    public float runMultiplier = 1.25f;
    public float jumpForce = 5.5f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;
    public float groundSkinWidth = 0.06f;
    public float groundCheckDistance = 0.1f;

    [Header("Input Settings")]
    public KeyCode jumpKey = KeyCode.Space;
    public KeyCode runKey = KeyCode.LeftShift;
    // Attack input is usually handled by Unity's Input Manager (Fire1), 
    // but you can add a KeyCode here if you prefer specific keys.

    [Header("Jump Feel (Juice)")]
    public float coyoteTime = 0.1f;
    public float jumpBufferTime = 0.12f;
    public float fallGravityMultiplier = 2.0f;
    public float lowJumpGravityMultiplier = 2.8f;
    public float apexGravityMultiplier = 0.75f;
    public float apexThreshold = 0.35f;
    public float maxFallSpeed = 18f;

    // References
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private PlayerCombat combat; 
    // Note: We don't strictly need CharacterStats here anymore 
    // unless you want movement to change based on health.

    // State Variables
    private bool isGrounded;
    private int facing = 1; // 1 = Right, -1 = Left

    // Timers
    private float coyoteCounter;
    private float jumpBufferCounter;

    // Input Cache
    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool runHeld;
    private bool attackPressed;
    private float baseGravity;
    private bool isAbilityOverridingMovement = false;

    private bool isClimbing;
    private float ladderClimbSpeed;
    private float ladderSlideSpeed;
    private float verticalInput;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        combat = GetComponent<PlayerCombat>();
        
        // Safety check for physics rotation
        rb.freezeRotation = true;
        baseGravity = Mathf.Max(0.0001f, rb.gravityScale);
    }

    void Update()
    {
         if (isAbilityOverridingMovement) return;
        // 1. Process Input
        ProcessInputs();
        

        // Jump off ladder logic
        if (isClimbing && Input.GetKeyDown(jumpKey))
        {
            SetLadderState(false, 0, 0);
            PerformJump();
        }
        // 2. Logic Checkers
        CheckGround();
        UpdateTimers();
        
        // 3. Action Execution
        HandleJumpLogic();
        HandleGravity();
        HandleFacing();
        HandleAttack();
    }

    void FixedUpdate()
    {
        if (isAbilityOverridingMovement) return; 
        
         if (isClimbing)
        {
            HandleLadderPhysics();
            return; 
        }

        Move();
    }

    // ============================================
    //              CORE LOGIC
    // ============================================

    void ProcessInputs()
    {
        moveInput     = Input.GetAxisRaw("Horizontal");
        jumpPressed   = Input.GetKeyDown(jumpKey);
        jumpHeld      = Input.GetKey(jumpKey);
        runHeld       = Input.GetKey(runKey);
        attackPressed = Input.GetButtonDown("Fire1"); 
        verticalInput = Input.GetAxisRaw("Vertical");
    }

    void Move()
    {
        // Calculate Speed
        float finalSpeed = walkSpeed * (runHeld ? runMultiplier : 1f);
        float targetVX = moveInput * finalSpeed;

        // Apply Velocity (Preserving Y velocity)
        rb.linearVelocity = new Vector2(targetVX, rb.linearVelocity.y);

        // Terminal Velocity (Prevent falling too fast)
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
        }
    }

void HandleLadderPhysics()
    {
        // Allow slow horizontal movement
        float targetVX = moveInput * (walkSpeed * 0.5f);
        
        float targetVY = 0f;

        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            targetVY = verticalInput * ladderClimbSpeed;
        }
        else
        {
            // The slow slide down effect
            targetVY = -ladderSlideSpeed;
        }

        rb.linearVelocity = new Vector2(targetVX, targetVY);
    }

    public void SetLadderState(bool active, float speed, float slide)
    {
        isClimbing = active;
        ladderClimbSpeed = speed;
        ladderSlideSpeed = slide;

        if (active)
        {
            rb.gravityScale = 0f;
            // Reset any specialized physics here
        }
        else
        {
            rb.gravityScale = 1f; // Or baseGravity variable
        }
    }
    
    void HandleJumpLogic()
    {
        // Jump is allowed if: Buffer is active AND Coyote is active
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            PerformJump();
            jumpBufferCounter = 0f; // Consume jump
        }
    }

    void PerformJump()
    {
        // Reset Y velocity for consistent jump height
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        
        // Apply impulse
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        
        // Reset coyote to prevent double jumping
        coyoteCounter = 0f;
    }

    void HandleGravity()
    {
        float vy = rb.linearVelocity.y;
        bool nearApex = Mathf.Abs(vy) < apexThreshold && !isGrounded;

        float g = baseGravity;

        // Apply "Mario-style" gravity modifiers
        if (vy < -0.01f)                      
            g = baseGravity * fallGravityMultiplier; // Falling fast
        else if (vy > 0.01f && !jumpHeld)     
            g = baseGravity * lowJumpGravityMultiplier; // Short hop
        else if (nearApex)                    
            g = baseGravity * apexGravityMultiplier; // Float at top

        rb.gravityScale = g;
    }

    void HandleFacing()
    {
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            facing = moveInput > 0 ? 1 : -1;
            
            // Apply scale to flip sprite
            var ls = transform.localScale;
            // Ensure x is positive before multiplying by facing to avoid double negative issues
            transform.localScale = new Vector3(facing * Mathf.Abs(ls.x), ls.y, ls.z);
        }
    }

    void HandleAttack()
    {
        if (attackPressed && combat != null)
        {
            combat.PerformAttack();
        }
    }
    

    // ============================================
    //              UTILITIES
    // ============================================

    void UpdateTimers()
    {
        // Coyote Time (Grace period after leaving ledge)
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        // Jump Buffer (Remember jump press before hitting ground)
        if (jumpPressed) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;
    }

    void CheckGround()
    {
        Bounds b = col.bounds;
        // Shrink width slightly to avoid sticking to walls
        Vector2 size = new Vector2(b.size.x - groundSkinWidth * 2f, b.size.y);
        Vector2 origin = new Vector2(b.center.x, b.min.y + size.y * 0.5f);
        
        RaycastHit2D hit = Physics2D.BoxCast(origin, size, 0f, Vector2.down, groundCheckDistance, groundLayer);
        isGrounded = hit.collider != null;
    }

    public void SetAbilityOverride(bool active)
{
    isAbilityOverridingMovement = active;
}

    void OnDrawGizmosSelected()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (col == null) return;

        Bounds b = col.bounds;
        Vector2 size = new Vector2(b.size.x - groundSkinWidth * 2f, b.size.y);
        Vector2 origin = new Vector2(b.center.x, b.min.y + size.y * 0.5f);
        Vector2 down = Vector2.down * groundCheckDistance;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(origin + down, size);
    }
}