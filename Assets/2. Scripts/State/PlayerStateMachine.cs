using UnityEngine;

public class PlayerStateMachine : StateMachineBase<Player>
{
    public PlayerStateMachine(Player owner) : base(owner) { }

    public override void Init()
    {
        base.Init();
        AddState(new AliveState(Owner));
        ChangeState<AliveState>();
    }
    
#region Alive
    public class AliveState : StateBase<Player>
    {
        public AliveState(Player owner) : base(owner)
        {
            AddChild(new Grounded(owner));
            AddChild(new Airborne(owner));
            AddChild(new Action(owner));
        }
    }

#region Alive Grounded
    public class Grounded : StateBase<Player>
    {
        public Grounded(Player owner) : base(owner)
        {
            AddChild(new IdleState(owner));
            AddChild(new MoveState(owner));
            AddChild(new TurnState(owner));
            AddChild(new LandState(owner));
        }
    }
    
    public class IdleState : StateBase<Player>
    {
        public IdleState(Player owner) : base(owner)
        {
            Machine.AddTransition<IdleState, MoveState>();
            Machine.AddTransition<IdleState, JumpState>();
            Machine.AddTransition<IdleState, FallState>();
            Machine.AddTransition<IdleState, AttackState>();
        }

        public override void Enter()
        {
            EventBus.Publish(new PlayerIdleEvent());
        }

        public override void Execute()
        {
            if (Mathf.Abs(InputHandler.Instance.MoveInput.x) > 0)
                Machine.ChangeState<MoveState>();
        }
    }

    public class MoveState : StateBase<Player>
    {
        private float currentSpeed => Mathf.Abs(Owner.Rb.linearVelocityX) / Owner.moveSpeed;

        public MoveState(Player owner) : base(owner)
        {
            Machine.AddTransition<MoveState, IdleState>();
            Machine.AddTransition<MoveState, JumpState>();
            Machine.AddTransition<MoveState, FallState>();
            Machine.AddTransition<MoveState, TurnState>();
            Machine.AddTransition<MoveState, AttackState>();
        }
        
        public override void Enter()
        {
            EventBus.Publish(new PlayerMovedEvent(currentSpeed));
        }

        public override void Execute()
        {
            if (InputHandler.Instance.MoveInput.x == 0 &&
                currentSpeed == 0)
            {
                Machine.ChangeState<IdleState>();
            }
            else
            {
                EventBus.Publish(new PlayerMovedEvent(currentSpeed));
            }
        }

        public override void FixedExecute()
        {
            Owner.Move(InputHandler.Instance.MoveInput);
        }

        public override void Exit()
        {
            EventBus.Publish(new PlayerMovedEvent(0));
        }
    }

    public class TurnState : StateBase<Player>
    {
        private float currentSpeed => Mathf.Abs(Owner.Rb.linearVelocityX) / Owner.moveSpeed;
        private Vector2 moveDir;
        
        public TurnState(Player owner) : base(owner) { }

        public override void Enter()
        {
            Machine.AddTransition<TurnState, JumpState>();
            Machine.AddTransition<TurnState, MoveState>();
            
            moveDir = InputHandler.Instance.MoveInput;
            EventBus.Publish(new PlayerTurnEvent());
        }

        public override void Execute()
        {
            if (currentSpeed >= 0.5f)
            {
                Machine.ChangeState<MoveState>();
            }
        }
        
        public override void FixedExecute()
        {
            Owner.Move(moveDir);
        }
    }

    public class LandState : StateBase<Player>
    {
        private float landTime = 0.2f;
        private float landStartTime;

        public LandState(Player owner) : base(owner)
        {
            Machine.AddTransition<LandState, IdleState>();
        }

        public override void Enter()
        {
            landStartTime = Time.time;
            EventBus.Publish(new PlayerLandedEvent());
        }

        public override void Execute()
        {
            if (landStartTime + landTime <= Time.time)
                Machine.ChangeState<IdleState>();
        }
    }
    
#endregion
#region Alive Airborne
    public class Airborne : StateBase<Player>
    {
        public Airborne(Player owner) : base(owner)
        {
            AddChild(new FallState(owner));
            AddChild(new JumpState(owner));
        }
    }
    
    public class JumpState : StateBase<Player>
    {
        public JumpState(Player owner) : base(owner)
        {
            Machine.AddTransition<JumpState, FallState>();
        }

        public override void Enter()
        {
            EventBus.Publish(new PlayerJumpedEvent());
            Owner.Jump();
        }

        public override void Execute()
        {
            if (Owner.Rb.linearVelocityY <= 0)
                Machine.ChangeState<FallState>();
        }

        public override void FixedExecute()
        {
            Owner.Move(InputHandler.Instance.MoveInput);
        }
    }
    
    public class FallState : StateBase<Player>
    {
        public FallState(Player owner) : base(owner)
        {
            Machine.AddTransition<FallState, IdleState>();
            Machine.AddTransition<FallState, LandState>();
        }

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
        public Action(Player owner) : base(owner)
        {
            AddChild(new AttackState(owner));
        }

        public override void Enter()
        {
            EventBus.Publish(new PlayerFallingEvent());
        }
    }
    
    public class AttackState : StateBase<Player>
    {
        private ComboBuffer buffer = new();
        private int comboIndex = 0;
        private const int MAX_COMBO = 3;

        public AttackState(Player owner) : base(owner)
        {
            AddChild(new Attack1State(owner));
            AddChild(new Attack2State(owner));
            AddChild(new Attack3State(owner));
            
            Machine.AddTransition<AttackState, IdleState>();
            
        }

        public override void Enter()
        {
            Debug.Log($"Attack Start {comboIndex}");
            Owner.Rb.linearVelocity = Vector2.zero;
            TransitionTo(comboIndex);
        }
        
        private void OnAttackInput()
        {
            if (comboIndex < MAX_COMBO)
                buffer.RegisterInput();
        }
        
        void OnAttackEnded()
        {
            if (buffer.ConsumeInput() && comboIndex < MAX_COMBO)
            {
                comboIndex++;
                TransitionTo(comboIndex);
            }
            else
            {
                comboIndex = 0;
            }
        }

        void TransitionTo(int index)
        {
            switch (index)
            {
                case 0: Machine.ChangeState<Attack1State>(); break;
                case 1: Machine.ChangeState<Attack2State>(); break;
                case 2: Machine.ChangeState<Attack3State>(); break;
            }
        }

        public override void Execute() => buffer.Tick(Time.deltaTime);

        public override void Exit()
        {
            comboIndex = 0;
        }
    }
    
    public abstract class IAttackState : StateBase<Player>
    {

        protected abstract float Duration { get; }
        protected abstract float ComboWindowStart { get; }
        protected abstract AttackData Data { get; }

        private float timer;
        private bool comboWindowOpen = false;
        private bool hitboxActive = false;
        protected IAttackState(Player owner) : base(owner) { }

        public override void Enter()
        {
            timer = 0f;
            comboWindowOpen = false;
            EventBus.Publish(new PlayerAttackEvent(Data));
        }

        public override void Execute()
        {
            timer += Time.deltaTime;

            if (!comboWindowOpen && timer >= ComboWindowStart)
            {
                comboWindowOpen = true;
            }

            if (timer >= Duration)
            {
                Machine.ChangeState<IdleState>();
            }
        }

        // public override void Exit() => DeactivateHitbox();
    }
    
    public class Attack1State : IAttackState
    {
        protected override float Duration => 0.4f;
        protected override float ComboWindowStart => 0.25f;
        protected override AttackData Data => new();
        public Attack1State(Player owner) : base(owner) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("Attack 1");
        }
    }
    
    public class Attack2State : IAttackState
    {
        protected override float Duration => 0.4f;
        protected override float ComboWindowStart => 0.25f;
        protected override AttackData Data => new();
        public Attack2State(Player owner) : base(owner) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("Attack 2");
        }
    }
    
    public class Attack3State : IAttackState
    {
        protected override float Duration => 0.4f;
        protected override float ComboWindowStart => 0.25f;
        protected override AttackData Data => new();
        public Attack3State(Player owner) : base(owner) { }

        public override void Enter()
        {
            base.Enter();
            Debug.Log("Attack 3");
        }
    }
#endregion
#endregion
}
