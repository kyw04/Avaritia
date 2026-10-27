using Unity.VisualScripting;
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
    public bool AttackHeld => InputAction.Gameplay.Attack.IsPressed();
    private bool attackRequested;

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
        InputAction.UI.Disable();
        
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
        InputAction.Gameplay.Pause.started += OnPauseOpen;
        
        InputAction.UI.Cancel.started += OnUICancel;
        InputAction.UI.CloseInventory.started += OnInventoryCloseAll;
    }

    private void OnDisable()
    {
        InputAction.Disable();
    }

    // 누르고 있기 무기: 공격 중이 아닐 때(콤보가 끝나고 쿨다운이 지난 뒤) 키가 눌려 있으면 다시 공격을 시작한다.
    // 공격 중이거나 쿨다운 중이면 상태 머신의 전환 조건에서 걸러진다.
    private void Update()
    {
        if (attackRequested)
        {
            attackRequested = false;
            EventBus.Publish(new EntityAttackBufferEvent());
            player.Machine.ChangeState<PlayerAttackState>();
        }

        if (player != null && player.Weapon != null && player.Weapon.holdToAttack && AttackHeld)
            player.Machine.ChangeState<PlayerAttackState>();
    }

    private void OnDash(InputAction.CallbackContext context) =>
        player.Machine.ChangeState<PlayerDashState>();
    private void OnJump(InputAction.CallbackContext context)
    {
        if (player.Weapon != null && player.Weapon.ConsumeJump())
            return;

        if (MoveInput.y < -0.5f && player.TryDropThroughPlatform())
            return;

        player.Machine.ChangeState<PlayerJumpState>();
    }
    
    // 공격은 Update에서 시작한다. 같은 키 입력의 다른 액션 콜백(예: 아래+공격의 DownAttack)이
    // 바인딩 순서상 Attack보다 늦게 오더라도, 그 프레임의 입력 콜백이 모두 끝난 뒤 공격하도록 하기 위함이다.
    // 실행 순서가 -1000이라 플레이어 상태 머신보다 먼저 돌아 공격 타이밍은 같은 프레임 그대로다.
    private void OnAttack(InputAction.CallbackContext context) => attackRequested = true;

    private void OnSkill1(InputAction.CallbackContext context) =>
        player.Abilities.TryActivate(player.Abilities.AbilityAt(0), new AbilityContext { Caster = player });
    private void OnSkill2(InputAction.CallbackContext context) =>
        player.Abilities.TryActivate(player.Abilities.AbilityAt(1), new AbilityContext { Caster = player });
    private void OnInteractStarted(InputAction.CallbackContext context) =>
        player.InteractionController.OnInteractStarted();
    private void OnInteractCanceled(InputAction.CallbackContext context) =>
        player.InteractionController.OnInteractCanceled();

    private void OnMove(InputAction.CallbackContext context) => MoveInput = context.ReadValue<Vector2>();

    private void OnMoveStopped(InputAction.CallbackContext context)
    {
        MoveInput = Vector2.zero;
        player.Move(MoveInput);
    }

    private void OnUICancel(InputAction.CallbackContext context) => UIManager.Instance.Pop();
    private void OnInventoryOpen(InputAction.CallbackContext context) => UIManager.Instance.Push(InventoryUI.Key);
    private void OnInventoryCloseAll(InputAction.CallbackContext context)
    {
        if (ItemReplaceController.IsReplacing)
            ItemReplaceController.Instance.CancelReplace();
        UIManager.Instance.PopUntil(InventoryUI.Key);
    }
    private void OnPauseOpen(InputAction.CallbackContext context) => UIManager.Instance.Push(PauseUI.Key);
}
