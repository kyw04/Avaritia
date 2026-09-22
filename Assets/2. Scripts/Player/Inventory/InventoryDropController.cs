using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDropController : MonoBehaviour
{
    private IInventoryItem pendingItem;
    private Player pendingPlayer;
    private InventoryUI inventoryUI;
    private UISelector selector;

    private void Awake()
    {
        inventoryUI = FindAnyObjectByType<InventoryUI>();
        selector = new UISelector
        {
            Submit = OnSubmit
        };
    }

    private void OnDestroy() => selector.Dispose();

    private void OnSubmit()
    {
        if (ConfirmationPopup.IsConfirming || ItemReplaceController.IsReplacing) return;

        var selected = EventSystem.current.currentSelectedGameObject;
        var slot = selected != null ? selected.GetComponent<InventorySlot>() : null;
        if (slot == null || slot.Item == null || slot.Target == null)
            return;

        pendingItem = slot.Item;
        pendingPlayer = slot.Target;
        ConfirmationPopup.Instance.Show(pendingItem.DisplayName, OnConfirmDrop);
    }

    private void OnConfirmDrop()
    {
        ExecuteDrop(pendingPlayer, pendingItem);
        if (inventoryUI != null)
            inventoryUI.Refresh();
    }

    private static void ExecuteDrop(Player player, IInventoryItem inventoryItem)
    {
        switch (inventoryItem)
        {
            case Weapon:
                return; 
                
            case Item item:
            {
                player.Inventory.Remove(inventoryItem);
                WorldInteractionManager.Instance.Spawn(new ItemPickup(item), player.transform.position);
                EventBus.Publish(new InventoryItemUnequip(item));
                break;
            }

            case AbilityData ability:
            {
                int index = player.Abilities.AbilityAt(0) == ability ? 0 : 1;
                player.Abilities.SetAbility(index, null);
                WorldInteractionManager.Instance.Spawn(new AbilityPickup(ability), player.transform.position);
                EventBus.Publish(new InventoryItemUnequip(ability));
                break;
            }
        }
        
    }
}
