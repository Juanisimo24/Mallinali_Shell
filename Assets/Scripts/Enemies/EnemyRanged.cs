using UnityEngine;

public class EnemyRanged : EnemyAI
{
    [Header("Ranged Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireRate = 2f;
    public float retreatDistance = 3f; // Run away if too close

    private float fireTimer;

    protected override void Update()
    {
        base.Update();
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.position);
        LookAtTarget();

        if (dist < detectionRange)
        {
            if (dist < retreatDistance)
            {
                // Run Away
                Vector2 dir = (transform.position - target.position).normalized;
                rb.linearVelocity = new Vector2(dir.x * 3f, rb.linearVelocity.y);
            }
            else
            {
                // Stand still and shoot
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                
                if (Time.time > fireTimer)
                {
                    Shoot();
                    fireTimer = Time.time + fireRate;
                }
            }
        }
    }

    public void Shoot()
    {
        if (anim != null) anim.SetTrigger("Attack");

        if (projectilePrefab && firePoint)
        {
            GameObject p = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Vector2 dir = (target.position - firePoint.position).normalized;
            
            // --- THE FIX ---
            // We look for the Parent Component. 
            // It doesn't matter if the prefab has PoisonProjectile or EnemyProjectile attached,
            // both are children of ProjectileBase.
            var projScript = p.GetComponent<ProjectileBase>();
            
            if (projScript != null)
            {
                projScript.Launch(dir);
            }
            else
            {
                Debug.LogError("The projectile prefab does not have a script inheriting from ProjectileBase!");
            }
        }
    }
}