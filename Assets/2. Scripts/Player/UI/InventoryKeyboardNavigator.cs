using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryKeyboardNavigator : MonoBehaviour
{
    private void Update()
    {
        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || selected.GetComponent<InventorySlot>() == null) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        var selectable = selected.GetComponent<Selectable>();
        Selectable next = null;

        if (kb.rightArrowKey.wasPressedThisFrame)
            next = selectable.FindSelectableOnRight();
        else if (kb.leftArrowKey.wasPressedThisFrame)
            next = selectable.FindSelectableOnLeft();
        else if (kb.upArrowKey.wasPressedThisFrame)
            next = selectable.FindSelectableOnUp();
        else if (kb.downArrowKey.wasPressedThisFrame)
            next = selectable.FindSelectableOnDown();

        if (next != null)
            EventSystem.current.SetSelectedGameObject(next.gameObject);
    }
}
