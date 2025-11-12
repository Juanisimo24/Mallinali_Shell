using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "MalinallisShell/EnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("Movimiento")]
    public float walkSpeed = 2.0f;
    public float chaseSpeed = 3.5f;

    [Header("Combate")]
    public int damage = 10;
    public float attackRange = 1.7f;
    public float attackCooldown = 1.2f;

    [Header("Percepción")]
    public float sightRange = 6f;
    public float sightAngle = 75f;        // cono de visión
    public float hearRange = 3.5f;        // “oído” opcional
    public LayerMask sightObstacles;      // paredes
}
