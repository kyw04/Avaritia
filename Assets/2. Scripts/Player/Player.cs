using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : Entity, IStateOwner<Player>
{
    [SerializeField] private Weapon weapon;
    [SerializeField, FormerlySerializedAs("pickupController")] private PlayerInteractionController interactionController;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float dropThroughDuration = 1f;
    private Collider2D col;

    public Player Owner { get; private set; }
    public IStateMachine Machine { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    private StateManager stateManager;
    public Weapon Weapon => weapon;
    public PlayerInteractionController InteractionController => interactionController;
    public float AttackReadyTime { get; set; }

    public bool CanAttack() => Weapon != null && Weapon.combo != null && Weapon.combo.Count > 0 && Time.time >= AttackReadyTime;

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
        col = GetComponent<Collider2D>();

        Owner = this;
        Machine = new PlayerStateMachine(Owner);
        Machine.Init();
        stateManager = StateManager.Instance;

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
        EventBus.Publish(new EntityDashCountChangedEvent(this, MaxDashCount, MaxDashCount));
    }

    protected override void OnGroundedChanged(bool grounded)
    {
        base.OnGroundedChanged(grounded);

        bool landed = grounded && Rb.linearVelocityY <= 0;

        if (landed)
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
        else if (!grounded && Rb.linearVelocityY <= 0)
        {
            Machine.ChangeState<PlayerFallState>();
        }
    }

    public override void Die()
    {
        if (!TryMarkDead()) return;
        stateManager.Unregister(Machine);
        Debug.Log("Player: 사망");
    }

    private void OnDestroy()
    {
        stateManager.Unregister(Machine);
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
