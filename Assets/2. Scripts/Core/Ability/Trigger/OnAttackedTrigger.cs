using System;

// 공격자를 알 수 있는 공격(근접)으로 owner가 실제 데미지를 입었을 때 발동.
// OnDamageTakenTrigger(체력 감소 기반)와 달리 Target = 공격자, Value = 받은 데미지.
[Serializable]
public class OnAttackedTrigger : IAbilityTrigger, IObserver<EntityDamagedEvent>
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
        if (e.Source != owner || e.Attacker == null) return;
        fire(new AbilityContext { Caster = owner, Target = e.Attacker.transform, EventSource = e.Attacker, Value = e.Amount });
    }
}
