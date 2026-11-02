using UnityEngine;

public struct AbilityContext
{
    public IAttacker Caster;
    public Transform Target;
    public Entity EventSource;
    public float Value;
    public AbilityRuntimeState State;
    // 발동시킨 입력(스킬 키 등)이 아직 눌려 있는지. 입력 없이 발동된 경우(이벤트 트리거, AI)는 null.
    public System.Func<bool> IsInputHeld;
}
