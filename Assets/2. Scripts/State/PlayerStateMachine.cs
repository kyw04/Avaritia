using UnityEngine;

public class PlayerStateMachine : StateMachineBase<Player>
{
    public PlayerStateMachine(Player owner) : base(owner)
    {
        AddState(new AliveState(Owner));
        
        ChangeState<AliveState>();
    }

    public class AliveState : StateBase<Player>
    {
        public AliveState(Player owner) : base(owner) { }

        public override void Enter()
        {
            Debug.Log("Alive Enter");
            AddChild(new Grounded(Owner));
            AddChild(new Airborne(Owner));
            AddChild(new Action(Owner));
        }
    }

    public class Grounded : StateBase<Player>
    {
        public Grounded(Player owner) : base(owner) { }

        public override void Enter()
        {
            AddChild(new IdleState(Owner));
        }
    }

    public class Airborne : StateBase<Player>
    {
        public Airborne(Player owner) : base(owner) { }

        public override void Enter()
        {
            AddChild(new JumpState(Owner));
            AddChild(new Fall(Owner));
        }
    }

    public class Action : StateBase<Player>
    {
        public Action(Player owner) : base(owner) { }

        public override void Enter()
        {
            
        }
    }
    
    public class IdleState : StateBase<Player>
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
    
    public class JumpState : StateBase<Player>
    {
        public JumpState(Player owner) : base(owner)
        {
        }

        public override void Enter()
        {
            Debug.Log("Jump Enter");
        }

        public override void Execute()
        {
            Debug.Log("Jump Execute");
        }
    }
    
    public class Fall : StateBase<Player>
    {
        public Fall(Player owner) : base(owner) { }

        public override void Enter()
        {
            Debug.Log("Fall Enter");
        }

        public override void Execute()
        {
            Debug.Log("Fall Execute");
        }
    }
}
