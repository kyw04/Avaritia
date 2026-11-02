// 시전자 반경 radius 안에 markerId 마커가 있으면 통과.
[System.Serializable]
public class NearbyMarkerCondition : IAbilityCondition
{
    public string markerId;
    public float radius = 1.5f;

    public bool IsMet(AbilityContext context) =>
        context.Caster != null &&
        AbilityMarker.FindNearest(markerId, context.Caster.Mono.transform.position, radius) != null;
}
