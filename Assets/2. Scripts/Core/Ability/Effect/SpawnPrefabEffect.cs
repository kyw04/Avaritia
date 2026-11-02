using UnityEngine;

// 시전자(Self) 또는 대상(Target) 위치에 프리팹을 풀로 생성한다.
[System.Serializable]
public class SpawnPrefabEffect : IAbilityEffect
{
    public GameObject prefab;
    public BuffTarget spawnAt = BuffTarget.Target;
    public Vector2 offset;

    public void Apply(AbilityContext context)
    {
        Transform t = spawnAt == BuffTarget.Self ? context.Caster.Mono.transform : context.Target;
        if (t == null) return;
        ObjectPoolManager.Instance.Spawn(prefab, t.position + (Vector3)offset, Quaternion.identity);
    }
}
