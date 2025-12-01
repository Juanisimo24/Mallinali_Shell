using UnityEngine;
using System.Collections;

public class EnemyPatrol : EnemyAI
{
    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float patrolSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Ataque Melee")]
    // Tiempo que tarda la animación en "golpear" desde que inicia
    // (Ajústalo para que coincida con el momento en que la espada baja)
    public float hitDelay = 0.5f; 

    private int currentWaypointIndex = 0;
    private bool isChasing = false;
    private bool isAttacking = false; // Para no atacar dos veces seguidas

    protected override void Update()
    {
        base.Update();
        if (target == null) return;

        // Si ya estamos atacando, no nos movemos ni calculamos nada
        if (isAttacking) 
        {
            rb.linearVelocity = Vector2.zero;
            return; 
        }

        float distToPlayer = Vector2.Distance(transform.position, target.position);

        // --- MÁQUINA DE ESTADOS ---
        if (distToPlayer < detectionRange)
        {
            isChasing = true;
        }
        else if (distToPlayer > detectionRange * 1.5f)
        {
            isChasing = false;
        }

        if (isChasing)
        {
            Chase(distToPlayer);
        }
        else
        {
            Patrol();
        }
    }

    void Chase(float distance)
    {
        LookAtTarget();

        // Lógica corregida: 
        // Si estamos lejos -> Corremos.
        // Si estamos cerca -> Atacamos (SIN necesidad de tocar coliders)
        
        if (distance > attackRange)
        {
            Vector2 dir = (target.position - transform.position).normalized;
            rb.linearVelocity = new Vector2(dir.x * chaseSpeed, rb.linearVelocity.y);
            
            // Aseguramos que la animación de caminar esté activa
            // (EnemyAI ya maneja el parámetro "speed" basado en velocity)
        }
        else
        {
            // FRENAR Y ATACAR
            rb.linearVelocity = Vector2.zero; 
            StartCoroutine(AttackRoutine());
        }
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        // 1. Iniciar Animación
        anim.SetTrigger("Attack");

        // 2. Esperar el momento del impacto (Windup)
        // Esto es clave: esperamos a que la espada baje visualmente
        yield return new WaitForSeconds(hitDelay);

        // 3. Verificar si el jugador sigue cerca para recibir daño
        if (target != null)
        {
            float dist = Vector2.Distance(transform.position, target.position);
            
            // Damos un poco de margen extra (attackRange * 1.1) para que no sea frustrante esquivar
            if (dist <= attackRange * 1.1f)
            {
                var damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    // Calculamos dirección de empuje
                    Vector2 knockbackDir = (target.position - transform.position).normalized;
                    damageable.TakeDamage(contactDamage, target.position, knockbackDir);
                    Debug.Log("¡Guardia acertó el espadazo!");
                }
            }
        }

        // 4. Esperar el resto del cooldown (Recuperación)
        // Restamos el hitDelay para que el damageCooldown sea el tiempo TOTAL del ciclo
        float remainingWait = Mathf.Max(0f, damageCooldown - hitDelay);
        yield return new WaitForSeconds(remainingWait);

        isAttacking = false;
    }

    void Patrol()
    {
        if (waypoints.Length == 0) return;

        Transform wp = waypoints[currentWaypointIndex];
        Vector2 dir = (wp.position - transform.position).normalized;
        
        rb.linearVelocity = new Vector2(dir.x * patrolSpeed, rb.linearVelocity.y);
        
        // Mirar al waypoint
        float facingDir = wp.position.x - transform.position.x;
        if (Mathf.Abs(facingDir) > 0.1f)
        {
             float scaleX = Mathf.Abs(transform.localScale.x) * (facingDir > 0 ? 1 : -1);
             transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
        }

        if (Vector2.Distance(transform.position, wp.position) < 0.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }
}