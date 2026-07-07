using UnityEngine;

public class Enemy : Entity, IStateOwner<Enemy>
{
    public Enemy Owner { get; private set; }
    public IStateMachine Machine { get; private set; }
    public Transform Target { get; private set; }
    public bool HasTarget => Target != null;

    [SerializeField] private PatrolArea patrolPoint;
    [SerializeField] private LayerMask viewLayerMask;
    [SerializeField, Range(0, 360)] private int viewAngle;
    [SerializeField] private float viewDensity;
    [SerializeField] private float viewRadius;
    [SerializeField] private float alertRadius;

    private float lastAttackTime = float.MinValue;

    public override int LookDirection => transform.right.x >= 0 ? 1 : -1;

    protected override void Awake()
    {
        base.Awake();
        Owner = this;
        Machine = new EnemyStateMachine(Owner);
        Machine.Init();
    }

    public void Move()
    {
        if (HasTarget)
        {
            var rot = Target.position.x > transform.position.x ? 0 : 180;
            transform.rotation = Quaternion.Euler(0, rot, 0);
        }

        Move((Vector2)transform.right);
    }

    public void Patrol()
    {
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
        if (patrolPoint == null) return true;
        float x = transform.position.x;
        float left  = patrolPoint.position.x - patrolPoint.size.x / 2f;
        float right = patrolPoint.position.x + patrolPoint.size.x / 2f;
        return x >= left && x <= right;
    }

    public void FaceTowardPatrolCenter()
    {
        if (patrolPoint == null) return;
        float rot = patrolPoint.position.x > transform.position.x ? 0 : 180;
        transform.rotation = Quaternion.Euler(0, rot, 0);
    }

    public bool IsInAttackRange()
    {
        if (!HasTarget || weapon == null || weapon.combo == null || weapon.combo.Count == 0) return false;
        return Vector2.Distance(transform.position, Target.position) <= weapon.combo.datas[0].maxRange;
    }

    public bool CanAttack()
    {
        if (weapon == null || weapon.combo == null || weapon.combo.Count == 0) return false;
        return Time.time - lastAttackTime >= weapon.combo.datas[0].cooldown;
    }

    public void Attack()
    {
        lastAttackTime = Time.time;
        weapon.combo.datas[0].Attack(this);
    }

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

        if (patrolPoint != null)
        {
            Gizmos.color = new Color32(0, 255, 0, 50);
            Gizmos.DrawCube(patrolPoint.position, patrolPoint.size);
        }

        Gizmos.color = Color.forestGreen;
        Gizmos.DrawWireSphere(transform.position +  transform.right * 0.5f, 0.25f);
    }

    protected override void OnHealthChanged()
    {
        EventBus.Publish(new EnemyHealthChangedEvent(CurrentHealth / MaxHealth));
        Debug.Log($"데미지 입음! 체력: {CurrentHealth}");
    }

    public override void Die()
    {
        if (!TryMarkDead()) return;
        Machine.ChangeState<EnemyDeadState>();
        StateManager.Instance.Unregister(Machine);
    }
}
