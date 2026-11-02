using System.Collections.Generic;
using UnityEngine;

// Ability가 "근처에 무엇이 있는지" 찾기 위한 표식. markerId로 구분한다.
// OnEnable/OnDisable로 등록하므로 풀 Spawn/Despawn과 씬 배치 모두 자동으로 목록에 반영된다.
public class AbilityMarker : MonoBehaviour
{
    public string markerId;

    public static readonly List<AbilityMarker> Active = new();

    private void OnEnable() => Active.Add(this);
    private void OnDisable() => Active.Remove(this);

    public static AbilityMarker FindNearest(string id, Vector2 from, float radius)
    {
        AbilityMarker nearest = null;
        float best = radius * radius;
        foreach (var marker in Active)
        {
            if (marker.markerId != id) continue;
            float sqr = ((Vector2)marker.transform.position - from).sqrMagnitude;
            if (sqr > best) continue;
            best = sqr;
            nearest = marker;
        }
        return nearest;
    }
}
