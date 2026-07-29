using System.Collections.Generic;
using UnityEngine;

public class WorldInteractionManager : Singleton<WorldInteractionManager>
{
    [SerializeField] private WorldPickup pickupPrefab;
    private readonly List<IInteractable> interactables = new();

    public void Register(IInteractable interactable) => interactables.Add(interactable);
    public void Unregister(IInteractable interactable) => interactables.Remove(interactable);

    public IInteractable GetNearestInRange(Vector3 position, float radius)
    {
        IInteractable nearest = null;
        float nearestSqr = radius * radius;
        foreach (var i in interactables)
        {
            if ((i as Object) == null) continue;
            float sqr = (i.Transform.position - position).sqrMagnitude;
            if (sqr > nearestSqr) continue;
            if (nearest == null || sqr < nearestSqr)
            {
                nearest = i;
                nearestSqr = sqr;
            }
        }
        return nearest;
    }

    public WorldPickup Spawn(IInteractable payload, Vector3 position)
    {
        var instance = ObjectPoolManager.Instance.Spawn(pickupPrefab, position, Quaternion.identity);
        instance.Init(payload);
        return instance;
    }

    public void ClearAll()
    {
        for (int i = interactables.Count - 1; i >= 0; i--)
            if (interactables[i] is WorldPickup wp)
                wp.Remove();
    }
}
