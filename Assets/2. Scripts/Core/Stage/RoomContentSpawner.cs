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

    // 미니맵이 방과 방 안 오브젝트를 표시할 때 읽는다.
    public GameObject CurrentRoom => currentRoom;
    public IReadOnlyList<Entity> AliveActors => aliveActors;
    public IReadOnlyList<WorldPickup> RoomPickups => roomPickups;
    public IReadOnlyList<ItemBox> RoomBoxes => roomBoxes;

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

    public void OnNotify(StageNodeChangedEvent e)  => Spawn(e.Current, e.Restored);

    public void OnNotify(EntityDeadEvent e)
    {
        if (!aliveActors.Remove(e.Source))
            return;

        if (aliveActors.Count == 0)
            StageManager.Instance.NotifyRoomCleared();
    }

    // 아군이 된 적은 방 클리어 대상에서 빠지지만, 방을 나갈 때는 함께 정리한다.
    // 방에서 스폰되지 않은 아군(포탑 등)도 방을 나갈 때 정리한다.
    public void OnNotify(EnemyConvertedEvent e)
    {
        allies.Add(e.Source);
        if (aliveActors.Remove(e.Source) && aliveActors.Count == 0)
            StageManager.Instance.NotifyRoomCleared();
    }

    public void OnNotify(PickupCollectedEvent e) => roomPickups.Remove(e.Pickup);
    public void OnNotify(StageNodeClearedEvent e)
    {
        // 복원된 클리어 방은 보상 상자를 다시 만들지 않는다.
        if (!e.Restored)
            SpawnPickups();
    }
    public void OnNotify(PickupSpawnedEvent e) => roomPickups.Add(e.Pickup);
    public void OnNotify(ItemBoxSpawnedEvent e) => roomBoxes.Add(e.Box);

    public void Spawn(StageNode node, bool restored = false)
    {
        if (currentRoom != null)
            Destroy(currentRoom);

        // 런 초기화로 이미 풀에 돌아간 적은 건너뛴다.
        foreach (var actor in aliveActors)
            if (actor != null && actor.gameObject.activeSelf)
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

        // 스킬로 생성된 마커 오브젝트(예: 기계장치 더미)도 다음 방으로 따라오지 않게 정리한다.
        foreach (var marker in new List<AbilityMarker>(AbilityMarker.Active))
            if (ObjectPoolManager.Instance.IsPooled(marker.gameObject))
                ObjectPoolManager.Instance.Despawn(marker.gameObject);

        currentRoom = Instantiate(node.gameObject, Vector3.zero, Quaternion.identity);

        PositionPlayer(restored);

        // 복원된 클리어 방은 적을 만들지 않는다.
        if (restored)
            return;

        var stageData = StageManager.Instance.CurrentStageData;
        SpawnActors(stageData);
    }

    // 클리어 상태로 복원한 방은 보상 위치에서 시작한다. 보상 위치가 없는 방(시작 방, 상점)은 플레이어 스폰 지점을 쓴다.
    private void PositionPlayer(bool restored)
    {
        Component spawnPoint = restored ? currentRoom.GetComponentInChildren<PickupSpawnPoint>() : null;
        if (spawnPoint == null)
            spawnPoint = currentRoom.GetComponentInChildren<PlayerSpawnPoint>();
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
            if (entity is Enemy enemy)
                enemy.SetPatrolArea(spawnPoint.PatrolCenter, spawnPoint.patrolSize);
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
