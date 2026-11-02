// 시전자 반경 안의 가장 가까운 markerId 마커 오브젝트를 제거(풀 반납)한다.
[System.Serializable]
public class ConsumeNearbyMarkerEffect : IAbilityEffect
{
    public string markerId;
    public float radius = 1.5f;

    public void Apply(AbilityContext context)
    {
        var marker = AbilityMarker.FindNearest(markerId, context.Caster.Mono.transform.position, radius);
        if (marker == null) return;
        if (ObjectPoolManager.Instance.IsPooled(marker.gameObject))
            ObjectPoolManager.Instance.Despawn(marker.gameObject);
        else
            marker.gameObject.SetActive(false);
    }
}
