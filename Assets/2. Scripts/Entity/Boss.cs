using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Boss : MonoBehaviour, IDamageable, IAttacker
{
    public IAIController Machine { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public MonoBehaviour Mono => this;
    public Transform Target { get; private set; }
    public bool IsAttacking { get; set; }

    [SerializeField] private StatData statDataAsset;
    [SerializeField] private BossAttackData attackData;
    [SerializeField] private Image healthBarImage;

    private RuntimeStats stats;
    private bool isDead;
    private readonly Dictionary<BossAttackEntry, float> cooldownEndTimes = new();

    public float MaxHealth => stats.Get<float>(StatType.MaxHealth);
    public float CurrentHealth => stats.Get<float>(StatType.CurrentHealth);
    public float MoveSpeed => stats.Get<float>(StatType.MoveSpeed);
    public float Damage => stats.Get<float>(StatType.Damage);

    private void Awake()
    {
        var player = FindAnyObjectByType<Player>();
        if (player != null) 
            Target = player.transform;
        
        Rb = GetComponent<Rigidbody2D>();
        stats = new RuntimeStats(statDataAsset);
        Machine = new BossBehaviorTree(this);
        Machine.Init();
    }

    public List<BossAttackEntry> GetAvailableAttacks(float targetDistance)
    {
        var available = new List<BossAttackEntry>();
        if (attackData == null) return available;
        foreach (var entry in attackData.entries)
        {
            if (targetDistance > entry.maxRange) continue;
            if (cooldownEndTimes.TryGetValue(entry, out var endTime) && Time.time < endTime) continue;
            available.Add(entry);
        }
        return available;
    }

    public void StartAttack(BossAttackEntry entry)
    {
        cooldownEndTimes[entry] = Time.time + entry.cooldown;
        entry.data.Attack(this, Target);
    }

    public void MoveToTarget()
    {
        if (Target == null) 
            return;
        
        int flip = Target.position.x > transform.position.x ? 1 : -1;
        float scale = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(scale * flip, scale, scale);
        
        Rb.linearVelocity = new Vector2(flip * MoveSpeed, Rb.linearVelocityY);
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;
        StateManager.Instance.Unregister(Machine);
        Debug.Log("Boss: 사망");
    }

    public void TakeDamage(float damage)
    {
        stats.Set(StatType.CurrentHealth, CurrentHealth - damage);
        healthBarImage.fillAmount = CurrentHealth / MaxHealth;
        
        if (CurrentHealth <= 0)
            Die();
    }
}
