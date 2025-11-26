using UnityEngine;

public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    public int maxHealth = 30;
    public int currentHealth;

    [Header("Feedback (opcional)")]
    public bool flashOnHit = true;
    public Color flashColor = Color.red;
    public float flashTime = 0.07f;
    private SpriteRenderer sr;
    private Color originalColor;
    int current;
    public System.Action OnDeath;
    void Awake()
    {
        currentHealth = maxHealth;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
    }

    public void TakeDamage(int damage, Vector2 hitPoint, Vector2 hitNormal)
    {
        currentHealth -= damage;
        if (flashOnHit && sr != null){
             StartCoroutine(Flash());
             };

        if (currentHealth <= 0)
        {
            Die();
        }

    }

    System.Collections.IEnumerator Flash()
    {
        sr.color = flashColor;
        yield return new WaitForSeconds(flashTime);
        sr.color = originalColor;
    }

    void Die()
    {
        OnDeath?.Invoke();
        Destroy(gameObject);
    }


}
