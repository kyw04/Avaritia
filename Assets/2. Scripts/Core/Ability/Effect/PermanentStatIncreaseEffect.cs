// float 스탯만 지원한다. Armor/DashCount처럼 int로 정의된 StatType에 쓰면
// AddBaseStat<float>가 타입 불일치로 Debug.LogError를 남기고 아무 것도 바꾸지 않는다.
[System.Serializable]
public class PermanentStatIncreaseEffect : IAbilityEffect
{
    public StatType statType;
    public float amount;

    public void Apply(AbilityContext context)
    {
        if (context.Caster is IStatMutable mutable)
            mutable.AddBaseStat(statType, amount);
    }
}
