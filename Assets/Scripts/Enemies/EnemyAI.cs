using UnityEngine;
using System.Collections; // Necesario para Corrutinas (Flash)

[RequireComponent(typeof(CharacterStats), typeof(Rigidbody2D), typeof(SpriteRenderer))]
public class EnemyAI : MonoBehaviour
{
    [Header("Ataque y Daño")]
    [Tooltip("Daño que hace al tocar al jugador")]
    public int contactDamage = 10;
    
    [Tooltip("Segundos que debe esperar antes de volver a hacer daño")]
    public float damageCooldown = 1.0f; // <--- AQUÍ CAMBIAS EL TIEMPO ENTRE ATAQUES

[Header("Configuración de Tipo")]
    public bool enableContactDamage = true; // <--- ¡NUEVO! Desactiva esto en el de rango

    [Header("Detección")]
    public float detectionRange = 5f;
    public float attackRange = 1.2f;

    [Header("Feedback Visual (Flash)")]
    public Color damageColor = new Color(1f, 0.3f, 0.3f); // Rojo claro
    public float flashDuration = 0.15f; // Cuánto dura prendido en rojo

    [Header("Referencias")]
    protected Rigidbody2D rb;
    protected Transform target; 
    protected CharacterStats stats;

    protected SpriteRenderer sr; // Referencia para cambiar el color

    // Variables Privadas
    private float nextDamageTime;
    private Color originalColor;
    private Coroutine flashCoroutine;
    protected Animator anim;
    protected Collider2D col;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<CharacterStats>();
        anim = GetComponent<Animator>();
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();

        // Guardamos el color original (generalmente blanco)
        if (sr != null) originalColor = sr.color;
    }

    protected virtual void Start()
    {
        // Nos suscribimos al evento de recibir daño de CharacterStats
        // Así, cuando el script de vida detecte daño, avisará aquí para el flash
        if (stats != null)
        {
            stats.OnDamageTaken += TriggerDamageFlash;
            stats.OnDeath += TriggerDeathAnimation;
        }
    }

    protected virtual void OnDestroy()
    {
        // Buena práctica: Desuscribirse para evitar errores si se destruye el objeto
        if (stats != null)
        {
            stats.OnDamageTaken -= TriggerDamageFlash;
            stats.OnDeath -= OnDeathCleanup;
        }
    }

    protected virtual void Update()
    {
        // Buscar Jugador
        if (PlayerManagerDual.Instance != null)
        {
            var activeObj = PlayerManagerDual.Instance.GetActive();
            if (activeObj != null) target = activeObj.transform;
        }
        float currentSpeed = Mathf.Abs(rb.linearVelocity.x);
        anim.SetFloat("speed", currentSpeed);
    }

    protected void LookAtTarget()
    {
        if (target == null) return;
        float dir = target.position.x - transform.position.x;
        if (Mathf.Abs(dir) > 0.1f)
        {
            float scaleX = Mathf.Abs(transform.localScale.x) * (dir > 0 ? 1 : -1);
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
        }
    }

    // --- LÓGICA MODIFICADA ---
    protected void OnCollisionStay2D(Collision2D collision)
    {
        // SI EL DAÑO DE CONTACTO ESTÁ APAGADO, NO HACEMOS NADA
        if (!enableContactDamage) return; 

        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= nextDamageTime)
            {
                var damageable = collision.gameObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    Vector2 knockbackDir = (collision.transform.position - transform.position).normalized;
                    damageable.TakeDamage(contactDamage, transform.position, knockbackDir);
                    nextDamageTime = Time.time + damageCooldown; 
                }
            }
        }
    }

    // --- LÓGICA DEL FLASH ROJO ---
    void TriggerDamageFlash()
    {
        if (sr == null) return;

        // Si ya está parpadeando, reiniciamos el parpadeo
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        
        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    IEnumerator FlashRoutine()
    {
        // Cambiar a Rojo
        sr.color = damageColor;

        // Esperar
        yield return new WaitForSeconds(flashDuration);

        // Volver al color original
        sr.color = originalColor;
    }

    void OnDeathCleanup()
    {
        // Lógica extra si quieres que pase algo específico en el AI al morir
        // (Aunque CharacterStats ya destruye el objeto, esto es por seguridad)
        StopAllCoroutines();
    }
    protected void TriggerDeathAnimation()
    {
        // 1. Evitar que siga moviéndose o atacando
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic; // Congela física
        if (col != null) col.enabled = false;    // Desactiva colisiones
        this.enabled = false;                    // Desactiva este script (IA)

        // 2. Activar Animación
        anim.SetTrigger("Die");

        // 3. Destruir el objeto después de que termine la animación (aprox 1 seg)
        // Nota: CharacterStats intenta destruir el objeto también. 
        // Para que esto funcione bien, ve al paso 3 de abajo.
        Destroy(gameObject, 1.5f); 
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}