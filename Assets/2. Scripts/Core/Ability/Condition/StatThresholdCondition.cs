public enum StatComparison { LessThan, LessOrEqual, GreaterThan, GreaterOrEqual }

// float 스탯만 지원한다. Armor/DashCount처럼 int로 정의된 StatType에 쓰면
// GetStat<float>가 타입 불일치로 Debug.LogError 후 0을 반환해 조건이 조용히 항상 거짓이 된다.
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
