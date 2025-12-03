using UnityEngine;

public class DeathZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. Intentar obtener las estadísticas del que cayó
        var stats = other.GetComponent<CharacterStats>();

        if (stats != null)
        {
            Debug.Log(other.name + " cayó al vacío.");
            
            // Muerte instantánea (Funciona para Jugador y Enemigos)
            stats.InstantDeath();
        }
        else
        {
            // 2. Si cae una caja, una bala o algo sin vida, lo destruimos
            // (Para no llenar la memoria de basura que cae al infinito)
            Destroy(other.gameObject);
        }
    }
}