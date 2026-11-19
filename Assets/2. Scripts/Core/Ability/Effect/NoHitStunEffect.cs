// duration초 동안 맞아도 피격 경직(피격 모션 + 행동 차단)에 걸리지 않아, 하던 공격을 이어갈 수 있다.
// 넉백은 그대로 받는다. duration 0(기본값) = 만료되지 않음(패시브용).
[System.Serializable]
public class NoHitStunEffect : IAbilityEffect
{
    public float duration;

    public void Apply(AbilityContext context)
    {
        if (context.Caster is Entity entity)
            entity.SetNoHitStun(duration, context.State?.Icon);
    }
}
