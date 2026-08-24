using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InventoryDropController : MonoBehaviour
{
    public static bool IsConfirming { get; private set; }

    private IInventoryItem pendingItem;
    private Player pendingPlayer;
    private InventoryUI inventoryUI;

    private void Awake() => inventoryUI = FindAnyObjectByType<InventoryUI>();

    // 인벤토리가 닫힐 때(부모 GameObject 비활성화) 확인 팝업이 떠 있었다면 상태를 정리한다.
    // 그렇지 않으면 IsConfirming이 true로 남아 다음에 인벤토리를 열었을 때 방향키/마우스 선택이 계속 막힌다.
    private void OnDisable() => CloseConfirm();

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (!IsConfirming)
        {
            if (!kb.xKey.wasPressedThisFrame) return;

            var selected = EventSystem.current.currentSelectedGameObject;
            var slot = selected != null ? selected.GetComponent<InventorySlot>() : null;
            if (slot == null || slot.Item == null || slot.Target == null) return;

            pendingItem = slot.Item;
            pendingPlayer = slot.Target;
            IsConfirming = true;
            EventBus.Publish(new InventoryDropConfirmShownEvent(pendingItem.DisplayName));
        }
        else
        {
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            {
                ExecuteDrop(pendingPlayer, pendingItem);
                CloseConfirm();
                if (inventoryUI != null) inventoryUI.Refresh();
            }
            else if (kb.xKey.wasPressedThisFrame)
            {
                CloseConfirm();
            }
        }
    }

    private void CloseConfirm()
    {
        if (!IsConfirming) return;

        IsConfirming = false;
        pendingItem = null;
        pendingPlayer = null;
        EventBus.Publish(new InventoryDropConfirmHiddenEvent());
    }

    private static void ExecuteDrop(Player player, IInventoryItem item)
    {
        if (item is Weapon weapon)
        {
            player.EquipWeapon(null);
            WorldInteractionManager.Instance.Spawn(new WeaponPickup(weapon), player.transform.position);
        }
        else if (item is SkillData skill)
        {
            int index = player.Skills.SkillAt(0) == skill ? 0 : 1;
            player.Skills.SetSkill(index, null);
            WorldInteractionManager.Instance.Spawn(new SkillPickup(skill), player.transform.position);
        }
        else
        {
            player.Inventory.Remove(item);
        }
    }
}
