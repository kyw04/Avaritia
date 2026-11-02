using System;

[System.Serializable]
public class OnKillTrigger : IAbilityTrigger, IObserver<EntityDeadEvent>
{
    public bool requireOwnerKill;
    private Entity owner;
    private Action<AbilityContext> fire;

    public void Bind(Entity owner, AbilityData data, Action<AbilityContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        EventBus.Subscribe(this);
    }

    public void Unbind(Entity owner) => EventBus.Unsubscribe(this);

    // requireOwnerKill이 false면 "owner 이외의 누군가가 죽었을 때" 발동한다(기존 동작).
    // true면 죽은 대상의 마지막 공격자(Entity.LastAttacker)가 owner일 때만 발동한다.
    public void OnNotify(EntityDeadEvent e)
    {
        if (e.Source == owner) return;
        if (requireOwnerKill && e.Source.LastAttacker != owner) return;
        fire(new AbilityContext { Caster = owner, Target = e.Source.transform, EventSource = e.Source });
    }
}
