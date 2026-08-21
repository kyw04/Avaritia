using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Image[] itemSlotImages;
    [SerializeField] private Image selectedItemImage;
    [SerializeField] private TextMeshProUGUI detailsText;

    private Player target;
    private IInventoryItem[] slotItems;

    private void Awake()
    {
        target = FindAnyObjectByType<Player>();
        slotItems = new IInventoryItem[itemSlotImages.Length];

        for (int i = 0; i < itemSlotImages.Length; i++)
        {
            int index = i;
            var trigger = itemSlotImages[i].gameObject.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => SelectSlot(index));
            trigger.triggers.Add(entry);
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void Refresh()
    {
        ClearSelection();
        if (target == null) return;

        var items = target.Inventory.Items;
        for (int i = 0; i < itemSlotImages.Length; i++)
        {
            var item = i < items.Count ? items[i] : null;
            slotItems[i] = item;
            itemSlotImages[i].sprite = item?.Icon;
            itemSlotImages[i].enabled = item != null;
        }
    }

    private void SelectSlot(int index)
    {
        var item = slotItems[index];
        if (item == null) return;

        selectedItemImage.sprite = item.Icon;
        selectedItemImage.enabled = true;
        detailsText.text = $"{item.DisplayName}\n{item.Details}";
    }

    private void ClearSelection()
    {
        selectedItemImage.enabled = false;
        detailsText.text = string.Empty;
    }
}
