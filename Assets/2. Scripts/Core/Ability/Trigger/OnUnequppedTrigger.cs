using System;

[Serializable]
public class OnUnequippedTrigger : IAbilityTrigger, IObserver<InventoryItemUnequip>
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

    public void OnNotify(InventoryItemUnequip e)
    {
        fire(new AbilityContext { Caster = owner, Target = owner.transform });
    }
}