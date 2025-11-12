using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    public int maxHealth = 100;
    private int current;

    void Awake() => current = maxHealth;

    public void TakeDamage(int damage, Vector2 hitPoint, Vector2 hitNormal)
    {
        current -= damage;
        // TODO: HUD, i-frames, empujón, SFX
        if (current <= 0) Die();
    }

    void Die()
    {
        // TODO: respawn, checkpoint, game over
        Debug.Log("Player died");
        Destroy(gameObject);
    }
}
