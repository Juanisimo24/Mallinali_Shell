using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile2D : MonoBehaviour
{
    public float lifeTime = 6f;
    public int damage = 5;
    public string allowedTargetTag = "PlayerMain"; // SOLO impacta al Guerrero
    public LayerMask destroyOnHitMask;             // Capas de entorno para destruirse (paredes, suelo)

    private Rigidbody2D _rb;
    private bool _launched;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 dir, float speed, int dmg)
    {
        damage = dmg;
        _rb.linearVelocity = dir.normalized * speed;
        _launched = true;
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_launched) return;

        // 1) Destruir si choca contra entorno
        if (((1 << other.gameObject.layer) & destroyOnHitMask) != 0)
        {
            Destroy(gameObject);
            return;
        }

        // 2) Solo daña si el tag coincide con el Guerrero
        if (!other.CompareTag(allowedTargetTag)) return;

        // 3) Aplicar daño si implementa IDamageable (tu sistema)
        var dmgable = other.GetComponent<IDamageable>();
        if (dmgable != null)
        {
            Vector2 hitPoint = other.bounds.ClosestPoint(transform.position);
            Vector2 hitNormal = -_rb.linearVelocity.normalized;
            dmgable.TakeDamage(damage, hitPoint, hitNormal);
        }

        Destroy(gameObject);
    }
}
