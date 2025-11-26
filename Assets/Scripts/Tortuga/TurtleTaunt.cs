using UnityEngine;

public class TurtleTaunt : MonoBehaviour
{
    [Header("Taunt")]
    public KeyCode tauntKey = KeyCode.E;
    public float tauntRadius = 6f;
    public float tauntDuration = 5f;
    public float tauntThreatBoost = 100f;   // sube threat a tope
    public float tauntCooldown = 14f;
    public LayerMask enemyLayer;

    [Header("Defensivo durante taunt")]
    public CharacterStats turtleHealth;
    public float tauntDamageMultiplier = 0.7f;   // 30% DR adicional
    public float tauntKnockbackScale = 0.5f;     // menos knockback
    public float visualPulseSeconds = 0.25f;     // tiempo de vfx inicial

    private float cooldownTimer;

        public bool isUnlocked = false;

    void Update()
    {
        if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(tauntKey) && cooldownTimer <= 0f && isUnlocked)
        {
            DoTaunt();
            cooldownTimer = tauntCooldown;
        }
    }

    void DoTaunt()
    {
        // VFX/SFX: aquí dispara tu pulso y sonido ritual
        // StartCoroutine(PulseVFX()); // opcional

        // Encontrar enemigos en radio
        var hits = Physics2D.OverlapCircleAll(transform.position, tauntRadius, enemyLayer);
        foreach (var h in hits)
        {
            var t = h.GetComponent<EnemyThreat>();
            if (t != null)
            {
                t.ApplyTaunt(transform, tauntDuration, tauntThreatBoost);
            }
        }

        // Buff defensivo temporal en la tortuga
        if (turtleHealth != null)
        {
            StartCoroutine(TemporaryDefenseBuff());
        }
    }

    System.Collections.IEnumerator TemporaryDefenseBuff()
    {
;
        // O puedes añadir banderas específicas; aquí usamos charging para reusar multiplicadores.
        float end = Time.time + tauntDuration;
        while (Time.time < end)
            yield return null;
   }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, tauntRadius);
    }

     public void UnlockTaunt()
    {
        isUnlocked = true;
    }

}
