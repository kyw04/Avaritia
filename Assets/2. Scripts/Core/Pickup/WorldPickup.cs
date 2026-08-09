using System.Collections.Generic;
using UnityEngine;

public class WorldPickup : MonoBehaviour, IInteractable, IPoolable
{
    [SerializeField] private Weapon weaponAsset;
    [SerializeField] private SkillData skillAsset;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;

    private IInteractable payload;
    private WorldInteractionManager manager;
    private List<WorldPickup> batchGroup;

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
        rb.bodyType = RigidbodyType2D.Dynamic;
    }

    public void OnDespawn()
    {
        manager.Unregister(this);
        batchGroup = null;
    }

    public void Init(IInteractable payload)
    {
        this.payload = payload;
        weaponAsset = (payload as WeaponPickup)?.Weapon;
        skillAsset = (payload as SkillPickup)?.Skill;
        ApplyIcon();
    }

    public void SetBatchGroup(List<WorldPickup> group) => batchGroup = group;

    // 상자에서 튀어나올 때 위/좌우 속도를 부여한다. Floor/Platform에 닿으면 OnCollisionEnter2D에서 고정된다.
    public void Launch(Vector2 velocity) => rb.linearVelocity = velocity;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        int layer = collision.gameObject.layer;
        if (layer != LayerMask.NameToLayer("Floor") && layer != LayerMask.NameToLayer("Platform"))
            return;

        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public bool NeedsChoice(Player player) => payload.NeedsChoice(player);

    public void Interact(Player player, InteractChoice choice)
    {
        EventBus.Publish(new PickupCollectedEvent(this));
        payload.Interact(player, choice);
        RemoveBatchMates();
        Remove();
    }

    // 같은 박스에서 나온 나머지 픽업들을 즉시 제거한다 (하나만 습득 가능하게).
    private void RemoveBatchMates()
    {
        if (batchGroup == null) return;

        foreach (var mate in batchGroup)
        {
            if (mate != null && mate != this)
                mate.Remove();
        }
        batchGroup = null;
    }

    // 풀에서 온 인스턴스면 반납(재사용), 씬에 직접 배치된 인스턴스면 기존처럼 파괴.
    public void Remove()
    {
        if (ObjectPoolManager.Instance.IsPooled(gameObject))
            ObjectPoolManager.Instance.Despawn(gameObject);
        else
            Destroy(gameObject);
    }

    private void ApplyIcon()
    {
        
        spriteRenderer.sprite = payload?.Icon != null ?
            payload.Icon : 
            Sprite.Create(new Texture2D(16, 16), new Rect(0, 0, 16, 16), Vector2.zero);
    }
}
