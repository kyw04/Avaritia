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
        if (ConfirmationPopup.IsConfirming) return;

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

    private static void ExecuteDrop(Player player, IInventoryItem item)
    {
        switch (item)
        {
            case Weapon:
                return;

            case SkillData skill:
            {
                int index = player.Skills.SkillAt(0) == skill ? 0 : 1;
                player.Skills.SetSkill(index, null);
                WorldInteractionManager.Instance.Spawn(new SkillPickup(skill), player.transform.position);
                break;
            }

            default:
                player.Inventory.Remove(item);
                // WorldInteractionManager.Instance.Spawn(new ItemPickup(item), player.transform.position);
                break;
        }
    }
}
