using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class HomingMinion : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 4f;
    public int damage = 10;
    public float lifeTime = 10f; // They die automatically after 10s if they don't hit anything

    private Transform target;
    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // It's underwater
    }

    void Start()
    {
        // Auto-target the active player
        if (PlayerManagerDual.Instance != null)
        {
            target = PlayerManagerDual.Instance.GetActive().transform;
        }
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        // Update target in case player switches character
        if (PlayerManagerDual.Instance != null)
        {
            target = PlayerManagerDual.Instance.GetActive().transform;
        }

        if (target != null)
        {
            // Rotate towards player
            Vector2 direction = (target.position - transform.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rb.rotation = angle;

            // Move forwards
            rb.linearVelocity = direction * speed;
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. Hit Player
        if (collision.gameObject.CompareTag("Player"))
        {
            var stats = collision.gameObject.GetComponent<IDamageable>();
            if (stats != null)
            {
                stats.TakeDamage(damage, transform.position, rb.linearVelocity.normalized);
            }
            Die();
        }
        // 2. Hit Wall (Ground layer)
        else if (collision.gameObject.layer == LayerMask.NameToLayer("Floor")|| collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            Die();
        }
        // Ignore Boss and other Minions (assuming they are on Enemy layer)
    }

    void Die()
    {
        // Optional: Instantiate(explosionEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}