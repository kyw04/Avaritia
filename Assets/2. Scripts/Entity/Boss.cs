using UnityEngine;

public class Boss : MonoBehaviour, IDamageable
{
    public IAIController Machine { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public Transform Target { get; private set; }

    [SerializeField] private StatData statDataAsset;
    private bool isDead;
    private RuntimeStats stats;

    public float MaxHealth => stats.Get<float>(StatType.MaxHealth);
    public float CurrentHealth => stats.Get<float>(StatType.CurrentHealth);
    public float MoveSpeed => stats.Get<float>(StatType.MoveSpeed);

    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        stats = new RuntimeStats(statDataAsset);
        Machine = new BossBehaviorTree(this);
        Machine.Init();
    }

    private void OnDestroy()
    {
        StateManager.Instance.Unregister(Machine);
    }

    public void MoveToTarget()
    {
        if (Target == null) return;
        var rot = Target.position.x > transform.position.x ? 0 : 180;
        transform.rotation = Quaternion.Euler(0, rot, 0);
        Rb.linearVelocity = new Vector2(transform.right.x * MoveSpeed, Rb.linearVelocityY);
    }

    public void MeleeAttack()
    {
        Debug.Log("Boss: 근접 공격");
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        StateManager.Instance.Unregister(Machine);
        Debug.Log("Boss: 사망");
    }

    public void SetTarget(Transform target) => Target = target;

    public void TakeDamage(float damage)
    {
        stats.Set(StatType.CurrentHealth, CurrentHealth - damage);
    }
}
