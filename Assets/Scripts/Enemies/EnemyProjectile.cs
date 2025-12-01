using UnityEngine;

public class EnemyProjectile : ProjectileBase
{
    protected override void OnTriggerEnter2D(Collider2D hit)
    {
        // 1. Ignore other enemies
        if (hit.gameObject.layer == LayerMask.NameToLayer("Enemy")) return;

        // 2. Hit Player
        if (hit.CompareTag("Player"))
        {
            var stats = hit.GetComponent<CharacterStats>();
            if (stats != null)
            {
                // Use variable from Base Class
                stats.TakeDamage(impactDamage, transform.position, rb.linearVelocity.normalized);
            }
            Destroy(gameObject);
        }
        // 3. Hit Ground
        else if (hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}