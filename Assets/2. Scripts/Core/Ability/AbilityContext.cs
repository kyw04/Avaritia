using UnityEngine;

public struct AbilityContext
{
    public IAttacker Caster;
    public Transform Target;
    public Entity EventSource;
    public float Value;
    public AbilityRuntimeState State;
}
