public struct InventoryItemEquip : ISubject
{
    public AbilityData ability;

    public InventoryItemEquip(AbilityData ability)
    {
        this.ability = ability;
    }
}

public struct InventoryItemUnequip : ISubject
{
    public AbilityData ability;

    public InventoryItemUnequip(AbilityData ability)
    {
        this.ability = ability;
    }
}