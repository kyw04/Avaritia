using UnityEngine;

// 시전자가 수평으로 이동하려 하지 않으면(이동 방향이 deadZone 이하) 통과.
// ChannelEffect.keepWhile에 넣으면 이동은 가능하지만 움직이는 순간 채널링이 끊긴다.
[System.Serializable]
public class StandStillCondition : IAbilityCondition
{
    public float deadZone = 0.2f;

    public bool IsMet(AbilityContext context) =>
        context.Caster?.Mono is not Entity caster || Mathf.Abs(caster.MoveDirection.x) <= deadZone;
}
