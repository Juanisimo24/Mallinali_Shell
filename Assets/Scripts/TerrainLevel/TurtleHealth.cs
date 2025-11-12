using UnityEngine;

public class TurtleHealth : MonoBehaviour, IDamageable
{
    [Header("Vida base")]
    public int maxHealth = 160;     // más vida que el guerrero
    public float knockbackScale = 0.6f; // menos knockback base

    [Header("Guardia")]
    public float guardDamageMultiplier = 0.5f; // recibe 50% del daño
    public float guardKnockbackScale = 0.4f;   // aún menos knockback en guardia

    [Header("Charge Armor")]
    public float chargeDamageMultiplier = 0.75f; // 25% de reducción al embestir
    public float chargeKnockbackScale = 0.5f;

    public System.Action OnDeath;
    public System.Action<int> OnDamaged; // daño recibido final

    public int current;
    bool isGuarding;
    bool isCharging;
    float extraKnockbackResist; // de controller (guardKnockbackResist)

    Rigidbody2D rb;

    void Awake()
    {
        current = maxHealth;
        rb = GetComponent<Rigidbody2D>();

    }

    public void SetGuarding(bool guarding, float extraResist)
    {
        isGuarding = guarding;
        extraKnockbackResist = Mathf.Clamp01(extraResist);
    }

    public void SetCharging(bool charging)
    {
        isCharging = charging;
    }

    public void TakeDamage(int damage, Vector2 hitPoint, Vector2 hitNormal)
    {
        // Calcular multiplicadores
        float dmgMul = 1f;
        float kbScale = knockbackScale;

        if (isGuarding)
        {
            dmgMul *= guardDamageMultiplier;
            kbScale *= guardKnockbackScale * (1f - extraKnockbackResist); // apila reducción
        }
        if (isCharging)
        {
            dmgMul *= chargeDamageMultiplier;
            kbScale *= chargeKnockbackScale;
        }

        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(damage * dmgMul));
        current -= finalDamage;
        OnDamaged?.Invoke(finalDamage);

        // Knockback leve
        if (rb != null)
        {
            Vector2 dir = hitNormal.sqrMagnitude > 0.01f ? -hitNormal.normalized : Vector2.left;
            rb.AddForce(dir * 100f * kbScale, ForceMode2D.Impulse);
        }

        if (current <= 0) Die();
    }

    void Die()
    {
        OnDeath?.Invoke();
        // TODO: animaciones/FX de muerte
        Destroy(gameObject);
    }
}
