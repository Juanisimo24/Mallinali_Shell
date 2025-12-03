using UnityEngine;

public class EnemyProjectile : ProjectileBase
{
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
}

