using System.Collections.Generic;
using UnityEngine;

public class RoomContentSpawner : MonoBehaviour, IObserver<StageNodeChangedEvent>, IObserver<EntityDeadEvent>
{
    private GameObject currentRoom;
    private readonly List<Entity> aliveActors = new();
    private readonly List<WorldPickup> roomPickups = new();

    private void Awake()
    {
        EventBus.Subscribe<StageNodeChangedEvent>(this);
        EventBus.Subscribe<EntityDeadEvent>(this);
    }

    private void OnDestroy()
    {
        EventBus.UnsubscribeAll(this);
    }

    public void OnNotify(StageNodeChangedEvent e)
    {
        Spawn(e.Current);
    }

    public void OnNotify(EntityDeadEvent e)
    {
        if (!aliveActors.Remove(e.Source))
            return;

        if (aliveActors.Count == 0)
            StageManager.Instance.NotifyRoomCleared();
    }

    public void Spawn(StageNode node)
    {
        if (currentRoom != null)
            Destroy(currentRoom);

        foreach (var actor in aliveActors)
            if (actor != null)
                ObjectPoolManager.Instance.Despawn(actor.gameObject);
        aliveActors.Clear();

        foreach (var pickup in roomPickups)
            if (pickup != null && pickup.gameObject.activeSelf)
                ObjectPoolManager.Instance.Despawn(pickup.gameObject);
        roomPickups.Clear();

        currentRoom = Instantiate(node.gameObject, Vector3.zero, Quaternion.identity);

        var stageData = StageManager.Instance.CurrentStageData;
        SpawnActors(stageData);
        SpawnPickups();
    }

    private void SpawnActors(StageData stageData)
    {
        foreach (var spawnPoint in currentRoom.GetComponentsInChildren<EntitySpawnPoint>())
        {
            var actor = ObjectPoolManager.Instance.Spawn(spawnPoint.entityPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            var entity = actor.GetComponent<Entity>();
            if (entity != null)
                aliveActors.Add(entity);
        }
    }

    private void SpawnPickups()
    {
        foreach (var point in currentRoom.GetComponentsInChildren<PickupSpawnPoint>())
        {
            IInteractable payload = null;
            if (point.weaponAsset != null) payload = new WeaponPickup(point.weaponAsset);
            else if (point.skillAsset != null) payload = new SkillPickup(point.skillAsset);

            if (payload == null) continue;

            var pickup = WorldInteractionManager.Instance.Spawn(payload, point.transform.position);
            roomPickups.Add(pickup);
        }
    }

    private GameObject PickRandom(List<GameObject> prefabs)
    {
        if (prefabs == null || prefabs.Count == 0)
            return null;

        return prefabs[Random.Range(0, prefabs.Count)];
    }
}
