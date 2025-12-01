using UnityEngine;

public class EnemySwimmerShooter : EnemyRanged
{
    // Inherits: projectilePrefab, firePoint, fireRate, retreatDistance
    // Inherits: Shooting logic

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0; // Essential for water enemies
        rb.linearDamping = 1f; // Water resistance feel
    }

    protected override void Update()
    {
        // We override Update to change MOVEMENT logic for water
        // But we want to keep the SHOOTING logic from the parent.
        
        // 1. Target Check
        if (PlayerManagerDual.Instance != null) target = PlayerManagerDual.Instance.GetActive().transform;
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.position);
        LookAtTarget();

        if (dist < detectionRange)
        {
            // WATER MOVEMENT LOGIC (XY Axis)
            if (dist < retreatDistance)
            {
                // Swim Away (In X and Y)
                Vector2 dir = (transform.position - target.position).normalized;
                rb.linearVelocity = dir * 3f; // Swim speed
            }
            else
            {
                // Stop and Float to Shoot
                rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 2f);
                
                // Reuse the shooting timer logic from EnemyRanged
                // Note: We need to ensure the variables in EnemyRanged are 'protected' not 'private'
                // If they are private, we copy the logic here:
                
                // Copy of Shoot Logic (safest bet if parent variables are private)
                ShootTimerLogic(); 
            }
        }
        else
        {
             // Idle / Drift
             rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime);
        }
    }

    // We create a helper here to access the Shoot method from the parent
    // Ensure the "Shoot()" method in EnemyRanged is 'protected' or 'public'
    void ShootTimerLogic()
    {
        // Simple internal timer since we can't always access private parent timers
        // Or we assume the parent Update isn't running.
        
        // NOTE: For this to work best, change 'private float fireTimer' in EnemyRanged to 'protected'
        // If you can't, use this local logic:
        
        if (IsReadyToFire()) 
        {
            // We need a way to call the parent Shoot(). 
            // Please change 'void Shoot()' in EnemyRanged to 'protected virtual void Shoot()'
            base.Shoot(); 
        }
    }

    private float localFireTimer;
    bool IsReadyToFire()
    {
        if (Time.time > localFireTimer)
        {
            localFireTimer = Time.time + fireRate;
            return true;
        }
        return false;
    }
}