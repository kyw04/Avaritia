using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D.Animation;

public class Player : Entity, IStateOwner<Player>, IObserver<EntityDamagedEvent>, IObserver<RunResetEvent>
{
    [SerializeField] private WeaponData defaultWeapon;
    [SerializeField] private ImageCountBar ammoCountBar;
    [SerializeField] private PlayerInteractionController interactionController;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float dropThroughDuration = 1f;
    private Collider2D col;
    private SpriteLibrary spriteLibrary;
    private WeaponInstance weapon;

    public Player Owner { get; private set; }
    public Inventory Inventory { get; private set; }
    public IStateMachine Machine { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    public AbilityManager WeaponAbilities { get; private set; }
    public AbilityManager ItemAbilities { get; private set; }
    private StateManager stateManager;
    public WeaponInstance Weapon => weapon;
    public PlayerInteractionController InteractionController => interactionController;
    public float AttackReadyTime { get; set; }

    public bool CanAttack() => Restrictions.Can(ActionBlock.Attack) && Weapon != null &&Weapon.Data.combo != null && Weapon.Data.combo.Count > 0 && Time.time >= AttackReadyTime;

    public void EquipWeapon(WeaponInstance newWeapon)
    {
        PreserveHealthRatio(() =>
        {
            weapon?.Unequip();
            weapon = newWeapon;
            weapon.Equip(this, ammoCountBar);
            spriteLibrary.spriteLibraryAsset = weapon.Data.spriteLibraryAsset;
            RebindWeaponAbilities();
        });

        OnHealthChanged();
        OnDashCountChanged();
    }

    // 아이템·무기 쿨타임도 상태 아이콘으로 띄운다(스킬 슬롯 쿨타임은 SkillUI가 따로 보여 준다).
    public override void CollectStatusIcons(List<StatusIconEntry> into)
    {
        base.CollectStatusIcons(into);
        ItemAbilities?.CollectCooldownIcons(into);
        weapon?.CollectStatusIcons(into);
    }

    public override void CancelChannels()
    {
        base.CancelChannels();
        WeaponAbilities?.CancelChannels();
        ItemAbilities?.CancelChannels();
    }

    private void RebindWeaponAbilities()
    {
        WeaponAbilities?.UnbindAll();
        WeaponAbilities = new AbilityManager(this, weapon != null ? weapon.Data.passiveAbilities.ToArray() : Array.Empty<AbilityData>(), publishEvents: false);
        WeaponAbilities.BindAll();
    }

    // Reconciles ItemAbilities to the current Inventory contents instead of tearing everything
    // down and rebuilding it every time. A full rebuild would hand every held item a fresh
    // AbilityRuntimeState on every pickup/drop, which breaks per-copy buff stacking (see
    // AbilityManager.Rebind) and loses cooldown progress for items that didn't actually change.
    private void RebindItemAbilities()
    {
        var passives = new List<AbilityData>();
        foreach (var invItem in Inventory.Items)
            if (invItem is AbilityData item)
                passives.Add(item);
        ItemAbilities.Rebind(passives.ToArray());
    }

    protected override void Awake()
    {
        base.Awake();
        Inventory = new Inventory();
        Inventory.Changed += RebindItemAbilities;
        ItemAbilities = new AbilityManager(this, Array.Empty<AbilityData>(), publishEvents: false);

        Renderer = GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        spriteLibrary = GetComponentInChildren<SpriteLibrary>();

        Owner = this;
        Machine = new PlayerStateMachine(Owner);
        Machine.Init();
        stateManager = StateManager.Instance;

        if (defaultWeapon != null)
            weapon = new WeaponInstance(defaultWeapon);

        InitPlayerStats();

        RebindWeaponAbilities();
        RebindItemAbilities();
    }

    private void InitPlayerStats()
    {
        stats.Set(StatType.DoubleJumpCount, 0);
        stats.Set(StatType.DashCount, 0);
    }

    public bool TryDropThroughPlatform()
    {
        if (!IsGrounded) return false;

        var platform = Physics2D.OverlapCircle(groundCheck.position, groundRadius, platformLayer);
        if (platform == null) return false;

        StartCoroutine(DropThroughRoutine(platform));
        return true;
    }

    private IEnumerator DropThroughRoutine(Collider2D platform)
    {
        Physics2D.IgnoreCollision(col, platform, true);
        Rb.WakeUp();

        float elapsed = 0f;
        while (platform != null && col.bounds.max.y > platform.bounds.min.y && elapsed < dropThroughDuration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (platform != null)
            Physics2D.IgnoreCollision(col, platform, false);
    }

    protected override void Start()
    {
        base.Start();
        weapon?.Equip(this, ammoCountBar);
        EventBus.Publish(new EntityDashCountChangedEvent(this, MaxDashCount, MaxDashCount, Time.time));
    }
    
    public override void Die()
    {
        if (!TryMarkDead()) return;
        Machine.ChangeState<PlayerDeadState>();
        EventBus.Publish(new PlayerDiedEvent(this));
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EntityDamagedEvent>(this);
        EventBus.Subscribe<RunResetEvent>(this);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<EntityDamagedEvent>(this);
        EventBus.Unsubscribe<RunResetEvent>(this);
    }

    // 다시 시작: 새 런의 플레이어로 되돌린다. 위치는 시작 방을 만들 때 RoomContentSpawner가 옮긴다.
    public void OnNotify(RunResetEvent e)
    {
        // 살아 있는 채로(일시정지 메뉴) 초기화해도 어느 상태에서든 Idle로 갈 수 있게 사망 상태를 거친다.
        // 이미 죽어 있으면 다시 들어가지 않는다(같은 프레임에 사망 모션이 다시 재생되면 Idle 모션을 덮는다).
        if (!IsDead)
            Machine.ChangeState<PlayerDeadState>();
        // 대시 쿨다운 등 이전 런의 코루틴이 초기화된 스탯을 건드리지 않게 멈춘다.
        StopAllCoroutines();
        Inventory.Clear();
        ResetEntityState();
        InitPlayerStats();
        // 스킬 UI는 장착 이벤트로만 갱신되므로 되돌린 슬롯을 알린다.
        for (int i = 0; i < abilities.Length; i++)
            EventBus.Publish(new EntitySkillEquippedEvent(this, i, abilities[i]));
        if (defaultWeapon != null)
            EquipWeapon(new WeaponInstance(defaultWeapon));
        Rb.linearVelocity = Vector2.zero;
        Machine.ChangeState<PlayerIdleState>();
        OnHealthChanged();
        OnDashCountChanged();
    }

    // 내 공격에 맞은 대상을 플레이어가 바라보는 방향으로 무기 넉백 값만큼 밀어낸다(근접/탄환 공통).
    // 내가 맞으면(지속 피해 등 NoInterrupt 제외) 피격 상태로 간다. 죽는 피해면 사망 처리에 맡긴다.
    public void OnNotify(EntityDamagedEvent e)
    {
        if (e.Source == this && !e.Info.Has(DamageFlags.NoInterrupt) && CurrentHealth > 0)
            Machine.ChangeState<PlayerHitState>();

        if (e.Attacker != this || weapon == null || e.Source == this || e.Info.Has(DamageFlags.NoKnockback)) return;
        e.Source.ApplyKnockback(new Vector2(LookDirection * weapon.Data.knockbackForce, weapon.Data.knockbackUpForce), weapon.Data.knockbackDuration);
    }

    private void OnDestroy()
    {
        stateManager.Unregister(Machine);
        Abilities?.UnbindAll();
        WeaponAbilities?.UnbindAll();
        ItemAbilities?.UnbindAll();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * transform.localScale.x * 0.5f);

        if (groundCheck != null)
        {
            if (IsGrounded) Gizmos.color = Color.green;
            else Gizmos.color = Color.gray2;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
}
