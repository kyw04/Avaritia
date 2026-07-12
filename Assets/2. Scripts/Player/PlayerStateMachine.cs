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
            EventBus.Publish(new EntityIdleEvent(Owner));
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
            EventBus.Publish(new EntityMovedEvent(Owner, currentSpeed));
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
                EventBus.Publish(new EntityMovedEvent(Owner, currentSpeed));
            }
        }

        public override void FixedExecute()
        {
            Owner.Move(InputHandler.Instance.MoveInput);
        }

        public override void Exit()
        {
            EventBus.Publish(new EntityMovedEvent(Owner, 0));
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
            EventBus.Publish(new EntityTurnEvent(Owner));
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
            EventBus.Publish(new EntityLandedEvent(Owner));
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
        private JumpDataList jumpDataList;

        public PlayerJumpState(Player owner) : base(owner)
        {
            Machine.AddTransition<PlayerJumpState, PlayerDashState>(
                () => owner.DashCount < owner.MaxDashCount);
            Machine.AddTransition<PlayerJumpState, PlayerFallState>();
            Machine.AddTransition<PlayerJumpState, PlayerJumpState>(
                () => owner.DoubleJumpCount < owner.MaxDoubleJumpCount);
            Machine.AddTransition<PlayerFallState, PlayerJumpState>(
                () => owner.DoubleJumpCount < owner.MaxDoubleJumpCount);

            DataLoad();
        }

        private async void DataLoad()
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<JumpDataList>("jump_data/player_001");
                await handle.Task;
                jumpDataList = handle.Result;
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }

        public override void Enter()
        {
            int index = Owner.IsGrounded ? 0 : 1;
            EventBus.Publish(new EntityJumpedEvent(Owner, jumpDataList.datas[index]));
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
                () => owner.DoubleJumpCount < owner.MaxDoubleJumpCount);
            Machine.AddTransition<PlayerDashState, PlayerDashState>(
                () => owner.DashCount < owner.MaxDashCount);
        }

        public override void Enter()
        {
            EventBus.Publish(new EntityDashEvent(Owner));
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
            Machine.AddTransition<PlayerFallState, PlayerDashState>(
                () => owner.DashCount < owner.MaxDashCount);
        }

        public override void Enter()
        {
            EventBus.Publish(new EntityFallingEvent(Owner));
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
            EventBus.Publish(new EntityFallingEvent(Owner));
        }
    }
    
    public class PlayerAttackState : StateBase<Player>, IObserver<EntityAttackBufferEvent>
    {
        private AttackDataCombo combo;
        private RaycastHit2D[] hitResults = new RaycastHit2D[10];

        private bool buffer;
        private bool hasAttack;
        private float timer;
        private int comboIndex;

        public PlayerAttackState(Player owner) : base(owner)
        {
            EventBus.Subscribe<EntityAttackBufferEvent>(this);

            Machine.AddTransition<PlayerAttackState, PlayerIdleState>();
            Machine.AddTransition<PlayerAttackState, PlayerDashState>();
        }

        public override void Enter()
        {
            if (Owner.Weapon == null || Owner.Weapon.combo == null || Owner.Weapon.combo.Count == 0)
            {
                Machine.ChangeState<PlayerIdleState>();
                return;
            }

            combo = Owner.Weapon.combo;
            comboIndex = 0;
            hasAttack = false;
            timer = 0f;

            Owner.Rb.linearVelocity = Vector2.zero;
            EventBus.Publish(new EntityAttackStartEvent(Owner, combo.datas[comboIndex]));
        }

        public override void Execute()
        {
            if (timer <= 0.01f)
                buffer = false;

            if (!hasAttack) // Attack Manager 만들어서 관리 하면 더 좋을 듯
            {
                hasAttack = true;

                combo.datas[comboIndex].Attack(Owner);
            }

            timer += Time.deltaTime;
            if (timer >= combo.datas[comboIndex].duration)
            {
                if (buffer)
                {
                    comboIndex = (comboIndex + 1) % combo.Count;

                    Owner.Rb.linearVelocity = Vector2.zero;
                    EventBus.Publish(new EntityAttackStartEvent(Owner, combo.datas[comboIndex]));
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

        public void OnNotify(EntityAttackBufferEvent e) => buffer = true;
    }

#endregion

#endregion
