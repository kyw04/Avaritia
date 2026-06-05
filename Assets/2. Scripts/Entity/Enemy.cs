using UnityEngine;

public class Enemy : MonoBehaviour, IStateOwner<Enemy>, IDamageable
{
    public Enemy Owner { get;  private set; }
    public IStateMachine Machine { get; private set; }
    public Rigidbody2D Rb { get;  private set; }
    public Transform Target { get; private set; }
    public bool HasTarget => Target != null;
    
    [SerializeField] private float speed;
    
    [SerializeField] private PatrolArea patrolPoint;
    [SerializeField] private LayerMask viewLayerMask;
    [SerializeField, Range(0, 360)] private int viewAngle;
    [SerializeField] private float viewDensity;
    [SerializeField] private float viewRadius;
    [SerializeField] private float alertRadius;

    private void Awake()
    {
        Owner = this;
        Machine = new EnemyStateMachine(Owner);
        Machine.Init();
        
        Rb = GetComponent<Rigidbody2D>();
    }

    public void Move()
    {
        if (HasTarget)
        {
            var rot = Target.position.x > transform.position.x ? 0 : 180;
            transform.rotation = Quaternion.Euler(0, rot, 0);
        }
        
        float targetVelocityX = transform.right.x * speed;
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
        
        FindTarget();
    }

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
        Debug.Log("데미지 입음!");
    }
}
