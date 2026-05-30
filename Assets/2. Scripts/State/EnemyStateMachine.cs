using UnityEngine;

public class EnemyStateMachine : StateMachineBase<Enemy>
{
    public EnemyStateMachine(Enemy owner) : base(owner) { }

    public override void Init()
    {
        base.Init();
        AddState(new EnemyAliveState(Owner));
        ChangeState<EnemyAliveState>();
    }
}

#region Alive
public class EnemyAliveState : StateBase<Enemy>
{
    public EnemyAliveState(Enemy owner) : base(owner)
    {
        AddChild(new EnemyPatrolState(Owner));
        AddChild(new EnemyCombatState(Owner));
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
    
#region CombatState
public class EnemyCombatState : StateBase<Enemy>
{
    public EnemyCombatState(Enemy owner) : base(owner)
    {
        AddChild(new EnemyMoveState(Owner));
        AddChild(new EnemyAttackState(Owner));
        AddChild(new EnemyHitState(Owner));
        
        Machine.AddTransition<EnemyCombatState, EnemyPatrolState>(); // 하위 객체 상태일 때 이동 될 수 있게 수정 필요
    }
}

public class EnemyMoveState : StateBase<Enemy>
{
    public EnemyMoveState(Enemy owner) : base(owner)
    {
    }
}
    
public class EnemyAttackState : StateBase<Enemy>
{
    public EnemyAttackState(Enemy owner) : base(owner)
    {
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