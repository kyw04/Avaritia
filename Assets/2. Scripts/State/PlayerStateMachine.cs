using UnityEngine;

public class PlayerStateMachine : StateMachineBase<Player>
{
    public PlayerStateMachine(Player owner) : base(owner)
    {
        AddState(new IdleState(owner));
        AddState(new MoveState(owner));
        
        ChangeState<IdleState>();
    }

    class IdleState : StateBase<Player>
    {
        public IdleState(Player owner) : base(owner) { }

        public override void Enter()
        {
            Debug.Log("Idle Enter");
        }

        public override void Execute()
        {
            Debug.Log("Idle Execute");
        }
    }
    
    class MoveState : StateBase<Player>
    {
        public MoveState(Player owner) : base(owner) { }
        
        public override void Enter()
        {
            Debug.Log("Move Enter");
        }

        public override void FixedExecute()
        {
            Debug.Log("Move fixed execute");
        }
    }
}
