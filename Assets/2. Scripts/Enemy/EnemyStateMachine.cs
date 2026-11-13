using UnityEngine;

public class EnemyStateMachine : StateMachineBase<Enemy>
{
    public EnemyStateMachine(Enemy owner) : base(owner) { }

    public override void Init()
    {
        base.Init();
        AddState(new EnemyAliveState(Owner));
        AddState(new EnemyDeadState(Owner));
        
        ChangeState<EnemyAliveState>();
    }
}

#region Alive
public class EnemyAliveState : StateBase<Enemy>
{
    public EnemyAliveState(Enemy owner) : base(owner)
    {
        AddChild(new EnemyPatrolState(Owner));
        AddChild(new EnemyTurnState(Owner));
        AddChild(new EnemyReturnToPatrolState(Owner));
        AddChild(new EnemyCombatState(Owner));

        Machine.AddTransition<EnemyAliveState, EnemyDeadState>();
    }
}

#region Turn
// 순찰 중 벽/낭떠러지를 만나 돌아서는 동작. 방향을 뒤집고 제자리에 멈춰 돌아서기 모션(Feedbacks.Turn)이 끝나면 순찰로 돌아간다.
// 돌아서는 동안에도 대상을 찾으면 바로 전투로 들어간다.
public class EnemyTurnState : StateBase<Enemy>
{
    private float endTime;

    public EnemyTurnState(Enemy owner) : base(owner)
    {
        Machine.AddTransition<EnemyTurnState, EnemyPatrolState>();
        Machine.AddTransition<EnemyTurnState, EnemyCombatState>();
    }

    public override void Enter()
    {
        var clip = Owner.Feedbacks.Turn.animClip;
        endTime = Time.time + (clip != null ? clip.length : 0f);

        Owner.TurnAround();
        Owner.Restrictions.Set(this, ActionBlock.Move);
        Owner.Move(Vector2.zero);

        if (clip != null)
            EventBus.Publish(new EntityAnimationEvent(Owner, clip));
    }

    public override void Execute()
    {
        Owner.FindTarget();
        if (Owner.HasTarget)
            return;

        if (Time.time >= endTime)
            Machine.ChangeState<EnemyPatrolState>();
    }

    public override void FixedExecute()
    {
        Owner.Move(Vector2.zero);
    }

    public override void Exit()
    {
        Owner.Restrictions.Remove(this);
    }
}
#endregion

#region Patrol
public class EnemyPatrolState : StateBase<Enemy>
{
    public EnemyPatrolState(Enemy owner) : base(owner)
    {
        Machine.AddTransition<EnemyPatrolState, EnemyCombatState>();
        Machine.AddTransition<EnemyPatrolState, EnemyTurnState>();
    }
    
    public override void Execute()
    {
        Owner.Patrol();
    }
    
    public override void FixedExecute()
    {
        if (!Owner.ShouldHoldPosition)
            Owner.Move();
        else if (!Owner.IsKnockedBack)
            Owner.Rb.linearVelocity = new Vector2(0, Owner.Rb.linearVelocityY);
    }
}
    
#endregion

#region ReturnToPatrol
public class EnemyReturnToPatrolState : StateBase<Enemy>
{
    public EnemyReturnToPatrolState(Enemy owner) : base(owner)
    {
        Machine.AddTransition<EnemyReturnToPatrolState, EnemyPatrolState>();
        Machine.AddTransition<EnemyReturnToPatrolState, EnemyCombatState>();
    }

    public override void Enter()
    {
        Owner.ClearTarget();
        Owner.FaceTowardPatrolCenter();
    }

    public override void Execute()
    {
        if (Owner.IsInPatrolArea(Owner.transform.position))
        {
            Machine.ChangeState<EnemyPatrolState>();
            return;
        }
        Owner.FindTarget();
    }

    public override void FixedExecute()
    {
        Owner.Move();
    }
}
#endregion

#region CombatState
public class EnemyCombatState : StateBase<Enemy>
{
    public EnemyCombatState(Enemy owner) : base(owner)
    {
        AddChild(new EnemyMoveState(Owner));
        AddChild(new EnemyAttackState(Owner));
        AddChild(new EnemyHitState(Owner));
        
        Machine.AddTransition<EnemyCombatState, EnemyPatrolState>();
        Machine.AddTransition<EnemyCombatState, EnemyReturnToPatrolState>();
    }
}

public class EnemyMoveState : StateBase<Enemy>
{
    public EnemyMoveState(Enemy owner) : base(owner)
    {
        Machine.AddTransition<EnemyMoveState, EnemyAttackState>();
    }

    public override void Execute()
    {
        Owner.FindTarget();

        if (!Owner.HasTarget)
        {
            if (Owner.IsInPatrolArea(Owner.transform.position))
                Machine.ChangeState<EnemyPatrolState>();
            else
                Machine.ChangeState<EnemyReturnToPatrolState>();
            return;
        }

        if (Owner.IsInAttackRange() && Owner.CanAttack())
            Machine.ChangeState<EnemyAttackState>();
    }

    public override void FixedExecute()
    {
        if (Owner.IsKnockedBack)
            return;
        
        if (!Owner.IsInAttackRange())
            Owner.Move();
        else
        {
            Owner.Rb.linearVelocity = new Vector2(0, Owner.Rb.linearVelocityY);
            Owner.FaceTarget();
        }
    }
}

public class EnemyAttackState : StateBase<Enemy>
{
    public EnemyAttackState(Enemy owner) : base(owner)
    {
        Machine.AddTransition<EnemyAttackState, EnemyMoveState>();
    }

    // 이동 상태는 FixedUpdate에서만 대상을 바라보므로, 공격 준비 동작에 들어가기 전에 한 번 더 대상을 바라본다.
    public override void Enter()
    {
        Owner.FaceTarget();
        Owner.Attack();
    }

    public override void Execute()
    {
        if (!Owner.IsAttacking)
            Machine.ChangeState<EnemyMoveState>();
    }
}
    
// 피격 경직. 준비 중이던 공격·채널링을 취소하고 피격 모션(Feedbacks.Hit)이 끝날 때까지 모든 행동을 막는다.
// 어느 상태에서든 들어올 수 있고(다시 맞으면 처음부터), 경직이 끝나면 전투 이동으로 돌아간다.
public class EnemyHitState : StateBase<Enemy>
{
    public const ActionBlock AllActions = ActionBlock.Move | ActionBlock.Attack | ActionBlock.Skill | ActionBlock.Jump
                                         | ActionBlock.Dash | ActionBlock.Turn | ActionBlock.Interact;
    private float endTime;
    private bool finished;

    public EnemyHitState(Enemy owner) : base(owner)
    {
        Machine.AddTransition<EnemyAliveState, EnemyHitState>(() => !Owner.IsDead);
        Machine.AddTransition<EnemyHitState, EnemyMoveState>(() => finished);
    }

    public override void Enter()
    {
        var clip = Owner.Feedbacks.Hit.animClip;
        finished = false;
        endTime = Time.time + (clip != null ? clip.length : 0f);

        Owner.CancelAttack();
        Owner.CancelChannels();
        Owner.Restrictions.Set(this, AllActions);
        // 이동 입력을 바로 0으로 만들어, 다음 FixedUpdate 전에 넉백 속도로 나가는 걷기 애니메이션이 피격 모션을 덮지 않게 한다.
        Owner.Move(Vector2.zero);

        if (clip != null)
            EventBus.Publish(new EntityAnimationEvent(Owner, clip));
    }

    public override void Execute()
    {
        if (Time.time < endTime)
            return;

        finished = true;
        Machine.ChangeState<EnemyMoveState>();
    }

    // 이동이 막혀 있어 멈추는 쪽으로 감속한다. 넉백 중이면 넉백 속도를 그대로 둔다.
    public override void FixedExecute()
    {
        Owner.Move(Vector2.zero);
    }

    public override void Exit()
    {
        Owner.Restrictions.Remove(this);
    }
}
#endregion

#endregion

public class EnemyDeadState : StateBase<Enemy>
{
    public EnemyDeadState(Enemy owner) : base(owner)
    {
        
    }

    // 진행 중인 행동을 멈추고 사망 모션(Feedbacks.Death)을 튼다. 풀 반납은 Enemy.Die가 모션 길이만큼 기다렸다 한다.
    // 사망 모션 동안 탄환/공격 판정에 걸리지 않게 물리에서 뺀다(OnSpawn에서 되돌린다).
    public override void Enter()
    {
        Owner.CancelAttack();
        Owner.CancelChannels();
        Owner.Restrictions.Set(this, EnemyHitState.AllActions);
        Owner.Rb.linearVelocity = Vector2.zero;
        Owner.Rb.simulated = false;

        var feedback = Owner.Feedbacks.Death;
        feedback.Play(Owner.FootPosition);
        if (feedback.animClip != null)
            EventBus.Publish(new EntityAnimationEvent(Owner, feedback.animClip));
    }
}