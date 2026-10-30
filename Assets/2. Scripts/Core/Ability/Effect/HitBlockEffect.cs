// 다음 피격 count회를 완전히 무효화하는 횟수를 (다시) 채운다. 누적되지 않고 리필.
[System.Serializable]
public class HitBlockEffect : IAbilityEffect
{
    public int count = 1;
    // 무효화 횟수가 남아 있는 동안 시전자를 따라다니는 파티클. 다 쓰거나 장착 해제되면 멈춘다.
    public PooledParticle particle;

    public void Apply(AbilityContext context)
    {
        if (context.Caster is not Entity entity) return;

        var source = context.State;
        entity.GrantHitBlocks(source, count);
        ParticleManager.Instance.SpawnFollow(particle, entity.transform, 0f, source, () => entity.HasHitBlocks(source));
    }
}
