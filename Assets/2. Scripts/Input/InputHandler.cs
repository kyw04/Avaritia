using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    private PlayerInputActions inputAction;

    private void Awake()
    {
        inputAction = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputAction.Enable();

        inputAction.Gameplay.Jump.performed += OnJump;
        // inputAction.Gameplay.Attack.performed += OnAttack;
        // inputAction.Gameplay.Dash.performed   += OnDash;
    }

    void OnDisable()
    {
        inputAction.Disable();
    }

    private void OnJump(InputAction.CallbackContext context)
    {
        Debug.Log("OnJump");
    }
}
