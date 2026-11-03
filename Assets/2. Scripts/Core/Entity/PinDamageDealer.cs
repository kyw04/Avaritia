using UnityEngine;

// 처음 맞은 적 하나에만 박히는 금속 핀. 작은 적은 공격을 캔슬하고, 큰 적(대형/보스)은 공격을 느리게 한다.
public class PinDamageDealer : DamageDealer
{
    [Range(0.05f, 1f)] public float attackSlowMultiplier = 0.5f;
    public float attackSlowDuration = 3f;
    public Sprite attackSlowIcon; // 맞은 대상의 상태 아이콘 UI에 띄울 아이콘(비우면 표시 안 함)

    protected override void Attack(Transform target)
    {
        if (target.TryGetComponent<Entity>(out var entity))
        {
            if (entity is Enemy enemy && enemy.Size == EnemySize.Normal)
                entity.CancelAttack();
            else
                entity.SlowAttack(attackSlowMultiplier, attackSlowDuration, attackSlowIcon);
        }

        base.Attack(target);
    }
}
