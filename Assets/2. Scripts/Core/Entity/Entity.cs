using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour, IDamageable, IAttacker, IBuffable
{
    [SerializeField] protected SkillData[] skill;
    [SerializeField] protected StatData statDataAsset;
    [SerializeReference, SubclassSelector] protected IMovementStrategy movementStrategy;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected float groundRadius;
    [SerializeField] protected LayerMask groundLayer;

    protected RuntimeStats stats;
    protected bool isDead;
    protected bool wasGroundCheckerChanged;

    private class ActiveBuff
    {
        public object source;
        public StatType type;
        public BuffValueType valueType;
        public float amount;
        public float expireTime;
    }

    private readonly List<ActiveBuff> activeBuffs = new();

    public Rigidbody2D Rb { get; protected set; }
    public MonoBehaviour Mono => this;
    public bool IsAttacking { get; set; }
    public SkillManager Skills { get; private set; }
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

    protected T GetStat<T>(StatType type)
    {
        var baseValue = stats.Get<T>(type);
        var withWeapon = ApplyWeaponBonus(type, baseValue);
        return ApplyBuffs(type, withWeapon);
    }

    protected virtual T ApplyWeaponBonus<T>(StatType type, T baseValue) => baseValue;

    private T ApplyBuffs<T>(StatType type, T value)
    {
        if (typeof(T) != typeof(float)) return value;

        activeBuffs.RemoveAll(b => b.expireTime <= Time.time);

        float result = (float)(object)value;
        float flatSum = 0f, percentSum = 0f;
        foreach (var b in activeBuffs)
        {
            if (b.type != type) continue;
            if (b.valueType == BuffValueType.Flat) flatSum += b.amount;
            else percentSum += b.amount;
        }
        result = (result + flatSum) * (1f + percentSum / 100f);
        return (T)(object)result;
    }

    public void ApplyBuff(object source, StatType type, BuffValueType valueType, float amount, float duration)
    {
        var existing = activeBuffs.Find(b => b.source == source && b.type == type && b.valueType == valueType);
        if (existing != null)
        {
            existing.amount = amount;
            existing.expireTime = Time.time + duration;
        }
        else
        {
            activeBuffs.Add(new ActiveBuff { source = source, type = type, valueType = valueType, amount = amount, expireTime = Time.time + duration });
        }
    }

    protected T GetAssetStat<T>(StatType type)
    {
        var baseValue = statDataAsset.TryGetValue<T>(type);
        return ApplyWeaponBonus(type, baseValue);
    }

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        stats = new RuntimeStats(statDataAsset);
        Skills = new SkillManager(skill);
        wasGroundCheckerChanged = !IsGrounded;
    }

    protected virtual void Start()
    {
    }

    protected virtual void Update()
    {
        if (groundCheck == null) return;

        IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundLayer);
        if (wasGroundCheckerChanged != IsGrounded)
        {
            wasGroundCheckerChanged = IsGrounded;
            OnGroundedChanged(IsGrounded);
        }
    }

    protected virtual void OnGroundedChanged(bool grounded)
    {
        if (grounded)
            stats.Set(StatType.DoubleJumpCount, 0);
    }

    public void Move(Vector2 direction) => movementStrategy?.Move(this, Rb, direction);

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
        Rb.AddForce(Vector2.right * transform.localScale.x * 50f, ForceMode2D.Impulse);

        StartCoroutine(DashCooldownRoutine());
    }

    private IEnumerator DashCooldownRoutine()
    {
        yield return new WaitForSeconds(DashCooldown);
        stats.Set(StatType.DashCount, DashCount - 1);
        OnDashCountChanged();
    }

    public void TakeDamage(float damage)
    {
        stats.Set(StatType.CurrentHealth, stats.Get<float>(StatType.CurrentHealth) - damage);
        OnHealthChanged();
        if (CurrentHealth <= 0) Die();
    }

    protected bool TryMarkDead()
    {
        if (isDead) return false;
        isDead = true;
        return true;
    }

    protected void OnHealthChanged() => EventBus.Publish(new EntityHealthChangedEvent(this, MaxHealth, CurrentHealth));
    protected void OnDashCountChanged() => EventBus.Publish(new EntityDashCountChangedEvent(this, MaxDashCount - DashCount, MaxDashCount));
    
    public abstract void Die();
}
