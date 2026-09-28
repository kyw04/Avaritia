// 체력보다 먼저 데미지를 흡수하는 실드를 amount로 (다시) 채운다. 누적되지 않고 리필.
[System.Serializable]
public class ShieldEffect : IAbilityEffect
{
    public float amount;

    public void Apply(AbilityContext context)
    {
        if (context.Caster is Entity entity)
            entity.GrantShield(context.State, amount);
    }
}
