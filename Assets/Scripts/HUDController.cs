using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    public static HUDController Instance;

    [Header("Active Character (Large)")]
    [SerializeField] private Image bigPortrait;   
    [SerializeField] private Image healthBarFill; // Barra Grande Roja
    [SerializeField] private Image energyBarFill; // Barra Grande Energía

    [Header("Companion Character (Small)")]
    [SerializeField] private Image smallPortrait; 
    [SerializeField] private Image companionHealthBarFill; // NUEVO: Barra Pequeña Roja
    [SerializeField] private Image companionEnergyBarFill; // NUEVO: Barra Pequeña Energía
     [SerializeField] private Image companionBackground; 

    [Header("Resources")]
    [SerializeField] private Sprite warriorSprite;
    [SerializeField] private Sprite turtleSprite;
    [SerializeField] private Color warriorThemeColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color turtleThemeColor = new Color(0f, 0.8f, 0.2f);
    
    // Colores de estado
    private Color normalColor = Color.white;
    private Color deadColor = Color.gray;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateMode(bool isWarriorActive)
    {
        if (isWarriorActive)
        {
            // El Guerrero es el ACTIVO (Grande)
            bigPortrait.sprite = warriorSprite;
            energyBarFill.color = warriorThemeColor;

            // La Tortuga es el COMPAÑERO (Pequeño)
            smallPortrait.sprite = turtleSprite;
            companionEnergyBarFill.color = turtleThemeColor; 
        }
        else
        {
            // La Tortuga es el ACTIVO (Grande)
            bigPortrait.sprite = turtleSprite;
            energyBarFill.color = turtleThemeColor;

            // El Guerrero es el COMPAÑERO (Pequeño)
            smallPortrait.sprite = warriorSprite;
            companionEnergyBarFill.color = warriorThemeColor;
        }

        // Reseteamos colores por si estaban muertos visualmente
        bigPortrait.color = normalColor;
        smallPortrait.color = normalColor;
    }

    // --- ACTUALIZACIÓN JUGADOR ACTIVO ---
    public void UpdateActiveHealth(float pct)
    {
        if (healthBarFill) healthBarFill.fillAmount = pct;
    }
    public void UpdateActiveEnergy(float pct)
    {
        if (energyBarFill) energyBarFill.fillAmount = pct;
    }

    // --- ACTUALIZACIÓN COMPAÑERO (NUEVO) ---
    public void UpdateCompanionHealth(float pct)
    {
        if (companionHealthBarFill) companionHealthBarFill.fillAmount = pct;
    }
    public void UpdateCompanionEnergy(float pct)
    {
        if (companionEnergyBarFill) companionEnergyBarFill.fillAmount = pct;
    }

    // --- VISUALES DE MUERTE ---
    public void SetCharacterDead(bool isWarrior, bool isWarriorActive)
    {
        // Si muere el guerrero
        if (isWarrior)
        {
            // Si el guerrero es el activo, oscurecemos el grande, si no, el pequeño
            if (isWarriorActive) bigPortrait.color = deadColor;
            else smallPortrait.color = deadColor;
        }
        else // Murió la tortuga
        {
            if (!isWarriorActive) bigPortrait.color = deadColor;
            else smallPortrait.color = deadColor;
        }
    }

    public void SetAllAlive()
    {
        bigPortrait.color = normalColor;
        smallPortrait.color = normalColor;
    }

    public void SetCompanionVisible(bool visible)
    {
        // Asumiendo que smallPortraitFrame es el padre del smallPortrait
        // Si no tienes referencia al frame, arrástrala o usa smallPortrait.gameObject
        if (smallPortrait != null)
        {
            smallPortrait.gameObject.SetActive(visible);
            
            // También ocultar barras pequeñas
            if(companionHealthBarFill) companionHealthBarFill.transform.parent.gameObject.SetActive(visible);
            if(companionEnergyBarFill) companionEnergyBarFill.transform.parent.gameObject.SetActive(visible);
            if(companionEnergyBarFill) companionBackground.transform.parent.gameObject.SetActive(visible);
        }
    }
}