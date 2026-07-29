using UnityEngine;

public class WorldPickup : MonoBehaviour, IInteractable, IPoolable
{
    [SerializeField] private Weapon weaponAsset;
    [SerializeField] private SkillData skillAsset;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private IInteractable payload;
    private WorldInteractionManager manager;

    public string DisplayName => payload.DisplayName;
    public Sprite Icon => payload.Icon;
    public Transform Transform => transform;

    // Enemy/Boss와 동일한 이유: Awake에서도 등록해야 씬 배치 픽업이 감지되고,
    // OnSpawn은 풀 재사용 시 다시 등록한다(이중 등록 방지를 위해 먼저 해제 후 등록).
    private void Awake()
    {
        if (weaponAsset != null) payload = new WeaponPickup(weaponAsset);
        else if (skillAsset != null) payload = new SkillPickup(skillAsset);

        ApplyIcon();
        manager = WorldInteractionManager.Instance;
        manager.Register(this);
    }

    private void OnDestroy() => manager.Unregister(this);

    public void OnSpawn()
    {
        manager.Unregister(this);
        manager.Register(this);
    }

    public void OnDespawn() => manager.Unregister(this);

    public void Init(IInteractable payload)
    {
        this.payload = payload;
        ApplyIcon();
    }

    public bool NeedsChoice(Player player) => payload.NeedsChoice(player);

    public void Interact(Player player, InteractChoice choice)
    {
        payload.Interact(player, choice);
        Remove();
    }

    // 풀에서 온 인스턴스면 반납(재사용), 씬에 직접 배치된 인스턴스면 기존처럼 파괴.
    public void Remove()
    {
        EventBus.Publish(new PickupCollectedEvent(this));

        if (ObjectPoolManager.Instance.IsPooled(gameObject))
            ObjectPoolManager.Instance.Despawn(gameObject);
        else
            Destroy(gameObject);
    }

    private void ApplyIcon()
    {
        if (payload?.Icon != null) spriteRenderer.sprite = payload.Icon;
    }
}
