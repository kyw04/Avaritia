using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, ISelectHandler
{
    public IInventoryItem Item { get; set; }

    public void OnSelect(BaseEventData eventData)
    {
        var target = FindAnyObjectByType<Player>();
        if (target == null) return;

        target.Inventory.Select(Item);
        EventBus.Publish(new InventorySelectionChangedEvent());
    }
}
