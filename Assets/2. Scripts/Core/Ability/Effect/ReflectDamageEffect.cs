using UnityEngine;

// context.Value(받은 데미지)의 percent%를 context.Target(공격자)에게 돌려준다.
// attacker를 넘기지 않으므로 반사 데미지가 다시 반사되지 않는다.
[System.Serializable]
public class ReflectDamageEffect : IAbilityEffect
{
    public float percent;

    public void Apply(AbilityContext context)
    {
        if (context.Target == null || !context.Target.TryGetComponent<IDamageable>(out var damageable)) return;
        damageable.TakeDamage(context.Value * percent / 100f);
    }
}
