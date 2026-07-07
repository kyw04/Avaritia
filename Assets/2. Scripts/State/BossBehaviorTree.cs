using System.Collections.Generic;
using UnityEngine;

public class BossBehaviorTree : BT.BehaviorTree
{
    public BossBehaviorTree(Boss boss) : this(boss, new BT.Blackboard()) { }

    private BossBehaviorTree(Boss boss, BT.Blackboard board) : base(BuildTree(boss, board), board) { }

    private static BT.Node BuildTree(Boss boss, BT.Blackboard board)
    {
        return new BT.Selector(

            new BT.Sequence(
                new BT.Condition(() => boss.CurrentHealth <= 0),
                new BT.Action(() =>
                {
                    boss.Die();
                    return BT.NodeStatus.Success;
                })
            ),

            new BT.Sequence(

                new BT.Action(() =>
                {
                    float dist = boss.Target != null
                        ? Vector2.Distance(boss.transform.position, boss.Target.position)
                        : float.MaxValue;
                    board.Set(BBKey.TargetDistance, dist);
                    board.Set(BBKey.AvailableAttacks, boss.GetAvailableAttacks(dist));
                    EventBus.Publish(new BossDeadEvent());
                    
                    return BT.NodeStatus.Success;
                }),

                new BT.Selector(

                    new BT.Sequence(
                        new BT.Condition(() => boss.IsAttacking),
                        new BT.Action(() => BT.NodeStatus.Running)
                    ),

                    new BT.Sequence(
                        new BT.Condition(() =>
                            board.Get<List<AttackData>>(BBKey.AvailableAttacks).Count > 0),
                        new BT.Action(() =>
                        {
                            var available = board.Get<List<AttackData>>(BBKey.AvailableAttacks);
                            var randomIndex = Random.Range(0, available.Count);
                            boss.StartAttack(available[randomIndex]);
                            EventBus.Publish(new BossAttackStartEvent(available[randomIndex]));

                            return BT.NodeStatus.Running;
                        })
                    ),

                    new BT.Action(() =>
                    {
                        boss.MoveToTarget();
                        EventBus.Publish(new BossMovedEvent());
                        
                        return BT.NodeStatus.Running;
                    })
                )
            )
        );
    }
}
