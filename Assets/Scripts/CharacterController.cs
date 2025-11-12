using UnityEngine;

public class CharacterController : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;            // Velocidad base
    public float runMultiplier = 1.5f;  // Multiplicador al correr
    public float jumpForce = 7f;        // Fuerza del salto
    public float slideSpeed = 10f;      // Velocidad del deslizamiento
    public float slideDuration = 0.5f;  // Tiempo del deslizamiento

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private bool isGrounded;
    private bool isSliding;
    private float slideTimer;

    public Transform groundCheck;       // Punto debajo del jugador
    public float groundRadius = 0.1f;   // Radio del círculo de chequeo
    public LayerMask groundLayer;       // Capa del suelo

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        // --- Movimiento ---
        float move = Input.GetAxis("Horizontal");

        if (!isSliding)
        {
            float finalSpeed = Input.GetKey(KeyCode.LeftShift) ? speed * runMultiplier : speed;
            rb.linearVelocity = new Vector2(move * finalSpeed, rb.linearVelocity.y);

            // Voltear sprite según dirección
            if (move > 0) spriteRenderer.flipX = false;
            if (move < 0) spriteRenderer.flipX = true;
        }

        // --- Saltar ---
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }

        // --- Deslizamiento (Ctrl) ---
        if (Input.GetKeyDown(KeyCode.LeftControl) && isGrounded && !isSliding)
        {
            isSliding = true;
            slideTimer = slideDuration;

            float dir = spriteRenderer.flipX ? -1 : 1;
            rb.linearVelocity = new Vector2(dir * slideSpeed, rb.linearVelocity.y);
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0)
            {
                isSliding = false;
            }
        }
    }
}
