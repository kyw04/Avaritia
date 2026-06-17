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

    private void OnEnable()
    {
        InputAction.Enable();

        InputAction.Gameplay.Jump.performed += OnJump;
        InputAction.Gameplay.Attack.performed += OnAttack;
        InputAction.Gameplay.Dash.performed += OnDash;
        InputAction.Gameplay.Move.performed += OnMove;
        InputAction.Gameplay.Move.canceled += OnMoveStopped;
    }

    private void OnDisable()
    {
        InputAction.Disable();
    }

    private void OnDash(InputAction.CallbackContext context) =>
        player.Machine.ChangeState<PlayerDashState>();
    private void OnJump(InputAction.CallbackContext context) => 
        player.Machine.ChangeState<PlayerJumpState>();
    private void OnAttack(InputAction.CallbackContext context)
    {
        EventBus.Publish(new PlayerAttackBufferEvent());
        player.Machine.ChangeState<PlayerAttackState>();
    }
    
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
