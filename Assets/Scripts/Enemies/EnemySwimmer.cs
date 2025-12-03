using UnityEngine;

public class EnemySwimmer : EnemyPatrol // Inherits from Patrol to reuse Waypoints!
{
    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0; // Fish don't sink
    }

    // We override Update because Patrol uses only X axis, but Fish use X and Y
    protected override void Update()
    {
        // Don't call base.Update() to avoid the ground logic.
        // We rewrite simplified logic for water.
        
        if (PlayerManagerDual.Instance != null) target = PlayerManagerDual.Instance.GetActive().transform;
        if (target == null) return;

        float dist = Vector2.Distance(transform.position, target.position);

        if (dist < detectionRange)
        {
            // Chase (Free movement XY)
            LookAtTarget();
            Vector2 dir = (target.position - transform.position).normalized;
            rb.linearVelocity = dir * chaseSpeed;
        }
        else
        {
            // Patrol (Free movement XY)
            if (waypoints.Length == 0) return;
            Transform wp = waypoints[0]; // Use index logic from base if made protected, or simple logic here
            // (For simplicity, let's just chase if near, idle if not)
            rb.linearVelocity = Vector2.zero;
        }
    }
}