using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.U2D.Animation;

// 인스펙터의 입력 함수 목록에서 고를 수 있는 모듈 메서드에 붙인다.
// 매개변수는 없거나 InputAction.CallbackContext 하나만 받을 수 있다.
[AttributeUsage(AttributeTargets.Method)]
public class InputMethodAttribute : Attribute { }

[CreateAssetMenu(menuName = "Scriptable Objects/Weapon/Weapon")]
public class Weapon : ScriptableObject, IInventoryItem
{
    [Serializable]
    public class InputMethodBinding
    {
        public InputActionReference action;
        public InputActionPhase phase = InputActionPhase.Performed;
        [Min(0)] public float holdDuration;
        public bool keepHoldOnOtherInput;
        public ActionFeedback holdStartFeedback;
        public string methodName;
        public ActionFeedback feedback;
    }

    public SpriteLibraryAsset spriteLibraryAsset;
    public AttackDataCombo combo;
    public bool holdToAttack;
    public Sprite icon;
    public string displayName;
    [TextArea] public string description;
    public List<AbilityData> passiveAbilities = new();
    public ActionFeedback swing;
    public ActionFeedback hit;
    public float shakeForce = 0.3f;
    [Range(0f, 1f)] public float hitStopTimeScale = 0.1f;
    [Min(0)] public float hitStopDuration = 0.08f;
    public float knockbackForce = 5f;
    public float knockbackUpForce = 2f;
    [Min(0)] public float knockbackDuration = 0.2f;
    [SerializeReference, SubclassSelector] public List<WeaponModule> modules = new();
    [SerializeField] private List<InputMethodBinding> inputMethods = new();

    private WeaponAttack currentAttack;
    private bool inputSubscribed;
    private Dictionary<string, MethodInfo> methodCache;
    private readonly Dictionary<InputMethodBinding, (Coroutine routine, InputAction action)> holds = new();

    public Entity Owner { get; private set; }
    public bool IsRuntimeInstance { get; private set; }
    public string DisplayName => displayName;
    public string Details => description;
    public Sprite Icon => icon;

    // 런타임 상태(Owner, 탄약)를 에셋에 직접 쓰지 않도록 에셋일 때만 복제한다.
    // 이미 복제본이면(떨어뜨렸다 다시 주운 무기) 그대로 반환해 복제본이 쌓이지 않게 한다.
    public Weapon GetRuntimeInstance()
    {
        if (IsRuntimeInstance)
            return this;

        var runtime = Instantiate(this);
        runtime.name = name;
        runtime.IsRuntimeInstance = true;
        return runtime;
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

    // 에디터 드롭다운과 런타임 호출이 같은 기준으로 메서드를 찾는다.
    public static IEnumerable<(Type moduleType, MethodInfo method)> GetInputMethods(IEnumerable<WeaponModule> modules)
    {
        foreach (var type in modules.Where(m => m != null).Select(m => m.GetType()).Distinct())
        {
            var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(m => m.IsDefined(typeof(InputMethodAttribute), true) && HasValidParameters(m));
            foreach (var method in methods)
                yield return (type, method);
        }
    }

    private static bool HasValidParameters(MethodInfo method)
    {
        var parameters = method.GetParameters();
        return parameters.Length == 0 ||
               (parameters.Length == 1 && parameters[0].ParameterType == typeof(InputAction.CallbackContext));
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

        if (inputMethods.Count > 0)
        {
            methodCache = GetInputMethods(modules)
                .GroupBy(x => x.moduleType.Name + "." + x.method.Name)
                .ToDictionary(g => g.Key, g => g.First().method);
            InputHandler.Instance.InputAction.Gameplay.Get().actionTriggered += HandleInput;
            inputSubscribed = true;
        }
    }

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
        var attack = new WeaponAttack { Attacker = attacker, Index = index, Defender = defender, Data = combo.datas[index] };

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
    // 시작할 때 holdStartFeedback, 완료하면 feedback을 재생한다.
    private void HandleHold(InputMethodBinding binding, InputAction.CallbackContext context)
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

    // methodName은 "모듈타입명.메서드명" 형식이며, 타입명이 같은 첫 모듈에서 호출한다.
    private void Execute(InputMethodBinding binding, InputAction.CallbackContext context)
    {
        PlayFeedback(binding.feedback);

        if (string.IsNullOrEmpty(binding.methodName))
            return;

        if (!methodCache.TryGetValue(binding.methodName, out var method))
        {
            Debug.LogWarning($"{name}: [InputMethod] method '{binding.methodName}' do not found.");
            return;
        }

        var typeName = binding.methodName.Split('.')[0];
        var module = modules.FirstOrDefault(m => m != null && m.GetType().Name == typeName);
        if (module == null)
        {
            Debug.LogWarning($"{name}: module '{typeName}' do not found.");
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
