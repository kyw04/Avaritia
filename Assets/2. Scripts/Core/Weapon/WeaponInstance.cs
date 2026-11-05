using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

// 플레이 중의 무기 하나. 모듈 상태(탄, 스택, 키트)를 가지므로 버렸다 다시 주워도 상태가 유지된다.
public class WeaponInstance : IInventoryItem
{
    private readonly List<WeaponModule> modules;
    private WeaponAttack currentAttack;
    private bool inputSubscribed;
    private Dictionary<string, MethodInfo> methodCache;
    private readonly Dictionary<WeaponData.InputMethodBinding, (Coroutine routine, InputAction action)> holds = new();

    public WeaponData Data { get; }
    public Entity Owner { get; private set; }
    public IReadOnlyList<WeaponModule> Modules => modules;
    public string DisplayName => string.IsNullOrEmpty(Data.displayName) ? Data.name : Data.displayName;
    public string Details => Data.description;
    public Sprite Icon => Data.icon;

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

    public void Equip(Entity owner, ImageCountBar countBar = null)
    {
        Owner = owner;

        foreach (var module in modules)
        {
            module.Bind(this);
            module.Equip(countBar);
        }

        if (modules.Count > 0 && !modules.Any(m => m.UsesCountBar))
            countBar?.Deactivate();

        if (Data.InputMethods.Count > 0)
        {
            methodCache = WeaponData.GetInputMethods(modules)
                .GroupBy(x => x.moduleType.Name + "." + x.method.Name)
                .ToDictionary(g => g.Key, g => g.First().method);
            InputHandler.Instance.InputAction.Gameplay.Get().actionTriggered += HandleInput;
            inputSubscribed = true;
        }
    }

    // Owner는 비우지 않는다: 모듈 Unequip이 Owner.Mono로 코루틴을 멈추며, 다음 Equip이 덮어쓴다.
    public void Unequip()
    {
        foreach (var binding in holds.Keys.ToList())
            StopHold(binding);

        if (inputSubscribed)
        {
            InputHandler.Instance.InputAction.Gameplay.Get().actionTriggered -= HandleInput;
            inputSubscribed = false;
        }

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

    public void Attack(IAttacker attacker, int index = 0, Transform defender = null)
    {
        var attack = new WeaponAttack { Attacker = attacker, Index = index, Defender = defender, Data = Data.combo.datas[index] };

        foreach (var module in modules)
        {
            if (!module.BeforeAttack(attack))
                return;
        }

        currentAttack = attack;
        EventBus.Publish(new EntityAttackStartEvent(Owner, attack.Data));
        currentAttack = null;

        attack.Data.Attack(attacker, defender, attack.Enhancement);

        foreach (var module in modules)
            module.AfterAttack(attack);
    }

    // 에셋의 액션(InputActionReference)과 생성된 PlayerInputActions의 액션은 인스턴스가 달라 id로 비교한다.
    // 메서드와 애니메이션은 각각 비워둘 수 있다.
    private void HandleInput(InputAction.CallbackContext context)
    {
        CancelHoldsByOtherInput(context);

        foreach (var binding in Data.InputMethods)
        {
            if (binding.action == null || binding.action.action.id != context.action.id)
                continue;

            if (binding.holdDuration > 0f)
                HandleHold(binding, context);
            else if (binding.phase == context.phase)
                Execute(binding, context);
        }
    }

    // 누르기 시작하면 홀드를 시작하고, 시간이 다 되기 전에 떼면 취소한다.
    // 시작할 때 holdStartFeedback, 완료하면 feedback을 재생한다.
    private void HandleHold(WeaponData.InputMethodBinding binding, InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            StopHold(binding);
            PlayFeedback(binding.holdStartFeedback);
            holds[binding] = (Owner.StartCoroutine(HoldRoutine(binding, context)), context.action);
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            StopHold(binding);
        }
    }

    private IEnumerator HoldRoutine(WeaponData.InputMethodBinding binding, InputAction.CallbackContext context)
    {
        yield return new WaitForSeconds(binding.holdDuration);
        holds.Remove(binding);
        Execute(binding, context);
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
    private void CancelHoldsByOtherInput(InputAction.CallbackContext context)
    {
        if (holds.Count == 0 || context.phase == InputActionPhase.Canceled || context.control == null || !context.control.IsPressed())
            return;

        foreach (var (binding, hold) in holds.ToList())
        {
            if (!binding.keepHoldOnOtherInput && hold.action != context.action && !hold.action.controls.Contains(context.control))
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
    private void Execute(WeaponData.InputMethodBinding binding, InputAction.CallbackContext context)
    {
        if (binding.phase != InputActionPhase.Canceled && !Owner.Restrictions.Can(ActionBlock.Attack))
            return;
        if (modules.Any(m => !m.CanExecute(binding)))
            return;
        foreach (var m in modules)
            m.OnExecuted(binding);

        PlayFeedback(binding.feedback);

        if (string.IsNullOrEmpty(binding.methodName))
            return;

        if (!methodCache.TryGetValue(binding.methodName, out var method))
        {
            Debug.LogWarning($"{Data.name}: [InputMethod] method '{binding.methodName}' do not found.");
            return;
        }

        var typeName = binding.methodName.Split('.')[0];
        var module = modules.FirstOrDefault(m => m.GetType().Name == typeName);
        if (module == null)
        {
            Debug.LogWarning($"{Data.name}: module '{typeName}' do not found.");
            return;
        }

        method.Invoke(module, method.GetParameters().Length == 0 ? null : new object[] { context });
    }

    private void PlayFeedback(ActionFeedback feedback)
    {
        feedback.Play(Owner.transform.position);
        if (feedback.animClip != null)
            EventBus.Publish(new EntityAnimationEvent(Owner, feedback.animClip));
    }
}
