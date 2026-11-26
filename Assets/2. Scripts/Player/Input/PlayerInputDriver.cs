using UnityEngine;

// 플레이어 입력(IPlayerInput)을 플레이어 행동으로 옮긴다. 입력이 키보드든 AI든 리플레이든 같은 규칙으로 움직인다.
// 실행 순서가 -1000이라 플레이어 상태 머신보다 먼저 돌아 공격 타이밍은 같은 프레임 그대로다.
[DefaultExecutionOrder(-1000)]
[RequireComponent(typeof(Player))]
public class PlayerInputDriver : MonoBehaviour
{
    private Player player;
    private IPlayerInput input;
    private bool attackRequested;
    private bool landingAttackRequested;

    private bool Can(ActionBlock action) => player.Restrictions.Can(action);

    private void Awake() => player = GetComponent<Player>();

    private void OnEnable()
    {
        Subscribe(player.Input);
        player.InputChanged += Subscribe;
    }

    private void OnDisable()
    {
        player.InputChanged -= Subscribe;
        Subscribe(null);
    }

    private void Subscribe(IPlayerInput newInput)
    {
        if (input != null)
            input.ActionTriggered -= OnSignal;
        input = newInput;
        if (input != null)
            input.ActionTriggered += OnSignal;
    }

    // 누르고 있기 무기: 공격 중이 아닐 때(콤보가 끝나고 쿨다운이 지난 뒤) 키가 눌려 있으면 다시 공격을 시작한다.
    // 공격 중이거나 쿨다운 중이면 상태 머신의 전환 조건에서 걸러진다.
    private void Update()
    {
        if (!Can(ActionBlock.Attack))
        {
            attackRequested = false;
            landingAttackRequested = false;
            return;
        }

        if (attackRequested)
        {
            attackRequested = false;
            // 곧 착지하면 공중 공격 대신 착지한 뒤 지상 공격을 한다.
            if (player.IsLandingSoon)
            {
                landingAttackRequested = true;
            }
            else
            {
                EventBus.Publish(new EntityAttackBufferEvent());
                player.Machine.ChangeState<PlayerAttackState>();
            }
        }

        // 착지 경직(Land) 등으로 바로 공격할 수 없으면 공격이 시작될 때까지 기다리고, 다시 떠오르면 취소한다.
        if (landingAttackRequested)
        {
            if (!player.IsGrounded)
            {
                if (!player.IsLandingSoon)
                    landingAttackRequested = false;
            }
            else
            {
                EventBus.Publish(new EntityAttackBufferEvent());
                player.Machine.ChangeState<PlayerAttackState>();
                if (player.IsAttacking)
                    landingAttackRequested = false;
            }
        }

        if (player.Weapon != null && player.Weapon.Data.holdToAttack && player.AttackHeld)
            player.Machine.ChangeState<PlayerAttackState>();
    }

    private void OnSignal(PlayerInputSignal signal)
    {
        switch (signal.Action)
        {
            case PlayerActions.Jump:
                if (signal.Phase == InputPhase.Performed) OnJump();
                break;
            // 공격은 Update에서 시작한다. 같은 키 입력의 다른 액션 콜백(예: 아래+공격의 DownAttack)이
            // 바인딩 순서상 Attack보다 늦게 오더라도, 그 프레임의 입력 콜백이 모두 끝난 뒤 공격하도록 하기 위함이다.
            case PlayerActions.Attack:
                if (signal.Phase == InputPhase.Performed) attackRequested = true;
                break;
            case PlayerActions.Dash:
                if (signal.Phase == InputPhase.Performed && Can(ActionBlock.Dash))
                    player.Machine.ChangeState<PlayerDashState>();
                break;
            case PlayerActions.Move:
                if (signal.Phase == InputPhase.Canceled) player.Move(player.MoveInput);
                break;
            case PlayerActions.Skill1:
                if (signal.Phase == InputPhase.Performed) UseSkill(0, PlayerActions.Skill1);
                break;
            case PlayerActions.Skill2:
                if (signal.Phase == InputPhase.Performed) UseSkill(1, PlayerActions.Skill2);
                break;
            case PlayerActions.Interact:
                if (signal.Phase == InputPhase.Started && Can(ActionBlock.Interact))
                    player.InteractionController.OnInteractStarted();
                else if (signal.Phase == InputPhase.Canceled)
                    player.InteractionController.OnInteractCanceled();
                break;
        }
    }

    private void OnJump()
    {
        if (!Can(ActionBlock.Jump)) return;
        if (player.Weapon != null && player.Weapon.ConsumeJump())
            return;

        if (player.MoveInput.y < -0.5f && player.TryDropThroughPlatform())
            return;

        player.Machine.ChangeState<PlayerJumpState>();
    }

    private void UseSkill(int slot, string action)
    {
        if (!Can(ActionBlock.Skill)) return;
        var source = input;
        player.Abilities.TryActivate(player.Abilities.AbilityAt(slot),
            new AbilityContext { Caster = player, IsInputHeld = () => source.IsPressed(action) });
    }
}
