using UnityEngine;

public enum HealthChangeMode { Heal, Damage }

[System.Serializable]
public class HealthChangeEffect : IAbilityEffect
{
    public HealthChangeMode mode;
    public float amount;
    public BuffTarget applyTo = BuffTarget.Self;

    public void Apply(AbilityContext context)
    {
        Transform t = applyTo == BuffTarget.Self ? context.Caster.Mono.transform : context.Target;
        if (t == null || !t.TryGetComponent<IDamageable>(out var damageable)) return;

        if (mode == HealthChangeMode.Heal) damageable.Heal(amount);
        else damageable.TakeDamage(amount);
    }
}
