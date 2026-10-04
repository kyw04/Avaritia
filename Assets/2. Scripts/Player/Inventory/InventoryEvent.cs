public struct InventoryItemUnequip : ISubject
{
    public AbilityData ability;

    public InventoryItemUnequip(AbilityData ability)
    {
        this.ability = ability;
    }
}