using System.Collections.Generic;
using UnityEngine;

public class WorldPickupManager : Singleton<WorldPickupManager>
{
    [SerializeField] private WorldPickup pickupPrefab;
    private readonly List<WorldPickup> pickups = new();

    public void Register(WorldPickup pickup) => pickups.Add(pickup);
    public void Unregister(WorldPickup pickup) => pickups.Remove(pickup);

    public WorldPickup GetNearestInRange(Vector3 position, float radius)
    {
        WorldPickup nearest = null;
        float nearestSqr = radius * radius;
        foreach (var p in pickups)
        {
            if (p == null) continue;
            float sqr = (p.transform.position - position).sqrMagnitude;
            if (sqr > nearestSqr) continue;
            if (nearest == null || sqr < nearestSqr)
            {
                nearest = p;
                nearestSqr = sqr;
            }
        }
        return nearest;
    }

    public void Spawn(IPickupable payload, Vector3 position)
    {
        var instance = Instantiate(pickupPrefab, position, Quaternion.identity);
        instance.Init(payload);
    }

    public void ClearAll()
    {
        foreach (var p in pickups)
        {
            if (p != null) Destroy(p.gameObject);
        }
        pickups.Clear();
    }
}
