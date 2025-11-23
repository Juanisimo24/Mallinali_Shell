using UnityEngine;

/// <summary>
/// Reliable 2D follower that walks and jumps to keep up with the player.
/// Works across tile-based platforms and vertical jumps.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FollowerGround2D : MonoBehaviour
{
    [Header("References")]
    public Transform leader;
    public Rigidbody2D leaderRb;
    public LayerMask groundLayer;

    [Header("Follow Settings")]
    public float followDistance = 2.0f;
    public float stopBuffer = 0.3f;
    public float maxSpeed = 6f;
    public float accel = 20f;
    public float decel = 25f;

    [Header("Jump Settings")]
    public float jumpForce = 7.5f;
    public float groundCheckDistance = 0.1f;
    public float frontRayDistance = 0.6f;
    public float upRayDistance = 2f;
    public float gapJumpCooldown = 0.35f;

    [Header("Leash Settings")]
    public float leashDistance = 8f;
    public float warpYOffset = 1.0f;

    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer leaderSR;
    private Rigidbody2D cachedLeaderRb;

    private bool myGrounded;
    private bool isWarping = false;
    private int facingDir = 1;
    private float halfHeight;
    private float lastJumpTime = -10f;

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

        if (col is BoxCollider2D box)
            halfHeight = box.size.y * 0.5f;
        else if (col is CapsuleCollider2D cap)
            halfHeight = cap.size.y * 0.5f;
        else if (col is CircleCollider2D cir)
            halfHeight = cir.radius;
        else
            halfHeight = 0.5f;
    }

    void FixedUpdate()
    {
        if (leader == null || isWarping) return;

        UpdateFacing();
        UpdateGrounded();

        float targetX = leader.position.x - facingDir * followDistance;
        float deltaX = targetX - rb.position.x;
        float absDeltaX = Mathf.Abs(deltaX);

        // Warp safety
        if (absDeltaX > leashDistance)
        {
            SoftWarpBehindLeader();
            return;
        }

        // Horizontal move
        float desiredVX = 0f;
        if (absDeltaX > stopBuffer)
        {
            float pOut = deltaX * 5f;
            desiredVX = Mathf.Clamp(pOut, -maxSpeed, maxSpeed);
        }

        float currentVX = rb.linearVelocity.x;
        float rate = (Mathf.Abs(desiredVX) > Mathf.Abs(currentVX)) ? accel : decel;
        float newVX = Mathf.MoveTowards(currentVX, desiredVX, rate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVX, rb.linearVelocity.y);

        // Handle jump logic only when grounded
        if (myGrounded)
            HandleSmartJump();
    }

    // ------------------ Grounding & Facing ------------------

    void UpdateGrounded()
    {
        Vector2 origin = new Vector2(rb.position.x, rb.position.y - halfHeight);
        myGrounded = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
    }

    void UpdateFacing()
    {
        if (leaderSR != null)
            facingDir = leaderSR.flipX ? -1 : 1;
        else if (cachedLeaderRb != null)
            facingDir = cachedLeaderRb.linearVelocity.x >= 0f ? 1 : -1;
    }

    // ------------------ Jump Decision ------------------

    void HandleSmartJump()
    {
        if (Time.time - lastJumpTime < gapJumpCooldown) return;

        Vector2 frontOrigin = new Vector2(rb.position.x + facingDir * (frontRayDistance * 0.5f), rb.position.y);
        Vector2 downOrigin = new Vector2(rb.position.x + facingDir * frontRayDistance, rb.position.y - halfHeight);

        bool groundAhead = Physics2D.Raycast(downOrigin, Vector2.down, 0.3f, groundLayer);

        // Condition 1: Gap ahead while moving forward
        if (!groundAhead && Mathf.Abs(cachedLeaderRb.linearVelocity.x) > 0.1f)
        {
            DoJump();
            return;
        }

        // Condition 2: Player is clearly above (platform climb)
        if (leader.position.y - rb.position.y > 0.6f)
        {
            // Check if there is platform above front side
            Vector2 upOrigin = new Vector2(rb.position.x + facingDir * frontRayDistance, rb.position.y);
            RaycastHit2D upHit = Physics2D.Raycast(upOrigin, Vector2.up, upRayDistance, groundLayer);

            if (!upHit)
            {
                DoJump();
                return;
            }
        }
    }

    void DoJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        myGrounded = false;
        lastJumpTime = Time.time;
    }

    // ------------------ Warp Recovery ------------------

    void SoftWarpBehindLeader()
    {
        isWarping = true;

        Vector2 targetPos = new Vector2(
            leader.position.x - facingDir * followDistance,
            leader.position.y + warpYOffset
        );

        RaycastHit2D hit = Physics2D.Raycast(targetPos, Vector2.down, 4f, groundLayer);
        if (hit)
            targetPos.y = hit.point.y + halfHeight;

        rb.position = targetPos;
        rb.linearVelocity = Vector2.zero;

        isWarping = false;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = myGrounded ? Color.green : Color.red;
        Vector2 origin = rb.position;
        Gizmos.DrawLine(origin, origin + Vector2.down * (halfHeight + groundCheckDistance));

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + Vector2.right * facingDir * frontRayDistance);
    }
}
