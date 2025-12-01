using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class FishAI : MonoBehaviour
{
    [Header("Comportamiento")]
    public float moveSpeed = 2f;
    public float roamRadius = 3f; // Radio de movimiento desde su punto de inicio
    public float changeDirectionTime = 2f;

    [Header("Valor Nutricional")]
    public int healthRestore = 15;
    public float growthAmount = 0.2f; // Cuánto crece la tortuga

    private Vector2 startPos;
    private Vector2 targetPos;
    private float timer;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        startPos = transform.position;
        PickNewTarget();
    }

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            PickNewTarget();
            timer = changeDirectionTime;
        }
    }

    void FixedUpdate()
    {
        // Moverse suavemente hacia el objetivo
        Vector2 direction = (targetPos - (Vector2)transform.position).normalized;
        rb.linearVelocity = direction * moveSpeed;

        // Girar sprite (Flip) según dirección
        if (Mathf.Abs(direction.x) > 0.1f)
        {
            sr.flipX = direction.x < 0; // Ajusta según tu sprite original
        }
    }

    void PickNewTarget()
    {
        // Elegir un punto aleatorio dentro del radio original
        Vector2 randomDir = Random.insideUnitCircle * roamRadius;
        targetPos = startPos + randomDir;
    }

    // Cuando la tortuga toca al pez (El pez debe ser IsTrigger)
    void OnTriggerEnter2D(Collider2D other)
    {
        // Buscamos el controlador de la tortuga
        var turtle = other.GetComponent<TortugaController>();
        
        if (turtle != null)
        {
            // Ejecutar lógica de comer en la tortuga
            turtle.EatFish(growthAmount, healthRestore);
            
            // Efecto de sonido o partículas aquí
            Destroy(gameObject); // El pez desaparece
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPos : transform.position, roamRadius);
    }
}