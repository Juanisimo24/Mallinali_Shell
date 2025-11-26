using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    public event System.Action OnJump;

    [Header("Movement")]
    public float speed = 5f;
    public float runMultiplier = 1.5f;

    [Header("Jump")]
    public float jumpForce = 7f;
    public int maxJumps = 2; 

    [Header("Ground Check")]
    public LayerMask groundLayer;
    public float groundSkinWidth = 0.06f;
    public float groundCheckDistance = 0.08f;

    [Header("Combat Reference")]
    public PlayerCombat combat; // Just a reference, logic is in PlayerCombat

    [Header("Jump Feel")]
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.12f;
    public float fallGravityMultiplier = 2.2f;
    public float lowJumpGravityMultiplier = 3.0f;
    public float apexGravityMultiplier = 0.65f;
    public float apexThreshold = 0.35f;
    public float maxFallSpeed = 20f;

    // --- State Management ---
    public bool canControl = true; 
    private bool isAbilityOverridingMovement = false; // NEW: Blocks movement input

    // Refs
    private Rigidbody2D rb;
    private Collider2D col;
    private Animator animator;

    // Internal State
    private bool isGrounded;
    private int jumpsUsed;
    private int facing = 1;

    // Input Cache
    private float moveInput;
    private bool jumpPressed;
    private bool jumpHeld;
    private bool attackPressed;

    // Timers
    private float coyoteCounter;
    private float jumpBufferCounter;

    [Header("Ladder State")]
    private bool isClimbing;
    private float ladderClimbSpeed;
    private float ladderSlideSpeed;
    private float verticalInput;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        animator = GetComponent<Animator>();
        rb.freezeRotation = true;
    }

    void Update()
    {
        // 1. If Ability is overriding (like Dashing), skip input processing
        if (isAbilityOverridingMovement) return;

        if (canControl)
        {
            moveInput = Input.GetAxisRaw("Horizontal");
            jumpPressed = Input.GetButtonDown("Jump");
            jumpHeld = Input.GetButton("Jump");
            attackPressed = Input.GetButtonDown("Fire1");
            verticalInput = Input.GetAxisRaw("Vertical");
        }
        else
        {
            moveInput = 0f;
            jumpPressed = jumpHeld = attackPressed = false;
        }

         // Check if we jump OFF the ladder
        if (isClimbing && Input.GetButtonDown("Jump"))
        {
            SetLadderState(false, 0, 0); // Exit ladder immediately on jump
            DoJump(); // Perform the jump
        }

        // 2. Ground Check
        isGrounded = CheckGrounded();
        if (isGrounded) jumpsUsed = 0;

        // 3. Jump Timers
        if (isGrounded) coyoteCounter = coyoteTime;
        else coyoteCounter -= Time.deltaTime;

        if (jumpPressed) jumpBufferCounter = jumpBufferTime;
        else jumpBufferCounter -= Time.deltaTime;

        // 4. Jump Logic
        if (jumpBufferCounter > 0f && (coyoteCounter > 0f || jumpsUsed < maxJumps - 1))
        {
            DoJump();
            jumpBufferCounter = 0f;
        }

        // 5. Attack (Logic is inside PlayerCombat)
        if (attackPressed && combat != null)
        {
            combat.PerformAttack();
        }
    }

    void FixedUpdate()
    {
        // IMPORTANT: If Ability is overriding (like Dashing), DO NOT touch Velocity
        if (isAbilityOverridingMovement) return;

        if (isClimbing)
        {
            HandleLadderPhysics();
            return; // STOP here, do not run normal gravity/movement logic
        }
        // --- Movement ---
        float currentSpeed = isGrounded && Input.GetKey(KeyCode.LeftShift) ? speed * runMultiplier : speed;
        float targetVX = moveInput * currentSpeed;
        
        // Soft Acceleration
        float smoothedVX = Mathf.MoveTowards(rb.linearVelocity.x, targetVX, 20f * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(smoothedVX, rb.linearVelocity.y);

        // --- Animations & Facing ---
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            facing = moveInput > 0 ? 1 : -1;
            animator.SetBool("iswalking", true);
            
            var ls = transform.localScale;
            transform.localScale = new Vector3(facing * Mathf.Abs(ls.x), ls.y, ls.z);
        }
        else
        {
            animator.SetBool("iswalking", false);
        }

        // --- Gravity ---
        ApplyBetterGravity();

        // Fall Clamp
        if (rb.linearVelocity.y < -maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -maxFallSpeed);
    }

    // --- NEW METHOD FOR ABILITIES ---
    // The DashAbility will call this: SetAbilityActive(true) -> Dash -> SetAbilityActive(false)
    public void SetAbilityOverride(bool active)
    {
        isAbilityOverridingMovement = active;
        if (active)
        {
            animator.SetBool("iswalking", false);
        }
    }

    void DoJump()
    {
        animator.SetBool("iswalking", false);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        coyoteCounter = 0f;
        jumpsUsed++;
        OnJump?.Invoke();
    }

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
        float gravity = Physics2D.gravity.y * rb.mass;
        float multiplier = 1f;

        if (vy < -0.01f) multiplier = fallGravityMultiplier;
        else if (vy > 0.01f && !jumpHeld) multiplier = lowJumpGravityMultiplier;
        else if (nearApex) multiplier = apexGravityMultiplier;

        rb.AddForce(Vector2.up * gravity * (multiplier - 1f));
    }

    void HandleLadderPhysics()
    {
        // 1. Horizontal Movement (Optional: allow moving slightly left/right on ladder)
        float currentSpeed = speed * 0.5f; // Move slower horizontally on ladder
        float targetVX = moveInput * currentSpeed;
        
        // 2. Vertical Movement
        float targetVY = 0f;

        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            // Climbing Up or Down intentionally
            targetVY = verticalInput * ladderClimbSpeed;
            animator.SetBool("isClimbing", true); // Ensure you have this param
            animator.SetFloat("climbSpeed", Mathf.Abs(verticalInput));
        }
        else
        {
            // "Get down slower" effect (Passive Slide)
            targetVY = -ladderSlideSpeed; 
            animator.SetBool("isClimbing", true); 
            animator.SetFloat("climbSpeed", 0f); // Paused animation
        }

        // Apply
        rb.linearVelocity = new Vector2(targetVX, targetVY);
    }

    public void SetLadderState(bool active, float speed, float slide)
    {
        isClimbing = active;
        ladderClimbSpeed = speed;
        ladderSlideSpeed = slide;

        if (active)
        {
            rb.gravityScale = 0f; // Disable Unity gravity
        }
        else
        {
            rb.gravityScale = 1f; // Restore gravity (ApplyBetterGravity takes over)
            //animator.SetBool("isClimbing", false);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (col == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(col.bounds.center, new Vector2(col.bounds.size.x - 0.12f, col.bounds.size.y));
    }
}