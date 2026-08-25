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

    private Selectable Move(Selectable selectable, System.Func<Selectable, Selectable> findNext)
    {
        if (InventoryDropController.IsConfirming || selectable == null || findNext == null)
            return selectable;

        var next = selectable;
        var visited = new HashSet<Selectable> { next };
        do
        {
            next = findNext(next);
        } while (next != null && next.GetComponent<InventorySlot>().Item == null && visited.Add(next));
        
        return next;
    }
}
