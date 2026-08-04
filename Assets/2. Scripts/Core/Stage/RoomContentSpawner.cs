using System.Collections.Generic;
using UnityEngine;

public class RoomContentSpawner : MonoBehaviour, 
    IObserver<StageNodeChangedEvent>,
    IObserver<EntityDeadEvent>,
    IObserver<PickupCollectedEvent>,
    IObserver<StageNodeClearedEvent>,
    IObserver<PickupSpawnedEvent>
{
    [SerializeField] private Player player;

    private GameObject currentRoom;
    private readonly List<Entity> aliveActors = new();
    private readonly List<WorldPickup> roomPickups = new();

    private void Awake()
    {
        EventBus.Subscribe<StageNodeChangedEvent>(this);
        EventBus.Subscribe<EntityDeadEvent>(this);
        EventBus.Subscribe<PickupCollectedEvent>(this);
        EventBus.Subscribe<StageNodeClearedEvent>(this);
        EventBus.Subscribe<PickupSpawnedEvent>(this);
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

    public void OnNotify(PickupCollectedEvent e) => roomPickups.Remove(e.Pickup);
    public void OnNotify(StageNodeClearedEvent e) => SpawnPickups();
    public void OnNotify(PickupSpawnedEvent e) => roomPickups.Add(e.Pickup);

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
        var stageData = StageManager.Instance.CurrentStageData;
        if (node.roomType != RoomType.Battle)
            return;

        foreach (var point in currentRoom.GetComponentsInChildren<PickupSpawnPoint>())
        {
            var payload = BuildPayload(point, node.battleRoomType, stageData);
            if (payload == null) continue;

            WorldInteractionManager.Instance.Spawn(payload, point.transform.position);
        }
    }

    private IInteractable BuildPayload(PickupSpawnPoint point, BattleRoomType roomType, StageData stageData)
    {
        if (point.rewardAsset is Weapon weapon) return new WeaponPickup(weapon);
        if (point.rewardAsset is SkillData skill) return new SkillPickup(skill);


        switch (roomType)
        {
            case BattleRoomType.Skill when stageData.skillRewardPool.Count > 0:
                return new SkillPickup(stageData.skillRewardPool[Random.Range(0, stageData.skillRewardPool.Count)]);
            case BattleRoomType.Weapon when stageData.weaponRewardPool.Count > 0:
                return new WeaponPickup(stageData.weaponRewardPool[Random.Range(0, stageData.weaponRewardPool.Count)]);
            default:
                return null;
        }
    }
}
