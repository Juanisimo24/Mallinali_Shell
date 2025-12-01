using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public abstract class ProjectileBase : MonoBehaviour
{
    [Header("Base Settings")]
    public float speed = 10f;
    public float lifeTime = 4f;
    public int impactDamage = 5; // Basic damage every projectile does

    protected Rigidbody2D rb;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Standard for top-down/RPG projectiles
    }

    // This method is now shared by everyone
    public virtual void Launch(Vector2 direction)
    {
        rb.linearVelocity = direction.normalized * speed;
        
        // Rotate logic
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
    }
    
    // We leave collision logic abstract or virtual so children define it
    protected abstract void OnTriggerEnter2D(Collider2D hit);
}