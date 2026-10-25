using UnityEngine;

public enum EnemySize
{
    Normal,
    Large,
}

public class Enemy : Entity, IStateOwner<Enemy>
{
    public Enemy Owner { get; private set; }
    public IStateMachine Machine { get; private set; }
    public Transform Target { get; private set; }
    public bool HasTarget => Target != null;
    private StateManager stateManager;

    [SerializeField] private Vector2 patrolSize;
    private Vector2 patrolCenter;
    // 순찰 폭이 0이어도 중심에 도착했다고 판정할 여유 거리
    private const float PatrolArriveDistance = 0.1f;
    // 추격 대상과 x 차이가 이 안이면 방향을 바꾸지 않고 멈춘다.
    private const float ChaseStopDistance = 0.1f;
    [SerializeField] private LayerMask viewLayerMask;
    [SerializeField, Range(0, 360)] private int viewAngle;
    [SerializeField] private float viewDensity;
    [SerializeField] private float viewRadius;
    [SerializeField] private float alertRadius;
    [SerializeField] private EnemySize size;

    // 아군 전환 시 바꾼 값을 풀 재사용(OnSpawn) 때 되돌리기 위한 원래 값
    private int hostileLayer;
    private LayerMask hostileViewLayerMask;
    private Transform leader;
    private float stunUntil;

    // 순찰 폭이 0이면 순찰하지 않고 순찰 중심에 서 있는다(테스트용 허수아비 등).
    public bool ShouldHoldPosition => patrolSize.x <= 0f && IsInPatrolArea();
    public EnemySize Size => size;
    public bool IsAlly { get; private set; }
    public bool IsStunned => Time.time < stunUntil;

    public override int LookDirection => transform.right.x >= 0 ? 1 : -1;

    // Machine is built here too (not just OnSpawn) so a directly scene-placed
    // (non-pooled) instance still gets a working state machine — OnSpawn only
    // fires for pool-spawned instances. OnSpawn unregisters and rebuilds it
    // again on every pool (re)spawn.
    protected override void Awake()
    {
        base.Awake();
        hostileLayer = gameObject.layer;
        hostileViewLayerMask = viewLayerMask;
        Owner = this;
        stateManager = StateManager.Instance;
        patrolCenter = transform.position;
        Machine = new EnemyStateMachine(Owner);
        Machine.Init();
    }

    public override void OnSpawn()
    {
        base.OnSpawn();
        if (Machine != null) stateManager.Unregister(Machine);
        patrolCenter = transform.position;
        Target = null;
        IsAlly = false;
        leader = null;
        stunUntil = 0f;
        gameObject.layer = hostileLayer;
        viewLayerMask = hostileViewLayerMask;
        Machine = new EnemyStateMachine(Owner);
        Machine.Init();
    }

    protected override void Update()
    {
        base.Update();
        // 아군은 순찰 범위가 플레이어를 따라다녀 플레이어 곁을 맴돌다 적이 보이면 싸운다.
        if (IsAlly && leader != null)
            patrolCenter = leader.position;
    }

    public void Stun(float duration)
    {
        stunUntil = Time.time + duration;
        Machine.ChangeState<EnemyStunState>();
    }

    // 플레이어와 같은 레이어로 옮겨 적들이 플레이어처럼 노리게 하고, 원래 자기 레이어(적)를 노리게 한다.
    // 스탯은 퍼센트 버프로 깎아 OnSpawn(ResetEntityState)에서 버프가 비워지면 원래대로 돌아간다.
    public void ConvertToAlly(Transform newLeader, float statMultiplier)
    {
        IsAlly = true;
        leader = newLeader;
        gameObject.layer = newLeader.gameObject.layer;
        viewLayerMask = 1 << hostileLayer;

        float percent = (statMultiplier - 1f) * 100f;
        foreach (var stat in statDataAsset.stats)
            if (stat.statType != StatType.CurrentHealth)
                ApplyBuff(this, stat.statType, BuffValueType.Percent, percent, 0f);

        stunUntil = 0f;
        Target = null;
        Machine.ChangeState<EnemyPatrolState>();
        EventBus.Publish(new EnemyConvertedEvent(this));
    }

    public override LayerMask ResolveTargetLayer(LayerMask mask) =>
        IsAlly ? (mask & ~hostileViewLayerMask) | (1 << hostileLayer) : mask;

    public void Move()
    {
        if (HasTarget)
        {
            // 대상과 x가 거의 같으면(겹치거나 머리 위) 지나쳤다 돌아서기를 반복하지 않도록 멈춘다.
            float dx = Target.position.x - transform.position.x;
            if (Mathf.Abs(dx) <= ChaseStopDistance)
            {
                Rb.linearVelocity = new Vector2(0, Rb.linearVelocityY);
                return;
            }

            var rot = dx > 0 ? 0 : 180;
            transform.rotation = Quaternion.Euler(0, rot, 0);
        }

        Move((Vector2)transform.right);
    }

    public void Patrol()
    {
        if (ShouldHoldPosition)
        {
            FindTarget();
            return;
        }

        bool onTurn = Physics2D.CircleCast(
            transform.position,
            0.25f,
            transform.right,
            0.5f,
            1 << LayerMask.NameToLayer("Wall") | 1 << LayerMask.NameToLayer("Floor"));

        if (onTurn)
        {
            transform.Rotate(0, 180, 0);
        }
        else if (!IsInPatrolArea())
        {
            FaceTowardPatrolCenter();
        }

        FindTarget();
    }

    public bool IsInPatrolArea()
    {
        float halfWidth = Mathf.Max(patrolSize.x / 2f, PatrolArriveDistance);
        return Mathf.Abs(transform.position.x - patrolCenter.x) <= halfWidth;
    }

    public void FaceTowardPatrolCenter()
    {
        float rot = patrolCenter.x > transform.position.x ? 0 : 180;
        transform.rotation = Quaternion.Euler(0, rot, 0);
    }

    public bool IsInAttackRange()
    {
        var first = Abilities.AbilityAt(0);
        if (!HasTarget || first == null) return false;
        return Vector2.Distance(transform.position, Target.position) <= first.maxRange;
    }

    public bool CanAttack()
    {
        var first = Abilities.AbilityAt(0);
        return first != null && !Abilities.IsOnCooldown(first);
    }

    public void Attack() => Abilities.TryActivate(Abilities.AbilityAt(0), new AbilityContext { Caster = this, Target = Target });

    public void FaceTarget()
    {
        if (!HasTarget) return;
        var rot = Target.position.x > transform.position.x ? 0 : 180;
        transform.rotation = Quaternion.Euler(0, rot, 0);
    }

    public void ClearTarget() => Target = null;

    public void FindTarget()
    {
        var currentPos = transform.position;

        var hit = Physics2D.CircleCast(currentPos, alertRadius, Vector2.zero, 1f, viewLayerMask);
        if (hit)
        {
            Target = hit.transform;
            Machine.ChangeState<EnemyCombatState>();
            return;
        }

        var halfAngle = viewAngle / 2f;
        var stepAngle = viewAngle / viewDensity;
        for (int i = 0; i <= viewDensity; i++)
        {
            var currentAngle = -halfAngle + (stepAngle * i);
            var dir = Quaternion.Euler(0, 0, currentAngle) * transform.right;

            hit = Physics2D.Raycast(currentPos, dir, viewRadius, ~(1 << gameObject.layer));
            if (hit)
            {
                if ((viewLayerMask & (1 << hit.transform.gameObject.layer)) != 0)
                {
                    Target = hit.transform;
                    Machine.ChangeState<EnemyCombatState>();
                    return;
                }

                Debug.DrawRay(currentPos, dir * hit.distance, Color.red);
            }
            else
                Debug.DrawRay(currentPos, dir * viewRadius, Color.red);
        }

        Target = null;
    }

    private void OnDrawGizmosSelected()
    {
        var currentPos = transform.position;
        Gizmos.color = Color.gray2;
        Gizmos.DrawWireSphere(currentPos, alertRadius);
        var halfAngle = viewAngle / 2f;
        var stepAngle = viewAngle / viewDensity;
        for (int i = 0; i <= viewDensity; i++)
        {
            var currentAngle = -halfAngle + (stepAngle * i);
            var dir = Quaternion.Euler(0, 0, currentAngle) * transform.right;

            Gizmos.DrawRay(currentPos, dir *  viewRadius);
        }

        Vector2 patrolGizmoCenter = Application.isPlaying ? patrolCenter : (Vector2)transform.position;
        Gizmos.color = new Color32(0, 255, 0, 50);
        Gizmos.DrawCube(patrolGizmoCenter, patrolSize);

        Gizmos.color = Color.forestGreen;
        Gizmos.DrawWireSphere(transform.position +  transform.right * 0.5f, 0.25f);
    }

    public override void Die()
    {
        if (!TryMarkDead()) return;
        Machine.ChangeState<EnemyDeadState>();
        stateManager.Unregister(Machine);
        ObjectPoolManager.Instance.Despawn(gameObject);
    }

    private void OnDestroy()
    {
        stateManager.Unregister(Machine);
    }
}
