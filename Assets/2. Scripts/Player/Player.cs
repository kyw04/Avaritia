using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : Entity, IStateOwner<Player>
{
    [SerializeField] private Weapon weapon;
    [SerializeField] private PlayerInteractionController interactionController;
    [SerializeField] private LayerMask platformLayer;
    [SerializeField] private float dropThroughDuration = 1f;
    private Collider2D col;

    public Player Owner { get; private set; }
    public Inventory Inventory { get; private set; }
    public IStateMachine Machine { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    public AbilityManager WeaponAbilities { get; private set; }
    public AbilityManager ItemAbilities { get; private set; }
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
        RebindWeaponAbilities();

        float newMaxHealth = MaxHealth;
        float newCurrentHealth = healthRatio * newMaxHealth;
        float currentHealthBonus = weapon != null && weapon.TryGetStatBonus<float>(StatType.CurrentHealth, out var bonus) ? bonus : 0f;
        stats.Set(StatType.CurrentHealth, newCurrentHealth - currentHealthBonus);

        OnHealthChanged();
        OnDashCountChanged();
    }

    private void RebindWeaponAbilities()
    {
        WeaponAbilities?.UnbindAll();
        WeaponAbilities = new AbilityManager(this, weapon != null ? weapon.passiveAbilities.ToArray() : Array.Empty<AbilityData>(), publishEvents: false);
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

        Owner = this;
        Machine = new PlayerStateMachine(Owner);
        Machine.Init();
        stateManager = StateManager.Instance;

        stats.Set(StatType.DoubleJumpCount, 0);
        stats.Set(StatType.DashCount, 0);

        RebindWeaponAbilities();
        RebindItemAbilities();
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
        Abilities?.UnbindAll();
        WeaponAbilities?.UnbindAll();
        ItemAbilities?.UnbindAll();
        Debug.Log("Player: 사망");
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
