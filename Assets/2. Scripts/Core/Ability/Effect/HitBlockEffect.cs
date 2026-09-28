// 다음 피격 count회를 완전히 무효화하는 횟수를 (다시) 채운다. 누적되지 않고 리필.
[System.Serializable]
public class HitBlockEffect : IAbilityEffect
{
    public int count = 1;

    public void Apply(AbilityContext context)
    {
        if (context.Caster is Entity entity)
            entity.GrantHitBlocks(context.State, count);
    }
}
