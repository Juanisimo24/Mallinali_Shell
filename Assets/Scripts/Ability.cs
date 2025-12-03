using UnityEngine;

public abstract class Ability : ScriptableObject
{
    [Header("Basic Info")]
    public string abilityName;
    public float energyCost = 10f;
    public float cooldownTime = 1f;

    // We pass the GameObject (parent) so the ability knows WHO is using it
    // We return 'true' if the ability successfully activated
    public abstract bool Activate(GameObject parent, CharacterStats stats);
}