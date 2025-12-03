using UnityEngine;
using System.Collections;

public class EnemySpeedster : EnemyAI
{
    [Header("Patrol Settings")]
    public Transform[] waypoints;
    public float patrolSpeed = 3f;
    private int currentWaypointIndex = 0;

    [Header("Dash Attack Settings")]
    public float dashForce = 20f;
    public float prepareTime = 0.8f; // Tiempo de la animación de carga
    public float dashDuration = 0.4f; // Cuánto dura el impulso
    public float cooldownTime = 2.0f; // Descanso tras el dash
    public int dashDamage = 25;       // Daño extra si te pega en dash

    // Estados internos
    private bool isAttacking = false;
    private bool isDashing = false;   // Para saber si está en movimiento letal

    protected override void Update()
    {
        base.Update(); // Actualiza target y animador de "speed"

        // Si el enemigo muere, el script base lo desactiva, así que no necesitamos chequear muerte aquí.
        if (target == null) return;

        // Si está atacando, no se mueve por patrulla ni busca
        if (isAttacking) return;

        float dist = Vector2.Distance(transform.position, target.position);

        // --- LÓGICA DE DECISIÓN ---
        if (dist < detectionRange)
        {
            // Iniciar secuencia de ataque
            StartCoroutine(DashAttackRoutine());
        }
        else
        {
            // Patrullar tranquilamente
            Patrol();
        }
    }

    // --- CORRUTINA DE ATAQUE (2 ANIMACIONES) ---
    IEnumerator DashAttackRoutine()
    {
        isAttacking = true;
        rb.linearVelocity = Vector2.zero; // Frenar en seco
        LookAtTarget(); // Mirar al jugador antes de cargar

        // FASE 1: PREPARAR (Cargar)
        anim.SetTrigger("Prepare"); // <--- ANIMACIÓN 1
        yield return new WaitForSeconds(prepareTime);

        // FASE 2: DASH (Ejecutar)
        isDashing = true; // Activamos modo daño alto
        anim.SetTrigger("Dash");    // <--- ANIMACIÓN 2
        
        // Calcular dirección actual hacia el jugador
        Vector2 dir = (target.position - transform.position).normalized;
        dir.y = 0; // Mantenerlo horizontal
        
        // Aplicar fuerza explosiva
        rb.AddForce(dir * dashForce, ForceMode2D.Impulse);

        // Esperar lo que dura el dash
        yield return new WaitForSeconds(dashDuration);

        // FASE 3: FRENADO Y COOLDOWN
        rb.linearVelocity = Vector2.zero; // Frenar el deslizamiento
        isDashing = false; // Ya no hace daño extra

        // Volver a Idle/Walk visualmente (Unity pasará a Idle por el parámetro speed=0)
        yield return new WaitForSeconds(cooldownTime);
        
        isAttacking = false; // Volver a patrullar
    }

    // --- LÓGICA DE PATRULLA (Idéntica al Guard) ---
    void Patrol()
    {
        if (waypoints.Length == 0) return;

        Transform wp = waypoints[currentWaypointIndex];
        Vector2 dir = (wp.position - transform.position).normalized;

        // Moverse
        rb.linearVelocity = new Vector2(dir.x * patrolSpeed, rb.linearVelocity.y);

        // Mirar
        float facingDir = wp.position.x - transform.position.x;
        if (Mathf.Abs(facingDir) > 0.1f)
        {
            float scaleX = Mathf.Abs(transform.localScale.x) * (facingDir > 0 ? 1 : -1);
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
        }

        // Cambiar waypoint
        if (Vector2.Distance(transform.position, wp.position) < 0.5f)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
        }
    }

    // --- OVERRIDE DE DAÑO ---
    // Queremos que el Dash haga MÁS daño que el contacto normal
    protected void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Si estamos en medio del dash, usamos dashDamage
                // Si solo estamos caminando, usamos contactDamage (del script padre)
                int dmg = isDashing ? dashDamage : contactDamage;
                
                Vector2 knockback = (collision.transform.position - transform.position).normalized;
                damageable.TakeDamage(dmg, transform.position, knockback);
            }
        }
    }
}