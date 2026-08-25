using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public struct InventoryUIOnEvent : ISubject { }
public struct InventoryUIOffEvent : ISubject { }
public struct InventorySelectionChangedEvent : ISubject { }
public struct InventoryDropConfirmShownEvent : ISubject
{
    public string ItemName { get; private set; }
    public InventoryDropConfirmShownEvent(string itemName) { ItemName = itemName; }
}
public struct InventoryDropConfirmHiddenEvent : ISubject { }

public class InventoryUI : MonoBehaviour,
    IObserver<InventoryUIOnEvent>, IObserver<InventoryUIOffEvent>, IObserver<InventorySelectionChangedEvent>,
    IObserver<InventoryDropConfirmShownEvent>, IObserver<InventoryDropConfirmHiddenEvent>
{
    [SerializeField] private Image weaponImage;
    [SerializeField] private Image[] skillImages;
    [SerializeField] private Image[] itemSlotImages;
    [SerializeField] private Image selectImage;
    [SerializeField] private Image selectedItemImage;
    [SerializeField] private TextMeshProUGUI detailsText;
    [SerializeField] private GameObject dropConfirmPanel;
    [SerializeField] private TextMeshProUGUI dropConfirmText;

    private Player target;

    private void Awake()
    {
        EventBus.Subscribe<InventoryUIOnEvent>(this);
        EventBus.Subscribe<InventoryUIOffEvent>(this);
        EventBus.Subscribe<InventorySelectionChangedEvent>(this);
        EventBus.Subscribe<InventoryDropConfirmShownEvent>(this);
        EventBus.Subscribe<InventoryDropConfirmHiddenEvent>(this);

        target = FindAnyObjectByType<Player>();
    }

    public void Refresh()
    {
        if (target == null) return;

        var weapon = target.Weapon;
        weaponImage.sprite = weapon?.Icon;
        weaponImage.enabled = weapon != null;
        var weaponSlot = weaponImage.GetComponent<InventorySlot>();
        weaponSlot.Target = target;
        weaponSlot.Item = weapon;
        weaponSlot.GetComponent<Selectable>().interactable = weapon != null;

        for (int i = 0; i < skillImages.Length; i++)
        {
            var skill = target.Skills.SkillAt(i);
            skillImages[i].sprite = skill?.Icon;
            skillImages[i].enabled = skill != null;
            var skillSlot = skillImages[i].GetComponent<InventorySlot>();
            skillSlot.Target = target;
            skillSlot.Item = skill;
            skillSlot.GetComponent<Selectable>().interactable = skill != null;
        }

        var items = target.Inventory.Items;
        for (int i = 0; i < itemSlotImages.Length; i++)
        {
            var item = i < items.Count ? items[i] : null;
            itemSlotImages[i].sprite = item?.Icon;
            itemSlotImages[i].enabled = item != null;
            var itemSlot = itemSlotImages[i].GetComponent<InventorySlot>();
            itemSlot.Target = target;
            itemSlot.Item = item;
            itemSlot.GetComponent<Selectable>().interactable = item != null;
        }

        EventSystem.current.SetSelectedGameObject(weaponImage.gameObject);
    }

    private void UpdateSelectionDisplay()
    {
        var selected = target?.Inventory.SelectedItem;
        selectedItemImage.sprite = selected?.Icon;
        selectedItemImage.enabled = selected != null;
        detailsText.text = selected != null ? $"{selected.DisplayName}\n{selected.Details}" : string.Empty;

        if (selectImage == null) return;

        var selectedGO = EventSystem.current.currentSelectedGameObject;
        selectImage.enabled = selectedGO != null;
        if (selectedGO != null)
            selectImage.rectTransform.position = selectedGO.GetComponent<RectTransform>().position;
    }

    public void OnNotify(InventoryUIOnEvent e)
    {
        var ui = transform.GetChild(0).gameObject;
        ui.SetActive(true);
        Canvas.ForceUpdateCanvases();
        Refresh();
    }

    public void OnNotify(InventoryUIOffEvent e)
    {
        var ui = transform.GetChild(0).gameObject;
        ui.SetActive(false);
    }

    public void OnNotify(InventorySelectionChangedEvent e)
    {
        UpdateSelectionDisplay();
    }

    public void OnNotify(InventoryDropConfirmShownEvent e)
    {
        dropConfirmText.text = $"{e.ItemName}\n(Enter: Yes / X: No)";
        dropConfirmPanel.SetActive(true);
    }

    public void OnNotify(InventoryDropConfirmHiddenEvent e)
    {
        dropConfirmPanel.SetActive(false);
    }
}
