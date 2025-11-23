using UnityEngine;
using Pathfinding;

/// <summary>
/// Grounded 2D companion using A* for navigation,
/// walks on ground and jumps across gaps / up ledges (like a turtle).
/// </summary>
[RequireComponent(typeof(Seeker), typeof(Rigidbody2D))]
public class GroundedAStarFollower2D : MonoBehaviour
{
    [Header("References")]
    public Transform target;
    public LayerMask groundLayer;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float jumpForce = 8f;
    public float groundCheckDistance = 0.1f;
    public float nextWaypointDistance = 0.4f;
    public float pathUpdateRate = 0.5f;
    public float jumpHeightThreshold = 0.6f;   // min vertical gap to trigger jump

    private Path path;
    private Seeker seeker;
    private Rigidbody2D rb;
    private int currentWaypoint = 0;
    private bool isGrounded;
    private float halfHeight;

    void Awake()
    {
        seeker = GetComponent<Seeker>();
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.gravityScale = 3f; // make sure it behaves like a grounded creature

        // estimate collider half height
        Collider2D c = GetComponent<Collider2D>();
        if (c is BoxCollider2D box) halfHeight = box.size.y * 0.5f;
        else if (c is CapsuleCollider2D cap) halfHeight = cap.size.y * 0.5f;
        else if (c is CircleCollider2D cir) halfHeight = cir.radius;
        else halfHeight = 0.5f;
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
        UpdateGrounded();

        if (path == null) return;
        if (currentWaypoint >= path.vectorPath.Count) return;

        Vector2 nextPoint = path.vectorPath[currentWaypoint];
        Vector2 dir = nextPoint - rb.position;

        // horizontal movement only
        float xDir = Mathf.Sign(dir.x);
        rb.linearVelocity = new Vector2(xDir * moveSpeed, rb.linearVelocity.y);

        // face direction (optional)
        if (xDir != 0)
            transform.localScale = new Vector3(Mathf.Sign(xDir), 1, 1);

        // check if we need to jump to reach next node
        float verticalGap = dir.y;
        if (isGrounded && verticalGap > jumpHeightThreshold)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // when close to current waypoint, move to the next
        float distance = dir.magnitude;
        if (distance < nextWaypointDistance)
            currentWaypoint++;
    }

    void UpdateGrounded()
    {
        Vector2 origin = new Vector2(rb.position.x, rb.position.y - halfHeight);
        isGrounded = Physics2D.Raycast(origin, Vector2.down, groundCheckDistance, groundLayer);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * (halfHeight + groundCheckDistance));
        if (path == null) return;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < path.vectorPath.Count - 1; i++)
            Gizmos.DrawLine(path.vectorPath[i], path.vectorPath[i + 1]);
    }
}
