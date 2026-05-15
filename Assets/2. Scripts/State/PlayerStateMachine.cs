using UnityEngine;

public class PlayerStateMachine : StateMachineBase<Player>
{
    public PlayerStateMachine(Player owner) : base(owner)
    {
        AddTransition<IdleState, MoveState>();
        AddTransition<IdleState, JumpState>();
        AddTransition<IdleState, FallState>();
        
        AddTransition<MoveState, IdleState>();
        AddTransition<MoveState, JumpState>();
        AddTransition<MoveState, FallState>();
        
        AddTransition<JumpState, FallState>();
        
        AddTransition<FallState, IdleState>();
        
        
        AddState(new AliveState(Owner));
        ChangeState<AliveState>();
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
    }

#region Alive Grounded
    public class Grounded : StateBase<Player>
    {
        public Grounded(Player owner) : base(owner)
        {
            AddChild(new IdleState(Owner));
            AddChild(new MoveState(Owner));
        }
    }
    
    public class IdleState : StateBase<Player>
    {
        public IdleState(Player owner) : base(owner) { }

        public override void Enter()
        {
            EventBus.Publish(new PlayerIdleEvent());
        }

        public override void Execute()
        {
            if (Mathf.Abs(InputHandler.Instance.MoveInput.x) > 0)
                Owner.StateMachine.ChangeState<MoveState>();
        }
    }

    public class MoveState : StateBase<Player>
    {
        public MoveState(Player owner) : base(owner) { }
        
        public override void Enter()
        {
            EventBus.Publish(new PlayerMovedEvent());
        }

        public override void Execute()
        {
            if (InputHandler.Instance.MoveInput.x == 0)
                Owner.StateMachine.ChangeState<IdleState>();
        }

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
            AddChild(new FallState(Owner));
            AddChild(new JumpState(Owner));
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
            EventBus.Publish(new PlayerJumpedEvent());
            Owner.Jump();
        }

        public override void Execute()
        {
            if (Owner.Rb.linearVelocityY <= 0)
                Owner.StateMachine.ChangeState<FallState>();
        }

        public override void FixedExecute()
        {
            Owner.Move(InputHandler.Instance.MoveInput);
        }
    }
    
    public class FallState : StateBase<Player>
    {
        public FallState(Player owner) : base(owner) { }

        public override void Enter()
        {
            EventBus.Publish(new PlayerFallingEvent());
        }

        public override void FixedExecute()
        {
            Owner.Move(InputHandler.Instance.MoveInput);
        }
    }
    
#endregion
#region Alive Action
    public class Action : StateBase<Player>
    {
        public Action(Player owner) : base(owner) { }

        public override void Enter()
        {
            EventBus.Publish(new PlayerFallingEvent());
        }
    }
#endregion
#endregion
}
