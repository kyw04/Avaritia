// 시전자의 현재 체력(절대값)이 대상보다 낮을 때. 대상이 없거나 Entity가 아니면 거짓.
[System.Serializable]
public class CasterHealthLowerCondition : IAbilityCondition
{
    public bool IsMet(AbilityContext context)
    {
        if (context.Caster is not Entity caster || context.Target == null) return false;
        if (!context.Target.TryGetComponent<Entity>(out var target)) return false;
        return caster.CurrentHealth < target.CurrentHealth;
    }
}
