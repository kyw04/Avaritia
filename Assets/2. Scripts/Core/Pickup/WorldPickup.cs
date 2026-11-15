using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WorldPickup : MonoBehaviour, IInteractable, IPoolable
{
    [SerializeField] private WeaponData weaponAsset;
    [SerializeField] private AbilityData abilityAsset;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private SpriteRenderer spriteRender;
    [SerializeField] private Animator animator;
    [SerializeField] private Light2D light2D;
    [SerializeField] private float groundCheckDelay = 0.25f;
    
    private Collider2D[] colliders;
    private IInteractable payload;
    private WorldInteractionManager manager;
    private List<WorldPickup> batchGroup;
    // 풀에서 무기 픽업으로 다시 쓸 때 등급 색을 지우기 위한 프리팹 원래 색.
    private Color defaultSpriteColor;
    private Color defaultLightColor;
    
    private float groundCheckTimer;
    private bool isReady;

    public string DisplayName => payload.DisplayName;
    public Sprite Icon => payload.Icon;
    public Transform Transform => transform;
    public string Details => payload.Details;
    // 상자에서 튀어나온 픽업은 같은 상자의 픽업이 모두 착지해야 줍을 수 있다. 그 외(드롭, 버린 무기, 씬 배치)는 바로 줍는다.
    public bool IsReady => isReady && BatchGroupIsReady();
    private bool BatchGroupIsReady() => batchGroup == null || batchGroup.All(pickup => pickup.isReady);

    // Enemy/Boss와 동일한 이유: Awake에서도 등록해야 씬 배치 픽업이 감지되고,
    // OnSpawn은 풀 재사용 시 다시 등록한다(이중 등록 방지를 위해 먼저 해제 후 등록).
    private void Awake()
    {
        colliders = GetComponents<Collider2D>();
        defaultSpriteColor = spriteRender.color;
        defaultLightColor = light2D.color;

        if (weaponAsset != null)
            payload = new WeaponPickup(weaponAsset);
        else if (abilityAsset != null)
            payload = abilityAsset is Item item ? new ItemPickup(item) : new AbilityPickup(abilityAsset);

        if (payload != null)
            ApplyVisual();

        manager = WorldInteractionManager.Instance;
        manager.Register(this);
        isReady = true;
    }

    private void OnDestroy() => manager.Unregister(this);

    public void OnSpawn()
    {
        isReady = true;
        SetVisible(true);
        manager.Unregister(this);
        manager.Register(this);
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
    }

    public void OnDespawn()
    {
        isReady = false;
        manager.Unregister(this);
        batchGroup = null;
    }

    public void Init(IInteractable payload)
    {
        isReady = true;
        this.payload = payload;
        weaponAsset = (payload as WeaponPickup)?.Weapon.Data;
        abilityAsset = (payload as AbilityPickup)?.Ability ?? (payload as ItemPickup)?.Item;
        ApplyVisual();
    }

    // 무기는 아이콘을 그대로 보여주고, 어빌리티/아이템은 애니메이션을 켜고 등급 색을 입힌다.
    // 풀에서 재사용되면 이전 픽업의 상태가 남아 있으므로 매번 양쪽을 모두 다시 정한다.
    private void ApplyVisual()
    {
        bool isItem = abilityAsset is Item;
        animator.enabled = isItem;

        if (!isItem)
        {
            spriteRender.color = defaultSpriteColor;
            light2D.color = defaultLightColor;
            ApplyIcon();
            return;
        }

        var color = abilityAsset.rank != null ? abilityAsset.rank.color : defaultSpriteColor;
        spriteRender.color = color;
        light2D.color = color;
    }

    // 상자 픽업은 Launch로 튀어나와 착지할 때까지 줍지 못하고, 튀어나오기 전까지는 보이지 않는다.
    public void SetBatchGroup(List<WorldPickup> group)
    {
        batchGroup = group;
        isReady = false;
        SetVisible(false);
    }

    private void SetVisible(bool value)
    {
        spriteRenderer.enabled = value;
        light2D.enabled = value;
    }

    private void SetCollidersEnabled(bool value)
    {
        foreach (var col in colliders)
            col.enabled = value;
    }

    public void Launch(Vector2 velocity)
    {
        isReady = false;
        SetVisible(true);
        SetCollidersEnabled(true);
        groundCheckTimer = Time.time + groundCheckDelay;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = velocity;
    }
    

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time < groundCheckTimer)
            return;
        
        int layer = collision.gameObject.layer;
        if (layer != LayerMask.NameToLayer("Floor") && layer != LayerMask.NameToLayer("Platform"))
            return;

        isReady = true;
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        SetCollidersEnabled(false);
    }

    public bool NeedsChoice(Player player) => payload.NeedsChoice(player);

    public void Interact(Player player, InteractChoice choice)
    {
        if (!IsReady)
            return;
        
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
