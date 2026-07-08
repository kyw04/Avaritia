using UnityEngine;

public class Player : Entity, IStateOwner<Player>
{
    public Player Owner { get; private set; }
    public IStateMachine Machine { get; private set; }
    public SpriteRenderer Renderer { get; private set; }

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
        EventBus.Publish(new PlayerDashCountChangedEvent(MaxDashCount, MaxDashCount));
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

    protected override void OnDashCountChanged()
    {
        EventBus.Publish(new PlayerDashCountChangedEvent(MaxDashCount - DashCount, MaxDashCount));
    }

    protected override void OnHealthChanged()
    {
        EventBus.Publish(new PlayerHealthChangedEvent(MaxHealth, CurrentHealth));
    }

    public override void Die()
    {
        if (!TryMarkDead()) return;
        StateManager.Instance.Unregister(Machine);
        Debug.Log("Player: 사망");
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
