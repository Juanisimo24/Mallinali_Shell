using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    //public Animator animator;
    public Transform attackPoint;
    public float attackRange = 0.5f;
    public LayerMask enemyLayers;
    public int attackDamage = 10;
    private SpriteRenderer spriteRenderer;
    public Transform tortugaTransform;
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }
    public void PerformAttack()
    {
        //animator.SetTrigger("Attack");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {


            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector2 hitPoint = attackPoint.position; // posición del golpe
                Vector2 hitNormal = new Vector2(spriteRenderer.flipX ? -1 : 1, 0f); // dirección del impacto
                bool isTargetTaunted = false;
                var enemyThreat = enemy.GetComponent<EnemyThreat>();
                if (enemyThreat != null && enemyThreat.isTaunted && enemyThreat.tauntTarget == tortugaTransform)
                {
                    isTargetTaunted = true;
                }

                int dmg = attackDamage;
                if (isTargetTaunted) dmg = Mathf.RoundToInt(dmg * 1.2f); // 20% bonus opcional

                damageable.TakeDamage(dmg, hitPoint, hitNormal);
            }
        }
    }

    public void PerformComboAttack()
    {
        //animator.SetTrigger("ComboAttack");

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange * 1.5f, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            var damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                Vector2 hitPoint = attackPoint.position; // posición del golpe
                Vector2 hitNormal = new Vector2(spriteRenderer.flipX ? -1 : 1, 0f); // dirección del impacto
                damageable.TakeDamage(attackDamage * 2, hitPoint, hitNormal);
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

