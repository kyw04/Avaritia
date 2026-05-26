using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : Singleton<InputHandler>, IObserver<PlayerEndAttackEvent>
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
        InputAction.Gameplay.Move.performed += OnMove;
        InputAction.Gameplay.Move.canceled += OnMoveStopped;
        
        EventBus.Subscribe<PlayerEndAttackEvent>(this);
    }

    private void OnDisable()
    {
        InputAction.Disable();
        EventBus.Unsubscribe(this);
    }

    private void OnJump(InputAction.CallbackContext context) => 
        player.Machine.ChangeState<PlayerStateMachine.JumpState>();
    private void OnAttack(InputAction.CallbackContext context) =>
        player.Machine.ChangeState<PlayerStateMachine.AttackState>();
    
    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveStopped(InputAction.CallbackContext context)
    {
        MoveInput = Vector2.zero;
        player.Move(MoveInput);
    }

    public void OnNotify(PlayerEndAttackEvent e) =>
        player.Machine.ChangeState<PlayerStateMachine.IdleState>();
}
