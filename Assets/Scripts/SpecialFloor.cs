using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using Pathfinding;
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
    [SerializeField] PolygonCollider2D mapBoundry;
    [SerializeField] PolygonCollider2D mapBoundry2;
    CinemachineConfiner2D confiner;
    [SerializeField]private Animator anim;
    bool upPressed;

    bool underWater=false;
    public void Update()
    {
        confiner = FindAnyObjectByType<CinemachineConfiner2D>();
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
        anim.SetBool("Change",true);
        if (manager.GetActive().layer == 7)
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
    private void OnCollisionExit2D(Collision2D collision)
    {
        anim.SetBool("Change",false);
    }

    private void OnHitFromAbove(Collision2D collision, ContactPoint2D contact)
    {
        Collider2D turtleCollision = turtle.GetComponent<Collider2D>();
        DropThroughPlatform(turtleCollision);
        var turltleCtrl = turtle.GetComponent<TortugaController>();
        var turltleRigid = turtle.GetComponent<Rigidbody2D>();
        
        var warriorFol = warrior.GetComponent<CompanionAStar2D>();
        var warriorSeek = warrior.GetComponent<Seeker>();
        if (warriorSeek) warriorSeek.enabled = false;
        if (turltleCtrl) turltleCtrl.SetSwimming(true);
        if (warriorFol) warriorFol.enabled = false;
        if (turltleRigid) turltleRigid.gravityScale = 0;
        confiner.BoundingShape2D = mapBoundry;

        Debug.Log($"Puedes bajar");

    }

    private void OnHitFromBelow(Collision2D collision, ContactPoint2D contact)
    {

        Collider2D turtleCollision = turtle.GetComponent<Collider2D>();
        UpThroughPlatform(turtleCollision);
        var turltleCtrl = turtle.GetComponent<TortugaController>();
        var turltleRigid = turtle.GetComponent<Rigidbody2D>();
        var warriorFol = warrior.GetComponent<CompanionAStar2D>();
        if (turltleCtrl) turltleCtrl.SetSwimming(false);
        if (warriorFol) warriorFol.enabled = true;
        if (turltleRigid) turltleRigid.gravityScale = 1;
        turtle.transform.rotation = Quaternion.identity;

         confiner.BoundingShape2D = mapBoundry2;
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
