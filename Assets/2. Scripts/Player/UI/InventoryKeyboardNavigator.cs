using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryKeyboardNavigator : MonoBehaviour
{
    private void Update()
    {
        if (InventoryDropController.IsConfirming) return;

        var selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || selected.GetComponent<InventorySlot>() == null) return;

        var kb = Keyboard.current;
        if (kb == null) return;

        var selectable = selected.GetComponent<Selectable>();
        System.Func<Selectable, Selectable> findNext = null;

        if (kb.rightArrowKey.wasPressedThisFrame)
            findNext = s => s.FindSelectableOnRight();
        else if (kb.leftArrowKey.wasPressedThisFrame)
            findNext = s => s.FindSelectableOnLeft();
        else if (kb.upArrowKey.wasPressedThisFrame)
            findNext = s => s.FindSelectableOnUp();
        else if (kb.downArrowKey.wasPressedThisFrame)
            findNext = s => s.FindSelectableOnDown();

        if (findNext == null) return;

        var visited = new HashSet<Selectable> { selectable };
        var next = findNext(selectable);
        while (next != null && next.GetComponent<InventorySlot>().Item == null && visited.Add(next))
            next = findNext(next);

        if (next != null && next.GetComponent<InventorySlot>().Item != null)
            EventSystem.current.SetSelectedGameObject(next.gameObject);
    }
}
