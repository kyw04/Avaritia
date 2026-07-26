using System.Collections.Generic;
using UnityEngine;

public class RoomContentSpawner : MonoBehaviour, IObserver<StageNodeChangedEvent>, IObserver<EntityDeadEvent>
{
    private GameObject currentRoom;
    private readonly List<Entity> aliveActors = new();

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
    
        aliveActors.Clear();
        currentRoom = Instantiate(node.gameObject, Vector3.zero, Quaternion.identity);
    
        var stageData = StageManager.Instance.CurrentStageData;
        SpawnActors(stageData);
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

    private GameObject PickRandom(List<GameObject> prefabs)
    {
        if (prefabs == null || prefabs.Count == 0)
            return null;

        return prefabs[Random.Range(0, prefabs.Count)];
    }
}
