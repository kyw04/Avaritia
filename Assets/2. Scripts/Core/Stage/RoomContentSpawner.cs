using System.Collections.Generic;
using UnityEngine;

public class RoomContentSpawner : MonoBehaviour, IObserver<StageNodeChangedEvent>
{
    private GameObject currentRoom;

    private void Awake()
    {
        EventBus.Subscribe<StageNodeChangedEvent>(this);
    }

    private void OnDestroy()
    {
        EventBus.UnsubscribeAll(this);
    }

    public void OnNotify(StageNodeChangedEvent gameEvent)
    {
        Spawn(gameEvent.Current);
    }

    public void Spawn(StageNode node)
    {
        if (currentRoom != null)
            Destroy(currentRoom);

        var stageData = StageManager.Instance.CurrentStageData;
        var roomPrefab = ResolveRoomPrefab(node.roomType, stageData);

        if (roomPrefab == null)
        {
            Debug.LogError($"RoomContentSpawner: no room prefab available for {node.roomType}");
            return;
        }

        currentRoom = Instantiate(roomPrefab, Vector3.zero, Quaternion.identity);
        SpawnActors(node.roomType, stageData);
    }

    private void SpawnActors(RoomType roomType, StageData stageData)
    {
        var actorPrefab = ResolveActorPrefab(roomType, stageData);
        if (actorPrefab == null)
            return;

        foreach (var spawnPoint in currentRoom.GetComponentsInChildren<EnemySpawnPoint>())
            Instantiate(actorPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation, currentRoom.transform);
    }

    private GameObject ResolveRoomPrefab(RoomType roomType, StageData stageData)
    {
        switch (roomType)
        {
            case RoomType.Battle:
                return PickRandom(stageData.battleRoomPrefabs);
            case RoomType.Boss:
                return stageData.bossRoomPrefab;
            case RoomType.Shop:
                return stageData.shopRoomPrefab;
            default:
                return null;
        }
    }

    private GameObject ResolveActorPrefab(RoomType roomType, StageData stageData)
    {
        switch (roomType)
        {
            case RoomType.Battle:
                return stageData.enemyPrefab;
            case RoomType.Boss:
                return stageData.bossPrefab;
            default:
                return null;
        }
    }

    private GameObject PickRandom(List<GameObject> prefabs)
    {
        if (prefabs == null || prefabs.Count == 0)
            return null;

        return prefabs[Random.Range(0, prefabs.Count)];
    }
}
