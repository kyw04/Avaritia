using UnityEngine;
using UnityEngine.InputSystem;

// UISelector의 생성자가 다른 오브젝트의 Awake에서 InputHandler.Instance.InputAction을 바로 읽는다.
// InputHandler가 씬에 미리 배치된 컴포넌트라 Awake 순서가 보장되지 않으면 InputAction이 아직 null일 수 있어,
// 다른 스크립트보다 먼저 Awake되도록 강제한다.
[DefaultExecutionOrder(-1000)]
public class InputHandler : Singleton<InputHandler>
{
    public Player player;
    public Vector2 MoveInput { get; private set; }
    
    public PlayerInputActions InputAction { get; private set; }
    
    protected override void Awake()
    {
        base.Awake();
        InputAction = new PlayerInputActions();
        _ = GameFreezeManager.Instance;
        _ = UIManager.Instance;
    }

    private void OnEnable()
    {
        InputAction.Enable();

        InputAction.Gameplay.Jump.performed += OnJump;
        InputAction.Gameplay.Attack.performed += OnAttack;
        InputAction.Gameplay.Dash.performed += OnDash;
        InputAction.Gameplay.Move.performed += OnMove;
        InputAction.Gameplay.Move.canceled += OnMoveStopped;
        InputAction.Gameplay.Skill1.performed += OnSkill1;
        InputAction.Gameplay.Skill2.performed += OnSkill2;
        InputAction.Gameplay.Interact.started += OnInteractStarted;
        InputAction.Gameplay.Interact.canceled += OnInteractCanceled;
        InputAction.Gameplay.Inventory.started += OnInventoryOpen;
        
        InputAction.UI.Cancel.started += OnInventoryClose;
    }

    private void OnDisable()
    {
        InputAction.Disable();
    }

    private void OnDash(InputAction.CallbackContext context) =>
        player.Machine.ChangeState<PlayerDashState>();
    private void OnJump(InputAction.CallbackContext context)
    {
        if (MoveInput.y < -0.5f && player.TryDropThroughPlatform())
            return;

        player.Machine.ChangeState<PlayerJumpState>();
    }
    private void OnAttack(InputAction.CallbackContext context)
    {
        EventBus.Publish(new EntityAttackBufferEvent());
        player.Machine.ChangeState<PlayerAttackState>();
    }

    private void OnSkill1(InputAction.CallbackContext context) =>
        player.Skills.TryUseSkill(player.Skills.SkillAt(0), player);
    private void OnSkill2(InputAction.CallbackContext context) =>
        player.Skills.TryUseSkill(player.Skills.SkillAt(1), player);
    private void OnInteractStarted(InputAction.CallbackContext context) =>
        player.InteractionController.OnInteractStarted();
    private void OnInteractCanceled(InputAction.CallbackContext context) =>
        player.InteractionController.OnInteractCanceled();

    private void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
    }

    private void OnMoveStopped(InputAction.CallbackContext context)
    {
        MoveInput = Vector2.zero;
        player.Move(MoveInput);
    }

    private void OnInventoryOpen(InputAction.CallbackContext context) =>
        UIManager.Instance.Push(InventoryUI.Key);

    private void OnInventoryClose(InputAction.CallbackContext context) =>
        UIManager.Instance.Pop();
}
