using UnityEngine;

public class Enemy : MonoBehaviour, IStateOwner<Enemy>
{
    public Enemy Owner { get;  private set; }
    public IStateMachine Machine { get; private set; }

    [SerializeField, Range(0, 360)] private int viewAngle;
    [SerializeField] private float viewDensity;
    [SerializeField] private float viewRadius;
    [SerializeField] private float alertRadius;
    
    private void Awake()
    {
        Owner = this;
        Machine = new EnemyStateMachine(Owner);
        Machine.Init();
    }

    public void Patrol()
    {
        RaycastHit2D hit;
        var currentPos = transform.position;

        hit = Physics2D.CircleCast(currentPos, alertRadius, Vector2.zero, 1f, LayerMask.GetMask("Player"));
        if (hit)
        {
            Machine.ChangeState<EnemyCombatState>();
            return;
        }
        
        var halfAngle = viewAngle / 2f;
        var stepAngle = viewAngle / viewDensity; 
        for (int i = 0; i <= viewDensity; i++)
        {
            var currentAngle = -halfAngle + (stepAngle * i);
            var dir = Quaternion.Euler(0, 0, currentAngle) * transform.right;
    
            hit = Physics2D.Raycast(currentPos, dir, viewRadius, ~LayerMask.GetMask("Enemy"));
            if (hit)
            {
                if (hit.transform.CompareTag("Player"))
                {
                    Machine.ChangeState<EnemyCombatState>();
                    return;
                }
                
                Debug.DrawRay(currentPos, dir * hit.distance, Color.red);
            }
            else
                Debug.DrawRay(currentPos, dir * viewRadius, Color.red);
        }
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
    }
}
