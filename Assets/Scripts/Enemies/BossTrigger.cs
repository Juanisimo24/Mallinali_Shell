using UnityEngine;

public class BossZoneTrigger : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí al Boss (el objeto que tiene el script WaterGuardianBoss)")]
    public WaterGuardianBoss bossScript;

    [Header("Opcional: Puertas")]
    [Tooltip("Si tienes una puerta/reja que se cierra al entrar, ponla aquí")]
    public GameObject entryDoor; 

    private bool hasActivated = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasActivated) return;

        // Detectar al Player (Guerrero o Tortuga)
        if (other.CompareTag("Player"))
        {
            ActivateBossFight();
        }
    }

    void ActivateBossFight()
    {
        hasActivated = true;

        // 1. Activar el Script del Boss
        // Al poner .enabled = true, Unity ejecuta inmediatamente el método Start() del Boss
        if (bossScript != null)
        {
            bossScript.enabled = true; 
            Debug.Log("¡Jefe Despertado!");
        }

        // 2. Cerrar la puerta (si asignaste una)
        if (entryDoor != null)
        {
            entryDoor.SetActive(true);
        }

        // 3. Desactivar este trigger para no volver a dispararlo
        GetComponent<Collider2D>().enabled = false;
    }
}