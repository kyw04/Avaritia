using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

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
    
    public class AttackState : StateBase<Player>, IObserver<PlayerAttackBufferEvent>
    {
        private AttackDataCombo comboData;

        private bool buffer;
        private bool isAttaking;
        private float timer;
        private int comboIndex;

        public AttackState(Player owner) : base(owner)
        {
            DataLoad();
            EventBus.Subscribe<PlayerAttackBufferEvent>(this);
            
            Machine.AddTransition<AttackState, IdleState>();
        }

        private async void DataLoad()
        {
            var hanlde = 
                Addressables.LoadAssetAsync<AttackDataCombo>("attack_combo/player_001");
            
            await hanlde.Task;
            
            comboData = hanlde.Result;
        }
        
        public override void Enter()
        {
            comboIndex = 0;
            isAttaking = false;
            timer = 0f;
          
            Owner.Rb.linearVelocity = Vector2.zero;
            EventBus.Publish(new PlayerAttackStartEvent(comboData.datas[comboIndex]));
        }

        public override void Execute()
        {
            if (timer <= 0.01f)
                buffer = false;
            
            timer += Time.deltaTime;
            if (timer >= comboData.datas[comboIndex].duration)
            {
                if (buffer)
                {
                    comboIndex = (comboIndex + 1) % comboData.Count;
                    Debug.Log($"Attack Start {comboIndex}");
                    Debug.Log($"Attack buffer {buffer}");

                    Owner.Rb.linearVelocity = Vector2.zero;
                    EventBus.Publish(new PlayerAttackStartEvent(comboData.datas[comboIndex]));
                    buffer = false;
                }
                else
                {
                    comboIndex = 0;
                    Machine.ChangeState<IdleState>();
                }
                timer = 0f;
            }
        }

        public void OnNotify(PlayerAttackBufferEvent e) {buffer = true; Debug.Log("OnNotify buffer");}
    }

#endregion
#endregion
}
