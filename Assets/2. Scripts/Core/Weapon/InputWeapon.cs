using System;
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
public abstract class InputWeapon : Weapon
{
    [Serializable]
    public class InputMethodBinding
    {
        public InputActionReference action;
        public InputActionPhase phase = InputActionPhase.Performed;
        public string methodName;
        public AnimationClip clip;
    }

    [SerializeField] private List<InputMethodBinding> inputMethods = new();

    private Dictionary<string, MethodInfo> methodCache;

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
        base.Unequip();
        Gameplay.Get().actionTriggered -= HandleInput;
    }

    // 에셋의 액션(InputActionReference)과 생성된 PlayerInputActions의 액션은 인스턴스가 달라 id로 비교한다.
    // 메서드와 애니메이션은 각각 비워둘 수 있다.
    private void HandleInput(InputAction.CallbackContext context)
    {
        foreach (var binding in inputMethods)
        {
            if (binding.action == null || binding.action.action.id != context.action.id || binding.phase != context.phase)
                continue;

            if (binding.clip != null)
                EventBus.Publish(new EntityAnimationEvent(Owner, binding.clip));

            if (string.IsNullOrEmpty(binding.methodName))
                continue;

            if (!methodCache.TryGetValue(binding.methodName, out var method))
            {
                Debug.LogWarning($"{name}: [InputMethod] 메서드 '{binding.methodName}'를 찾을 수 없습니다.");
                continue;
            }

            method.Invoke(this, method.GetParameters().Length == 0 ? null : new object[] { context });
        }
    }
}
