using System;

// owner의 체력이 0 이하가 된 직후, 죽기 전에 발동. 여기서 회복시키면 사망이 취소된다.
[Serializable]
public class OnLethalDamageTrigger : IAbilityTrigger, IObserver<EntityLethalDamageEvent>
{
    private Entity owner;
    private Action<AbilityContext> fire;

    public void Bind(Entity owner, AbilityData data, Action<AbilityContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        EventBus.Subscribe(this);
    }

    public void Unbind(Entity owner) => EventBus.Unsubscribe(this);

    public void OnNotify(EntityLethalDamageEvent e)
    {
        if (e.Source != owner) return;
        fire(new AbilityContext { Caster = owner, Target = owner.transform });
    }
}
