using System.Collections.Generic;
using UnityEngine;

public class BossBehaviorTree : BT.BehaviorTree
{
    public BossBehaviorTree(Boss boss) : this(boss, new BT.Blackboard()) { }

    private BossBehaviorTree(Boss boss, BT.Blackboard board) : base(BuildTree(boss, board), board) { }

    private static BT.Node BuildTree(Boss boss, BT.Blackboard board)
    {
        return new BT.Selector(

            // 사망 처리
            new BT.Sequence(
                new BT.Condition(() => boss.CurrentHealth <= 0),
                new BT.Action(() =>
                {
                    boss.Die();
                    return BT.NodeStatus.Success;
                })
            ),

            // 생존 행동
            new BT.Sequence(

                // Blackboard 갱신: 거리 + 사용 가능한 공격 목록
                new BT.Action(() =>
                {
                    float dist = boss.Target != null
                        ? Vector2.Distance(boss.transform.position, boss.Target.position)
                        : float.MaxValue;
                    board.Set(BBKey.TargetDistance, dist);
                    board.Set(BBKey.AvailableAttacks, boss.GetAvailableAttacks(dist));
                    return BT.NodeStatus.Success;
                }),

                new BT.Selector(

                    // 공격 진행 중 → 대기
                    new BT.Sequence(
                        new BT.Condition(() => boss.IsAttacking),
                        new BT.Action(() => BT.NodeStatus.Running)
                    ),

                    // 공격 가능한 공격이 있으면 랜덤 선택 후 시작
                    new BT.Sequence(
                        new BT.Condition(() =>
                            board.Get<List<BossAttackEntry>>(BBKey.AvailableAttacks).Count > 0),
                        new BT.Action(() =>
                        {
                            var available = board.Get<List<BossAttackEntry>>(BBKey.AvailableAttacks);
                            boss.StartAttack(available[Random.Range(0, available.Count)]);
                            return BT.NodeStatus.Running;
                        })
                    ),

                    // 기본: 타겟 방향으로 이동
                    new BT.Action(() =>
                    {
                        boss.MoveToTarget();
                        return BT.NodeStatus.Running;
                    })
                )
            )
        );
    }
}
