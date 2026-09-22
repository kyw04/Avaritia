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
