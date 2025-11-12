using UnityEngine;

public class UnderWaterControl : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;          // Velocidad base
    public float acceleration = 3f;       // Qué tan rápido alcanza la velocidad deseada
    public float rotationSpeed = 5f;      // Qué tan rápido rota hacia la dirección
    public float dashForce = 12f;         // Fuerza de embestida
    public float dashCooldown = 1f;       // Tiempo entre dashes
    private Rigidbody2D rb;
    private Vector2 input;
    private bool canDash = true;

    [Header("Growth Settings")]
    public float size = 1f;               // Escala actual de la tortuga
    public float growthRate = 0.1f;       // Cuánto crece por cada “comida”
    public float maxSize = 3f;            // Tamaño máximo
    public float growthSpeed = 2f;        // Suavidad visual del crecimiento
    private Vector3 targetScale;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0;
        rb.linearDamping = 2f;
        targetScale = transform.localScale;
    }

    void Update()
    {
        // --- Movimiento ---
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;

        // --- Dash / embestida ---
        if (Input.GetKeyDown(KeyCode.Space) && canDash)
        {
            StartCoroutine(Dash());
        }

        // --- Rotación visual ---
        if (input.sqrMagnitude > 0.1f)
        {
            float angle = Mathf.Atan2(input.y, input.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.Euler(0, 0, angle - 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // --- Escalado visual suave ---
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * growthSpeed);
    }

    void FixedUpdate()
    {
        // --- Movimiento físico con inercia ---
        if (input.sqrMagnitude > 0.1f)
        {
            Vector2 targetVelocity = input * moveSpeed;
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, targetVelocity, acceleration * Time.fixedDeltaTime);
        }
        else
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, acceleration * Time.fixedDeltaTime);
        }
    }

    private System.Collections.IEnumerator Dash()
    {
        canDash = false;
        Vector2 dashDir = input.sqrMagnitude > 0.1f ? input : transform.up;
        rb.AddForce(dashDir * dashForce, ForceMode2D.Impulse);
        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    // --- MÉTODO PÚBLICO PARA HACER CRECER LA TORTUGA ---
    public void Eat()
    {
        if (size < maxSize)
        {
            size += growthRate;
            targetScale = Vector3.one * size;
            moveSpeed += 0.5f; // se vuelve un poco más rápida
            dashForce += 0.3f; // su embestida mejora
        }
    }
}
