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
public class WeaponData : ScriptableObject, IInfo, ISaveId
{
    [SerializeField, HideInInspector] private string saveId;
    public string SaveId => saveId;

    private void OnValidate()
    {
#if UNITY_EDITOR
        SaveIdUtility.Sync(this, ref saveId);
#endif
    }

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
    // 공중에서 쓰는 콤보. 비워두면 이 무기는 공중 공격을 할 수 없다.
    public AttackDataCombo airCombo;
    public bool holdToAttack;
    [SerializeField] private DisplayInfo info = new();
    public List<AbilityData> passiveAbilities = new();
    public ActionFeedback swing;
    public ActionFeedback hit;
    [SerializeReference, SubclassSelector] public List<WeaponModule> modules = new();
    [SerializeField] private List<InputMethodBinding> inputMethods = new();

    public IReadOnlyList<InputMethodBinding> InputMethods => inputMethods;
    public string DisplayName => info.DisplayName;
    public string Details => info.Details;
    public Sprite Icon => info.Icon;

    // 스킬 공격과 구분해 지상·공중 콤보에 속한 공격인지 확인한다.
    public bool IsComboAttack(AttackData data) =>
        (combo != null && combo.datas.Contains(data)) || (airCombo != null && airCombo.datas.Contains(data));

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
