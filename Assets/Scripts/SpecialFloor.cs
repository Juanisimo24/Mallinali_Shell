using System.Collections;
using UnityEngine;

public class SpecialFloor : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    [Header("Configuración")]
    [Tooltip("Ángulo máximo (en grados) para considerar una colisión como 'desde arriba'")]
    [Range(0f, 180f)] public float topAngleThreshold = 45f;

    [Tooltip("Ángulo máximo (en grados) para considerar una colisión como 'desde abajo'")]
    [Range(0f, 180f)] public float bottomAngleThreshold = 45f;

    public PlayerManagerDual manager;
    public GameObject warrior;    // Guerrero azteca
    public GameObject turtle;

    public KeyCode diveKey = KeyCode.S;
    bool divePressed;
    public KeyCode upKey = KeyCode.W;
    public KeyCode upKey2 = KeyCode.Space;
    bool upPressed;

    bool underWater=false;
    public void Update()
    {
        divePressed = Input.GetKey(diveKey);
        upPressed = Input.GetKey(upKey) && Input.GetKey(upKey2);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Tomamos el primer punto de contacto
        ContactPoint2D contact = collision.contacts[0];
        Vector2 normal = contact.normal;

        // Ángulo entre la normal del contacto y el eje vertical
        float angle = Vector2.Angle(normal, Vector2.up);
        if (manager.GetActive().tag == "Tortuga")
        {
            if (angle <= topAngleThreshold)
            {
                Debug.Log($"ia pls");
                if (upPressed)
                {
                    underWater = false;
                    OnHitFromBelow(collision, contact);
                }
                    
            }
            else if (angle >= 180f - bottomAngleThreshold)
            {
                Debug.Log($"si se puede");
                if (divePressed)
                {
                    underWater = true;
                    OnHitFromAbove(collision, contact);
                }
            }
        }

    }

    private void OnHitFromAbove(Collision2D collision, ContactPoint2D contact)
    {
        Collider2D turtleCollision = turtle.GetComponent<Collider2D>();
        DropThroughPlatform(turtleCollision);
        var turltleCtrl = turtle.GetComponent<TortugaController>();
        var turltleRigid = turtle.GetComponent<Rigidbody2D>();
        var turltleWaterCtrl = turtle.GetComponent<UnderWaterControl>();
        var warriorFol = warrior.GetComponent<CompanionAStar2D>();
        if (turltleCtrl) turltleCtrl.enabled = false;
        if (warriorFol) warriorFol.enabled = false;
        if (turltleRigid) turltleRigid.gravityScale = 0;
        if (turltleWaterCtrl) turltleWaterCtrl.enabled = true;

        Debug.Log($"Puedes bajar");

    }

    private void OnHitFromBelow(Collision2D collision, ContactPoint2D contact)
    {

        Collider2D turtleCollision = turtle.GetComponent<Collider2D>();
        UpThroughPlatform(turtleCollision);
        var turltleCtrl = turtle.GetComponent<TortugaController>();
        var turltleRigid = turtle.GetComponent<Rigidbody2D>();
        var turltleWaterCtrl = turtle.GetComponent<UnderWaterControl>();
        var warriorFol = warrior.GetComponent<CompanionAStar2D>();
        if (turltleCtrl) turltleCtrl.enabled = true;
        if (warriorFol) warriorFol.enabled = true;
        if (turltleRigid) turltleRigid.gravityScale = 1;
        if (turltleWaterCtrl) turltleWaterCtrl.enabled = false;
        turtle.transform.rotation = Quaternion.identity;
        Debug.Log($"Puedes salir");

        // Ejemplo: daño, rebote o bloqueo espiritual
        // collision.gameObject.GetComponent<PlayerHealth>()?.TakeDamage(10);
    }

    private void OnDrawGizmos()
    {
        if (!showDebug) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(GetComponent<Collider2D>().bounds.center, GetComponent<Collider2D>().bounds.size);
    }

void DropThroughPlatform(Collider2D turtleCollider)
    {
        Rigidbody2D turtleRigidbody = turtle.GetComponent<Rigidbody2D>();
Collider2D platformCollision = GetComponent<Collider2D>();
    // 1. Ignorar colisión temporal
    Physics2D.IgnoreCollision(turtleCollider, platformCollision, true);

    // 2. Mover tortuga un poco hacia abajo
    turtleRigidbody.position += Vector2.down * 3f;

    // 3. Rehabilitar la colisión después de un momento
    StartCoroutine(RestoreCollision(turtleCollider, 0.3f));
}

    void UpThroughPlatform(Collider2D turtleCollider)
    {
        Rigidbody2D turtleRigidbody = turtle.GetComponent<Rigidbody2D>();
        Collider2D platformCollision = GetComponent<Collider2D>();
        // 1. Ignorar colisión temporal
        Physics2D.IgnoreCollision(turtleCollider, platformCollision, true);

        // 2. Mover tortuga un poco hacia arriba
        turtleRigidbody.position += Vector2.up * 7f;

        // 3. Rehabilitar la colisión después de un momento
        StartCoroutine(RestoreCollision(turtleCollider, 0.3f));
    }

    private IEnumerator RestoreCollision(Collider2D turtleCollider, float delay)
    {
        yield return new WaitForSeconds(delay);
        Collider2D platformCollision = GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(platformCollision, turtleCollider, false);
    }

    public bool getUnderWater()
    {
        return underWater;
    }



}
