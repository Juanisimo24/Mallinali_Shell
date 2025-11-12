using UnityEngine;

public class Sensor2D : MonoBehaviour
{
  [Header("Refs")]
    public Transform eye;                   // punto de visión (colócalo al frente del enemigo)
    public Rigidbody2D ownerRb;             // rigidbody del enemigo (para facing por movimiento)

    [Header("Vision")]
    public float sightRange = 6f;
    [Range(0f, 180f)]
    public float sightAngle = 75f;
    public LayerMask sightObstacles;        // paredes/suelo
    public LayerMask targetsMask;           // Capa con guerrero + tortuga (p.ej. "Player")
    public float nearSense = 0.0f;          // si >0, detecta por proximidad aunque esté detrás

    [Header("Perf")]
    public int maxCandidates = 8;           // tope de candidatos por frame
    public float minMoveForFacing = 0.05f;  // velocidad mínima para cambiar facing
    public bool useMovementFacing = true;   // si no, usa escala local

    Collider2D[] _hits;

    int lastFacing = 1; // 1 derecha, -1 izquierda

    void Awake()
    {
        _hits = new Collider2D[Mathf.Max(1, maxCandidates)];
    }

    public bool TryGetBestTarget(out Transform bestTarget)
    {
        bestTarget = null;
        if (eye == null) return false;

        // 1) Overlap no asignante
        int count = Physics2D.OverlapCircleNonAlloc(eye.position, sightRange, _hits, targetsMask);
        if (count <= 0) return false;

        // Facing
        int facing = ComputeFacing();

        // Precalcular cos del semi-ángulo para comparar con dot (más barato que Angle)
        float half = sightAngle * 0.5f;
        float cosHalf = Mathf.Cos(half * Mathf.Deg2Rad);

        float bestDist = float.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var c = _hits[i];
            if (c == null) continue;

            Vector2 to = (Vector2)c.transform.position - (Vector2)eye.position;
            float dist = to.magnitude;
            if (dist <= 0.0001f) continue;

            // 2) Proximidad directa (opcional)
            if (nearSense > 0f && dist <= nearSense)
            {
                if (HasLineOfSight(eye.position, c.transform.position, dist)) // aún verificamos LOS
                {
                    bestTarget = c.transform;
                    return true; // proximidad gana
                }
            }

            // 3) Angulo dentro del cono (dot con “forward”)
            Vector2 dirTo = to / dist;
            Vector2 forward = facing == 1 ? Vector2.right : Vector2.left;
            float dot = Vector2.Dot(forward, dirTo);
            if (dot < cosHalf) continue; // fuera de cono

            // 4) Línea de vista
            if (!HasLineOfSight(eye.position, c.transform.position, dist)) continue;

            // 5) Guardar el más cercano
            if (dist < bestDist)
            {
                bestDist = dist;
                bestTarget = c.transform;
            }
        }

        return bestTarget != null;
    }

    public bool CanSee(Transform target)
    {
        if (eye == null || target == null) return false;
        Vector2 to = (Vector2)target.position - (Vector2)eye.position;
        float dist = to.magnitude;
        if (dist > sightRange) return false;

        // proximidad opcional
        if (nearSense > 0f && dist <= nearSense)
            return HasLineOfSight(eye.position, target.position, dist);

        int facing = ComputeFacing();
        Vector2 forward = facing == 1 ? Vector2.right : Vector2.left;
        float half = sightAngle * 0.5f;
        float cosHalf = Mathf.Cos(half * Mathf.Deg2Rad);

        Vector2 dirTo = dist > 0.0001f ? to / dist : Vector2.right;
        float dot = Vector2.Dot(forward, dirTo);
        if (dot < cosHalf) return false;

        return HasLineOfSight(eye.position, target.position, dist);
    }

    bool HasLineOfSight(Vector2 origin, Vector2 targetPos, float dist)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, (targetPos - origin).normalized, dist, sightObstacles);
        return hit.collider == null;
    }

    int ComputeFacing()
    {
        if (useMovementFacing && ownerRb != null)
        {
            float vx = ownerRb.linearVelocity.x;
            if (vx > minMoveForFacing) lastFacing = 1;
            else if (vx < -minMoveForFacing) lastFacing = -1;
            return lastFacing;
        }
        // fallback: escala local del root
        var root = eye != null ? eye.root : transform;
        lastFacing = root.localScale.x >= 0f ? 1 : -1;
        return lastFacing;
    }

    void OnDrawGizmosSelected()
    {
        if (eye == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(eye.position, sightRange);
        if (nearSense > 0f)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawWireSphere(eye.position, nearSense);
        }

        int facing = ComputeFacing();
        Vector3 forward = (facing == 1) ? Vector3.right : Vector3.left;
        float half = sightAngle * 0.5f;
        Quaternion q1 = Quaternion.AngleAxis(+half, Vector3.forward);
        Quaternion q2 = Quaternion.AngleAxis(-half, Vector3.forward);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(eye.position, eye.position + (q1 * forward) * sightRange);
        Gizmos.DrawLine(eye.position, eye.position + (q2 * forward) * sightRange);
    }
}
