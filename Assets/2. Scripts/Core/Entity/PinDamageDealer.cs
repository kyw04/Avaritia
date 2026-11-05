using UnityEngine;

// 처음 맞은 적 하나에만 박히는 금속 핀. 작은 적은 공격을 캔슬하고, 큰 적(대형/보스)은 공격을 느리게 한다.
public class PinDamageDealer : DamageDealer
{
    // 큰 적에게 거는 디버프(AttackSlowModule). 아이콘과 배율은 디버프 에셋에서 정한다.
    public DebuffData attackSlowDebuff;
    public float attackSlowDuration = 3f;

    protected override void Attack(Transform target)
    {
        if (target.TryGetComponent<Entity>(out var entity))
        {
            if (entity is Enemy enemy && enemy.Size == EnemySize.Normal)
                entity.CancelAttack();
            else
                entity.Debuffs.Apply(attackSlowDebuff, attackSlowDuration, attacker);
        }

        base.Attack(target);
    }
}
