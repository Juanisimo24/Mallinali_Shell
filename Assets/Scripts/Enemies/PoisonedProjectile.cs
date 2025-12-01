using UnityEngine;

[RequireComponent(typeof(Animator))] 
public class PoisonProjectile : ProjectileBase
{
    [Header("Poison Specifics")]
    public int poisonDamage = 5;      
    public int poisonTicks = 4;       
    public float poisonInterval = 1f; 
    public float explosionDuration = 0.5f;

    private Animator anim;
    private Collider2D col;
    private bool hasHit = false;

    protected override void Awake()
    {
        base.Awake(); // Run the base Awake (Get Rigidbody)
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

    protected override void OnTriggerEnter2D(Collider2D hit)
    {
        if (hit.gameObject.layer == LayerMask.NameToLayer("Enemy") || hasHit) return;

        if (hit.CompareTag("Player"))
        {
            var stats = hit.GetComponent<CharacterStats>();
            if (stats != null)
            {
                // Base damage
                stats.TakeDamage(impactDamage, transform.position, rb.linearVelocity.normalized);
                // Poison effect
                stats.ApplyPoison(poisonDamage, poisonTicks, poisonInterval);
            }
            TriggerExplosion();
        }
        else if (hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            TriggerExplosion();
        }
    }

    void TriggerExplosion()
    {
        hasHit = true;
        rb.linearVelocity = Vector2.zero; 
        rb.bodyType = RigidbodyType2D.Kinematic; 
        if (col != null) col.enabled = false;
        
        if (anim != null) anim.SetTrigger("Explode");

        Destroy(gameObject, explosionDuration);
    }
}