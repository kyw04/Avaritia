public enum StatComparison { LessThan, LessOrEqual, GreaterThan, GreaterOrEqual }

[System.Serializable]
public class StatThresholdCondition : IAbilityCondition
{
    public StatType statType;
    public StatComparison comparison;
    public float value;

    public bool IsMet(AbilityContext context)
    {
        if (context.Caster is not IStatReadable readable) return true;
        float current = readable.GetStat<float>(statType);
        return comparison switch
        {
            StatComparison.LessThan => current < value,
            StatComparison.LessOrEqual => current <= value,
            StatComparison.GreaterThan => current > value,
            StatComparison.GreaterOrEqual => current >= value,
            _ => true,
        };
    }
}
