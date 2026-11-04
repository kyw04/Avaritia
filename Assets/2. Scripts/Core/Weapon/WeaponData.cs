using System;
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

// 무기 설계값. 플레이 중에는 읽기만 하고, 런타임 상태는 WeaponInstance가 가진다.
[CreateAssetMenu(menuName = "Scriptable Objects/Weapon/WeaponData")]
public class WeaponData : ScriptableObject
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

    public IReadOnlyList<InputMethodBinding> InputMethods => inputMethods;
    public string DisplayName => displayName;
    public string Details => description;
    public Sprite Icon => icon;

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
}
