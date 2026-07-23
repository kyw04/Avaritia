using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : Singleton<ObjectPoolManager>
{
    private readonly Dictionary<GameObject, Queue<GameObject>> pools = new();
    private readonly Dictionary<GameObject, GameObject> instanceToPrefab = new();

    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            Debug.LogError("ObjectPoolManager: prefab not assigned");
            return null;
        }

        if (!pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            pools[prefab] = queue;
        }

        GameObject instance;
        if (queue.Count > 0)
        {
            instance = queue.Dequeue();
            instance.transform.SetParent(null);
            instance.transform.SetPositionAndRotation(position, rotation);
        }
        else
        {
            instance = Instantiate(prefab, position, rotation);
            instanceToPrefab[instance] = prefab;
        }

        instance.SetActive(true);
        if (instance.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnSpawn();

        return instance;
    }

    public T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
    {
        if (prefab == null)
        {
            Debug.LogError("ObjectPoolManager: prefab not assigned");
            return null;
        }

        var go = Spawn(prefab.gameObject, position, rotation);
        return go == null ? null : go.GetComponent<T>();
    }

    /// <summary>
    /// 풀에 반납할 때는 반드시 이 메서드를 통해서만 반환해야 한다. SetActive(false)로 직접 비활성화하면
    /// activeSelf를 이용한 중복 반납 감지가 오작동하여 인스턴스가 유실될 수 있다.
    /// </summary>
    public void Despawn(GameObject instance)
    {
        if (instance == null) return;

        if (!instanceToPrefab.TryGetValue(instance, out var prefab))
        {
            Debug.LogWarning($"ObjectPoolManager: {instance.name} is not tracked by any pool, ignoring Despawn");
            return;
        }

        if (!instance.activeSelf)
        {
            Debug.LogWarning($"ObjectPoolManager: {instance.name} is already despawned, ignoring duplicate Despawn");
            return;
        }

        if (instance.TryGetComponent<IPoolable>(out var poolable))
            poolable.OnDespawn();

        instance.SetActive(false);
        instance.transform.SetParent(transform);
        pools[prefab].Enqueue(instance);
    }

    public void Despawn(Component instance)
    {
        if (instance == null) return;
        Despawn(instance.gameObject);
    }
}
