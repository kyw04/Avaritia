using System.Collections;
using UnityEngine;

public abstract class Entity : MonoBehaviour, IDamageable, IAttacker
{
    [SerializeField] protected StatData statDataAsset;
    [SerializeField] protected Weapon weapon;
    [SerializeReference, SubclassSelector] protected IMovementStrategy movementStrategy;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected float groundRadius;
    [SerializeField] protected LayerMask groundLayer;

    protected RuntimeStats stats;
    protected bool isDead;
    protected bool wasGroundCheckerChanged;

    public Rigidbody2D Rb { get; protected set; }
    public MonoBehaviour Mono => this;
    public bool IsAttacking { get; set; }
    public Weapon Weapon => weapon;
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
        return weapon != null ? weapon.ApplyBonus(type, baseValue) : baseValue;
    }

    protected T GetAssetStat<T>(StatType type)
    {
        var baseValue = statDataAsset.TryGetValue<T>(type);
        return weapon != null ? weapon.ApplyBonus(type, baseValue) : baseValue;
    }

    protected virtual void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        stats = new RuntimeStats(statDataAsset);
        wasGroundCheckerChanged = !IsGrounded;
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
        if (grounded) stats.Set(StatType.DoubleJumpCount, 0);
    }

    public void EquipWeapon(Weapon newWeapon) => weapon = newWeapon;

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

    protected virtual void OnDashCountChanged() { }

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

    protected abstract void OnHealthChanged();
    public abstract void Die();
}
