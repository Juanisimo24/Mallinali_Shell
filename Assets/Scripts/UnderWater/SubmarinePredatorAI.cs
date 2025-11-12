using UnityEngine;

/// <summary>
/// Enemigo submarino estilo HungryShark:
/// - Persigue a la tortuga cuando entra en rango/cono de visión.
/// - Daño por contacto con cooldown (mordida).
/// - Implementa IDamageable (vida y muerte).
/// - Al morir, otorga crecimiento a la tortuga.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class SubmarinePredatorAI : MonoBehaviour, IDamageable
{
    [Header("Detección")]
    public float sightRange = 12f;
    [Range(0, 180)] public float sightHalfAngle = 75f; // mitad del cono
    public LayerMask losBlockMask; // Capas que bloquean la visión (Rocas, Mapa)
    public Transform eyes;         // Punto desde donde raycastear (si es null, usa transform)

    [Header("Movimiento submarino")]
    public float maxSpeed = 6f;
    public float acceleration = 16f;
    public float waterDrag = 3f;     // arrastre para simular agua
    public float turnLerp = 10f;     // qué tan rápido gira/suaviza dirección

    [Header("Ataque por contacto")]
    public int contactDamage = 8;
    public float biteCooldown = 1.2f;
    public string turtleTag = "Turtle"; // Asegúrate de asignar este tag a la tortuga

    [Header("Vida")]
    public int maxHealth = 25;
    //public GameObject deathVFX;
    //public AudioClip deathSFX;

    [Header("Recompensa")]
    public float growthOnDeath = 1f; // cuántas "unidades" de crecimiento otorga (cada unidad llama GainGrowth una vez)

    private int _currentHealth;
    private Rigidbody2D _rb;
    private Transform _turtle;
    private float _lastBiteTime;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.linearDamping = waterDrag;

        _currentHealth = maxHealth;
    }

    private void Start()
    {
        var turtleObj = GameObject.FindGameObjectWithTag(turtleTag);
        if (turtleObj != null) _turtle = turtleObj.transform;
    }

    private void FixedUpdate()
    {
        if (_turtle == null) return;

        if (CanSeeTurtle())
        {
            SeekTurtle();
        }
        else
        {
            // Patrulla simple opcional: desacelerar
            _rb.linearVelocity = Vector2.Lerp(_rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 1.5f);
        }

        // Limitar velocidad
        if (_rb.linearVelocity.sqrMagnitude > maxSpeed * maxSpeed)
        {
            _rb.linearVelocity = _rb.linearVelocity.normalized * maxSpeed;
        }
    }

    private bool CanSeeTurtle()
    {
        Vector3 eyePos = eyes ? eyes.position : transform.position;
        Vector2 toTurtle = (Vector2)(_turtle.position - eyePos);
        float dist = toTurtle.magnitude;
        if (dist > sightRange) return false;

        // Cono de visión
        Vector2 forward = transform.right; // define "frente" del pez
        float angle = Vector2.Angle(forward, toTurtle.normalized);
        if (angle > sightHalfAngle) return false;

        // Línea de visión
        RaycastHit2D hit = Physics2D.Raycast(eyePos, toTurtle.normalized, dist, losBlockMask);
        if (hit.collider != null) return false;

        return true;
    }

    private void SeekTurtle()
    {
        Vector2 toTurtle = ((Vector2)_turtle.position - _rb.position).normalized;

        // Suaviza la dirección de frente (rotación del pez), para que "mire" hacia donde va
        Vector2 desiredForward = Vector2.Lerp(transform.right, toTurtle, Time.fixedDeltaTime * turnLerp).normalized;
        transform.right = new Vector3(desiredForward.x, desiredForward.y, 0f);

        // Acelera hacia la tortuga (steering básico)
        Vector2 desiredVelocity = desiredForward * maxSpeed;
        Vector2 steering = (desiredVelocity - _rb.linearVelocity);
        _rb.AddForce(steering.normalized * acceleration, ForceMode2D.Force);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Mordida por contacto con cooldown, SOLO a la tortuga
        if (other.CompareTag(turtleTag) && Time.time >= _lastBiteTime + biteCooldown)
        {
            _lastBiteTime = Time.time;

            var dmgable = other.GetComponent<IDamageable>();
            if (dmgable != null)
            {
                Vector2 hitPoint = other.bounds.ClosestPoint(transform.position);
                Vector2 hitNormal = (other.transform.position - transform.position).normalized;
                dmgable.TakeDamage(contactDamage, hitPoint, hitNormal);
            }
        }
    }

    // ---- IDamageable ----
    public void TakeDamage(int damage, Vector2 hitPoint, Vector2 hitNormal)
    {
        _currentHealth -= damage;
        if (_currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // (Opcional) reacción/retroceso leve bajo el agua
            _rb.AddForce(-hitNormal * Mathf.Clamp(damage, 2, 10), ForceMode2D.Impulse);
        }
    }

    private void Die()
    {
        //if (deathVFX) Instantiate(deathVFX, transform.position, Quaternion.identity);
        //if (deathSFX) AudioSource.PlayClipAtPoint(deathSFX, transform.position);

        GrantGrowthToTurtle();

        Destroy(gameObject);
    }

    private void GrantGrowthToTurtle()
    {
        if (_turtle == null) return;

        var growth = _turtle.GetComponent<TurtleGrowth>();
        if (growth == null) growth = _turtle.GetComponentInChildren<TurtleGrowth>();

        if (growth != null)
        {
            int times = Mathf.Max(1, Mathf.RoundToInt(growthOnDeath));
            for (int i = 0; i < times; i++)
                growth.GainGrowth();
        }
    }
}