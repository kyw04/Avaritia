using System.Collections;
using UnityEngine;

public enum EnemySize
{
    Normal,
    Large,
}

public class Enemy : Entity, IStateOwner<Enemy>, IObserver<EntityDamagedEvent>
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
    // 이 속도보다 느리면 걷기 대신 대기 애니메이션을 튼다.
    private const float MoveAnimationSpeed = 0.05f;
    // 낭떠러지 검사 레이를 몸 앞쪽 끝/발바닥에서 이만큼 떨어뜨려 쏜다.
    private const float LedgeProbeOffset = 0.1f;
    // 앞쪽 발밑으로 이 높이 안에 바닥/발판이 있을 때만 내려간다. 더 깊으면 낭떠러지로 보고 돌아서거나 멈춘다.
    [SerializeField] private float maxDropHeight = 1f;
    // 추격 중 낭떠러지나 벽에 막혀 이 시간 동안 못 가면 추격을 포기하고 순찰 구역으로 돌아간다.
    [SerializeField] private float chaseGiveUpTime = 2f;
    // 추격을 포기한 뒤 이 시간 동안은 대상을 찾지 않는다(근접 감지 포함). 포기 직후 다시 쫓는 반복을 막는다.
    [SerializeField] private float giveUpSearchCooldown = 3f;
    // 벽 감지 위치. 몸 중심 기준이며 x는 바라보는 방향 쪽(왼쪽을 보면 뒤집힌다). 몸 앞쪽 끝보다 조금 앞에 둔다.
    [SerializeField] private Vector2 wallCheckOffset = new(1.1f, 0.1f);
    private const float WallCheckRadius = 0.15f;
    [SerializeField] private LayerMask viewLayerMask;
    [SerializeField, Range(0, 360)] private int viewAngle;
    [SerializeField] private float viewDensity;
    [SerializeField] private float viewRadius;
    [SerializeField] private float alertRadius;
    [SerializeField] private EnemySize size;
    // 켜면 걷지 않고 제자리에서 대상만 바라본다(포탑 등). 아군이 돼도 플레이어를 따라가지 않는다.
    [SerializeField] private bool stationary;
    // 아군이 되면 적과 구분되도록 스프라이트에 입히는 색
    [SerializeField] private Color allyColor = new(0.5f, 1f, 0.5f, 1f);

    // 아군 전환 시 바꾼 값을 풀 재사용(OnSpawn) 때 되돌리기 위한 원래 값
    private int hostileLayer;
    private LayerMask hostileViewLayerMask;
    private Transform leader;
    private SpriteRenderer[] spriteRenderers;
    private Color[] hostileColors;
    private Collider2D bodyCollider;
    private int groundMask;
    private int wallMask;
    private float blockedTime;
    private float searchBlockedUntil;

    // 순찰 폭이 0이면 순찰하지 않고 순찰 중심에 서 있는다(테스트용 허수아비 등).
    public bool ShouldHoldPosition => patrolSize.x <= 0f && IsInPatrolArea();
    public EnemySize Size => size;
    public bool IsAlly { get; private set; }

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
        bodyCollider = GetComponent<Collider2D>();
        groundMask = LayerMask.GetMask("Floor", "Platform");
        wallMask = LayerMask.GetMask("Wall", "Floor");
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        hostileColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
            hostileColors[i] = spriteRenderers[i].color;
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
        blockedTime = 0f;
        searchBlockedUntil = 0f;
        IsAlly = false;
        leader = null;
        gameObject.layer = hostileLayer;
        viewLayerMask = hostileViewLayerMask;
        Rb.simulated = true;
        for (int i = 0; i < spriteRenderers.Length; i++)
            spriteRenderers[i].color = hostileColors[i];
        Machine = new EnemyStateMachine(Owner);
        Machine.Init();
    }

    protected override void Update()
    {
        base.Update();
        // 아군은 순찰 범위가 플레이어를 따라다녀 플레이어 곁을 맴돌다 적이 보이면 싸운다.
        if (IsAlly && leader != null && !stationary)
            patrolCenter = leader.position;
        PublishLocomotionAnimation();
    }

    // 걷기/대기 애니메이션. 공격 중이나 사망 후에는 그쪽 모션을 덮지 않는다.
    // 피격 경직 중에는 EntityAnimator가 행동 제한을 보고 무시한다.
    private void PublishLocomotionAnimation()
    {
        if (IsDead || IsAttacking)
            return;

        float speed = Mathf.Abs(Rb.linearVelocityX);
        if (speed > MoveAnimationSpeed)
            EventBus.Publish(new EntityMovedEvent(this, speed));
        else
            EventBus.Publish(new EntityIdleEvent(this));
    }

    private void OnEnable() => EventBus.Subscribe<EntityDamagedEvent>(this);
    private void OnDisable() => EventBus.Unsubscribe<EntityDamagedEvent>(this);

    // 맞으면(지속 피해 등 NoInterrupt 제외) 피격 경직. 죽는 피해면 사망 처리에 맡기고, 피격 모션이 없는 적은 경직하지 않는다.
    public void OnNotify(EntityDamagedEvent e)
    {
        if (e.Source == this && !e.Info.Has(DamageFlags.NoInterrupt) && CurrentHealth > 0
            && Feedbacks.Hit.animClip != null)
            Machine.ChangeState<EnemyHitState>();
    }

    // 플레이어와 같은 레이어로 옮겨 적들이 플레이어처럼 노리게 하고, 원래 자기 레이어(적)를 노리게 한다.
    // 스탯은 퍼센트 버프로 깎아 OnSpawn(ResetEntityState)에서 버프가 비워지면 원래대로 돌아간다.
    public void ConvertToAlly(Transform newLeader, float statMultiplier)
    {
        IsAlly = true;
        leader = newLeader;
        gameObject.layer = newLeader.gameObject.layer;
        viewLayerMask = 1 << hostileLayer;
        foreach (var r in spriteRenderers)
            r.color = allyColor;

        float percent = (statMultiplier - 1f) * 100f;
        foreach (var stat in statDataAsset.stats)
            if (stat.statType != StatType.CurrentHealth)
                ApplyBuff(this, stat.statType, BuffValueType.Percent, percent, 0f);

        // 적일 때 걸린 기절 등은 아군이 되면서 풀어 준다.
        Debuffs.ClearAll();
        Target = null;
        Machine.ChangeState<EnemyPatrolState>();
        EventBus.Publish(new EnemyConvertedEvent(this));
    }

    public override LayerMask ResolveTargetLayer(LayerMask mask) =>
        IsAlly ? (mask & ~hostileViewLayerMask) | (1 << hostileLayer) : mask;

    public void Move()
    {
        if (stationary)
        {
            FaceTarget();
            if (!IsKnockedBack)
                Rb.linearVelocity = new Vector2(0, Rb.linearVelocityY);
            return;
        }

        if (HasTarget)
        {
            // 대상과 x가 거의 같으면(겹치거나 머리 위) 지나쳤다 돌아서기를 반복하지 않도록 멈춘다.
            float dx = Target.position.x - transform.position.x;
            if (Mathf.Abs(dx) <= ChaseStopDistance)
            {
                Rb.linearVelocity = new Vector2(0, Rb.linearVelocityY);
                return;
            }

            Face(dx > 0 ? 1 : -1);
        }

        // 앞이 낭떠러지나 벽이면 걷지 않는다. 추격 중이면 그 자리에서 대상을 바라보며 기다린다.
        if (IsLedgeAhead() || IsWallAhead())
        {
            if (!IsKnockedBack)
                Rb.linearVelocity = new Vector2(0, Rb.linearVelocityY);
            if (HasTarget)
                WaitWhileBlocked();
            return;
        }

        blockedTime = 0f;
        Move((Vector2)transform.right);
    }

    // 바라보는 쪽 wallCheckOffset 위치에 벽/바닥 타일이 있으면 벽이다. 일방향 발판은 벽으로 보지 않는다.
    public bool IsWallAhead() => Physics2D.OverlapCircle(WallCheckPoint, WallCheckRadius, wallMask);

    private Vector2 WallCheckPoint =>
        (Vector2)transform.position + new Vector2(wallCheckOffset.x * LookDirection, wallCheckOffset.y);

    // 바라보는 쪽 발 앞에서 아래로 쏴서 maxDropHeight 안에 바닥/발판이 없으면 낭떠러지다.
    public bool IsLedgeAhead()
    {
        var bounds = bodyCollider.bounds;
        var origin = new Vector2(
            bounds.center.x + LookDirection * (bounds.extents.x + LedgeProbeOffset),
            bounds.min.y + LedgeProbeOffset);
        return !Physics2D.Raycast(origin, Vector2.down, maxDropHeight + LedgeProbeOffset, groundMask);
    }

    // 낭떠러지나 벽에 막힌 채 chaseGiveUpTime이 지나면 추격을 포기하고 순찰 구역으로 돌아간다.
    private void WaitWhileBlocked()
    {
        blockedTime += Time.fixedDeltaTime;
        if (blockedTime < chaseGiveUpTime)
            return;

        blockedTime = 0f;
        searchBlockedUntil = Time.time + giveUpSearchCooldown;
        Machine.ChangeState<EnemyReturnToPatrolState>();
    }

    public void Patrol()
    {
        if (ShouldHoldPosition)
        {
            FindTarget();
            return;
        }

        if (IsWallAhead() || IsLedgeAhead())
        {
            Face(-LookDirection);
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

    public void FaceTowardPatrolCenter() => Face(patrolCenter.x > transform.position.x ? 1 : -1);

    // 바라보는 방향을 바꾸는 곳은 모두 여기를 거친다. 방향 전환이 막혀 있으면(기절 등) 그대로 둔다.
    // 콜라이더가 한쪽으로 치우쳐 있으면 뒤집을 때 몸이 옮겨져 벽에 박히므로, 콜라이더 중심을 축으로 돌도록 위치를 보정한다.
    private void Face(int direction)
    {
        int look = direction > 0 ? 1 : -1;
        if (look == LookDirection || !Restrictions.Can(ActionBlock.Turn))
            return;

        float shift = 2f * bodyCollider.offset.x * transform.lossyScale.x * LookDirection;
        transform.SetPositionAndRotation(
            transform.position + new Vector3(shift, 0f, 0f),
            Quaternion.Euler(0, look > 0 ? 0 : 180, 0));
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
        return first != null && Restrictions.Can(ActionBlock.Attack) && !Abilities.IsOnCooldown(first);
    }

    public void Attack() => Abilities.TryActivate(Abilities.AbilityAt(0), new AbilityContext { Caster = this, Target = Target });

    public void FaceTarget()
    {
        if (!HasTarget) return;
        Face(Target.position.x > transform.position.x ? 1 : -1);
    }

    public void ClearTarget() => Target = null;

    public void FindTarget()
    {
        if (Time.time < searchBlockedUntil)
        {
            Target = null;
            return;
        }

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
        Gizmos.DrawWireSphere(WallCheckPoint, WallCheckRadius);
    }

    public override void Die()
    {
        if (!TryMarkDead()) return;
        Machine.ChangeState<EnemyDeadState>();
        stateManager.Unregister(Machine);

        var clip = Feedbacks.Death.animClip;
        if (clip != null)
            StartCoroutine(DespawnAfter(clip.length));
        else
            ObjectPoolManager.Instance.Despawn(gameObject);
    }

    // 사망 모션이 끝난 뒤 풀에 돌려보낸다. 그 전에 풀에 먼저 돌아가면(방 이동 등) 비활성화로 코루틴도 멈춘다.
    private IEnumerator DespawnAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        ObjectPoolManager.Instance.Despawn(gameObject);
    }

    private void OnDestroy()
    {
        stateManager.Unregister(Machine);
    }
}
