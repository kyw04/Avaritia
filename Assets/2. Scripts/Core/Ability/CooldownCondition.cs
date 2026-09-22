using UnityEngine;

[System.Serializable]
public class CooldownCondition : IAbilityCondition
{
    public bool IsMet(AbilityContext context) => Time.time >= context.State.CooldownEndTime;
}
