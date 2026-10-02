using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour, IDamageable, IAttacker, IBuffable, IStatReadable, IStatMutable, IPoolable
{
    [SerializeField] protected AbilityData[] abilities;
    [SerializeField] protected StatData statDataAsset;
    [SerializeReference, SubclassSelector] protected IMovementStrategy movementStrategy;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected float groundRadius;
    [SerializeField] protected LayerMask groundLayer;
    [SerializeField] protected Weapon dropWeaponAsset;
    [SerializeField] protected AbilityData dropAbilityAsset;
    [SerializeField, Range(0, 100)] protected float dropChance;
    [SerializeField] protected EntityParticles particles;
    
    protected RuntimeStats stats;
    protected bool isDead;
    protected bool wasGroundCheckerChanged;
    
    public T GetDefaultStat<T>(StatType statType) => statDataAsset.TryGetValue<T>(statType);
    public EntityParticles Particles => particles;

    private class ActiveBuff
    {
        public object source;
        public StatType type;
        public BuffValueType valueType;
        public float amount;
        public float expireTime;
        public List<IAbilityCondition> conditions;
        public bool clearOnDamageTaken;
    }

    private readonly List<ActiveBuff> activeBuffs = new();
    // Keyed by the granting binding's AbilityRuntimeState (same as buff sources), so each copy
    // of an item refills/loses only its own share and RemoveBuffsBySource can drop it on unequip.
    private readonly Dictionary<object, float> shields = new();
    private readonly Dictionary<object, int> hitBlocks = new();
    private float invincibleUntil;

    public Rigidbody2D Rb { get; protected set; }
    public MonoBehaviour Mono => this;
    public bool IsAttacking { get; set; }
    public AbilityManager Abilities { get; private set; }
    public bool IsGrounded { get; protected set; }

    public virtual int LookDirection => transform.localScale.x >= 0 ? 1 : -1;

    public float MaxHealth => GetStat<float>(StatType.MaxHealth);
    public float CurrentHealth => GetStat<float>(StatType.CurrentHealth);
    public float MoveSpeed => GetStat<float>(StatType.MoveSpeed);
    public virtual float Damage => GetStat<float>(StatType.Damage);
    public int MaxDoubleJumpCount => GetAssetStat<int>(StatType.DoubleJumpCount);
    public int DoubleJumpCount => stats.Get<int>(StatType.DoubleJumpCount);
    public float JumpForce => GetStat<float>(StatType.JumpForce);
    public int MaxDashCount => GetAssetStat<int>(StatType.DashCount);
    public int DashCount => stats.Get<int>(StatType.DashCount);
    public float DashCooldown => GetStat<float>(StatType.DashCooldown);
    public float DashForce => GetStat<float>(StatType.DashForce);

    public T GetStat<T>(StatType type)
    {
        var baseValue = stats.Get<T>(type);
        var withEquipment = ApplyEquipmentBonus(type, baseValue);
        return ApplyBuffs(type, withEquipment);
    }

    // Damage against a specific target: also counts buffs whose conditions depend on the target
    // (e.g. "HP lower than the target's"), which plain `Damage` (no target) always leaves out.
    public float GetDamageAgainst(Transform target)
    {
        var withEquipment = ApplyEquipmentBonus(StatType.Damage, stats.Get<float>(StatType.Damage));
        return ApplyBuffs(StatType.Damage, withEquipment, target);
    }

    protected virtual T ApplyEquipmentBonus<T>(StatType type, T baseValue) => baseValue;

    // A buff's conditions may themselves read stats (e.g. HealthRatioCondition -> MaxHealth), which
    // re-enters ApplyBuffs — so this must never mutate activeBuffs; expired ones are skipped here and
    // pruned in ApplyBuff instead.
    private bool IsBuffActive(ActiveBuff b, Transform target)
    {
        if (b.expireTime <= Time.time) return false;
        if (b.conditions == null || b.conditions.Count == 0) return true;
        var context = new AbilityContext { Caster = this, Target = target, State = b.source as AbilityRuntimeState };
        return b.conditions.TrueForAll(c => c == null || c.IsMet(context));
    }

    private T ApplyBuffs<T>(StatType type, T value, Transform target = null)
    {
        if (typeof(T) != typeof(float) && typeof(T) != typeof(int)) return value;

        if (typeof(T) == typeof(int))
        {
            // Int stats (DoubleJumpCount, DashCount, ...) only make sense as flat additions —
            // a "Percent" modifier is ignored here rather than silently truncating.
            int intResult = (int)(object)value;
            foreach (var b in activeBuffs)
            {
                if (b.type != type || b.valueType != BuffValueType.Flat || !IsBuffActive(b, target)) continue;
                intResult += (int)b.amount;
            }
            return (T)(object)intResult;
        }

        float result = (float)(object)value;
        float flatSum = 0f, percentSum = 0f;
        foreach (var b in activeBuffs)
        {
            if (b.type != type || !IsBuffActive(b, target)) continue;
            if (b.valueType == BuffValueType.Flat) flatSum += b.amount;
            else percentSum += b.amount;
        }

        result = (result + flatSum) * (1f + percentSum / 100f);
        return (T)(object)result;
    }

    public void ApplyBuff(object source, StatType type, BuffValueType valueType, float amount, float duration,
        List<IAbilityCondition> conditions = null, bool clearOnDamageTaken = false)
    {
        activeBuffs.RemoveAll(b => b.expireTime <= Time.time);

        // duration <= 0 (the Inspector default when a designer leaves it unset) means "never expires
        // on its own" — it only ever ends when its binding is torn down (drop/replace), same as any
        // other buff, via AbilityManager.Rebind -> RemoveBuffsBySource.
        float expireTime = duration > 0f ? Time.time + duration : float.MaxValue;

        var existing = activeBuffs.Find(b => b.source == source && b.type == type && b.valueType == valueType);
        if (existing != null)
        {
            existing.amount = amount;
            existing.expireTime = expireTime;
            existing.conditions = conditions;
            existing.clearOnDamageTaken = clearOnDamageTaken;
        }
        else
        {
            activeBuffs.Add(new ActiveBuff
            {
                source = source, type = type, valueType = valueType, amount = amount, expireTime = expireTime,
                conditions = conditions, clearOnDamageTaken = clearOnDamageTaken
            });
        }
    }

    public float GetBuffAmount(object source, StatType type, BuffValueType valueType)
    {
        var existing = activeBuffs.Find(b => b.source == source && b.type == type && b.valueType == valueType && b.expireTime > Time.time);
        return existing?.amount ?? 0f;
    }

    // Refills (not adds to) this source's share — re-granting every room shouldn't stack up.
    public void GrantShield(object source, float amount) => shields[source] = amount;

    public void GrantHitBlocks(object source, int count) => hitBlocks[source] = count;

    public void SetInvincible(float duration) => invincibleUntil = Mathf.Max(invincibleUntil, Time.time + duration);

    public void AddBaseStat<T>(StatType type, T amount) =>
        stats.Set(type, StatMath.Add(stats.Get<T>(type), amount));

    public void Heal(float amount)
    {
        // Clamped at 0 so healing from lethal (negative) HP — e.g. a revive — lands on the intended amount.
        float rawCurrent = Mathf.Max(0f, stats.Get<float>(StatType.CurrentHealth));
        stats.Set(StatType.CurrentHealth, Mathf.Min(MaxHealth, rawCurrent + amount));
        OnHealthChanged();
    }

    protected T GetAssetStat<T>(StatType type)
    {
        var baseValue = statDataAsset.TryGetValue<T>(type);
        var withEquipment = ApplyEquipmentBonus(type, baseValue);
        return ApplyBuffs(type, withEquipment);
    }

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        ResetEntityState();
    }

    public virtual void OnSpawn() => ResetEntityState();

    public virtual void OnDespawn()
    {
        Abilities?.UnbindAll();
        EventBus.UnsubscribeAll(this);
    }

    private void ResetEntityState()
    {
        isDead = false;
        stats = new RuntimeStats(statDataAsset);
        Abilities?.UnbindAll();
        Abilities = new AbilityManager(this, abilities);
        Abilities.BindAll();
        activeBuffs.Clear();
        shields.Clear();
        hitBlocks.Clear();
        invincibleUntil = 0f;
        wasGroundCheckerChanged = !IsGrounded;
    }

    protected virtual void Start()
    {
    }

    protected virtual void Update()
    {
        if (groundCheck == null) return;

        var groundHit = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        IsGrounded = groundHit != null; // && groundCheck.position.y >= groundHit.bounds.max.y - groundRadius;
        if (wasGroundCheckerChanged != IsGrounded && Rb.linearVelocityY <= 0)
        {
            wasGroundCheckerChanged = IsGrounded;
            OnGroundedChanged(IsGrounded);
        }
    }

    protected virtual void OnGroundedChanged(bool grounded)
    {
        stats.Set(StatType.DoubleJumpCount, 0);
    }

    public void Move(Vector2 direction) => movementStrategy?.Move(this, Rb, direction);
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

    public void TakeDamage(float damage, Entity attacker = null)
    {
        if (Time.time < invincibleUntil) return;
        if (Random.Range(0f, 100f) < GetStat<float>(StatType.Evasion)) return;
        if (TryConsumeHitBlock()) return;

        damage = AbsorbWithShields(damage);
        if (damage <= 0f) return;

        stats.Set(StatType.CurrentHealth, stats.Get<float>(StatType.CurrentHealth) - damage);
        activeBuffs.RemoveAll(b => b.clearOnDamageTaken);
        OnHealthChanged();
        EventBus.Publish(new EntityDamagedEvent(this, attacker, damage));

        if (CurrentHealth > 0) return;
        // Listeners (e.g. a revive item) may heal in response; only die if nothing did.
        EventBus.Publish(new EntityLethalDamageEvent(this));
        if (CurrentHealth <= 0) Die();
    }

    private bool TryConsumeHitBlock()
    {
        foreach (var source in new List<object>(hitBlocks.Keys))
        {
            if (hitBlocks[source] <= 0) continue;
            hitBlocks[source]--;
            return true;
        }
        return false;
    }

    private float AbsorbWithShields(float damage)
    {
        foreach (var source in new List<object>(shields.Keys))
        {
            if (damage <= 0f) break;
            float absorbed = Mathf.Min(shields[source], damage);
            shields[source] -= absorbed;
            damage -= absorbed;
        }
        return damage;
    }

    protected bool TryMarkDead()
    {
        if (isDead) return false;
        isDead = true;
        EventBus.Publish(new EntityDeadEvent(this));
        TryDropPickup();
        return true;
    }

    private void TryDropPickup()
    {
        IInteractable payload = null;
        if (dropWeaponAsset != null)
            payload = new WeaponPickup(dropWeaponAsset);
        else if (dropAbilityAsset != null)
            payload = dropAbilityAsset is Item item ? new ItemPickup(item) : new AbilityPickup(dropAbilityAsset);

        if (payload == null) return;
        if (Random.Range(0f, 100f) >= dropChance) return;

        WorldInteractionManager.Instance.Spawn(payload, transform.position);
    }

    protected void OnHealthChanged() => EventBus.Publish(new EntityHealthChangedEvent(this, MaxHealth, CurrentHealth));
    protected void OnDashCountChanged() => EventBus.Publish(new EntityDashCountChangedEvent(this, MaxDashCount - DashCount, MaxDashCount));

    public void RemoveBuffsBySource(object source)
    {
        activeBuffs.RemoveAll(b => b.source == source);
        shields.Remove(source);
        hitBlocks.Remove(source);
    }
    
    public abstract void Die();
}
