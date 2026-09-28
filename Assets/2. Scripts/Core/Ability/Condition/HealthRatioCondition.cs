// 시전자의 현재 체력 비율(0~100%)을 percent와 비교.
[System.Serializable]
public class HealthRatioCondition : IAbilityCondition
{
    public StatComparison comparison;
    public float percent;

    public bool IsMet(AbilityContext context)
    {
        if (context.Caster is not Entity entity || entity.MaxHealth <= 0f) return false;
        float current = entity.CurrentHealth / entity.MaxHealth * 100f;
        return comparison switch
        {
            StatComparison.LessThan => current < percent,
            StatComparison.LessOrEqual => current <= percent,
            StatComparison.GreaterThan => current > percent,
            StatComparison.GreaterOrEqual => current >= percent,
            _ => true,
        };
    }
}
