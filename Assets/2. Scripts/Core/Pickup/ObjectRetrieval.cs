using UnityEngine;

// 시전자가 자기가 내보낸 오브젝트(되돌아온 톱날 등)를 다시 얻었을 때 알린다.
// prefab은 풀 원본 기준이라 같은 프리팹으로 만든 인스턴스는 모두 같은 값으로 전달된다.
public static class ObjectRetrieval
{
    public static event System.Action<Entity, GameObject> Retrieved;

    // 도메인 리로드가 꺼져 있어 static 이벤트 구독이 Play 세션 사이에 남으므로 매 Play 시작 시 비운다
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Retrieved = null;

    public static void Notify(Entity owner, GameObject instance)
    {
        var prefab = ObjectPoolManager.Instance.GetPrefab(instance);
        Retrieved?.Invoke(owner, prefab != null ? prefab : instance);
    }
}
