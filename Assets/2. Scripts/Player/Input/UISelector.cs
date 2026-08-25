using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UISelector
{
    private PlayerInputActions inputAction;
    public GameObject SelectedGameObject => EventSystem.current.currentSelectedGameObject;
    public System.Func<Selectable, System.Func<Selectable, Selectable>, Selectable> MoveNext;

    public UISelector()
    {
        inputAction = InputHandler.Instance.InputAction;
        inputAction.UI.Navigate.performed += OnMovePoint;
    }
    
    private void OnMovePoint(InputAction.CallbackContext context)
    {
        Vector2 input = context.ReadValue<Vector2>();
        if (input == Vector2.zero)
            return;
        
        System.Func<Selectable, Selectable> findNext = null;

        if (input.x > 0)
            findNext = s => s.FindSelectableOnRight();
        if (input.x < 0)
            findNext = s => s.FindSelectableOnLeft();
        if (input.y > 0)
            findNext = s => s.FindSelectableOnUp();
        if (input.y < 0)
            findNext = s => s.FindSelectableOnDown();

        if (findNext == null)
            return;
        var selectable = SelectedGameObject.GetComponent<Selectable>();
        var next = findNext(selectable);
        next = MoveNext(next, findNext);
        if (next != null)
            EventSystem.current.SetSelectedGameObject(next.gameObject);
    }
}
