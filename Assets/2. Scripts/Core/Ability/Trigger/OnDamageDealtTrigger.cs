using System;

// owner가 누군가에게 실제로 데미지를 입혔을 때 발동 (attacker가 전달되는 근접 공격만 해당).
// Target = 맞은 대상, Value = 들어간 데미지.
[Serializable]
public class OnDamageDealtTrigger : IAbilityTrigger, IObserver<EntityDamagedEvent>
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

    public void OnNotify(EntityDamagedEvent e)
    {
        if (e.Attacker != owner || e.Source == owner) return;
        fire(new AbilityContext { Caster = owner, Target = e.Source.transform, EventSource = e.Source, Value = e.Amount });
    }
}
