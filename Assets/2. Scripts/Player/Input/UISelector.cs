using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UISelector : System.IDisposable
{
    private PlayerInputActions inputAction;
    // 현재 selectable과 방향 탐색 함수(next)를 받아 다음 selectable을 결정하는 훅.
    // 기본값은 next를 그대로 적용하며, 빈 슬롯 건너뛰기 등 커스텀 탐색 규칙을 주입할 때 교체한다.
    public System.Func<Selectable, System.Func<Selectable, Selectable>, Selectable> SelectionStrategy =
        (selectable, next) => next(selectable);
    public System.Action Submit;
    public bool IsActive { get; private set; }
    private Selectable lastSelectable;

    public UISelector()
    {
        IsActive = false;
        
        inputAction = InputHandler.Instance.InputAction;
        inputAction.UI.Navigate.performed += OnMovePoint;
        inputAction.UI.Submit.performed += OnSubmit;
    }
    
    public void SetActive(bool active) => IsActive = active;

    public void Dispose()
    {
        inputAction.UI.Navigate.performed -= OnMovePoint;
        inputAction.UI.Submit.performed -= OnSubmit;
    }
    
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
        var selectable = select != null ? select.GetComponent<Selectable>() : lastSelectable;

        selectable = SelectionStrategy(selectable, findNext);
        if (selectable != null)
        {
            EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            lastSelectable = selectable;
        }
    }

    private void OnSubmit(InputAction.CallbackContext context)
    {
        Submit?.Invoke();
    }
}
