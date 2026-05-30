using UnityEngine;

public class Enemy : MonoBehaviour, IStateOwner<Enemy>
{
    public Enemy Owner { get;  private set; }
    public IStateMachine Machine { get; private set; }
    public Rigidbody2D Rb { get;  private set; }
    public Transform Target { get; private set; }
    
    [SerializeField] private float speed;
    
    [SerializeField] private PatrolArea patrolPoint;
    [SerializeField] private LayerMask viewLayerMask;
    [SerializeField, Range(0, 360)] private int viewAngle;
    [SerializeField] private float viewDensity;
    [SerializeField] private float viewRadius;
    [SerializeField] private float alertRadius;

    private int direction = 1;
    
    private void Awake()
    {
        Owner = this;
        Machine = new EnemyStateMachine(Owner);
        Machine.Init();
        
        Rb = GetComponent<Rigidbody2D>();
    }

    public void Move()
    {
        float targetVelocityX = direction * speed;
        Rb.linearVelocity = new Vector2(targetVelocityX, Rb.linearVelocityY);
    }
    
    public void Patrol()
    {
        FindTarget();
    }

    public void FindTarget()
    {
        RaycastHit2D hit;
        var currentPos = transform.position;

        hit = Physics2D.CircleCast(currentPos, alertRadius, Vector2.zero, 1f, viewLayerMask);
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

        direction = 1;
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
    }
}
