using UnityEngine;
using Pathfinding;

/// <summary>
/// Grounded A* follower for 2D platformers.
/// Walks using physics and jumps intelligently between platforms.
/// </summary>
[RequireComponent(typeof(Seeker), typeof(Rigidbody2D))]
public class CompanionAStar2D : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public LayerMask groundLayer;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float nextWaypointDistance = 0.4f;
    public float pathUpdateRate = 0.5f;
    public float turnSmooth = 10f;
    public float jumpForce = 8f;
    public float gravity = 1.5f;

    [Header("Platform logic")]
    public float groundCheckDistance = 0.15f;
    public float jumpHeightThreshold = 0.6f;
    public float maxJumpHeight = 4f;
    public float reGroundRay = 2f;
    public float maxFallSpeed = 12f;
    public float recheckDelay = 0.25f;

    private Path path;
    private Seeker seeker;
    private Rigidbody2D rb;
    private Collider2D col;
    private int currentWaypoint;
    private bool isGrounded;
    private bool jumpRequested;
    private float halfHeight;
    private float lastJumpTime;
    private float lastPathTime;

    void Awake()
    {
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.freezeRotation = true;
        

        // Cache collider height
        if (col is BoxCollider2D box) halfHeight = box.size.y * 0.5f;
        else if (col is CapsuleCollider2D cap) halfHeight = cap.size.y * 0.5f;
        else if (col is CircleCollider2D cir) halfHeight = cir.radius;
        else halfHeight = 0.5f;
    }
    void Start()
    {
        rb.gravityScale = gravity;
    }
    void OnEnable()
    {
        if (target != null)
            InvokeRepeating(nameof(UpdatePath), 0f, pathUpdateRate);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(UpdatePath));
    }

    void UpdatePath()
    {
        if (target == null) return;
        if (seeker.IsDone())
            seeker.StartPath(rb.position, target.position, OnPathComplete);
    }

    void OnPathComplete(Path p)
    {
        if (!p.error)
        {
            path = p;
            currentWaypoint = 0;
        }
    }

    void FixedUpdate()
    {
        if (target == null) return;

        UpdateGrounded();

        if (path == null || currentWaypoint >= path.vectorPath.Count)
            return;

        // Get direction toward next node
        Vector2 nextPoint = path.vectorPath[currentWaypoint];
        Vector2 dir = nextPoint - rb.position;

        // Horizontal only when grounded or jumping toward next node
        float xDir = Mathf.Sign(dir.x);
        float newVX = xDir * moveSpeed;
        rb.linearVelocity = new Vector2(Mathf.Lerp(rb.linearVelocity.x, newVX, Time.fixedDeltaTime * turnSmooth),
                                  Mathf.Clamp(rb.linearVelocity.y, -maxFallSpeed, 20f));

        // Face direction (keep base scale)
        if (xDir != 0)
        {
            Vector3 s = transform.localScale;
            transform.localScale = new Vector3(Mathf.Abs(s.x) * Mathf.Sign(xDir), s.y, s.z);
        }

        // Jump decision
        if (isGrounded && Time.time - lastJumpTime > 0.15f)
        {
            HandlePlatformJump(dir);
        }

        // Advance waypoint
        float distance = dir.magnitude;
        if (distance < nextWaypointDistance)
            currentWaypoint++;

        // Auto re-ground check if stuck falling
        if (!isGrounded && rb.linearVelocity.y < -1f)
        {
            RaycastHit2D hit = Physics2D.Raycast(rb.position, Vector2.down, reGroundRay, groundLayer);
            if (hit && Mathf.Abs(hit.point.y - rb.position.y) < 0.5f)
            {
                Vector2 corrected = rb.position;
                corrected.y = hit.point.y + halfHeight + 0.02f;
                rb.position = corrected;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    // ----------------------------------------------------------
    void UpdateGrounded()
    {
        Vector2 origin = new Vector2(rb.position.x, rb.position.y - halfHeight);
        isGrounded = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
    }

    void HandlePlatformJump(Vector2 dir)
    {
        float verticalGap = dir.y;

        // Jump if next waypoint is clearly above
        if (verticalGap > jumpHeightThreshold && verticalGap < maxJumpHeight)
        {
            RaycastHit2D ceiling = Physics2D.Raycast(rb.position, Vector2.up, verticalGap, groundLayer);
            if (!ceiling)
                DoJump();
            return;
        }

        // Jump small gaps (no ground ahead)
        Vector2 forwardOrigin = new Vector2(rb.position.x + Mathf.Sign(dir.x) * 0.5f, rb.position.y - halfHeight);
        RaycastHit2D groundAhead = Physics2D.Raycast(forwardOrigin, Vector2.down, 1.2f, groundLayer);
        if (!groundAhead)
            DoJump();
    }

    void DoJump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        lastJumpTime = Time.time;
        isGrounded = false;
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (halfHeight + groundCheckDistance));
        if (path == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < path.vectorPath.Count - 1; i++)
            Gizmos.DrawLine(path.vectorPath[i], path.vectorPath[i + 1]);
    }
}
