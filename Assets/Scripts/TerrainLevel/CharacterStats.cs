using UnityEngine;
using System;

public class CharacterStats : MonoBehaviour, IDamageable
{
    [Header("Health System")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;
    public bool isPlayerCharacter = false;
    public bool isInvincible = false;

    [Header("Energy System (Stamina/Mana)")]
    [SerializeField] public float maxEnergy = 100f;
    [SerializeField] private float energyRegenRate = 5f; // Energy per second
    [SerializeField] private float energyRegenDelay = 1f; // Seconds before regen starts
    private float currentEnergy;
    private float lastEnergyUseTime;

    [Header("Defense & Resistance")]
    [Range(0f, 1f)] public float baseDamageReduction = 0f; // 0 = 0%, 1 = 100% reduction
    [Header("References")]
    private Rigidbody2D rb;

public Color poisonColor = Color.cyan;
private Color originalColor;
private SpriteRenderer sr;
    
    public event Action OnRevive;
    public event Action<int, int> OnHealthChanged; // Current, Max
    public event Action<float, float> OnEnergyChanged; // Current, Max
    public event Action<Vector2> OnKnockback; // Direction/Force for Controller to handle
    public event Action OnDeath;
    public event Action OnDamageTaken; // For flashing red, sounds, etc.

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        currentHealth = maxHealth;
        currentEnergy = maxEnergy;
    }

    void Start()
    {
        // Initialize UI
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }

    void Update()
    {
        HandleEnergyRegen();
    }

    // ==========================================
    //              DAMAGE LOGIC
    // ==========================================
    public void TakeDamage(int rawDamage, Vector2 hitPoint, Vector2 hitNormal)
    {
        if (isInvincible || currentHealth <= 0) return;

        // 1. Calculate Multipliers based on State
        float finalDamageMult = 1f - baseDamageReduction;

        // 2. Apply Damage
        int finalDamage = Mathf.RoundToInt(rawDamage * finalDamageMult);
        finalDamage = Mathf.Max(1, finalDamage); // Minimum 1 damage

        currentHealth -= finalDamage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnDamageTaken?.Invoke();


        // 4. Death Check
        if (currentHealth <= 0)
        {
            Die();
        }
        

    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died.");
        OnDeath?.Invoke();
        
        // If it's an enemy, destroy. If player, Manager handles logic.
        if (gameObject.layer != LayerMask.NameToLayer("Enemy"))
        {
            
            // Disable interactions, play animation, wait for Respawn
             gameObject.SetActive(false); 
        }
        else
        {
            //Destroy(gameObject);
        }
    }
    
    public void InstantDeath()
    {
        // 1. Bajar la vida a 0 visualmente
        currentHealth = 0;
        OnHealthChanged?.Invoke(0, maxHealth);

        // 2. Ejecutar la lógica de muerte existente
        Die(); 
    }

    // ==========================================
    //              ENERGY LOGIC
    // ==========================================
    public bool ConsumeEnergy(float amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            lastEnergyUseTime = Time.time;
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            return true;
        }
        return false;
    }

private void HandleEnergyRegen()
{
    // Only regen if we haven't used energy recently
    if (currentEnergy < maxEnergy && Time.time > lastEnergyUseTime + energyRegenDelay)
    {
        currentEnergy += energyRegenRate * Time.deltaTime;
        
        // Clamp it
        if(currentEnergy > maxEnergy) currentEnergy = maxEnergy;

        OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
    }
}

public void RestoreEnergy(float amount)
{
    currentEnergy += amount;
    currentEnergy = Mathf.Min(currentEnergy, maxEnergy); // Don't go over max
    OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
}

    public void Revive(float healthPercentage = 1.0f)
    {
        currentHealth = Mathf.RoundToInt(maxHealth * healthPercentage);
        isInvincible = false; // Quitamos invencibilidad si tenía
        gameObject.SetActive(true); // Reactivamos el objeto
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnRevive?.Invoke();
        Debug.Log($"{gameObject.name} ha revivido!");
    }

    public void Heal(int amount)
{
    currentHealth += amount;
    currentHealth = Mathf.Min(currentHealth, maxHealth); // No pasar del máximo
    OnHealthChanged?.Invoke(currentHealth, maxHealth);
    // Opcional: OnHeal visual effect
}

public void ApplyPoison(int damagePerTick, int ticks, float timeBetweenTicks)
{
    StartCoroutine(PoisonRoutine(damagePerTick, ticks, timeBetweenTicks));
}

private System.Collections.IEnumerator PoisonRoutine(int damage, int ticks, float interval)
{
    // 1. Visual Feedback
   
    Debug.Log($"{gameObject.name} is Poisoned!");

    // 2. Damage Loop
    for (int i = 0; i < ticks; i++)
    {
         if (sr) sr.color = poisonColor;
        // Deal direct damage (bypassing normal Hit logic like knockback)
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        // Flash or sound effect could go here
        
        if (currentHealth <= 0)
        {
            // Die logic
            // Call your existing Die() method here if private, or replicate logic
             Die(); 
            // Since Die() is private in your previous scripts, 
            // ensure currentHealth check in TakeDamage or Update catches this, 
            // OR make Die() public/protected.
            if(currentHealth <= 0) { /* trigger death */ } 
        }

        yield return new WaitForSeconds(interval);
    }

    // 3. Restore Visuals
    if (sr) sr.color = originalColor;
    Debug.Log($"{gameObject.name} Poison wore off.");
}

    // ==========================================
    //              STATE SETTERS
    // ==========================================
    // Call these from your PlayerController / TurtleController / EnemyAI
    
    // Call this if a buff/item gives extra armor temporarily

    // Getters for UI or Logic
    public float GetCurrentHealthPct() => (float)currentHealth / maxHealth;
    public float GetCurrentEnergyPct() => currentEnergy / maxEnergy;
}