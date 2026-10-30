// 체력보다 먼저 데미지를 흡수하는 실드를 amount로 (다시) 채운다. 누적되지 않고 리필.
[System.Serializable]
public class ShieldEffect : IAbilityEffect
{
    public float amount;
    // 실드가 남아 있는 동안 시전자를 따라다니는 파티클. 실드가 다 깎이거나 장착 해제되면 멈춘다.
    public PooledParticle particle;

    public void Apply(AbilityContext context)
    {
        if (context.Caster is not Entity entity) return;

        var source = context.State;
        entity.GrantShield(source, amount);
        ParticleManager.Instance.SpawnFollow(particle, entity.transform, 0f, source, () => entity.HasShield(source));
    }
}
