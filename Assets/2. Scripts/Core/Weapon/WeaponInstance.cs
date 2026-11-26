using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 플레이 중의 무기 하나. 모듈 상태(탄, 스택, 키트)를 가지므로 버렸다 다시 주워도 상태가 유지된다.
public class WeaponInstance : IInventoryItem
{
    private readonly List<WeaponModule> modules;
    private WeaponAttack currentAttack;
    // 무기 입력을 받는 입력. 장착한 쪽(플레이어)의 입력이며, 입력 메서드가 없는 무기는 구독하지 않는다.
    private IPlayerInput input;
    // 장착 중이고 입력 메서드가 있어 입력을 받아야 하는지. 입력이 잠시 없어도(null) 이 값은 유지된다.
    private bool receivesInput;
    private bool inputSubscribed;
    // "모듈타입명.메서드명" → 런타임 모듈 인스턴스에 바인딩된 호출. 입력마다 리플렉션과 박싱을 피하려고 Equip 때 만든다.
    private readonly Dictionary<string, System.Action<PlayerInputSignal>> inputMethods = new();
    private readonly Dictionary<WeaponData.InputMethodBinding, (Coroutine routine, string action)> holds = new();
    // 누를 때 애니메이션을 튼 액션 이름. 그 키를 떼면 애니메이션을 되돌린다.
    private readonly HashSet<string> animatedActions = new();
    private Animator ownerAnimator;

    public WeaponData Data { get; }
    public Entity Owner { get; private set; }
    public IReadOnlyList<WeaponModule> Modules => modules;
    public string DisplayName => string.IsNullOrEmpty(Data.DisplayName) ? Data.name : Data.DisplayName;
    public string Details => Data.Details;
    public Sprite Icon => Data.Icon;
    // 모듈이 읽는 이동 입력. 행동 제한(기절 등) 중이거나 입력이 없으면 0이다.
    public Vector2 MoveInput => input != null && Owner != null && Owner.Restrictions.Can(ActionBlock.Move) ? input.MoveInput : Vector2.zero;

    public WeaponInstance(WeaponData data)
    {
        Data = data;
        modules = data.modules.Where(m => m != null).Select(m => m.CreateRuntimeCopy()).ToList();
    }

    public bool TryGetModule<T>(out T module)
    {
        foreach (var m in modules)
        {
            if (m is T found)
            {
                module = found;
                return true;
            }
        }
        module = default;
        return false;
    }

    // 공격 시작 이벤트 동안 어빌리티가 이번 공격을 강화할 때 쓴다.
    public void EnhanceCurrentAttack(BulletEnhancement enhancement)
    {
        if (currentAttack != null)
            currentAttack.Enhancement = enhancement;
    }

    // 런 세이브용. 상태가 있는 모듈만 담는다. 인덱스는 런타임 모듈 목록 기준이다.
    public List<ModuleSave> CaptureModuleStates()
    {
        var saves = new List<ModuleSave>();
        for (int i = 0; i < modules.Count; i++)
        {
            var state = modules[i].CaptureState();
            if (state != null)
                saves.Add(new ModuleSave { index = i, type = modules[i].GetType().Name, state = state });
        }
        return saves;
    }

    // 반드시 Equip 전에 호출한다. 인덱스가 범위 밖이거나 타입이 다르면 그 항목은 건너뛴다.
    public void RestoreModuleStates(List<ModuleSave> saves)
    {
        if (saves == null)
            return;
        foreach (var save in saves)
        {
            if (save == null || save.index < 0 || save.index >= modules.Count || modules[save.index].GetType().Name != save.type)
                continue;
            modules[save.index].RestoreState(save.state);
        }
    }

    public void Equip(Entity owner, ImageCountBar countBar = null, IPlayerInput playerInput = null)
    {
        Owner = owner;
        input = playerInput;
        ownerAnimator = owner.GetComponentInChildren<EntityAnimator>()?.AnimatorComponent;

        foreach (var module in modules)
        {
            module.Bind(this);
            module.Equip(countBar);
        }

        if (modules.Count > 0 && !modules.Any(m => m.UsesCountBar))
            countBar?.Deactivate();

        receivesInput = Data.InputMethods.Count > 0;
        if (receivesInput)
        {
            BindInputMethods();
            SubscribeInput();
        }
    }

    // 장착 중에 입력이 바뀌면(AI·리플레이로 전환 등) 새 입력으로 다시 구독한다. 진행 중인 홀드는 취소한다.
    public void SetInput(IPlayerInput playerInput)
    {
        UnsubscribeInput();
        CancelHolds();
        input = playerInput;
        if (receivesInput)
            SubscribeInput();
    }

    private void SubscribeInput()
    {
        if (input == null || inputSubscribed) return;
        input.ActionTriggered += HandleInput;
        inputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!inputSubscribed) return;
        input.ActionTriggered -= HandleInput;
        inputSubscribed = false;
    }

    // 메서드는 타입명이 같은 첫 모듈(런타임 복사본)에 바인딩한다. 매개변수 없는 메서드는 감싸서 같은 타입으로 맞춘다.
    private void BindInputMethods()
    {
        inputMethods.Clear();
        foreach (var (moduleType, method) in WeaponData.GetInputMethods(modules))
        {
            var key = moduleType.Name + "." + method.Name;
            if (inputMethods.ContainsKey(key))
                continue;

            WeaponModule target = null;
            foreach (var m in modules)
            {
                if (m.GetType().Name == moduleType.Name)
                {
                    target = m;
                    break;
                }
            }
            if (target == null)
                continue;

            if (method.GetParameters().Length == 0)
            {
                var call = (System.Action)System.Delegate.CreateDelegate(typeof(System.Action), target, method);
                inputMethods[key] = _ => call();
            }
            else
            {
                inputMethods[key] = (System.Action<PlayerInputSignal>)System.Delegate.CreateDelegate(
                    typeof(System.Action<PlayerInputSignal>), target, method);
            }
        }
    }

    // Owner는 비우지 않는다: 모듈 Unequip이 Owner.Mono로 코루틴을 멈추며, 다음 Equip이 덮어쓴다.
    public void Unequip()
    {
        CancelHolds();
        animatedActions.Clear();
        inputMethods.Clear();
        receivesInput = false;
        UnsubscribeInput();

        foreach (var module in modules)
            module.Unequip();
    }

    // true를 반환하면 이번 점프 입력을 무기가 쓴 것으로 보고 플레이어는 점프하지 않는다.
    public bool ConsumeJump()
    {
        foreach (var module in modules)
        {
            if (module.ConsumeJump())
                return true;
        }
        return false;
    }

    // combo를 비우면 지상 콤보를 쓴다.
    public void Attack(IAttacker attacker, int index = 0, Transform defender = null, AttackDataCombo combo = null)
    {
        combo ??= Data.combo;
        var attack = new WeaponAttack { Attacker = attacker, Index = index, Defender = defender, Data = combo.datas[index] };

        foreach (var module in modules)
        {
            if (!module.BeforeAttack(attack))
                return;
        }

        currentAttack = attack;
        EntityEventRouter.Raise(new EntityAttackStartEvent(Owner, attack.Data));
        currentAttack = null;

        attack.Data.Attack(attacker, defender, attack.Enhancement);

        foreach (var module in modules)
            module.AfterAttack(attack);
    }

    // 바인딩과 입력 신호는 액션 이름으로 맞춘다. 메서드와 애니메이션은 각각 비워둘 수 있다.
    private void HandleInput(PlayerInputSignal signal)
    {
        CancelHoldsByOtherInput(signal);
        if (signal.Phase == InputPhase.Canceled)
            HandleRelease(signal.Action);

        foreach (var binding in Data.InputMethods)
        {
            if (binding.ActionName != signal.Action)
                continue;

            if (binding.holdDuration > 0f)
                HandleHold(binding, signal);
            else if (binding.phase == signal.Phase)
                Execute(binding, signal);
        }
    }

    // 누르기 시작하면 홀드를 시작하고, 시간이 다 되기 전에 떼면 취소한다.
    // 시작할 때 holdStartFeedback, 완료하면 feedback을 재생한다.
    private void HandleHold(WeaponData.InputMethodBinding binding, PlayerInputSignal signal)
    {
        if (signal.Phase == InputPhase.Started)
        {
            StopHold(binding);
            PlayFeedback(binding.holdStartFeedback, binding);
            holds[binding] = (Owner.StartCoroutine(HoldRoutine(binding, signal)), signal.Action);
        }
        else if (signal.Phase == InputPhase.Canceled)
        {
            StopHold(binding);
        }
    }

    // 키를 떼면 누를 때 튼 애니메이션을 되돌리고, 그 키를 누르는 바인딩의 모듈들에 알린다.
    private void HandleRelease(string action)
    {
        if (animatedActions.Remove(action))
            RestoreAnimation();

        foreach (var binding in Data.InputMethods)
        {
            if (binding.ActionName != action || binding.phase == InputPhase.Canceled)
                continue;
            foreach (var module in modules)
                module.OnReleased(binding);
        }
    }

    // 현재 이동 상태의 애니메이션으로 되돌린다. 움직이는 중이면 이동 상태가 매 프레임 덮어쓰고,
    // 공격 중이면 공격 애니메이션을 끊지 않도록 건드리지 않는다.
    private bool IsStandingStill() =>
        Owner.IsGrounded && Mathf.Abs(Owner.Rb.linearVelocityX) < 0.01f && !Owner.IsAttacking;

    public void RestoreAnimation()
    {
        if (Owner.IsAttacking)
            return;
        if (!Owner.IsGrounded)
            EntityEventRouter.Raise(new EntityFallingEvent(Owner));
        else if (Mathf.Abs(Owner.Rb.linearVelocityX) < 0.01f)
            EntityEventRouter.Raise(new EntityIdleEvent(Owner));
    }

    // 홀드 애니메이션이 있으면 그 애니메이션이 실제로 재생 중인 동안만 시간을 센다.
    // 다른 애니메이션(이동, 대기 등)에 덮이면 처음부터 다시 세고, 멈춰 서면 홀드 애니메이션을 다시 튼다.
    // 그래서 홀드 애니메이션 상태는 누르는 동안 유지돼야 한다(반복 재생).
    private IEnumerator HoldRoutine(WeaponData.InputMethodBinding binding, PlayerInputSignal signal)
    {
        var clip = binding.holdStartFeedback.animClip;
        if (clip == null || ownerAnimator == null)
        {
            yield return new WaitForSeconds(binding.holdDuration);
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < binding.holdDuration)
            {
                yield return null;
                if (ownerAnimator.GetCurrentAnimatorStateInfo(0).IsName(clip.name))
                {
                    elapsed += Time.deltaTime;
                    continue;
                }

                elapsed = 0f;
                if (IsStandingStill())
                    EntityEventRouter.Raise(new EntityAnimationEvent(Owner, clip));
            }
        }
        holds.Remove(binding);
        Execute(binding, signal);
    }

    // 진행 중인 홀드를 모두 취소한다(피격 등). 다시 하려면 키를 다시 눌러야 한다.
    public void CancelHolds()
    {
        foreach (var binding in holds.Keys.ToList())
            StopHold(binding);
    }

    private void StopHold(WeaponData.InputMethodBinding binding)
    {
        if (!holds.Remove(binding, out var hold))
            return;
        if (hold.routine != null && Owner != null)
            Owner.StopCoroutine(hold.routine);
    }

    // 홀드 중인 액션에 속하지 않은 키가 눌리면 keepHoldOnOtherInput이 꺼진 홀드를 취소한다.
    // 아래 방향키처럼 같은 키를 쓰는 다른 액션(이동 등)은 홀드 액션의 컨트롤이므로 취소하지 않는다.
    private void CancelHoldsByOtherInput(PlayerInputSignal signal)
    {
        if (holds.Count == 0 || signal.Phase == InputPhase.Canceled || signal.Control == null || !signal.ControlPressed)
            return;

        foreach (var (binding, hold) in holds.ToList())
        {
            if (!binding.keepHoldOnOtherInput && hold.action != signal.Action && !input.IsControlOf(signal.Control, hold.action))
                StopHold(binding);
        }
    }

    // methodName("모듈타입명.메서드명")을 호출하는 바인딩에 걸린 입력 쿨타임을 시작한다.
    public void StartInputCooldown(string methodName)
    {
        foreach (var binding in Data.InputMethods)
        {
            if (binding.methodName != methodName)
                continue;
            foreach (var module in modules)
            {
                if (module is InputCooldownModule cooldown && cooldown.Matches(binding))
                    cooldown.StartCooldown();
            }
        }
    }

    public void CollectStatusIcons(List<StatusIconEntry> into)
    {
        foreach (var module in modules)
            module.CollectStatusIcons(into);
    }

    // methodName은 "모듈타입명.메서드명" 형식이며, 타입명이 같은 첫 모듈에서 호출한다.
    // 모듈이 막거나(쿨타임 등) 공격이 막혀 있으면(기절, 채널링 등) 피드백도 재생하지 않는다.
    // 뗌(Canceled)은 막지 않는다. 누르는 동안 이어지는 동작(자석 흡인 등)이 끝나지 않게 되기 때문이다.
    private void Execute(WeaponData.InputMethodBinding binding, PlayerInputSignal signal)
    {
        if (binding.phase != InputPhase.Canceled && !Owner.Restrictions.Can(ActionBlock.Attack))
            return;
        for (int i = 0; i < modules.Count; i++)
        {
            if (!modules[i].CanExecute(binding))
                return;
        }
        foreach (var m in modules)
            m.OnExecuted(binding);

        PlayFeedback(binding.feedback, binding);

        if (string.IsNullOrEmpty(binding.methodName))
            return;

        if (!inputMethods.TryGetValue(binding.methodName, out var method))
        {
            Debug.LogWarning($"{Data.name}: [InputMethod] method '{binding.methodName}' do not found.");
            return;
        }

        method(signal);
    }

    private void PlayFeedback(ActionFeedback feedback, WeaponData.InputMethodBinding binding)
    {
        feedback.Play(Owner.transform.position);
        if (feedback.animClip == null)
            return;

        EntityEventRouter.Raise(new EntityAnimationEvent(Owner, feedback.animClip));
        if (binding.phase != InputPhase.Canceled)
            animatedActions.Add(binding.ActionName);
    }
}
