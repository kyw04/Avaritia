using UnityEngine;

public class ItemPickup : IInteractable
{
    private readonly Item item;
    public ItemPickup(Item item) => this.item = item;

    public Item Item => item;
    public string DisplayName => item.name;
    public Sprite Icon => item.icon;
    // Payload-only: never registered directly, always wrapped by WorldPickup.
    public Transform Transform => null;
    public bool NeedsChoice(Player player) => false;

    public void Interact(Player player, InteractChoice choice)
    {
        if (player.Inventory.TryAdd(item)) return;
        ItemReplaceController.Instance.BeginReplace(player, item);
    }
}
