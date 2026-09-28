// 이 바인딩(아이템 한 개)이 지금까지 count번 미만 발동했을 때만 참. 1이면 "1회용".
[System.Serializable]
public class MaxActivationsCondition : IAbilityCondition
{
    public int count = 1;

    public bool IsMet(AbilityContext context) => context.State != null && context.State.ActivationCount < count;
}
