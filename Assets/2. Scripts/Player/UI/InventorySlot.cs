using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, ISelectHandler, IPointerEnterHandler
{
    public IInventoryItem Item { get; set; }
    public Player Target { get; set; }

    public void OnSelect(BaseEventData eventData)
    {
        if (InventoryDropController.IsConfirming) return;
        if (Target == null) return;

        Target.Inventory.Select(Item);
        EventBus.Publish(new InventorySelectionChangedEvent());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (InventoryDropController.IsConfirming) return;
        if (eventData.delta == Vector2.zero) return;
        if (!GetComponent<Selectable>().IsInteractable()) return;

        EventSystem.current.SetSelectedGameObject(gameObject);
    }
}
