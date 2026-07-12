using System.Collections.Generic;
using UnityEngine;

public class Boss : Entity
{
    public IAIController Machine { get; private set; }
    public Transform Target { get; private set; }

    private readonly Dictionary<AttackData, float> cooldownEndTimes = new();

    protected override void Awake()
    {
        base.Awake();

        var player = FindAnyObjectByType<Player>();
        if (player != null)
            Target = player.transform;

        Machine = new BossBehaviorTree(this);
        Machine.Init();
    }

    public List<AttackData> GetAvailableAttacks(float targetDistance)
    {
        var available = new List<AttackData>();
        if (weapon == null || weapon.combo == null) return available;
        foreach (var entry in weapon.combo.datas)
        {
            if (targetDistance > entry.maxRange) continue;
            if (cooldownEndTimes.TryGetValue(entry, out var endTime) && Time.time < endTime) continue;
            available.Add(entry);
        }
        return available;
    }

    public void StartAttack(AttackData entry)
    {
        cooldownEndTimes[entry] = Time.time + entry.cooldown;
        FlipToTarget();
        entry.Attack(this, Target);
    }

    public void MoveToTarget()
    {
        if (Target == null)
            return;

        int flip = FlipToTarget();
        if (Mathf.Abs(Target.position.x) - Mathf.Abs(transform.position.x) <= 0.5f)
            return;

        Move(new Vector2(flip, 0));
    }

    private int FlipToTarget()
    {
        int flip = Target.position.x > transform.position.x ? 1 : -1;
        float scale = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(scale * flip, scale, scale);
        return flip;
    }

    public override void Die()
    {
        if (!TryMarkDead()) return;
        StateManager.Instance.Unregister(Machine);
        Debug.Log("Boss: 사망");
    }
}
