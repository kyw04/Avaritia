using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

// 인스펙터의 입력 함수 목록에서 고를 수 있는 무기 메서드에 붙인다.
// 매개변수는 없거나 InputAction.CallbackContext 하나만 받을 수 있다.
[AttributeUsage(AttributeTargets.Method)]
public class InputMethodAttribute : Attribute { }

// 장착 중에만 PlayerInputActions의 Gameplay 입력(started/performed/canceled)을 받아,
// 인스펙터에서 고른 액션/단계에 맞는 애니메이션을 재생하고 [InputMethod] 메서드를 실행하는 무기.
// holdDuration이 0보다 크면 단계 대신 액션을 그 시간만큼 누르고 있어야 실행된다.
public abstract class InputWeapon : Weapon
{
    [Serializable]
    public class InputMethodBinding
    {
        public InputActionReference action;
        public InputActionPhase phase = InputActionPhase.Performed;
        [Min(0)] public float holdDuration;
        public bool keepHoldOnOtherInput;
        public AnimationClip holdStartClip;
        public string methodName;
        public AnimationClip clip;
    }

    [SerializeField] private List<InputMethodBinding> inputMethods = new();

    private Dictionary<string, MethodInfo> methodCache;
    private readonly Dictionary<InputMethodBinding, (Coroutine routine, InputAction action)> holds = new();

    protected PlayerInputActions.GameplayActions Gameplay => InputHandler.Instance.InputAction.Gameplay;

    // 에디터 드롭다운과 런타임 호출이 같은 기준으로 메서드를 찾는다.
    public static IEnumerable<MethodInfo> GetInputMethods(Type type) =>
        type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.IsDefined(typeof(InputMethodAttribute), true) && HasValidParameters(m));

    private static bool HasValidParameters(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.Length == 0 ||
               (parameters.Length == 1 && parameters[0].ParameterType == typeof(InputAction.CallbackContext));
    }

    public override void Equip(Entity owner, ImageCountBar countBar = null)
    {
        base.Equip(owner, countBar);
        methodCache ??= GetInputMethods(GetType()).GroupBy(m => m.Name).ToDictionary(g => g.Key, g => g.First());
        Gameplay.Get().actionTriggered += HandleInput;
    }

    public override void Unequip()
    {
        foreach (var binding in holds.Keys.ToList())
            StopHold(binding);
        base.Unequip();
        Gameplay.Get().actionTriggered -= HandleInput;
    }

    // 에셋의 액션(InputActionReference)과 생성된 PlayerInputActions의 액션은 인스턴스가 달라 id로 비교한다.
    // 메서드와 애니메이션은 각각 비워둘 수 있다.
    private void HandleInput(InputAction.CallbackContext context)
    {
        CancelHoldsByOtherInput(context);

        foreach (var binding in inputMethods)
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
    // 시작할 때 holdStartClip, 완료하면 clip을 재생한다.
    private void HandleHold(InputMethodBinding binding, InputAction.CallbackContext context)
    {
        if (context.phase == InputActionPhase.Started)
        {
            StopHold(binding);
            PlayClip(binding.holdStartClip);
            holds[binding] = (Owner.StartCoroutine(HoldRoutine(binding, context)), context.action);
        }
        else if (context.phase == InputActionPhase.Canceled)
        {
            StopHold(binding);
        }
    }

    private IEnumerator HoldRoutine(InputMethodBinding binding, InputAction.CallbackContext context)
    {
        yield return new WaitForSeconds(binding.holdDuration);
        holds.Remove(binding);
        Execute(binding, context);
    }

    private void StopHold(InputMethodBinding binding)
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

    private void Execute(InputMethodBinding binding, InputAction.CallbackContext context)
    {
        PlayClip(binding.clip);

        if (string.IsNullOrEmpty(binding.methodName))
            return;

        if (!methodCache.TryGetValue(binding.methodName, out var method))
        {
            Debug.LogWarning($"{name}: [InputMethod] 메서드 '{binding.methodName}'를 찾을 수 없습니다.");
            return;
        }

        method.Invoke(this, method.GetParameters().Length == 0 ? null : new object[] { context });
    }

    private void PlayClip(AnimationClip clip)
    {
        if (clip != null)
            EventBus.Publish(new EntityAnimationEvent(Owner, clip));
    }
}
