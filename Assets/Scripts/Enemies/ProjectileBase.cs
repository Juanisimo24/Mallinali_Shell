using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Animator))]
public abstract class ProjectileBase : MonoBehaviour
{
    [Header("Base Settings")]
    public float speed = 10f;
    public float lifeTime = 4f;
    public int impactDamage = 5;

    [Header("Animation Settings")]
    [Tooltip("Duration of the 'Hit' or 'Explode' animation clip.")]
    public float hitAnimationDuration = 0.5f;

    // References for the child classes
    protected Rigidbody2D rb;
    protected Animator anim;
    protected Collider2D col;
    
    // State to prevent double-hits
    protected bool hasHit = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        
        rb.gravityScale = 0;
    }

    public virtual void Launch(Vector2 direction)
    {
        // 1. "Charge/Fly" animation plays automatically as Default State
        
        rb.linearVelocity = direction.normalized * speed;
        
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        // Auto-destroy if it hits nothing after X seconds
        Destroy(gameObject, lifeTime);
    }
    
    // --- NEW HELPER METHOD FOR CHILDREN ---
    // Child classes call this when they hit a valid target
    protected void TriggerHitSequence()
    {
        if (hasHit) return;
        hasHit = true;

        // 1. Stop Physics
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; 

        // 2. Disable Collider (so it doesn't hit again)
        if (col != null) col.enabled = false;

        // 3. Play Animation
        if (anim != null) anim.SetTrigger("Hit");

        // 4. Delayed Destruction
        Destroy(gameObject, hitAnimationDuration);
    }

    protected abstract void OnTriggerEnter2D(Collider2D hit);
}