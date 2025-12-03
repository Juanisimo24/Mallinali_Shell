using UnityEngine;

public class AbilityHolder : MonoBehaviour
{
    [Header("Configuration")]
    public Ability ability; 
    public KeyCode activationKey = KeyCode.LeftControl; // Default Dash key

    private float cooldownTimer;
    private CharacterStats stats;
    
    // Auto-detect controllers
    private PlayerController warriorCtrl;
    private TortugaController turtleCtrl;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        warriorCtrl = GetComponent<PlayerController>();
        turtleCtrl = GetComponent<TortugaController>();
    }

    void Update()
    {
        if (cooldownTimer > 0) cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(activationKey))
        {
            TryActivateAbility();
        }
    }

    void TryActivateAbility()
    {
        if (ability == null || cooldownTimer > 0) return;
        if (stats == null) return;

        // 1. Consume Essence
        if (stats.ConsumeEnergy(ability.energyCost))
        {
            // 2. Lock Movement
            SetControllerOverride(true);

            // 3. Activate Ability
            // We expect the ability to return 'true' if it started
            // Ideally, we pass a callback to unlock movement when done
            bool started = ability.Activate(gameObject, stats);

            if (started)
            {
                cooldownTimer = ability.cooldownTime;
                // We need a way to know when the ability ENDS to unlock movement.
                // The cleanest way in this simple system is to let the Ability Coroutine unlock it.
                // But since ScriptableObjects can't easily reference the specific instance logic:
                
                // Hack/Fix: We start a coroutine here to wait for duration
                // Assuming Ability has a duration public field (we added it in previous step)
                StartCoroutine(UnlockMovementAfterDelay(ability is DashAbility ? ((DashAbility)ability).dashDuration : 0.5f));
            }
            else
            {
                stats.RestoreEnergy(ability.energyCost); // Refund
                SetControllerOverride(false);
            }
        }
        else
        {
            Debug.Log("Not enough Soul Essence!");
        }
    }

    System.Collections.IEnumerator UnlockMovementAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetControllerOverride(false);
    }

    void SetControllerOverride(bool active)
    {
        if (warriorCtrl != null) warriorCtrl.SetAbilityOverride(active);
        if (turtleCtrl != null) turtleCtrl.SetAbilityOverride(active);
    }
}