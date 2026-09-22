using System;

[Serializable]
public class OnAttackStartTrigger : IAbilityTrigger, IObserver<EntityAttackStartEvent>
{
    private Entity owner;
    private Action<AbilityContext> fire;

    public void Bind(Entity owner, Action<AbilityContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        EventBus.Subscribe(this);
    }

    public void Unbind(Entity owner) => EventBus.Unsubscribe(this);

    public void OnNotify(EntityAttackStartEvent e)
    {
        if (e.Source != owner) return;
        fire(new AbilityContext { Caster = owner, Target = owner.transform });
    }
}
