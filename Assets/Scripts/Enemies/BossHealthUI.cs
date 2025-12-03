using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthUI : MonoBehaviour
{
    public static BossHealthUI Instance;

    [Header("UI References")]
    [SerializeField] private GameObject visualsParent; // El objeto padre para ocultar todo
    [SerializeField] private Image healthBarFill;      // La barra roja
    [SerializeField] private TextMeshProUGUI bossNameText; // El nombre del jefe

    private void Awake()
    {
        // Singleton simple
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Empezamos ocultos
        HideBar();
    }

    public void ShowBar(string name)
    {
        if (bossNameText) bossNameText.text = name;
        healthBarFill.fillAmount = 1f; // Vida llena al inicio
        visualsParent.SetActive(true);
    }

    public void HideBar()
    {
        visualsParent.SetActive(false);
    }

    public void UpdateHealth(float current, float max)
    {
        if (visualsParent.activeSelf)
        {
            healthBarFill.fillAmount = current / max;
        }
    }
}