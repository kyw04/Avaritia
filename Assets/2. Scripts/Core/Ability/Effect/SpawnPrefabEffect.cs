using UnityEngine;

// 시전자(Self) 또는 대상(Target) 위치에 프리팹을 풀로 생성한다.
// throwOnSpawn이면 왼쪽/오른쪽 중 무작위로 위 대각선(throwVelocity)으로 던지고, 바닥에 닿으면 멈춘다(ThrowLanding).
[System.Serializable]
public class SpawnPrefabEffect : IAbilityEffect
{
    public GameObject prefab;
    public BuffTarget spawnAt = BuffTarget.Target;
    public Vector2 offset;
    public bool throwOnSpawn;
    // x는 옆으로, y는 위로 던지는 속도.
    public Vector2 throwVelocity = new(3f, 7f);
    // 0이면 항상 같은 속도, 0.3이면 70~100% 사이에서 무작위.
    [Range(0f, 1f)] public float throwRandomness = 0.3f;

    public void Apply(AbilityContext context)
    {
        Transform t = spawnAt == BuffTarget.Self ? context.Caster.Mono.transform : context.Target;
        if (t == null) return;
        var instance = ObjectPoolManager.Instance.Spawn(prefab, t.position + (Vector3)offset, Quaternion.identity);
        if (!throwOnSpawn) return;

        // 던지기용 컴포넌트가 없는 프리팹도 옵션만 켜면 던질 수 있게 붙여 준다(풀 인스턴스에 한 번만 붙는다).
        if (!instance.TryGetComponent<ThrowLanding>(out var landing))
            landing = instance.AddComponent<ThrowLanding>();

        float side = Random.value < 0.5f ? -1f : 1f;
        float scale = Random.Range(1f - throwRandomness, 1f);
        landing.Launch(new Vector2(throwVelocity.x * side, throwVelocity.y) * scale);
    }
}
