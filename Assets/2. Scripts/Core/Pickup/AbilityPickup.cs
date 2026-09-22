using UnityEngine;

public class AbilityPickup : IInteractable
{
    private readonly AbilityData ability;
    public AbilityPickup(AbilityData ability) => this.ability = ability;

    public AbilityData Ability => ability;
    public string DisplayName => ability.name;
    public Sprite Icon => ability.icon;
    // Payload-only: never registered directly, always wrapped by WorldPickup.
    public Transform Transform => null;

    public bool NeedsChoice(Player player) =>
        player.Abilities.AbilityAt(0) != null && player.Abilities.AbilityAt(1) != null;

    public void Interact(Player player, InteractChoice choice)
    {
        int index = NeedsChoice(player)
            ? (choice == InteractChoice.Primary ? 0 : 1)
            : (player.Abilities.AbilityAt(0) == null ? 0 : 1);

        var previous = player.Abilities.SetAbility(index, ability);
        if (previous != null)
            WorldInteractionManager.Instance.Spawn(new AbilityPickup(previous), player.transform.position);
        else
            EventBus.Publish(new InventoryItemEquip(ability));
    }
}
