using System;

[Serializable]
public class OnEquippedTrigger : IAbilityTrigger, IObserver<InventoryItemEquip>
{
    private Entity owner;
    private AbilityData data;
    private Action<AbilityContext> fire;

    public void Bind(Entity owner, AbilityData data, Action<AbilityContext> fire)
    {
        this.owner = owner;
        this.data = data;
        this.fire = fire;
        EventBus.Subscribe(this);
    }

    public void Unbind(Entity owner) => EventBus.Unsubscribe(this);

    public void OnNotify(InventoryItemEquip e)
    {
        if (e.ability != data) return;
        fire(new AbilityContext { Caster = owner, Target = owner.transform });
    }
}