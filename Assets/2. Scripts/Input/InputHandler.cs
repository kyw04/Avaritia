using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : Singleton<InputHandler>
{
    public Player player;
    public Vector2 MoveInput { get; private set; }
    
    public PlayerInputActions InputAction { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        InputAction = new PlayerInputActions();
    }

    void OnEnable()
    {
        InputAction.Enable();

        InputAction.Gameplay.Jump.performed += OnJump;
        InputAction.Gameplay.Move.performed += OnMove;
        InputAction.Gameplay.Move.canceled += OnMoveStopped;
        // inputAction.Gameplay.Attack.performed += OnAttack;
        // inputAction.Gameplay.Dash.performed   += OnDash;
    }

    void OnDisable()
    {
        InputAction.Disable();
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
