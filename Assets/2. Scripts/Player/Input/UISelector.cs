using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UISelector
{
    private PlayerInputActions inputAction;
    public System.Func<Selectable, System.Func<Selectable, Selectable>, Selectable> MoveNext = 
        (selectable, next) => next(selectable);
    public System.Action Submit;
    public bool IsActive { get; private set; }
    
    public UISelector()
    {
        IsActive = false;
        
        inputAction = InputHandler.Instance.InputAction;
        inputAction.UI.Navigate.performed += OnMovePoint;
        inputAction.UI.Submit.performed += OnSubmit;
    }
    
    public void SetActive(bool active) => IsActive = active;
    
    private void OnMovePoint(InputAction.CallbackContext context)
    {
        if (!IsActive)
            return;
        
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

        var select = EventSystem.current.currentSelectedGameObject;
        var selectable = select.GetComponent<Selectable>();
        selectable = MoveNext(selectable, findNext);
        if (selectable != null)
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
    }

    private void OnSubmit(InputAction.CallbackContext context)
    {
        if (Submit != null)
            Submit();
    }
}
