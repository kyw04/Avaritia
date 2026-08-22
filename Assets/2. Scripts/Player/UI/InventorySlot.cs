using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, ISelectHandler
{
    public IInventoryItem Item { get; set; }
    public Player Target { get; set; }

    public void OnSelect(BaseEventData eventData)
    {
        if (Target == null) return;

        Target.Inventory.Select(Item);
        EventBus.Publish(new InventorySelectionChangedEvent());
    }
}
