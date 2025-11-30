using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using Pathfinding;

public class SpecialFloor : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    [Header("Configuración")]
    [Range(0f, 180f)] public float topAngleThreshold = 45f;
    [Range(0f, 180f)] public float bottomAngleThreshold = 45f;

    public PlayerManagerDual manager;
    public GameObject warrior;
    public GameObject turtle;

    [Header("Inputs")]
    public KeyCode diveKey = KeyCode.S;
    public KeyCode upKey = KeyCode.W;
    
    [Header("Límites de Cámara")]
    [SerializeField] PolygonCollider2D landBoundary; // Antes mapBoundry
    [SerializeField] PolygonCollider2D waterBoundary; // Antes mapBoundry2
    private CinemachineConfiner2D confiner;

    // Estado interno
    private bool underWater = false;

    void Start()
    {
        confiner = FindFirstObjectByType<CinemachineConfiner2D>();
    }

    // Usamos Stay para detectar el input mientras estamos parados sobre la plataforma
    private void OnCollisionStay2D(Collision2D collision)
    {
        // 1. Validar que sea la tortuga la activa
        if (manager.GetActive() != turtle) return;

        // Inputs actuales
        bool divePressed = Input.GetKey(diveKey);
        // Para subir pedimos W y que la tortuga esté nadando (underWater)
        bool upPressed = Input.GetKey(upKey); 

        // Analizar colisión
        foreach (ContactPoint2D contact in collision.contacts)
        {
            Vector2 normal = contact.normal;
            float angle = Vector2.Angle(normal, Vector2.up);

            // --- CASO 1: ESTAMOS ARRIBA (Queremos bajar) ---
            // El ángulo es pequeño (la normal apunta arriba), estamos pisando el suelo
            if (angle <= topAngleThreshold && !underWater)
            {
                if (divePressed)
                {
                    EnterWaterMode(collision.collider);
                    break; // Evitar múltiples llamadas
                }
            }
            // --- CASO 2: ESTAMOS ABAJO (Queremos subir) ---
            // El ángulo es grande (la normal apunta abajo), estamos golpeando el techo
            else if (angle >= 180f - bottomAngleThreshold && underWater)
            {
                if (upPressed)
                {
                    ExitWaterMode(collision.collider);
                    break;
                }
            }
        }
    }

    private void EnterWaterMode(Collider2D turtleCol)
    {
        Debug.Log("Bajando al agua...");
        underWater = true;

        // 1. Desactivar IA y Guerrero (El guerrero se queda arriba)
        DisableWarriorFollow();

        // 2. Activar Modo Nado en el Controlador Unificado
        var tortugaCtrl = turtle.GetComponent<TortugaController>();
        if (tortugaCtrl) tortugaCtrl.SetSwimming(true);

        // 3. Física de atravesar plataforma
        DropThroughPlatform(turtleCol);

        // 4. Cámara
        if (confiner && waterBoundary) confiner.BoundingShape2D = waterBoundary;
    }

    private void ExitWaterMode(Collider2D turtleCol)
    {
        Debug.Log("Saliendo a tierra...");
        underWater = false;

        // 1. Reactivar IA del Guerrero
        EnableWarriorFollow();

        // 2. Desactivar Modo Nado (Vuelve a caminar)
        var tortugaCtrl = turtle.GetComponent<TortugaController>();
        if (tortugaCtrl) tortugaCtrl.SetSwimming(false);

        // 3. Física de atravesar plataforma hacia arriba
        UpThroughPlatform(turtleCol);

        // 4. Cámara
        if (confiner && landBoundary) confiner.BoundingShape2D = landBoundary;
    }

    // --- UTILIDADES ---

    void DisableWarriorFollow()
    {
        var warriorAI = warrior.GetComponent<CompanionAStar2D>();
        var warriorSeeker = warrior.GetComponent<Seeker>();
        if (warriorAI) warriorAI.enabled = false;
        if (warriorSeeker) warriorSeeker.enabled = false;
        
        // Opcional: Detener al guerrero visualmente
        var warriorRb = warrior.GetComponent<Rigidbody2D>();
        if(warriorRb) warriorRb.linearVelocity = Vector2.zero;
    }

    void EnableWarriorFollow()
    {
        var warriorAI = warrior.GetComponent<CompanionAStar2D>();
        if (warriorAI) warriorAI.enabled = true;
        // Seeker se activa solo si es necesario, AStar suele manejarlo
    }

    void DropThroughPlatform(Collider2D turtleCollider)
    {
        Collider2D platformCollider = GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(turtleCollider, platformCollider, true);
        
        // Empujón hacia abajo para asegurar que cruce
        turtle.transform.position += Vector3.down * 2.5f; 

        StartCoroutine(RestoreCollision(turtleCollider, platformCollider, 0.5f));
    }

    void UpThroughPlatform(Collider2D turtleCollider)
    {
        Collider2D platformCollider = GetComponent<Collider2D>();
        Physics2D.IgnoreCollision(turtleCollider, platformCollider, true);

        // Empujón hacia arriba y rotación a 0 para caer de pie
        turtle.transform.position += Vector3.up * 3.5f;
        turtle.transform.rotation = Quaternion.identity;

        StartCoroutine(RestoreCollision(turtleCollider, platformCollider, 0.5f));
    }

    private IEnumerator RestoreCollision(Collider2D turtleCol, Collider2D platformCol, float delay)
    {
        yield return new WaitForSeconds(delay);
        Physics2D.IgnoreCollision(turtleCol, platformCol, false);
    }

    private void OnDrawGizmos()
    {
        if (!showDebug) return;
        Gizmos.color = Color.yellow;
        if (GetComponent<Collider2D>())
            Gizmos.DrawWireCube(GetComponent<Collider2D>().bounds.center, GetComponent<Collider2D>().bounds.size);
    }

    public bool getUnderWater()
    {
        return underWater;
    }
}