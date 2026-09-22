[System.Serializable]
public class TargetCondition : IAbilityCondition
{
    public bool requireDamageable;
    public bool requireDifferentFromCaster;

    public bool IsMet(AbilityContext context)
    {
        if (context.Target == null) return false;
        if (requireDamageable && !context.Target.TryGetComponent<IDamageable>(out _)) return false;
        if (requireDifferentFromCaster && context.Target == context.Caster?.Mono.transform) return false;
        return true;
    }
}
