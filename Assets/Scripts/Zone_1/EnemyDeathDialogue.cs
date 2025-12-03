using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
public class EnemyDeathDialogue : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Arrastra aquí el objeto que tiene el script de diálogo")]
    public SimpleNPC_Dialogue dialogueToTrigger;

    [Tooltip("¿Quieres que el diálogo aparezca instantáneamente o esperar un poco?")]
    public float delayBeforeDialogue = 1.0f;

    private CharacterStats stats;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    void Start()
    {
        // Nos suscribimos al evento de muerte
        if (stats != null)
        {
            stats.OnDeath += OnEnemyDied;
        }
    }

    void OnDestroy()
    {
        // Buena práctica: desuscribirse para evitar errores de memoria
        if (stats != null)
        {
            stats.OnDeath -= OnEnemyDied;
        }
    }

    void OnEnemyDied()
    {
        // Usamos Invoke para permitir un pequeño retraso (ej. para ver la animación de muerte)
        Invoke(nameof(TriggerDialogue), delayBeforeDialogue);
    }

    void TriggerDialogue()
    {
        if (dialogueToTrigger != null)
        {
            // Activamos el objeto por si estaba oculto en la escena
            dialogueToTrigger.gameObject.SetActive(true);
            
            // Iniciamos la conversación
            dialogueToTrigger.StartDialogueManually();
        }
        else
        {
            Debug.LogWarning("¡El enemigo murió pero no tiene un diálogo asignado!");
        }
    }
}