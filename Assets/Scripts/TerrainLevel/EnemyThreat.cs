using System.Collections.Generic;
using UnityEngine;

public class EnemyThreat : MonoBehaviour
{
    [Header("Taunt")]
    public bool isTaunted;
    public Transform tauntTarget;
    public float tauntExpireTime;
    public float maxTauntDuration = 6f; // cap total por balance

    // Threat table: objetivo → valor
    private Dictionary<Transform, float> threat = new Dictionary<Transform, float>();

    // Config
    public float threatDecayPerSec = 2f;
    public float damageToThreatFactor = 1f; // 1 daño = 1 threat (ajustable)

    void Update()
    {
        // Decaimiento pasivo
        if (threat.Count > 0)
        {
            float dt = Time.deltaTime;
            var keys = new List<Transform>(threat.Keys);
            foreach (var k in keys)
            {
                threat[k] = Mathf.Max(0f, threat[k] - threatDecayPerSec * dt);
            }
        }

        // Expiración del taunt
        if (isTaunted && Time.time > tauntExpireTime)
        {
            isTaunted = false;
            tauntTarget = null;
        }
    }

    public void AddThreat(Transform source, float amount)
    {
        if (source == null || amount <= 0f) return;
        if (!threat.ContainsKey(source)) threat[source] = 0f;
        threat[source] += amount;
    }

    public void OnDamaged(Transform attacker, int damage)
    {
        AddThreat(attacker, damage * damageToThreatFactor);
    }

    public void ApplyTaunt(Transform source, float duration, float threatBoost)
    {
        if (source == null) return;

        // Forzar target y duración con cap
        isTaunted = true;
        tauntTarget = source;
        tauntExpireTime = Time.time + Mathf.Min(duration, maxTauntDuration);

        // Subir threat del taunter al tope
        AddThreat(source, threatBoost);
    }

    public Transform GetPreferredTarget(Transform fallbackTarget = null)
    {
        // Si está taunted y el target sigue vivo / no null → forzar
        if (isTaunted && tauntTarget != null) return tauntTarget;

        // Si no hay threat, fallback (p. ej., jugador cercano)
        if (threat.Count == 0) return fallbackTarget;

        // Mayor threat
        Transform best = null;
        float bestValue = -1f;
        foreach (var kv in threat)
        {
            if (kv.Value > bestValue)
            {
                bestValue = kv.Value;
                best = kv.Key;
            }
        }
        return best != null ? best : fallbackTarget;
    }

    // Útil si quieres limpiar threat al morir el target:
    public void ClearTarget(Transform target)
    {
        if (target == null) return;
        if (threat.ContainsKey(target)) threat.Remove(target);
        if (tauntTarget == target)
        {
            isTaunted = false;
            tauntTarget = null;
        }
    }
}
