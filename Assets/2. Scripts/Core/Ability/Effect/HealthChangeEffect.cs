using UnityEngine;

public enum HealthChangeMode { Heal, Damage }

[System.Serializable]
public class HealthChangeEffect : IAbilityEffect
{
    public HealthChangeMode mode;
    public float amount;
    public bool percentOfMaxHealth; // true면 amount를 대상 최대 체력의 %로 해석 (30 = 30%)
    public BuffTarget applyTo = BuffTarget.Self;

    public void Apply(AbilityContext context)
    {
        Transform t = applyTo == BuffTarget.Self ? context.Caster.Mono.transform : context.Target;
        if (t == null || !t.TryGetComponent<IDamageable>(out var damageable)) return;

        float value = amount;
        if (percentOfMaxHealth)
            value = t.TryGetComponent<Entity>(out var entity) ? entity.MaxHealth * amount / 100f : 0f;

        if (mode == HealthChangeMode.Heal) damageable.Heal(value);
        else damageable.TakeDamage(value);
    }
}
