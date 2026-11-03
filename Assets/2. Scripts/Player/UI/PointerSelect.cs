using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 마우스를 올리면 이 Selectable을 선택 상태로 만들어, 이후 키보드 이동이 여기서부터 이어지게 한다(InventorySlot과 같은 규칙).
[RequireComponent(typeof(Selectable))]
public class PointerSelect : MonoBehaviour, IPointerEnterHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ConfirmationPopup.IsConfirming) return;
        if (eventData.delta == Vector2.zero) return;
        if (!GetComponent<Selectable>().IsInteractable()) return;

        EventSystem.current.SetSelectedGameObject(gameObject);
    }
}
