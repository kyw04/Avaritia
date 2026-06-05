using UnityEngine;

public class Player : MonoBehaviour, IStateOwner<Player>
{
    public Player Owner { get; private set; }
    public IStateMachine Machine { get; private set; }
    
    public Rigidbody2D Rb { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    
    public bool IsGrounded { get; private set; }
    private bool wasGroundCheckerChanged;
    private bool isTurning;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private StatData statData;

    public float MoveSpeed => statData.TryGetValue<float>(StatType.Speed);
    public float JumpForce => statData.TryGetValue<float>(StatType.JumpForce);
    
    private float acceleration = 20f; // 지면 가속도
    private float deceleration = 10f; // 지면 감속도
    
    private float airAcceleration = 30f; // 공중 가속도
    private float airDeceleration = 15f; // 공중 감속도
    
    private void Awake()
    {
        Owner = this;
        Machine = new PlayerStateMachine(Owner);
        Machine.Init();
        
        Rb = GetComponent<Rigidbody2D>();
        Renderer = GetComponentInChildren<SpriteRenderer>();
        
        wasGroundCheckerChanged = !IsGrounded;
    }

    private void Update()
    {
        IsGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundRadius,
            groundLayer
        );

        if (wasGroundCheckerChanged != IsGrounded)
        {
            wasGroundCheckerChanged = IsGrounded;
            if (IsGrounded)
            {
                if (Rb.linearVelocityY <= -5)
                {
                    Machine.ChangeState<PlayerLandState>();
                }
                else
                {
                    Machine.ChangeState<PlayerIdleState>();
                }
            }
            else if (Rb.linearVelocityY <= 0)
            {
                Machine.ChangeState<PlayerFallState>();
            }
        }
    }

    public void Move(Vector2 input)
    {
        float inputX = input.x;
        if (inputX == 0)
            return;
        
        Renderer.flipX = inputX < 0;
        
        float targetVelX  = inputX * MoveSpeed;
        float currentVelX = Rb.linearVelocity.x;
 
        float accel, decel;
        if (IsGrounded)
        {
            accel = acceleration;
            decel = deceleration;
        }
        else
        {
            accel = airAcceleration;
            decel = airDeceleration;
        }
 
        bool isTurnStarting = (currentVelX > 0.1f && inputX < 0f) || (currentVelX < -0.1f && inputX > 0f);
        float rate = accel;
        float absCntSpeedPer = Mathf.Abs(currentVelX) / MoveSpeed;
        if (!isTurning && isTurnStarting && absCntSpeedPer >= 0.9f)
        {
            rate = accel + decel;
            isTurning = true;
        }
        if (isTurning)
        {
            Machine.ChangeState<PlayerTurnState>();
            
            if (absCntSpeedPer <= 0.55f)
            {
                isTurning = false;
            }
        }
           
        
        float newVelX = Mathf.MoveTowards(currentVelX, targetVelX, rate * Time.fixedDeltaTime);
 
        Rb.linearVelocity = new Vector2(newVelX, Rb.linearVelocity.y);
    }

    public void Jump()
    {
        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, JumpForce);
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            if (IsGrounded) Gizmos.color = Color.green;
            else Gizmos.color = Color.gray2;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
        
    }
}