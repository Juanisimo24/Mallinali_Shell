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

    void Shoot()
    {
        anim.SetTrigger("Attack");
        if (projectilePrefab && firePoint)
        {
            GameObject p = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Vector2 dir = (target.position - firePoint.position).normalized;
            p.GetComponent<EnemyProjectile>().Launch(dir);
        }
    }
}