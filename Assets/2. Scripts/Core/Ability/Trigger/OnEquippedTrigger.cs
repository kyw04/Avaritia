using System;

[Serializable]
public class OnEquippedTrigger : IAbilityTrigger, IObserver<InventoryItemEquip>
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

    public void OnNotify(InventoryItemEquip e)
    {
        fire(new AbilityContext { Caster = owner, Target = owner.transform });
    }
}