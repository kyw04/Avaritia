using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : Singleton<InputHandler>
{
    public Player player;
    public Vector2 MoveInput { get; private set; }
    
    private PlayerInputActions inputAction;

    protected override void Awake()
    {
        base.Awake();
        inputAction = new PlayerInputActions();
    }

    void OnEnable()
    {
        inputAction.Enable();

        inputAction.Gameplay.Jump.performed += OnJump;
        inputAction.Gameplay.Move.performed += OnMove;
        inputAction.Gameplay.Move.canceled += OnMoveStopped;
        // inputAction.Gameplay.Attack.performed += OnAttack;
        // inputAction.Gameplay.Dash.performed   += OnDash;
    }

    void OnDisable()
    {
        inputAction.Disable();
    }

    private void OnJump(InputAction.CallbackContext context) => 
        player.StateMachine.ChangeState<PlayerStateMachine.JumpState>();
    
    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveStopped(InputAction.CallbackContext context)
    {
        MoveInput = Vector2.zero;
        player.Move(MoveInput);
    }
}
