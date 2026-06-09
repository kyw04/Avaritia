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
                    board.Set(BBKey.TargetDistance, boss.Target != null
                        ? Vector2.Distance(boss.transform.position, boss.Target.position)
                        : float.MaxValue);
                    return BT.NodeStatus.Success;
                }),
                new BT.Selector(
                    new BT.Sequence(
                        new BT.Condition(() => board.Get<float>(BBKey.TargetDistance) < 2f),
                        new BT.Action(() =>
                        {
                            boss.MeleeAttack();
                            return BT.NodeStatus.Running;
                        })
                    ),
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
