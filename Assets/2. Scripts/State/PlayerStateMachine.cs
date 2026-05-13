using UnityEngine;

public class PlayerStateMachine : StateMachineBase<Player>
{
    public PlayerStateMachine(Player owner) : base(owner)
    {
        AddState(new AliveState(Owner));
        
        ChangeState<AliveState>();
        ChangeState<Grounded>();
        ChangeState<IdleState>();
    }
#region Alive
    public class AliveState : StateBase<Player>
    {
        public AliveState(Player owner) : base(owner)
        {
            AddChild(new Grounded(Owner));
            AddChild(new Airborne(Owner));
            AddChild(new Action(Owner));
        }

        public override void Enter()
        {
            Debug.Log("Alive Enter");
        }
    }

#region Alive Grounded
    public class Grounded : StateBase<Player>
    {
        public Grounded(Player owner) : base(owner)
        {
            AddChild(new IdleState(Owner));
            AddChild(new MoveState(Owner));
            AddChild(new JumpState(Owner));
        }

        public override void Enter()
        {
            Debug.Log("Grounded Enter");
        }
    }
    
    public class IdleState : StateBase<Player>
    {
        public IdleState(Player owner) : base(owner) { }

        public override void Enter()
        {
        }
    }

    public class MoveState : StateBase<Player>
    {
        public MoveState(Player owner) : base(owner) { }

        public override void FixedExecute()
        {
            Owner.Move(InputHandler.Instance.MoveInput);
        }
    }
    
#endregion
#region Alive Airborne
    public class Airborne : StateBase<Player>
    {
        public Airborne(Player owner) : base(owner)
        {
            AddChild(new Fall(Owner));
        }

        public override void Enter()
        {
            Debug.Log("Airborne Enter");
        }
    }
    
    public class JumpState : StateBase<Player>
    {
        public JumpState(Player owner) : base(owner) { }

        public override void Enter()
        {
            Owner.Jump();
        }

        public override void FixedExecute()
        {
            Owner.Move(InputHandler.Instance.MoveInput);
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
    
#endregion
#region Alive Action
    public class Action : StateBase<Player>
    {
        public Action(Player owner) : base(owner) { }

        public override void Enter()
        {
            
        }
    }
#endregion
#endregion
}
