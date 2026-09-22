using UnityEngine.EventSystems;

public class ItemReplaceController : Singleton<ItemReplaceController>
{
    public static bool IsReplacing { get; private set; }

    private Item pendingItem;
    private Player pendingPlayer;
    private InventoryUI inventoryUI;
    private UISelector selector;

    protected override void Awake()
    {
        base.Awake();
        inventoryUI = FindAnyObjectByType<InventoryUI>();
        selector = new UISelector { Submit = OnSubmit };
    }

    private void OnDestroy() => selector.Dispose();

    public void BeginReplace(Player player, Item item)
    {
        pendingPlayer = player;
        pendingItem = item;
        IsReplacing = true;
        UIManager.Instance.Push(InventoryUI.Key);
    }

    public void CancelReplace()
    {
        WorldInteractionManager.Instance.Spawn(new ItemPickup(pendingItem), pendingPlayer.transform.position);
        IsReplacing = false;
        pendingItem = null;
        pendingPlayer = null;
    }

    private void OnSubmit()
    {
        if (!IsReplacing || ConfirmationPopup.IsConfirming) return;

        var selected = EventSystem.current.currentSelectedGameObject;
        var slot = selected != null ? selected.GetComponent<InventorySlot>() : null;
        if (slot == null || slot.Item is not Item oldItem) return;

        ConfirmationPopup.Instance.Show(
            $"{oldItem.DisplayName}을(를) 버리고 {pendingItem.DisplayName}을(를) 넣으시겠습니까?",
            () => ConfirmReplace(oldItem));
    }

    private void ConfirmReplace(Item oldItem)
    {
        pendingPlayer.Inventory.Remove(oldItem);
        WorldInteractionManager.Instance.Spawn(new ItemPickup(oldItem), pendingPlayer.transform.position);
        pendingPlayer.Inventory.TryAdd(pendingItem);
        IsReplacing = false;
        pendingItem = null;
        pendingPlayer = null;
        if (inventoryUI != null)
            inventoryUI.Refresh();
    }
}
