using UnityEngine;
using System.Collections;

public class EnemySwimmerDasher : EnemyAI
{
    [Header("Patrol Settings")]
    public Transform[] waypoints; // Arrastra objetos vacíos aquí
    public float patrolSpeed = 2f;
    private int currentWaypointIndex = 0;

    [Header("Water Dash Settings")]
    public float dashForce = 20f;
    public float prepareTime = 0.8f;
    public float dashDuration = 0.4f;
    public float cooldownTime = 2.0f;
    
    [Header("Daño")]
    public int dashDamage = 25;

    // Estados
    private bool isAttacking = false;
    private bool isDashing = false;

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0;       
        rb.linearDamping = 2f;     
        rb.freezeRotation = true;  
    }

    protected override void Update()
    {
        // 1. Seguridad del Target
        if (PlayerManagerDual.Instance != null)
        {
            GameObject activeChar = PlayerManagerDual.Instance.GetActive();
            if (activeChar != null) target = activeChar.transform;
        }

        if (target == null) return;

        // Si ataca, ignoramos movimiento normal
        if (isAttacking) return;

        float dist = Vector2.Distance(transform.position, target.position);

        // --- LÓGICA DE DECISIÓN ---
        if (dist < detectionRange)
        {
            // Jugador cerca -> Atacar
            StartCoroutine(WaterDashRoutine());
        }
        else
        {
            // Jugador lejos -> Patrullar
            Patrol();
        }
    }

    // --- NUEVO MÉTODO DE PATRULLA ACUÁTICA ---
    void Patrol()
    {
        if (waypoints.Length == 0) return; // Si no hay puntos, se queda quieto

        Transform wp = waypoints[currentWaypointIndex];
        
        // 1. Calcular dirección hacia el waypoint (X e Y)
        Vector2 dir = (wp.position - transform.position).normalized;

        // 2. Moverse
        // Usamos Lerp para que el giro sea suave si cambiamos de dirección
        Vector2 targetVelocity = dir * patrolSpeed;
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, Time.deltaTime * 5f);

        // 3. Mirar hacia el waypoint (Flip X)
        if (Mathf.Abs(dir.x) > 0.1f)
        {
            float scaleX = Mathf.Abs(transform.localScale.x) * (dir.x > 0 ? 1 : -1);
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
        }

        // 4. Verificar si llegamos
        if (Vector2.Distance(transform.position, wp.position) < 0.5f)
        {
            // Pasar al siguiente punto (ciclíco)
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    IEnumerator WaterDashRoutine()
    {
        isAttacking = true;
        
        rb.linearVelocity = Vector2.zero; 
        LookAtTarget(); 

        // PREPARAR
        if(anim) anim.SetTrigger("Prepare");
        yield return new WaitForSeconds(prepareTime);

        // DASH
        isDashing = true;
        if(anim) anim.SetTrigger("Dash");

        Vector2 dir = (target.position - transform.position).normalized;
        rb.AddForce(dir * dashForce, ForceMode2D.Impulse);

        yield return new WaitForSeconds(dashDuration);

        // FRENAR
        rb.linearVelocity = rb.linearVelocity * 0.1f; 
        isDashing = false;

        yield return new WaitForSeconds(cooldownTime);
        
        isAttacking = false; // Volverá a patrullar o atacar según distancia
    }

    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                int dmg = isDashing ? dashDamage : contactDamage;
                Vector2 knockback = (collision.transform.position - transform.position).normalized;
                damageable.TakeDamage(dmg, transform.position, knockback);
            }
        }
    }
}