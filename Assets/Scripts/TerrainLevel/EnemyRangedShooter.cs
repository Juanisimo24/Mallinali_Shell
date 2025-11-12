using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyRangedShooter : MonoBehaviour
{
    [Header("Refs")]
    public EnemyStats stats;                    // Usa sightRange, sightAngle, attackCooldown, damage (para pasar a proyectil si quieres)
    public Transform firePoint;                 // Punto de disparo
    public GameObject projectilePrefab;         // Prefab del proyectil
    public LayerMask lineOfSightMask;           // Capas que bloquean la visión (Tilemap, Walls, Platforms)

    [Header("Targeting")]
    [Tooltip("Asignar el Transform del Guerrero. También puedes buscar por tag 'PlayerMain'.")]
    public Transform warriorTarget;             // SOLO objetivo Guerrero
    public string warriorTag = "PlayerMain";    // Tag para el Guerrero (asegúrate de crearlo)

    [Header("Tuning")]
    public float projectileSpeed = 12f;
    public float anticipationLead = 0f;         // 0 = disparo directo; >0 = un poco de 'lead' si el guerrero se mueve
    public bool faceTarget = true;              // Girar sprite hacia el objetivo

    private float _lastShotTime;
    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (warriorTarget == null)
        {
            var go = GameObject.FindGameObjectWithTag(warriorTag);
            if (go) warriorTarget = go.transform;
        }
    }

    private void Update()
    {
        if (warriorTarget == null || stats == null || projectilePrefab == null || firePoint == null) return;

        if (CanSeeWarrior())
        {
            if (faceTarget) Face(warriorTarget.position);

            if (Time.time >= _lastShotTime + stats.attackCooldown)
            {
                ShootAt(warriorTarget);
                _lastShotTime = Time.time;
            }
        }
    }

    private bool CanSeeWarrior()
    {
        Vector2 myPos = firePoint.position;
        Vector2 toTarget = (Vector2)warriorTarget.position - myPos;
        float dist = toTarget.magnitude;

        if (dist > stats.sightRange) return false;

        // Cono de visión respecto al frente del enemigo (transform.right)
        float angle = Vector2.Angle(transform.right, toTarget.normalized);
        if (angle > stats.sightAngle * 0.5f) return false;

        // Línea de visión (bloqueada por paredes, plataformas, etc.)
        RaycastHit2D hit = Physics2D.Raycast(myPos, toTarget.normalized, dist, lineOfSightMask);
        if (hit.collider != null) return false; // algo bloquea la vista

        return true;
    }

    private void ShootAt(Transform tgt)
    {
        Vector2 myPos = firePoint.position;
        Vector2 aimDir;

        // Opcional: un poquito de 'lead' si conoces la velocidad del guerrero (si tienes su Rigidbody2D)
        if (anticipationLead > 0f)
        {
            var rbTarget = tgt.GetComponent<Rigidbody2D>();
            if (rbTarget != null)
            {
                Vector2 toTarget = (Vector2)tgt.position - (Vector2)myPos;
                Vector2 lead = rbTarget.linearVelocity * anticipationLead;
                aimDir = (toTarget + lead).normalized;
            }
            else
            {
                aimDir = ((Vector2)tgt.position - (Vector2)myPos).normalized;
            }
        }
        else
        {
            aimDir = ((Vector2)tgt.position - (Vector2)myPos).normalized;
        }

        GameObject proj = Instantiate(projectilePrefab, myPos, Quaternion.identity);
        var p = proj.GetComponent<Projectile2D>();
        if (p != null)
        {
            p.Launch(aimDir, projectileSpeed, stats.damage);
            // IMPORTANTÍSIMO: marca al Guerrero como único objetivo válido
            p.allowedTargetTag = warriorTag;  // solo dañará al Guerrero
        }
    }

    private void Face(Vector3 worldPoint)
    {
        bool lookRight = (worldPoint.x - transform.position.x) >= 0f;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (lookRight ? 1f : -1f);
        transform.localScale = scale;

        // Alinear transform.right con la dirección horizontal
        transform.right = new Vector3(lookRight ? 1f : -1f, 0f, 0f);
    }
}
