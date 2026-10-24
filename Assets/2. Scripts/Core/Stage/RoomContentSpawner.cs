using System.Collections.Generic;
using UnityEngine;

public class RoomContentSpawner : MonoBehaviour,
    IObserver<StageNodeChangedEvent>,
    IObserver<EntityDeadEvent>,
    IObserver<EnemyConvertedEvent>,
    IObserver<PickupCollectedEvent>,
    IObserver<StageNodeClearedEvent>,
    IObserver<PickupSpawnedEvent>,
    IObserver<ItemBoxSpawnedEvent>
{
    [SerializeField] private Player player;

    private GameObject currentRoom;
    private readonly List<Entity> aliveActors = new();
    private readonly List<Entity> allies = new();
    private readonly List<WorldPickup> roomPickups = new();
    private readonly List<ItemBox> roomBoxes = new();

    private void Awake()
    {
        EventBus.Subscribe<StageNodeChangedEvent>(this);
        EventBus.Subscribe<EntityDeadEvent>(this);
        EventBus.Subscribe<EnemyConvertedEvent>(this);
        EventBus.Subscribe<PickupCollectedEvent>(this);
        EventBus.Subscribe<StageNodeClearedEvent>(this);
        EventBus.Subscribe<PickupSpawnedEvent>(this);
        EventBus.Subscribe<ItemBoxSpawnedEvent>(this);
    }

    private void OnDestroy()
    {
        EventBus.UnsubscribeAll(this);
    }

    public void OnNotify(StageNodeChangedEvent e)  => Spawn(e.Current);

    public void OnNotify(EntityDeadEvent e)
    {
        if (!aliveActors.Remove(e.Source))
            return;

        if (aliveActors.Count == 0)
            StageManager.Instance.NotifyRoomCleared();
    }

    // 아군이 된 적은 방 클리어 대상에서 빠지지만, 방을 나갈 때는 함께 정리한다.
    public void OnNotify(EnemyConvertedEvent e)
    {
        if (!aliveActors.Remove(e.Source))
            return;

        allies.Add(e.Source);
        if (aliveActors.Count == 0)
            StageManager.Instance.NotifyRoomCleared();
    }

    public void OnNotify(PickupCollectedEvent e) => roomPickups.Remove(e.Pickup);
    public void OnNotify(StageNodeClearedEvent e) => SpawnPickups();
    public void OnNotify(PickupSpawnedEvent e) => roomPickups.Add(e.Pickup);
    public void OnNotify(ItemBoxSpawnedEvent e) => roomBoxes.Add(e.Box);

    public void Spawn(StageNode node)
    {
        if (currentRoom != null)
            Destroy(currentRoom);

        foreach (var actor in aliveActors)
            if (actor != null)
                ObjectPoolManager.Instance.Despawn(actor.gameObject);
        aliveActors.Clear();

        // 방 안에서 죽은 아군은 이미 풀로 돌아갔으므로 활성 상태인 것만 정리한다.
        foreach (var ally in allies)
            if (ally != null && ally.gameObject.activeSelf)
                ObjectPoolManager.Instance.Despawn(ally.gameObject);
        allies.Clear();

        foreach (var pickup in roomPickups)
            if (pickup != null && pickup.gameObject.activeSelf)
                ObjectPoolManager.Instance.Despawn(pickup.gameObject);
        roomPickups.Clear();

        foreach (var box in roomBoxes)
            if (box != null && box.gameObject.activeSelf)
                ObjectPoolManager.Instance.Despawn(box.gameObject);
        roomBoxes.Clear();

        currentRoom = Instantiate(node.gameObject, Vector3.zero, Quaternion.identity);

        PositionPlayer();

        var stageData = StageManager.Instance.CurrentStageData;
        SpawnActors(stageData);
    }

    private void PositionPlayer()
    {
        var spawnPoint = currentRoom.GetComponentInChildren<PlayerSpawnPoint>();
        if (spawnPoint == null)
            return;

        player.Rb.position = spawnPoint.transform.position;
        player.Rb.linearVelocity = Vector2.zero;
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
        var node = StageManager.Instance.CurrentNode;
        if (node.roomType != RoomType.Battle)
            return;

        var (pool, count) = ResolvePool(StageManager.Instance.CurrentStageData, StageManager.Instance.CurrentBattleRoomType);
        if (pool == null) return;

        foreach (var point in currentRoom.GetComponentsInChildren<PickupSpawnPoint>())
            WorldInteractionManager.Instance.SpawnBox(pool, count, point.transform.position);
    }

    private static (List<ScriptableObject> pool, int count) ResolvePool(StageData stageData, BattleRoomType type) => type switch
    {
        BattleRoomType.Weapon => (stageData.weaponRewardPool.rewards, stageData.weaponRewardPool.itemCount),
        BattleRoomType.Skill => (stageData.skillRewardPool.rewards, stageData.skillRewardPool.itemCount),
        BattleRoomType.Item => (stageData.itemRewardPool.rewards, stageData.itemRewardPool.itemCount),
        _ => (null, 0),
    };
}
