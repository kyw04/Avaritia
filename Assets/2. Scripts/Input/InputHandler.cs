using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : Singleton<InputHandler>
{
    public Player player;
    
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

    private void OnJump(InputAction.CallbackContext context)  => player.stateMachine.ChangeState<PlayerStateMachine.JumpState>();
    private void OnMove(InputAction.CallbackContext context) =>  player.stateMachine.ChangeState<PlayerStateMachine.MoveState>();
    private void OnMoveStopped(InputAction.CallbackContext context) => player.stateMachine.ChangeState<PlayerStateMachine.IdleState>();
}
