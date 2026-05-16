using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerStateMachine StateMachine { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    
    public bool IsGrounded { get; private set; }
    private bool wasGroundCheckerChanged;
    private bool isTurning;

    [SerializeField] private string state;
    
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius;
    [SerializeField] private LayerMask groundLayer;
    
    public float moveSpeed;
    public float jumpForce;
    
    private float acceleration = 20f; // 지면 가속도
    private float deceleration = 10f; // 지면 감속도
    
    private float airAcceleration = 30f; // 공중 가속도
    private float airDeceleration = 15f; // 공중 감속도
    
    private void Awake()
    {
        StateMachine = new PlayerStateMachine(this);
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
                StateMachine.ChangeState<PlayerStateMachine.IdleState>();
            }
            else if (Rb.linearVelocityY <= 0)
            {
                StateMachine.ChangeState<PlayerStateMachine.FallState>();
            }
        }
        
        state = StateMachine.currentState.CurrentChild.CurrentChild.ToString();
    }

    public void Move(Vector2 input)
    {
        float inputX = input.x;
        if (inputX == 0)
            return;
        
        Renderer.flipX = inputX < 0;
        
        float targetVelX  = inputX * moveSpeed;
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
        float absCntVelX = Mathf.Abs(currentVelX);
        Debug.Log(absCntVelX);
        if (!isTurning && isTurnStarting && absCntVelX >= 0.9f)
        {
            rate = accel + decel;
            isTurning = true;
        }
        if (isTurning)
        {
            StateMachine.ChangeState<PlayerStateMachine.TurnState>();
            
            if (absCntVelX / moveSpeed >= 0.55f)
            {
                isTurning = false;
            }
        }
           
        
        float newVelX = Mathf.MoveTowards(currentVelX, targetVelX, rate * Time.fixedDeltaTime);
 
        Rb.linearVelocity = new Vector2(newVelX, Rb.linearVelocity.y);
    }

    public void Jump()
    {
        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, jumpForce);
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