using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public struct InventoryUIOnEvent : ISubject { }
public struct InventoryUIOffEvent : ISubject { }

public class InventoryUI : MonoBehaviour, IObserver<InventoryUIOnEvent>, IObserver<InventoryUIOffEvent>
{
    [SerializeField] private Image[] itemSlotImages;
    [SerializeField] private Image weaponImage;
    [SerializeField] private Image[] skillImages;
    [SerializeField] private Image selectedItemImage;
    [SerializeField] private TextMeshProUGUI detailsText;

    private Player target;
    private IInventoryItem[] slotItems;

    private void Awake()
    {
        EventBus.Subscribe<InventoryUIOnEvent>(this);
        EventBus.Subscribe<InventoryUIOffEvent>(this);
        
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

    private void Refresh()
    {
        ClearSelection();
        if (target == null) return;

        var weapon = target.Weapon;
        weaponImage.sprite = weapon?.Icon;
        weaponImage.enabled = weapon != null;

        for (int i = 0; i < skillImages.Length; i++)
        {
            var skill = target.Skills.SkillAt(i);
            skillImages[i].sprite = skill?.Icon;
            skillImages[i].enabled = skill != null;
        }

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

    public void OnNotify(InventoryUIOnEvent e)
    {
        Refresh();
        var ui = transform.GetChild(0).gameObject;
        ui.SetActive(true);
    }

    public void OnNotify(InventoryUIOffEvent e)
    {
        var ui = transform.GetChild(0).gameObject;
        ui.SetActive(false);
    }
}
