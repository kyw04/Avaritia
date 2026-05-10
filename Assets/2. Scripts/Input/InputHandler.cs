using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : Singleton<InputHandler>
{
    public PlayerInputActions inputAction;

    protected override void Awake()
    {
        base.Awake();
        inputAction = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputAction.Enable();

        inputAction.Gameplay.Jump.performed += OnJump;
        inputAction.Gameplay.Move.performed += Move;
        // inputAction.Gameplay.Attack.performed += OnAttack;
        // inputAction.Gameplay.Dash.performed   += OnDash;
    }

    void OnDisable()
    {
        inputAction.Disable();
    }

    private void OnJump(InputAction.CallbackContext context)  => Debug.Log("OnJump");
    private void Move(InputAction.CallbackContext context) => Debug.Log($"Move {context.ReadValue<Vector2>()}");
}
