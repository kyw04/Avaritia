using System;

// 바인딩(장착)되는 즉시 발동. 아이템/스킬/무기가 어떤 경로(줍기, 교체 등)로 장착되든
// AbilityManager가 바인딩하는 시점이 곧 장착 시점이므로 별도 장착 이벤트에 의존하지 않는다.
[Serializable]
public class OnEquippedTrigger : IAbilityTrigger
{
    public void Bind(Entity owner, AbilityData data, Action<AbilityContext> fire) =>
        fire(new AbilityContext { Caster = owner, Target = owner.transform });

    public void Unbind(Entity owner) { }
}
