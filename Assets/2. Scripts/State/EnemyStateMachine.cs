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
        AddChild(new EnemyReturnToPatrolState(Owner));
        AddChild(new EnemyCombatState(Owner));
        
        Machine.AddTransition<EnemyAliveState, EnemyDeadState>();
    }
}
    
        
#region Patrol
public class EnemyPatrolState : StateBase<Enemy>
{
    public EnemyPatrolState(Enemy owner) : base(owner)
    {
        Machine.AddTransition<EnemyPatrolState, EnemyCombatState>();
    }
    
    public override void Execute()
    {
        Owner.Patrol();
    }
    
    public override void FixedExecute()
    {
        Owner.Move();
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
        if (Owner.IsInPatrolArea())
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
            if (Owner.IsInPatrolArea())
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
        if (!Owner.IsInAttackRange())
            Owner.Move();
        else
        {
            Owner.Rb.linearVelocity = new UnityEngine.Vector2(0, Owner.Rb.linearVelocityY);
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

    public override void Enter()
    {
        Owner.Attack();
    }

    public override void Execute()
    {
        if (!Owner.IsAttacking)
            Machine.ChangeState<EnemyMoveState>();
    }
}
    
public class EnemyHitState : StateBase<Enemy>
{
    public EnemyHitState(Enemy owner) : base(owner)
    {
    }
}
#endregion

#endregion

public class EnemyDeadState : StateBase<Enemy>
{
    public EnemyDeadState(Enemy owner) : base(owner)
    {
        
    }

    public override void Enter()
    {
        Debug.Log("죽음");
    }
}