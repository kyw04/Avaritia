using System;

[System.Serializable]
public class OnKillTrigger : IAbilityTrigger, IObserver<EntityDeadEvent>
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

    public void OnNotify(EntityDeadEvent e)
    {
        if (e.Source == owner) return;
        fire(new AbilityContext { Caster = owner, Target = e.Source.transform, EventSource = e.Source });
    }
}
