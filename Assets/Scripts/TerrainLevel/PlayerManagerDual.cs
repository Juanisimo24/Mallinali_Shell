using UnityEngine;
using Unity.Cinemachine;
using Pathfinding;

public class PlayerManagerDual : MonoBehaviour
{
    // --- Singleton Instance (Easy access) ---
    public static PlayerManagerDual Instance;

    [Header("Configuration")]
    public CinemachineCamera camera; // Updated for Unity 6

    public GameObject warrior;
    public GameObject turtle;
private bool isWarriorAlive = true;
    private bool isTurtleAlive = true;

    public SpecialFloor floorsito;
    public KeyCode switchKey = KeyCode.Tab;

    [Header("Combo State")]
    public int hitsRequired = 3;
    private int currentComboCount = 0;

    public HUDController hudController;

    // State Tracking
    private GameObject active;
    private GameObject follower;
    private bool underWater;
    private CharacterStats currentActiveStats; 
    private CharacterStats currentFollowerStats; 

    

    void Awake()
    {
        // Setup Singleton
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (hudController == null) 
            hudController = FindFirstObjectByType<HUDController>();

        var wStats = warrior.GetComponent<CharacterStats>();
        var tStats = turtle.GetComponent<CharacterStats>();
        if (wStats) wStats.OnDeath += () => HandlePlayerDeath(warrior);
        if (tStats) tStats.OnDeath += () => HandlePlayerDeath(turtle);

        // Start Game
        SetActive(warrior); 
    }

    void Update()
    {
        if (floorsito != null) underWater = floorsito.getUnderWater();
        if (Input.GetKeyDown(switchKey)) AttemptSwitch();
    }

    // --- LOGIC FOR COMBO ---
    public void RegisterHit()
    {
        currentComboCount++;
        Debug.Log($"Combo: {currentComboCount}/{hitsRequired}");

        if (currentComboCount >= hitsRequired)
        {
            TriggerFollowerAttack();
            currentComboCount = 0; // Reset
        }
    }

    private void TriggerFollowerAttack()
    {
        if (follower != null)
        {
            // We look for PlayerCombat OR the specific Turtle/Warrior combat script on the follower
            var combat = follower.GetComponent<PlayerCombat>();
            
            if (combat != null)
            {
                Debug.Log($"{follower.name} is performing a Combo Assist!");
                // Make sure the Follower uses a bigger range or "Super" logic
                combat.PerformComboAttack(); 
            }
        }
    }
    // -----------------------

    void Switch()
    {
        // Reset combo when switching characters (optional, but recommended)
        currentComboCount = 0; 

        if (active == warrior)
        {
            SetActive(turtle);
            if(isTurtleAlive)
            CambiarTarget(turtle.transform);
        }
        else
        {
            SetActive(warrior);
            if(isWarriorAlive)
            CambiarTarget(warrior.transform);
        }
    }

    void CambiarTarget(Transform nuevoObjetivo)
    {
        camera.Follow = nuevoObjetivo;
        camera.LookAt = nuevoObjetivo;
    }

     // MANEJO DE MUERTE
    void HandlePlayerDeath(GameObject deadCharacter)
    {
        if (deadCharacter == warrior)
        {
            isWarriorAlive = false;
            if (hudController) hudController.SetCharacterDead(true,false); // true = guerrero (ejemplo)
        }
        else
        {
            isTurtleAlive = false;
            if (hudController) hudController.SetCharacterDead(false,false); // false = tortuga
        }

        // CASO 1: Murió el personaje que estamos controlando (Activo)
        if (active == deadCharacter)
        {
            // Verificamos si el OTRO sigue vivo para cambiar forzosamente
            if (deadCharacter == warrior && isTurtleAlive)
            {
                Debug.Log("Guerrero murió. Cambiando a Tortuga...");
                Switch(); // Forzamos el cambio
            }
            else if (deadCharacter == turtle && isWarriorAlive)
            {
                Debug.Log("Tortuga murió. Cambiando a Guerrero...");
                Switch(); // Forzamos el cambio
            }
            else
            {
                // CASO CRÍTICO: Ambos están muertos
                GameOver();
            }
        }
        // CASO 2: Murió el seguidor (follower)
        else 
        {
            // Simplemente desactivamos su IA o visuales (CharacterStats ya hizo SetActive(false))
            // Aquí podrías mostrar un mensaje de "Compañero abatido"
            Debug.Log("Tu compañero ha caído.");
        }
    }

    // MÉTODO PARA REVIVIR (Llama a esto desde un Altar, Item o Checkpoint)
    public void ReviveCharacter(bool reviveWarrior)
    {
        if (reviveWarrior && !isWarriorAlive)
        {
            isWarriorAlive = true;
            warrior.GetComponent<CharacterStats>().Revive(); // Restaura vida y activa GameObject
            
            // Si el jugador actual es la tortuga, ahora el guerrero vuelve a aparecer como follower
            if (active == turtle)
            {
                // Reactivar lógica de follower del guerrero si es necesario
                 ToggleAI(warrior, true, turtle.transform); 
            }
            if (hudController) hudController.SetAllAlive();
        }
        else if (!reviveWarrior && !isTurtleAlive)
        {
            isTurtleAlive = true;
            turtle.GetComponent<CharacterStats>().Revive();
            
            if (active == warrior)
            {
                // Reactivar lógica de follower de la tortuga
                 ToggleAI(turtle, true, warrior.transform);
            }
            if (hudController) hudController.SetAllAlive();
        }
    }

    void GameOver()
    {
        Debug.Log("GAME OVER - Ambos han muerto");
        Time.timeScale = 0;
        // Mostrar pantalla de Game Over
    }

    void SetActive(GameObject newActive)
    {
           // 1. Limpieza de eventos anteriores (Vital para evitar errores)
        UnsubscribeEvents();

        // 2. Asignación de Roles
        active = newActive;
        if (active == warrior) follower = turtle;
        else follower = warrior;

        // 3. Control y AI
        ConfigureControlsAndAI();

        // 4. Configuración del HUD y Eventos
        if (hudController != null)
        {
            bool isWarriorActive = (active == warrior);
            
            // A. Cambiar las caras y colores
            hudController.UpdateMode(isWarriorActive);
            
            // B. Verificar estado de muerte visual
            if (!isWarriorAlive) hudController.SetCharacterDead(true, isWarriorActive);
            if (!isTurtleAlive) hudController.SetCharacterDead(false, isWarriorActive);

            // C. Conectar las barras de vida/energía
            ConnectStatsToHUD();
        }
    }

 void ConnectStatsToHUD()
    {
        // --- JUGADOR ACTIVO ---
        currentActiveStats = active.GetComponent<CharacterStats>();
        if (currentActiveStats != null)
        {
            // Actualizar ya mismo
            hudController.UpdateActiveHealth(currentActiveStats.GetCurrentHealthPct());
            hudController.UpdateActiveEnergy(currentActiveStats.GetCurrentEnergyPct());

            // Suscribir eventos
            currentActiveStats.OnHealthChanged += OnActiveHealthChanged;
            currentActiveStats.OnEnergyChanged += OnActiveEnergyChanged;
        }

        // --- COMPAÑERO ---
        currentFollowerStats = follower.GetComponent<CharacterStats>();
        if (currentFollowerStats != null)
        {
            // Actualizar ya mismo
            hudController.UpdateCompanionHealth(currentFollowerStats.GetCurrentHealthPct());
            hudController.UpdateCompanionEnergy(currentFollowerStats.GetCurrentEnergyPct());

            // Suscribir eventos
            currentFollowerStats.OnHealthChanged += OnFollowerHealthChanged;
            currentFollowerStats.OnEnergyChanged += OnFollowerEnergyChanged;
        }
    }

    void UnsubscribeEvents()
    {
        if (currentActiveStats != null)
        {
            currentActiveStats.OnHealthChanged -= OnActiveHealthChanged;
            currentActiveStats.OnEnergyChanged -= OnActiveEnergyChanged;
        }
        if (currentFollowerStats != null)
        {
            currentFollowerStats.OnHealthChanged -= OnFollowerHealthChanged;
            currentFollowerStats.OnEnergyChanged -= OnFollowerEnergyChanged;
        }
    }

    // --- HANDLERS PARA EL HUD (Puente entre Stats y UI) ---
    // Estos métodos existen para diferenciar quién envía el evento
    void OnActiveHealthChanged(int cur, int max) => hudController.UpdateActiveHealth((float)cur / max);
    void OnActiveEnergyChanged(float cur, float max) => hudController.UpdateActiveEnergy(cur / max);
    
    void OnFollowerHealthChanged(int cur, int max) => hudController.UpdateCompanionHealth((float)cur / max);
    void OnFollowerEnergyChanged(float cur, float max) => hudController.UpdateCompanionEnergy(cur / max);


    // ---------------------------------------------------------
    //                 LÓGICA DE JUEGO
    // ---------------------------------------------------------
    void ConfigureControlsAndAI()
    {
        // Activar control del jugador
        ToggleCharacterControl(active, true);
        ToggleAI(active, false);

        // Activar IA del compañero
        ToggleCharacterControl(follower, false);
        if (!underWater) ToggleAI(follower, true, active.transform);
        else ToggleAI(follower, false);

        // Cámara
        camera.Follow = active.transform;
        camera.LookAt = active.transform;
    }

    void AttemptSwitch()
    {
        if (active == warrior)
        {
            if (isTurtleAlive) SwitchTo(turtle);
            else Debug.Log("¡La Tortuga está abatida!");
        }
        else
        {
            if (isWarriorAlive) SwitchTo(warrior);
            else Debug.Log("¡El Guerrero está abatido!");
        }
    }
        void SwitchTo(GameObject target)
    {
        currentComboCount = 0; // Resetear combo al cambiar
        SetActive(target);
    }
    

     void ToggleCharacterControl(GameObject charObj, bool isPlayerControlled)
    {
        var pCtrl = charObj.GetComponent<PlayerController>();
        var abilitiesCtrl = charObj.GetComponent<AbilityHolder>();
        var tCtrl = charObj.GetComponent<TortugaController>();
        if (pCtrl) pCtrl.enabled = isPlayerControlled;
        if (abilitiesCtrl) abilitiesCtrl.enabled = isPlayerControlled;
        if (tCtrl) tCtrl.enabled = isPlayerControlled;
    }

    void ToggleAI(GameObject charObj, bool enableAI, Transform target = null)
    {
        var companionAI = charObj.GetComponent<CompanionAStar2D>();
        var seeker = charObj.GetComponent<Seeker>();
        if (companionAI) 
        {
            companionAI.enabled = enableAI;
            if (enableAI && target != null) companionAI.target = target;
        }
        if (seeker) seeker.enabled = enableAI;
    }

         public GameObject GetActive()
    {
        return active;
    }
        public GameObject GetFollower()
    {
        return follower;
    }
}