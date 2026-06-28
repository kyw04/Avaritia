using UnityEngine;

public class Enemy : MonoBehaviour, IStateOwner<Enemy>, IDamageable, IAttacker
{
    public Enemy Owner { get;  private set; }
    public IStateMachine Machine { get; private set; }
    public Rigidbody2D Rb { get;  private set; }
    public MonoBehaviour Mono => this;
    public Transform Target { get; private set; }
    public bool HasTarget => Target != null;
    
    [SerializeField] private PatrolArea patrolPoint;
    [SerializeField] private LayerMask viewLayerMask;
    [SerializeField, Range(0, 360)] private int viewAngle;
    [SerializeField] private float viewDensity;
    [SerializeField] private float viewRadius;
    [SerializeField] private float alertRadius;

    [SerializeField] private StatData statDataAsset; // 수정 사항 Addressables로 관리
    private RuntimeStats stats;
    public float MaxHealth => stats.Get<float>(StatType.MaxHealth);
    public float CurrentHealth => stats.Get<float>(StatType.CurrentHealth);
    public float MoveSpeed => stats.Get<float>(StatType.MoveSpeed);
    public float Damage => stats.Get<float>(StatType.Damage);
    public bool IsAttacking { get; set; }

    private void Awake()
    {
        Owner = this;
        Machine = new EnemyStateMachine(Owner);
        Machine.Init();
        
        Rb = GetComponent<Rigidbody2D>();

        stats = new RuntimeStats(statDataAsset);
    }

    public void Move()
    {
        if (HasTarget)
        {
            var rot = Target.position.x > transform.position.x ? 0 : 180;
            transform.rotation = Quaternion.Euler(0, rot, 0);
        }
        
        float targetVelocityX = transform.right.x * MoveSpeed;
        Rb.linearVelocity = new Vector2(targetVelocityX, Rb.linearVelocityY);
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

    public void TakeDamage(float damage)
    {
        stats.Set(StatType.CurrentHealth, CurrentHealth - damage);

        if (CurrentHealth <= 0)
        {
            Machine.ChangeState<EnemyDeadState>();
        }
        Debug.Log($"데미지 입음! 체력: {CurrentHealth}");
    }
}
