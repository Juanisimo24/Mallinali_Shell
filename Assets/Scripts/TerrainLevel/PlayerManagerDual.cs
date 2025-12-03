using UnityEngine;
using Unity.Cinemachine;
using Pathfinding;

public class PlayerManagerDual : MonoBehaviour
{
    public static PlayerManagerDual Instance;

    [Header("Referencias Globales")]
    public CinemachineCamera camera; 
    public GameObject warrior;
    public GameObject turtle; // ARRASTRA LA TORTUGA AQUÍ SIEMPRE
    public HUDController hudController;
    public SpecialFloor floorsito;

    [Header("Estado de Progreso")]
    [Tooltip("Marca esto si quieres empezar ya con la tortuga (para pruebas). En el juego real, déjalo desmarcado.")]
    public bool startWithTurtleUnlocked = false; 
    private bool isTurtleUnlocked = false; 

    // Estados de Vida
    private bool isWarriorAlive = true;
    private bool isTurtleAlive = true;

    [Header("Configuración")]
    public KeyCode switchKey = KeyCode.Tab;
    public int hitsRequired = 3;
    private int currentComboCount = 0;

    // Estado Interno
    private GameObject active;
    private GameObject follower;
    private bool underWater;
    
    // Stats Eventos
    private CharacterStats currentActiveStats; 
    private CharacterStats currentFollowerStats; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (hudController == null) hudController = FindFirstObjectByType<HUDController>();

        // Suscribir eventos de muerte
        if (warrior) warrior.GetComponent<CharacterStats>().OnDeath += () => HandlePlayerDeath(warrior);
        if (turtle) turtle.GetComponent<CharacterStats>().OnDeath += () => HandlePlayerDeath(turtle);

        // CONFIGURACIÓN INICIAL
        isTurtleUnlocked = startWithTurtleUnlocked;

        if (!isTurtleUnlocked)
        {
            // MODO SOLO (Zona 1)
            // Aseguramos que la tortuga no se mueva ni haga nada
            DisableTurtleLogic();
            
            // Ocultamos la parte del HUD del compañero (Opcional, si tu HUD lo soporta)
             if(hudController) hudController.SetCompanionVisible(false); // Necesitas añadir esto al HUD si quieres
        }
        else
        {
            // Si empezamos desbloqueados (Debug o Load Game)
             if(hudController) hudController.SetCompanionVisible(true);
        }

        // Arrancar siempre con el Guerrero
        SetActive(warrior); 
    }

    void Update()
    {
        if (floorsito != null) underWater = floorsito.getUnderWater();
        
        // SOLO CAMBIAMOS SI LA TORTUGA ESTÁ DESBLOQUEADA
        if (isTurtleUnlocked && Input.GetKeyDown(switchKey)) 
        {
            AttemptSwitch();
        }
    }

    // --- MÉTODO PÚBLICO PARA DESBLOQUEAR A LA TORTUGA (Llamar en Zona 2) ---
    public void UnlockTurtle()
    {
        if (isTurtleUnlocked) return; // Ya estaba desbloqueada

        Debug.Log("¡TORTUGA DESBLOQUEADA! Modo Dual Activado.");
        isTurtleUnlocked = true;

        // 1. Reactivar la tortuga (si estaba desactivada)
        turtle.SetActive(true);

        // 2. Hacerla follower inmediatamente
        follower = turtle;

        // 3. Activar su IA para que empiece a seguirte
        ToggleAI(follower, true, active.transform);

        // 4. Actualizar HUD para mostrar la cara y vida de la tortuga
        if(hudController) 
        {
            hudController.SetCompanionVisible(true);
            // Forzamos actualización visual
            SetActive(active); 
        }
    }

    // Ayuda para apagar la tortuga al inicio
    void DisableTurtleLogic()
    {
        if (turtle == null) return;
        
        // Desactivar controles e IA
        ToggleCharacterControl(turtle, false);
        ToggleAI(turtle, false);
        
        // Opcional: Si la tortuga está "esperando" en la Zona 2, no la desactives (SetActive false).
        // Si la tortuga "aparece mágicamente", entonces sí:
        // turtle.SetActive(false); 
    }

    // --- LOGICA DE COMBO (Solo si está desbloqueada) ---
    public void RegisterHit()
    {
        if (!isTurtleUnlocked) return; // No hay combo en Zona 1

        currentComboCount++;
        if (currentComboCount >= hitsRequired)
        {
            TriggerFollowerAttack();
            currentComboCount = 0; 
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
        // 1. Update Internal State
        if (deadCharacter == warrior) isWarriorAlive = false;
        else isTurtleAlive = false;

        // 2. Update HUD Visuals (Gray out the face)
        bool isWarriorActive = (active == warrior);
        if (hudController) hudController.SetCharacterDead(deadCharacter == warrior, isWarriorActive);

        // -----------------------------------------------------------------
        //                DECISION LOGIC: GAME OVER vs SWITCH
        // -----------------------------------------------------------------

        // CASE A: We are in Zone 1 (Solo Mode)
        if (!isTurtleUnlocked)
        {
            // If the only character we have dies -> Game Over
            Debug.Log("Solo Mode Death.");
            GameOver();
            return;
        }

        // CASE B: The ACTIVE character died (The one the camera is following)
        if (active == deadCharacter)
        {
            // We need to check if the PARTNER is alive to switch the camera to them
            GameObject survivor = null;

            if (deadCharacter == warrior && isTurtleAlive)
            {
                survivor = turtle;
            }
            else if (deadCharacter == turtle && isWarriorAlive)
            {
                survivor = warrior;
            }

            if (survivor != null)
            {
                // --- SWITCH CAMERA AND CONTROL ---
                Debug.Log($"Active character died! Switching control to {survivor.name}");
                
                // This function already sets camera.Follow/LookAt to the new target
                SwitchTo(survivor); 
            }
            else
            {
                // --- EVERYONE IS DEAD ---
                Debug.Log("Both characters are dead.");
                GameOver();
            }
        }
        // CASE C: The FOLLOWER died (The one behind you)
        else 
        {
            // The camera is already on the Survivor (Active), so we don't move it.
            // We just update the state (which we did in step 1 & 2).
            Debug.Log("Your companion has fallen!");
            
            // Optional: You could play a specific sound or UI flash here
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
        UnsubscribeEvents();

        active = newActive;
        
        // --- LOGIC FOR DUAL MODE VS SOLO MODE ---
        if (isTurtleAlive)
        {
            if (active == warrior) follower = turtle;
            else follower = warrior;

            // --- TAG SWITCHING (THE MAGIC) ---
            // The Active character becomes "Player" (Targetable)
            active.tag = "Player";
            
            // The Follower becomes "Follower" (Ignored by Enemies)
            if (follower != null) 
            {
                follower.tag = "Follower";
                
                // Optional: Make follower ignored by physics layer to prevent blocking the player?
                // active.layer = LayerMask.NameToLayer("Player");
                // follower.layer = LayerMask.NameToLayer("Follower"); // If you have a Follower layer
            }
        }
        else
        {
            follower = null; 
            // In Solo mode, ensure active is Player
            active.tag = "Player";
        }
        // ----------------------------------------

        // CONTROL AND AI
        ConfigureControlsAndAI();

        // HUD UPDATES
        if (hudController != null)
        {
            bool isWarriorActive = (active == warrior);
            hudController.UpdateMode(isWarriorActive);
            
            if (!isWarriorAlive) hudController.SetCharacterDead(true, isWarriorActive);
            if (isTurtleAlive && !isTurtleAlive) hudController.SetCharacterDead(false, isWarriorActive);

            ConnectStatsToHUD();
        }
    }

    void ConfigureControlsAndAI()
    {
        ToggleCharacterControl(active, true);
        ToggleAI(active, false);

        // Solo activar IA del follower si existe y está desbloqueado
        if (follower != null && isTurtleUnlocked)
        {
            ToggleCharacterControl(follower, false);
            if (!underWater) ToggleAI(follower, true, active.transform);
            else ToggleAI(follower, false);
                camera.Follow = active.transform;
                camera.LookAt = active.transform;
        }
    }

void ConnectStatsToHUD()
    {
        currentActiveStats = active.GetComponent<CharacterStats>();
        if (currentActiveStats != null)
        {
            hudController.UpdateActiveHealth(currentActiveStats.GetCurrentHealthPct());
            hudController.UpdateActiveEnergy(currentActiveStats.GetCurrentEnergyPct());
            currentActiveStats.OnHealthChanged += OnActiveHealthChanged;
            currentActiveStats.OnEnergyChanged += OnActiveEnergyChanged;
        }

        if (follower != null)
        {
            currentFollowerStats = follower.GetComponent<CharacterStats>();
            if (currentFollowerStats != null)
            {
                hudController.UpdateCompanionHealth(currentFollowerStats.GetCurrentHealthPct());
                hudController.UpdateCompanionEnergy(currentFollowerStats.GetCurrentEnergyPct());
                currentFollowerStats.OnHealthChanged += OnFollowerHealthChanged;
                currentFollowerStats.OnEnergyChanged += OnFollowerEnergyChanged;
            }
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
   

    void AttemptSwitch()
    {
        if (!isTurtleUnlocked) return; // Bloqueo extra

        if (active == warrior)
        {
            if (isTurtleAlive) SwitchTo(turtle);
        }
        else
        {
            if (isWarriorAlive) SwitchTo(warrior);
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