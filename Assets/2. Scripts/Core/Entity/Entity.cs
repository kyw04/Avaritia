using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour, IDamageable, IAttacker, IBuffable, IStatReadable, IStatMutable, IPoolable, IInfo
{
    [SerializeField] protected AbilityData[] abilities;
    [SerializeField] protected StatData statDataAsset;
    [SerializeReference, SubclassSelector] protected IMovementStrategy movementStrategy;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected float groundRadius;
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected EntityFeedbacks feedbacks;
    [SerializeField, Range(0, 1)] protected float knockbackResistance;
    // 이름은 결과 화면 사망 원인 등에 보인다. 비면 오브젝트 이름을 쓴다.
    [SerializeField] private DisplayInfo info = new();

    // 타일맵(Composite) 바닥 위에서는 접촉 해결 오차로 세로 속도가 +0.00001 정도로 남을 수 있다.
    // 착지/낙하 판정에서 "올라가는 중이 아님"을 vy <= 0으로 엄격하게 보면 착지를 놓치므로 이만큼은 허용한다.
    public const float GroundedVelocityTolerance = 0.01f;

    protected RuntimeStats stats;
    protected bool isDead;
    // 스폰 시점의 스킬 구성. 런 중 SetAbility로 바뀐 슬롯을 리셋 때 되돌린다.
    private AbilityData[] initialAbilities;
    protected bool wasGroundCheckerChanged;

    // 자기 엔티티 이벤트용 로컬 허브. 필드 초기화로 만들어 자식 컴포넌트의 OnEnable이 Awake보다 먼저 돌아도 쓸 수 있다.
    // 구독자가 OnDisable에서 해제하므로 풀에서 다시 꺼내도 그대로 재사용한다.
    public readonly EntityEventHub Events = new();

    public T GetDefaultStat<T>(StatType statType) => statDataAsset.TryGetValue<T>(statType);
    public EntityFeedbacks Feedbacks => feedbacks;
    // 발밑 위치. 발소리 먼지 등 발에서 나와야 하는 연출용(없으면 중심).
    public Vector3 FootPosition => groundCheck != null ? groundCheck.position : transform.position;

    private BuffContainer buffs;
    private DamageReceiver damageReceiver;
    private float knockbackUntil;
    private float trackedMaxHealth;

    public Rigidbody2D Rb { get; protected set; }
    // 무기 등이 이동 방식을 잠시 바꿨다가 되돌릴 수 있게 연다(예: 로프에 매달린 동안).
    public IMovementStrategy MovementStrategy { get => movementStrategy; set => movementStrategy = value; }
    public MonoBehaviour Mono => this;
    public Entity SourceEntity => this;
    public bool IsAttacking { get; set; }
    public AbilityManager Abilities { get; private set; }
    public DebuffController Debuffs { get; private set; }
    public bool IsDead => isDead;
    public string DisplayName => string.IsNullOrEmpty(info.DisplayName) ? gameObject.name.Replace("(Clone)", "").Trim() : info.DisplayName;
    public Sprite Icon => info.Icon;
    public string Details => info.Details;
    public bool IsGrounded { get; protected set; }
    public bool IgnoresHitStun => damageReceiver.IgnoresHitStun;
    public bool IsKnockedBack => Time.time < knockbackUntil;
    // 공격 준비/진행 시간과 공격 애니메이션에 곱하는 배율. 1보다 작으면 공격이 느려진다(AttackSlowModule 디버프).
    public float AttackSpeedMultiplier => Debuffs.AttackSpeedMultiplier;
    // 공격이 캔슬될 때마다 올라간다. 진행 중인 공격 코루틴은 시작 때 값과 달라지면 멈춘다.
    public int AttackCancelVersion { get; private set; }
    // 마지막으로 실제 데미지를 준 공격자/시각. 킬 판정(OnKillTrigger)과 채널링 피격 중단(ChannelEffect)에 쓴다.
    public Entity LastAttacker { get; private set; }
    public float LastDamagedTime { get; private set; } = float.NegativeInfinity;
    // 채널링, 디버프 등이 출처별로 막은 행동. 행동을 시작하는 쪽은 Restrictions.Can으로 확인한다.
    public ActionRestrictions Restrictions { get; } = new();
    // 마지막으로 Move에 넘어온 이동 방향(플레이어는 이동 입력). 실제 속도와 달리 감속으로 미끄러지는 동안은 0이다.
    public Vector2 MoveDirection { get; private set; }

    public virtual int LookDirection => transform.localScale.x >= 0 ? 1 : -1;

    public float MaxHealth => GetStat<float>(StatType.MaxHealth);
    public float CurrentHealth => GetStat<float>(StatType.CurrentHealth);
    public float HealthRatio => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
    public float MoveSpeed => GetStat<float>(StatType.MoveSpeed);
    public virtual float Damage => GetStat<float>(StatType.Damage);
    public int MaxDoubleJumpCount => GetAssetStat<int>(StatType.DoubleJumpCount);
    public int DoubleJumpCount => stats.Get<int>(StatType.DoubleJumpCount);
    public float JumpForce => GetStat<float>(StatType.JumpForce);
    public int MaxDashCount => GetAssetStat<int>(StatType.DashCount);
    public int DashCount => stats.Get<int>(StatType.DashCount);
    public float DashCooldown => GetStat<float>(StatType.DashCooldown);
    public float DashForce => GetStat<float>(StatType.DashForce);
    // 내가 거는 디버프의 지속 시간에 더하는 초.
    public float DebuffDurationBonus => TryGetStat<float>(StatType.DebuffDuration, out var bonus) ? Mathf.Max(0f, bonus) : 0f;

    // 기본 스탯에 없는 항목(예: 탄환 스탯)도 활성 버프(무기 패시브 등)가 있으면 기본값 0에서 버프를 더해 반환한다.
    public bool TryGetStat<T>(StatType type, out T stat)
    {
        if (!stats.TryGet<T>(type, out var baseValue) && !buffs.HasActive(type))
        {
            stat = default;
            return false;
        }
        stat = buffs.Apply(type, baseValue);
        return true;
    }

    public T GetStat<T>(StatType type) => buffs.Apply(type, stats.Get<T>(type));

    // Damage against a specific target: also counts buffs whose conditions depend on the target
    // (e.g. "HP lower than the target's"), which plain `Damage` (no target) always leaves out.
    public float GetDamageAgainst(Transform target) =>
        buffs.Apply(StatType.Damage, stats.Get<float>(StatType.Damage), target);

    public void ApplyBuff(object source, StatType type, BuffValueType valueType, float amount, float duration,
        List<IAbilityCondition> conditions = null, bool clearOnDamageTaken = false)
    {
        buffs.RemoveExpired();

        // duration <= 0 (the Inspector default when a designer leaves it unset) means "never expires
        // on its own" — it only ever ends when its binding is torn down (drop/replace), same as any
        // other buff, via AbilityManager.Rebind -> RemoveBuffsBySource.
        float expireTime = duration > 0f ? Time.time + duration : float.MaxValue;

        if (type == StatType.MaxHealth)
            PreserveHealthRatio(() => buffs.AddOrUpdate(source, type, valueType, amount, expireTime, duration, conditions, clearOnDamageTaken));
        else
            buffs.AddOrUpdate(source, type, valueType, amount, expireTime, duration, conditions, clearOnDamageTaken);
    }

    // 최대 체력이 바뀌는 변경(버프 추가/제거, 기본 스탯, 무기 교체)을 감싸 현재 체력 비율을 유지한다.
    protected void PreserveHealthRatio(System.Action change)
    {
        SyncHealthRatio();
        float ratio = MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        change();
        SetHealthRatio(ratio);
    }

    // 조건부/만료 버프처럼 변경 시점을 알 수 없는 최대 체력 변화는 마지막으로 본 최대 체력과 비교해 맞춘다.
    private void SyncHealthRatio()
    {
        if (Mathf.Approximately(MaxHealth, trackedMaxHealth)) return;
        SetHealthRatio(trackedMaxHealth > 0f ? CurrentHealth / trackedMaxHealth : 0f);
    }

    // 세이브 복원용. 0이면 죽은 채로 시작하므로 최소값을 둔다.
    public void RestoreHealthRatio(float ratio) => SetHealthRatio(Mathf.Clamp(ratio, 0.0001f, 1f));

    private void SetHealthRatio(float ratio)
    {
        float maxHealth = MaxHealth;
        trackedMaxHealth = maxHealth;

        // CurrentHealth getter에는 장비/버프 보너스가 포함되므로 그 차이만큼 빼서 기본값으로 저장한다.
        float raw = stats.Get<float>(StatType.CurrentHealth);
        float target = ratio * maxHealth - (CurrentHealth - raw);
        if (Mathf.Approximately(raw, target)) return;

        stats.Set(StatType.CurrentHealth, target);
        OnHealthChanged();
    }

    public float GetBuffAmount(object source, StatType type, BuffValueType valueType) =>
        buffs.GetAmount(source, type, valueType);

    public void GrantShield(object source, float amount) => damageReceiver.GrantShield(source, amount);

    public void GrantHitBlocks(object source, int count) => damageReceiver.GrantHitBlocks(source, count);

    public bool HasShield(object source) => damageReceiver.HasShield(source);

    public bool HasHitBlocks(object source) => damageReceiver.HasHitBlocks(source);

    public void SetInvincible(float duration, Sprite icon = null) => damageReceiver.SetInvincible(duration, icon);

    public void SetNoHitStun(float duration, Sprite icon = null) => damageReceiver.SetNoHitStun(duration, icon);

    // 상태 아이콘 UI용: 지금 걸려 있는 버프/실드/무효화/무적/디버프를 into에 추가한다.
    public virtual void CollectStatusIcons(List<StatusIconEntry> into)
    {
        buffs.CollectStatusIcons(into);
        damageReceiver.CollectStatusIcons(into);
        Debuffs.CollectStatusIcons(into);
    }

    public void AddBaseStat<T>(StatType type, T amount)
    {
        if (type == StatType.MaxHealth)
            PreserveHealthRatio(() => stats.Set(type, StatMath.Add(stats.Get<T>(type), amount)));
        else
            stats.Set(type, StatMath.Add(stats.Get<T>(type), amount));
    }

    public void Heal(float amount)
    {
        // Clamped at 0 so healing from lethal (negative) HP — e.g. a revive — lands on the intended amount.
        float rawCurrent = Mathf.Max(0f, stats.Get<float>(StatType.CurrentHealth));
        stats.Set(StatType.CurrentHealth, Mathf.Min(MaxHealth, rawCurrent + amount));
        OnHealthChanged();
    }

    protected T GetAssetStat<T>(StatType type)
    {
        return buffs.Apply(type, statDataAsset.TryGetValue<T>(type));
    }

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        ResetEntityState();
    }

    private void OnDestroy() => OnDespawn();

    public virtual void OnSpawn() => ResetEntityState();

    public virtual void OnDespawn()
    {
        Abilities?.UnbindAll();
        EventBus.UnsubscribeAll(this);
    }

    protected void ResetEntityState()
    {
        isDead = false;
        if (stats == null)
            stats = new RuntimeStats(statDataAsset);
        else
            stats.Reset(statDataAsset);
        // 생성자에 this가 필요해 필드 초기화 대신 여기서 만든다. 스탯을 읽는 다른 초기화보다 먼저 있어야 한다.
        buffs ??= new BuffContainer(this);
        damageReceiver ??= new DamageReceiver(this);
        // 에셋 기준(장비 보너스 제외) 비율에서 출발해, 스폰 시 장착된 무기/아이템 보너스도 비율 유지로 반영되게 한다.
        trackedMaxHealth = stats.Get<float>(StatType.MaxHealth);
        Abilities?.UnbindAll();
        // 장착 즉시 발동하는 어빌리티(OnEquippedTrigger)의 버프가 지워지지 않도록 바인딩 전에 비운다.
        Debuffs?.ClearAll();
        Debuffs ??= new DebuffController(this);
        buffs.Clear();
        damageReceiver.ClearGrants();
        initialAbilities ??= (AbilityData[])abilities?.Clone();
        // 풀에서 다시 꺼낼 때는 매니저와 슬롯 배열을 그대로 두고 슬롯만 처음 구성으로 되돌린다(바인딩은 매니저가 재사용).
        if (Abilities != null && abilities != null && initialAbilities != null)
        {
            Array.Copy(initialAbilities, abilities, initialAbilities.Length);
        }
        else
        {
            abilities = (AbilityData[])initialAbilities?.Clone();
            Abilities = new AbilityManager(this, abilities);
        }
        Abilities.BindAll();
        damageReceiver.ClearInvincible();
        knockbackUntil = 0f;
        LastAttacker = null;
        LastDamagedTime = float.NegativeInfinity;
        Restrictions.Clear();
        MoveDirection = Vector2.zero;
        wasGroundCheckerChanged = !IsGrounded;
    }

    protected virtual void Start() { }

    protected virtual void Update()
    {
        SyncHealthRatio();
        Debuffs.Tick();

        if (groundCheck == null) return;

        var groundHit = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        IsGrounded = groundHit != null; // && groundCheck.position.y >= groundHit.bounds.max.y - groundRadius;
        if (wasGroundCheckerChanged != IsGrounded && Rb.linearVelocityY <= GroundedVelocityTolerance)
        {
            wasGroundCheckerChanged = IsGrounded;
            OnGroundedChanged(IsGrounded);
        }
    }

    protected virtual void OnGroundedChanged(bool grounded)
    {
        if (IsGrounded) stats.Set(StatType.DoubleJumpCount, 0);
    }

    // 넉백 중에는 이동 전략이 속도를 덮어써 넉백을 지우지 않도록 이동을 막는다.
    // 이동이 막혀 있으면(기절 등) 멈추는 쪽으로 이동시킨다. 넉백과 중력은 그대로 받는다.
    public void Move(Vector2 direction)
    {
        if (!Restrictions.Can(ActionBlock.Move))
            direction = Vector2.zero;
        MoveDirection = direction;
        if (IsKnockedBack) return;
        movementStrategy?.Move(this, Rb, direction);
    }

    // 공격 대상 레이어를 바꿔야 하는 엔티티(아군이 된 적)가 공격 필터/탄환 대상 레이어를 치환한다.
    public virtual LayerMask ResolveTargetLayer(LayerMask mask) => mask;

    public void ApplyKnockback(Vector2 velocity, float duration)
    {
        if (knockbackResistance >= 1f) return;
        Rb.linearVelocity = velocity * (1f - knockbackResistance);
        knockbackUntil = Time.time + duration;
    }
    // 진행 중인 공격(준비 동작 포함)을 멈춘다. 이미 발사된 탄환은 그대로 날아간다.
    public void CancelAttack()
    {
        if (!IsAttacking) return;
        AttackCancelVersion++;
        IsAttacking = false;
        EntityEventRouter.Raise(new EntityAttackCanceledEvent(this));
    }

    // 진행 중인 채널링을 모두 중단한다(완료 효과는 적용되지 않는다).
    public virtual void CancelChannels() => Abilities?.CancelChannels();

    public void UpdateFacing(Vector2 direction) => movementStrategy?.UpdateFacing(this, direction.x);

    public virtual void Jump()
    {
        if (!IsGrounded)
            stats.Set(StatType.DoubleJumpCount, DoubleJumpCount + 1);

        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, JumpForce);
    }

    public virtual void Dash()
    {
        stats.Set(StatType.DashCount, DashCount + 1);
        OnDashCountChanged();
        Rb.linearVelocity = Vector2.zero;
        Rb.AddForce(Vector2.right * transform.localScale.x * DashForce, ForceMode2D.Impulse);

        StartCoroutine(DashCooldownRoutine());
    }

    private IEnumerator DashCooldownRoutine()
    {
        yield return new WaitForSeconds(DashCooldown);
        stats.Set(StatType.DashCount, DashCount - 1);
        OnDashCountChanged();
    }

    public void TakeDamage(DamageInfo info, KnockbackSettings? knockback, HitCameraSettings? hitCamera)
    {
        var attacker = info.Attacker;
        var hitPoint = info.HitPoint;
        if (isDead) return;
        float damage = damageReceiver.Resolve(info, out var outcome);
        if (outcome == DamageOutcome.Evaded)
            EventBus.Publish(new EntityEvadedEvent(this, attacker, hitPoint));
        else if (outcome == DamageOutcome.Blocked)
            EventBus.Publish(new EntityHitBlockedEvent(this, attacker, hitPoint));
        if (outcome != DamageOutcome.Damaged) return;

        stats.Set(StatType.CurrentHealth, stats.Get<float>(StatType.CurrentHealth) - damage);
        if (attacker != null) LastAttacker = attacker;
        // 채널링 피격 중단(LastDamagedTime 폴링)과 피격 시 사라지는 버프는 NoInterrupt 피해(지속 피해 등)를 무시한다.
        if (!info.Has(DamageFlags.NoInterrupt))
        {
            LastDamagedTime = Time.time;
            buffs.RemoveClearedOnDamage();
        }
        OnHealthChanged();
        info.Amount = damage;
        EntityEventRouter.Raise(new EntityDamagedEvent(this, info, knockback ?? new KnockbackSettings(), hitCamera ?? new HitCameraSettings()));

        if (CurrentHealth > 0) return;
        // Listeners (e.g. a revive item) may heal in response; only die if nothing did.
        EventBus.Publish(new EntityLethalDamageEvent(this));
        if (CurrentHealth <= 0) Die();
    }

    protected bool TryMarkDead()
    {
        if (isDead) return false;
        isDead = true;
        Debuffs.ClearAll();
        EventBus.Publish(new EntityDeadEvent(this));
        // 무엇을 떨어뜨릴지는 DropTable이 정한다(없으면 드롭 없음).
        if (TryGetComponent<DropTable>(out var dropTable))
            dropTable.Drop(transform.position);
        return true;
    }

    protected void OnHealthChanged() => EventBus.Publish(new EntityHealthChangedEvent(this, MaxHealth, CurrentHealth));
    protected void OnDashCountChanged() => EventBus.Publish(new EntityDashCountChangedEvent(this, MaxDashCount - DashCount, MaxDashCount, Time.time));

    // 지울 버프가 없으면(풀 반납 때 대부분) 람다를 만들지 않는다. 그때 PreserveHealthRatio가 하는 일은 SyncHealthRatio뿐이다.
    public void RemoveBuffsBySource(object source)
    {
        if (buffs.HasFrom(source))
            RemoveActiveBuffsBySource(source);
        else
            SyncHealthRatio();
        damageReceiver.RemoveSource(source);
    }

    // 캡처용 클로저가 RemoveBuffsBySource 진입마다 만들어지지 않도록 람다를 별도 메서드로 뺐다.
    private void RemoveActiveBuffsBySource(object source) =>
        PreserveHealthRatio(() => buffs.RemoveBySource(source));
    
    public abstract void Die();
}
