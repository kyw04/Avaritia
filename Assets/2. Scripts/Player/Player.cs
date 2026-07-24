using UnityEngine;

public class Player : Entity, IStateOwner<Player>
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private PlayerPickupController pickupController;

    public Player Owner { get; private set; }
    public IStateMachine Machine { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    public Weapon Weapon => weapon;
    public PlayerPickupController PickupController => pickupController;

    protected override T ApplyEquipmentBonus<T>(StatType type, T baseValue) =>
        weapon != null ? weapon.ApplyBonus(type, baseValue) : baseValue;

    public void EquipWeapon(Weapon newWeapon)
    {
        float healthRatio = MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
        weapon = newWeapon;

        float newMaxHealth = MaxHealth;
        float newCurrentHealth = healthRatio * newMaxHealth;
        float currentHealthBonus = weapon != null && weapon.TryGetStatBonus<float>(StatType.CurrentHealth, out var bonus) ? bonus : 0f;
        stats.Set(StatType.CurrentHealth, newCurrentHealth - currentHealthBonus);

        OnHealthChanged();
        OnDashCountChanged();
    }

    protected override void Awake()
    {
        base.Awake();
        Renderer = GetComponentInChildren<SpriteRenderer>();

        Owner = this;
        Machine = new PlayerStateMachine(Owner);
        Machine.Init();

        stats.Set(StatType.DoubleJumpCount, 0);
        stats.Set(StatType.DashCount, 0);
    }

    protected override void Start()
    {
        base.Start();
        EventBus.Publish(new EntityDashCountChangedEvent(this, MaxDashCount, MaxDashCount));
    }

    protected override void OnGroundedChanged(bool grounded)
    {
        base.OnGroundedChanged(grounded);

        if (grounded)
        {
            if (Rb.linearVelocityY <= -5)
            {
                Machine.ChangeState<PlayerLandState>();
            }
            else
            {
                Machine.ChangeState<PlayerIdleState>();
            }
        }
        else if (Rb.linearVelocityY <= 0)
        {
            Machine.ChangeState<PlayerFallState>();
        }
    }

    public override void Die()
    {
        if (!TryMarkDead()) return;
        StateManager.Instance.Unregister(Machine);
        Debug.Log("Player: 사망");
    }

    private void OnDestroy()
    {
        StateManager.Instance.Unregister(Machine);
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
