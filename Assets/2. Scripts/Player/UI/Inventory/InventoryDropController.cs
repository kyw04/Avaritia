using UnityEngine;
using UnityEngine.EventSystems;

public class InventoryDropController : MonoBehaviour
{
    public static bool IsConfirming { get; private set; }

    [SerializeField] private GameObject noImage;
    private IInventoryItem pendingItem;
    private Player pendingPlayer;
    private InventoryUI inventoryUI;
    private UISelector selector;
    private GameObject selectedItemGameObject;

    private void Awake()
    {
        inventoryUI = FindAnyObjectByType<InventoryUI>();
        selector = new UISelector
        {
            Submit = OnSubmit
        };
    }

    // 인벤토리가 닫힐 때(부모 GameObject 비활성화) 확인 팝업이 떠 있었다면 상태를 정리한다.
    // 그렇지 않으면 IsConfirming이 true로 남아 다음에 인벤토리를 열었을 때 방향키/마우스 선택이 계속 막힌다.
    private void OnDisable() => CloseConfirm();

    private void OnSubmit()
    {
        var selected = EventSystem.current.currentSelectedGameObject;
        if (!IsConfirming)
        {
            var slot = selected != null ? selected.GetComponent<InventorySlot>() : null;
            if (slot == null || slot.Item == null || slot.Target == null)
                return;
            
            selectedItemGameObject = slot.gameObject;
            pendingItem = slot.Item;
            pendingPlayer = slot.Target;
            IsConfirming = true;
            
            selector.SetActive(true);
            EventSystem.current.SetSelectedGameObject(noImage);
            EventBus.Publish(new InventoryDropConfirmShownEvent(pendingItem.DisplayName));
        }
        else if (selected == noImage)
        {
            CloseConfirm();
        }
        else
        {
            ExecuteDrop(pendingPlayer, pendingItem);
            CloseConfirm();
            if (inventoryUI != null) 
                inventoryUI.Refresh();
        }
    }

    private void CloseConfirm()
    {
        if (!IsConfirming) return;

        IsConfirming = false;
        pendingItem = null;
        pendingPlayer = null;
        selector.SetActive(false);

        EventSystem.current.SetSelectedGameObject(selectedItemGameObject);
        EventBus.Publish(new InventoryDropConfirmHiddenEvent());
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
