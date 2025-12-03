using UnityEngine;

public class SoulPickup : MonoBehaviour
{
    public float amountToRestore = 10f;
    public GameObject pickupEffect; // Optional Particle System

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object colliding is the Player (Layer or Tag check)
        // Or simply check if it has CharacterStats
        CharacterStats stats = other.GetComponent<CharacterStats>();

        if (stats != null && stats.isPlayerCharacter)
        {
            stats.RestoreEnergy(amountToRestore);
            
            // Visuals
            if (pickupEffect != null)
            {
                Instantiate(pickupEffect, transform.position, Quaternion.identity);
            }

            // Sound logic could go here

            // Destroy the pickup
            Destroy(gameObject);
        }
    }
}