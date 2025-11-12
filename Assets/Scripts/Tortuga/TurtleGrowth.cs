using UnityEngine;

public class TurtleGrowth : MonoBehaviour
{
    [Header("Escalado")]
    public float baseScale = 1f;
    public float growthPerKill = 0.1f;
    public float maxScale = 2.0f;
    public float growthLerpSpeed = 6f;

    [Header("Ajustes de stats (opcionales)")]
    public float extraMaxHealthPerKill = 5f;
    public float extraDamagePerKill = 1f;

    private float _targetScale;
    private int _kills;
    private Vector3 _initialScale;

    // Referencias opcionales (si las tienes en tu proyecto)
    public TurtleHealth turtleHealth;    // Si manejas vida propia
    public TurtleTaunt turtleTaunt;      // Por si quieres mejorar algo al crecer

    private void Awake()
    {
        _initialScale = transform.localScale;
        _targetScale = baseScale;
        if (turtleHealth == null) turtleHealth = GetComponent<TurtleHealth>();
        if (turtleTaunt == null) turtleTaunt = GetComponent<TurtleTaunt>();
    }

    private void Update()
    {
        // Interpolar suavemente hacia el tamaño objetivo
        float current = transform.localScale.x;
        float next = Mathf.Lerp(current, _targetScale, Time.deltaTime * growthLerpSpeed);
        transform.localScale = new Vector3(next, next, 1f);
    }

    public void GainGrowth()
    {
        _kills++;
        _targetScale = Mathf.Min(baseScale + growthPerKill * _kills, maxScale);

        if (turtleHealth != null)
        {
            turtleHealth.maxHealth += (int)extraMaxHealthPerKill;
            turtleHealth.current = Mathf.Min(turtleHealth.current + (int)extraMaxHealthPerKill, turtleHealth.maxHealth);
        }

        // Si tu daño depende de algún componente, puedes incrementar aquí.
        // Por ejemplo, si la embestida (Shell Charge) usa daño base configurable.
        // (Deja comentado si no lo necesitas)
        // var charge = GetComponent<TurtleCharge>();
        // if (charge) charge.baseDamage += (int)extraDamagePerKill;
    }
}