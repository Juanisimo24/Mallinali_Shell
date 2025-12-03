using UnityEngine;
using System.Collections;

public class WaterGuardianBoss : EnemyAI
{
    [Header("Boss Movement")]
    public float moveSpeed = 2f;
    public float stopDistance = 4f; // Keeps some distance to shoot
    public float rotationSpeed = 5f;

    [Header("Ability 1: Ranged Attack")]
    public GameObject projectilePrefab; // Your Poison or Normal Projectile Prefab
    public Transform firePoint;
    public float shootCooldown = 3f;

    [Header("Ability 2: Dash Attack")]
    public int dashDamageAmount = 20; // Specific damage for the dash
    public float dashForce = 25f;
    public float dashWarningTime = 1f; 
    public float dashDuration = 0.5f;
    public float dashCooldown = 6f;

    // Flag to control damage
    private bool isDashing = false;

    [Header("Ability 3: Spawn Minions")]
    public GameObject minionPrefab;
    public int minionCount = 3;
    public float spawnCooldown = 12f;

    [Header("Drop on Death")]
    public GameObject abilityItemPrefab; // We will create this in the next step

    // Timers
    private float shootTimer;
    private float dashTimer;
    private float spawnTimer;

    // State
    private bool isBusy = false; // True when performing an animation/attack

    [Header("Zone Progression")]
    public TransitionPoint exitPortal;

    [Header("Boss UI")]
    public string bossDisplayName = "Axolotl Guard";

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0; // Underwater
        rb.linearDamping = 1f; // Physics drag
    }

    protected override void Start()
    {
        base.Start();
        // Initialize timers
        shootTimer = 1f;
        dashTimer = 5f;
        spawnTimer = 8f;
        if (BossHealthUI.Instance != null)
        {
            BossHealthUI.Instance.ShowBar(bossDisplayName);
        }

        // 2. Suscribirse a cambios de vida y muerte
        if (stats != null)
        {
            stats.OnHealthChanged += UpdateBossBar;
            stats.OnDeath += OnBossDied;
            
            // Mantenemos tus otras suscripciones (DropItem, UnlockExit)
            stats.OnDeath += DropItem;     
            stats.OnDeath += UnlockExit;   
        }
    }

    protected override void Update()
    {
        base.Update(); // Updates 'target' variable from Parent
        if (target == null || isBusy) return;

        // 1. Manage Cooldowns
        shootTimer -= Time.deltaTime;
        dashTimer -= Time.deltaTime;
        spawnTimer -= Time.deltaTime;

        float dist = Vector2.Distance(transform.position, target.position);

        // 2. AI Decision Logic
        
        // Priority A: Spawn Minions (Rare event)
        if (spawnTimer <= 0)
        {
            StartCoroutine(SpawnMinionsRoutine());
        }
        // Priority B: Dash (If player is close or just purely by timer)
        else if (dashTimer <= 0 && dist < 10f)
        {
            StartCoroutine(DashAttackRoutine());
        }
        // Priority C: Shoot (Frequent)
        else if (shootTimer <= 0 && dist < 15f)
        {
            StartCoroutine(ShootRoutine());
        }
        // Priority D: Movement (Idle / Chase)
        else
        {
            HandleMovement(dist);
        }
    }

    // --- MOVEMENT ---
    void HandleMovement(float dist)
    {
        // Face the player smoothly
        Vector2 dir = (target.position - transform.position).normalized;
        
        // Only move if we are far away (maintain distance)
        if (dist > stopDistance)
        {
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, dir * moveSpeed, Time.deltaTime * 2f);
        }
        else
        {
            // Slow down if close
            rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, Vector2.zero, Time.deltaTime * 2f);
        }

        // Flip Sprite based on X
        if (Mathf.Abs(dir.x) > 0.1f)
        {
            float scaleX = Mathf.Abs(transform.localScale.x) * (dir.x > 0 ? 1 : -1);
            transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);
        }
    }

    // --- ABILITY 1: SHOOT ---
    IEnumerator ShootRoutine()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero; // Stop to aim
        
        anim.SetTrigger("Attack"); // Reuse generic attack anim
        
        // Small delay to match animation
        yield return new WaitForSeconds(0.9f);

        if (projectilePrefab && firePoint)
        {
            // Use the ProjectileBase system we created!
            GameObject p = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Vector2 dir = (target.position - firePoint.position).normalized;
            p.GetComponent<ProjectileBase>().Launch(dir);
        }

        shootTimer = shootCooldown;
        yield return new WaitForSeconds(0.5f); // Recovery time
        isBusy = false;
    }

    // --- ABILITY 2: DASH ---
    IEnumerator DashAttackRoutine()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;

        // 1. Prepare
        anim.SetTrigger("Prepare"); 
        
        // Lock direction
        Vector2 dashDir = (target.position - transform.position).normalized;
        
        yield return new WaitForSeconds(dashWarningTime);

        // 2. Execute Dash (ENABLE DAMAGE HERE)
        isDashing = true; // <--- NOW he is dangerous
        
        anim.SetTrigger("Dash"); 
        rb.AddForce(dashDir * dashForce, ForceMode2D.Impulse);

        // 3. Wait for dash to finish
        yield return new WaitForSeconds(dashDuration);

        // 4. Stop and Cleanup (DISABLE DAMAGE HERE)
        rb.linearVelocity = Vector2.zero; 
        isDashing = false; // <--- NOW he is harmless again
        
        dashTimer = dashCooldown;
        isBusy = false;
    }

    // --- ABILITY 3: SPAWN MINIONS ---
    IEnumerator SpawnMinionsRoutine()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;
        
        anim.SetTrigger("Roar"); // Or "Prepare"

        yield return new WaitForSeconds(1f);

        for (int i = 0; i < minionCount; i++)
        {
            Instantiate(minionPrefab, transform.position + (Vector3)Random.insideUnitCircle, Quaternion.identity);
            yield return new WaitForSeconds(0.3f); // Stagger spawns
        }

        spawnTimer = spawnCooldown;
        isBusy = false;
    }


protected void OnCollisionEnter2D(Collision2D collision)
    {
        // Only deal damage if we are currently dashing
        if (!isDashing) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            var damageable = collision.gameObject.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Calculate Knockback direction
                Vector2 dir = (collision.transform.position - transform.position).normalized;
                
                // Deal the specific Dash Damage
                damageable.TakeDamage(dashDamageAmount, transform.position, dir);
                
                // Optional: Stop the dash on impact so he doesn't slide through the player
                // rb.linearVelocity = Vector2.zero;
                // isDashing = false; 
            }
        }
    }

    // --- DEATH EVENT ---

    
    // Remember to unsubscribe
protected override void OnDestroy()
{
    base.OnDestroy();
    if (stats != null)
        {
            stats.OnHealthChanged -= UpdateBossBar;
            stats.OnDeath -= OnBossDied;
            stats.OnDeath -= DropItem;
            stats.OnDeath -= UnlockExit;
        }
        
        // Si destruimos el objeto (escena cambia), ocultamos la barra por si acaso
        if (BossHealthUI.Instance != null) BossHealthUI.Instance.HideBar();
}

// New Method
void UnlockExit()
{
    if (exitPortal != null)
    {
        exitPortal.UnlockPortal();
    }
    else
    {
        Debug.LogWarning("Boss died but no Exit Portal was assigned in Inspector!");
    }
}

    void DropItem()
    {
        if (abilityItemPrefab != null)
        {
            Instantiate(abilityItemPrefab, transform.position, Quaternion.identity);
        }
    }

    void UpdateBossBar(int current, int max)
    {
        if (BossHealthUI.Instance != null)
        {
            BossHealthUI.Instance.UpdateHealth((float)current, (float)max);
        }
    }

    void OnBossDied()
    {
        if (BossHealthUI.Instance != null)
        {
            BossHealthUI.Instance.HideBar();
        }
    }
}