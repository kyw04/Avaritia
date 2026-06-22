using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class PlayerStateMachine : StateMachineBase<Player>
{
    public PlayerStateMachine(Player owner) : base(owner) { }

    public override void Init()
    {
        base.Init();
        AddState(new PlayerAliveState(Owner));
        ChangeState<PlayerAliveState>();
    }
}
   
#region Alive
    public class PlayerAliveState : StateBase<Player>
    {
        public PlayerAliveState(Player owner) : base(owner)
        {
            AddChild(new PlayerGrounded(owner));
            AddChild(new PlayerAirborne(owner));
            AddChild(new PlayerAction(owner));
        }
    }

#region Grounded
    public class PlayerGrounded : StateBase<Player>
    {
        public PlayerGrounded(Player owner) : base(owner)
        {
            AddChild(new PlayerIdleState(owner));
            AddChild(new PlayerMoveState(owner));
            AddChild(new PlayerTurnState(owner));
            AddChild(new PlayerLandState(owner));
        }
    }
    
    public class PlayerIdleState : StateBase<Player>
    {
        public PlayerIdleState(Player owner) : base(owner)
        {
            Machine.AddTransition<PlayerIdleState, PlayerMoveState>();
            Machine.AddTransition<PlayerIdleState, PlayerJumpState>();
            Machine.AddTransition<PlayerIdleState, PlayerFallState>();
            Machine.AddTransition<PlayerIdleState, PlayerAttackState>();
            Machine.AddTransition<PlayerIdleState, PlayerDashState>();
        }

        public override void Enter()
        {
            EventBus.Publish(new PlayerIdleEvent());
        }

        public override void Execute()
        {
            if (Mathf.Abs(InputHandler.Instance.MoveInput.x) > 0)
                Machine.ChangeState<PlayerMoveState>();
        }
    }

    public class PlayerMoveState : StateBase<Player>
    {
        private float currentSpeed => Mathf.Abs(Owner.Rb.linearVelocityX) / Owner.MoveSpeed;

        public PlayerMoveState(Player owner) : base(owner)
        {
            Machine.AddTransition<PlayerMoveState, PlayerIdleState>();
            Machine.AddTransition<PlayerMoveState, PlayerJumpState>();
            Machine.AddTransition<PlayerMoveState, PlayerFallState>();
            Machine.AddTransition<PlayerMoveState, PlayerTurnState>();
            Machine.AddTransition<PlayerMoveState, PlayerAttackState>();
            Machine.AddTransition<PlayerMoveState, PlayerDashState>();
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
                Machine.ChangeState<PlayerIdleState>();
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

    public class PlayerTurnState : StateBase<Player>
    {
        private float currentSpeed => Mathf.Abs(Owner.Rb.linearVelocityX) / Owner.MoveSpeed;
        private Vector2 moveDir;
        
        public PlayerTurnState(Player owner) : base(owner) { }

        public override void Enter()
        {
            Machine.AddTransition<PlayerTurnState, PlayerJumpState>();
            Machine.AddTransition<PlayerTurnState, PlayerMoveState>();
            
            moveDir = InputHandler.Instance.MoveInput;
            EventBus.Publish(new PlayerTurnEvent());
        }

        public override void Execute()
        {
            if (currentSpeed >= 0.5f)
            {
                Machine.ChangeState<PlayerMoveState>();
            }
        }
        
        public override void FixedExecute()
        {
            Owner.Move(moveDir);
        }
    }

    public class PlayerLandState : StateBase<Player>
    {
        private float landTime = 0.2f;
        private float landStartTime;

        public PlayerLandState(Player owner) : base(owner)
        {
            Machine.AddTransition<PlayerLandState, PlayerIdleState>();
        }

        public override void Enter()
        {
            landStartTime = Time.time;
            EventBus.Publish(new PlayerLandedEvent());
        }

        public override void Execute()
        {
            if (landStartTime + landTime <= Time.time)
                Machine.ChangeState<PlayerIdleState>();
        }
    }
    
#endregion
#region Airborne
    public class PlayerAirborne : StateBase<Player>
    {
        public PlayerAirborne(Player owner) : base(owner)
        {
            AddChild(new PlayerFallState(owner));
            AddChild(new PlayerJumpState(owner));
            AddChild(new PlayerDashState(owner));
        }
    }
    
    public class PlayerJumpState : StateBase<Player>
    {
        public PlayerJumpState(Player owner) : base(owner)
        {
            Machine.AddTransition<PlayerJumpState, PlayerDashState>();
            Machine.AddTransition<PlayerJumpState, PlayerFallState>();
            Machine.AddTransition<PlayerFallState, PlayerJumpState>(
                () => owner.JumpCount < owner.MaxJumpCount);
        }

        public override void Enter()
        {
            EventBus.Publish(new PlayerJumpedEvent());
            Owner.Jump();
        }

        public override void Execute()
        {
            if (Owner.Rb.linearVelocityY <= 0)
                Machine.ChangeState<PlayerFallState>();
        }

        public override void FixedExecute()
        {
            Owner.Move(InputHandler.Instance.MoveInput);
        }
    }

    public class PlayerDashState : StateBase<Player>
    {
        private float dashTime = 0.2f;
        private float dashStartTime;
        public PlayerDashState(Player owner) : base(owner)
        {
            Machine.AddTransition<PlayerDashState, PlayerIdleState>();
            Machine.AddTransition<PlayerDashState, PlayerFallState>();
            Machine.AddTransition<PlayerDashState, PlayerMoveState>();
            Machine.AddTransition<PlayerDashState, PlayerJumpState>(
                () => owner.JumpCount < owner.MaxJumpCount);
        }

        public override void Enter()
        {
            EventBus.Publish(new PlayerDashEvent());
            Owner.Rb.gravityScale = 0f;
            Owner.Dash();
            dashStartTime = Time.time;
        }

        public override void Execute()
        {
            if (dashStartTime + dashTime <= Time.time)
            {
                Owner.Rb.linearVelocity = Vector2.zero;
                Owner.Machine.ChangeState<PlayerFallState>();
            }
        }

        public override void Exit()
        {
            Owner.Rb.gravityScale = 1f;
        }
    }
    
    public class PlayerFallState : StateBase<Player>
    {
        public PlayerFallState(Player owner) : base(owner)
        {
            Machine.AddTransition<PlayerFallState, PlayerIdleState>();
            Machine.AddTransition<PlayerFallState, PlayerLandState>();
            Machine.AddTransition<PlayerFallState, PlayerDashState>();
        }

        public override void Enter()
        {
            EventBus.Publish(new PlayerFallingEvent());
        }

        public override void Execute()
        {
            if (Owner.IsGrounded)
            {
                Owner.Machine.ChangeState<PlayerIdleState>();
            }
        }

        public override void FixedExecute()
        {
            Owner.Move(InputHandler.Instance.MoveInput);
        }
    }
    
#endregion
#region Action
    public class PlayerAction : StateBase<Player>
    {
        public PlayerAction(Player owner) : base(owner)
        {
            AddChild(new PlayerAttackState(owner));
        }

        public override void Enter()
        {
            EventBus.Publish(new PlayerFallingEvent());
        }
    }
    
    public class PlayerAttackState : StateBase<Player>, IObserver<PlayerAttackBufferEvent>
    {
        private AttackDataCombo comboData;
        private RaycastHit2D[] hitResults = new RaycastHit2D[10];

        private bool buffer;
        private bool hasAttack;
        private float timer;
        private int comboIndex;

        public PlayerAttackState(Player owner) : base(owner)
        {
            DataLoad();
            EventBus.Subscribe<PlayerAttackBufferEvent>(this);
            
            Machine.AddTransition<PlayerAttackState, PlayerIdleState>();
            Machine.AddTransition<PlayerAttackState, PlayerDashState>();
        }

        private async void DataLoad() // 데이터 받아오는 애 만들어서 관리 하면 좋을 듯
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<AttackDataCombo>("attack_combo/player_001");
                await handle.Task;
            
                comboData = handle.Result;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }
        
        public override void Enter()
        {
            comboIndex = 0;
            hasAttack = false;
            timer = 0f;
          
            Owner.Rb.linearVelocity = Vector2.zero;
            EventBus.Publish(new PlayerAttackStartEvent(comboData.datas[comboIndex]));
        }

        public override void Execute()
        {
            if (timer <= 0.01f)
                buffer = false;

            if (!hasAttack) // Attack Manager 만들어서 관리 하면 더 좋을 듯
            {
                hasAttack = true;

                comboData.datas[comboIndex].Attack(Owner);
            }
            
            timer += Time.deltaTime;
            if (timer >= comboData.datas[comboIndex].duration)
            {
                if (buffer)
                {
                    comboIndex = (comboIndex + 1) % comboData.Count;

                    Owner.Rb.linearVelocity = Vector2.zero;
                    EventBus.Publish(new PlayerAttackStartEvent(comboData.datas[comboIndex]));
                    buffer = false;
                    hasAttack = false;
                }
                else
                {
                    comboIndex = 0;
                    Machine.ChangeState<PlayerIdleState>();
                }
                timer = 0f;
            }
        }

        public void OnNotify(PlayerAttackBufferEvent e) => buffer = true;
    }

#endregion

#endregion
