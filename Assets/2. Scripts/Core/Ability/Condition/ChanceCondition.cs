using UnityEngine;

[System.Serializable]
public class ChanceCondition : IAbilityCondition
{
    [Range(0, 100)] public float percent;

    public bool IsMet(AbilityContext context) => Random.Range(0f, 100f) < percent;
}
