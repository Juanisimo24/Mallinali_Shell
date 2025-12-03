using UnityEngine;

[RequireComponent(typeof(Animator))] 
public class PoisonProjectile : ProjectileBase
{
    [Header("Poison Specifics")]
    public int poisonDamage = 5;      
    public int poisonTicks = 4;       
    public float poisonInterval = 1f; 
    public float explosionDuration = 0.5f;


    protected override void Awake()
    {
        base.Awake(); // Run the base Awake (Get Rigidbody)
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
    }

 protected override void OnTriggerEnter2D(Collider2D hit)
    {
        // Check if we already hit something to be safe
        if (hasHit) return;

        // Ignore other enemies
        if (hit.gameObject.layer == LayerMask.NameToLayer("Enemy")) return;

        bool validHit = false;

        // 1. Logic for Player
        if (hit.CompareTag("Player"))
        {
            var stats = hit.GetComponent<CharacterStats>();
            if (stats != null)
            {
                stats.TakeDamage(impactDamage, transform.position, rb.linearVelocity.normalized);
            }
            validHit = true;
        }
        // 2. Logic for Ground
        else if (hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            validHit = true;
        }

        // 3. If we hit something valid, run the sequence
        if (validHit)
        {
            TriggerHitSequence(); // <--- REPLACES Destroy(gameObject)
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