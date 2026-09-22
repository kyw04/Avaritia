using System;

[System.Serializable]
public class OnDamageTakenTrigger : IAbilityTrigger, IObserver<EntityHealthChangedEvent>
{
    private Entity owner;
    private Action<AbilityContext> fire;
    private float lastHealth;

    public void Bind(Entity owner, Action<AbilityContext> fire)
    {
        this.owner = owner;
        this.fire = fire;
        lastHealth = owner.CurrentHealth;
        EventBus.Subscribe(this);
    }

    public void Unbind(Entity owner) => EventBus.Unsubscribe(this);

    public void OnNotify(EntityHealthChangedEvent e)
    {
        if (e.Source != owner) return;
        float delta = lastHealth - e.Current;
        lastHealth = e.Current;
        if (delta <= 0f) return;
        fire(new AbilityContext { Caster = owner, Target = owner.transform, Value = delta });
    }
}
