using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryKeyboardNavigator : MonoBehaviour
{
    private UISelector selector;
    private void Awake()
    {
        selector = new UISelector
        {
            MoveNext = Move
        };
    }

    private Selectable Move(Selectable selectable, System.Func<Selectable, Selectable> nextSelector)
    {
        if (InventoryDropController.IsConfirming || selectable == null || nextSelector == null)
            return selectable;

        var next = selectable;
        var visited = new HashSet<Selectable> { next };
        while (next.GetComponent<InventorySlot>().Item == null && visited.Add(next))
            next = nextSelector(next);
       
        return next;
    }
}
