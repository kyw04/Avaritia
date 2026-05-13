using UnityEngine;

public class Player : MonoBehaviour
{
    public PlayerStateMachine StateMachine { get; private set; }
    public Rigidbody2D Rb { get; private set; }

    public bool IsGrounded { get; private set; }

    public float moveSpeed;
    public float jumpForce;
    
    private float acceleration = 20f; // 지면 가속도
    private float deceleration = 10f; // 지면 감속도
    
    private float airAcceleration = 50f; // 공중 가속도
    private float airDeceleration = 30f; // 공중 감속도
    
    private void Awake()
    {
        StateMachine = new PlayerStateMachine(this);
        Rb =  GetComponent<Rigidbody2D>();
        IsGrounded = true; // 테스트용
    }
    
    public void Move(Vector2 input)
    {
        float inputX = input.x;
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
 
        bool isTurning = (currentVelX > 0.1f && inputX < 0f) || (currentVelX < -0.1f && inputX > 0f);
        float rate = isTurning ? accel + decel : accel;
        float newVelX = Mathf.MoveTowards(currentVelX, targetVelX, rate * Time.fixedDeltaTime);
 
        Rb.linearVelocity = new Vector2(newVelX, Rb.linearVelocity.y);
        Debug.Log(Rb.linearVelocity);
    }

    public void Jump()
    {
        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, jumpForce);
    }
}