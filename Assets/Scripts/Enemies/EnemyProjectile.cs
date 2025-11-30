using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 10f;
    public int damage = 10;

    void Start() => Destroy(gameObject, 3f); // Auto destroy
    
    public void Launch(Vector2 dir)
    {
        GetComponent<Rigidbody2D>().linearVelocity = dir * speed;
    }

    void OnTriggerEnter2D(Collider2D hit)
    {
        if (hit.CompareTag("Player"))
        {
            hit.GetComponent<IDamageable>()?.TakeDamage(damage, transform.position, Vector2.zero);
            Destroy(gameObject);
        }
        else if (hit.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}