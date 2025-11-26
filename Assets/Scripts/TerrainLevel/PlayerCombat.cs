using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    // public Animator animator;
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public float comboRangeMultiplier = 2.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 10;
    
    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void PerformAttack()
    {
        // animator.SetTrigger("Attack");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        
        bool landedHit = false; // Flag to check if we hit at least one enemy

        foreach (Collider2D enemy in hitEnemies)
        {
            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                landedHit = true; 

                // Standard damage logic
                Vector2 hitPoint = attackPoint.position;
                Vector2 hitNormal = new Vector2(spriteRenderer.flipX ? -1 : 1, 0f);
                damageable.TakeDamage(attackDamage, hitPoint, hitNormal);
            }
        }

        // --- REPORT TO MANAGER ---
        // If this is the ACTIVE player and they hit something
        if (landedHit)
        {
            // Only the Active player generates combo points
            // We check against the singleton instance
            if (PlayerManagerDual.Instance != null)
            {
                // Verify if THIS gameObject is the active one before counting the combo
                // (Prevents the AI from generating combos for itself randomly)
                /* 
                   NOTE: If you can't access 'active' publicly, 
                   you can just call RegisterHit and let Manager decide logic, 
                   but checking here is cleaner.
                */
                 PlayerManagerDual.Instance.RegisterHit();
            }
        }
    }

    // This function is called by the Manager when it's YOUR turn to be the companion attacker
    public void PerformComboAttack()
    {
        Debug.Log(gameObject.name + " performs Combo Attack!");
        // animator.SetTrigger("ComboAttack");

        // Use a larger range for the Assist attack
        float comboRange = attackRange * comboRangeMultiplier; 

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, comboRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector2 hitNormal = new Vector2(spriteRenderer.flipX ? -1 : 1, 0f);
                // Double damage for the combo assist
                damageable.TakeDamage(attackDamage * 2, attackPoint.position, hitNormal);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}