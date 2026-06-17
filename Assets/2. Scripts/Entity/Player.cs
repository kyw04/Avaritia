using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class Player : MonoBehaviour, IStateOwner<Player>, IDamageable, IAttacker
{
    public Player Owner { get; private set; }
    public IStateMachine Machine { get; private set; }
    public MonoBehaviour Mono => this;
    public Rigidbody2D Rb { get; private set; }
    public SpriteRenderer Renderer { get; private set; }
    
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private StatData statDataAsset;
    [SerializeField] private Image healthBarImage;

    private RuntimeStats stats;
    private bool wasGroundCheckerChanged;
    private bool isTurning;
    private bool isDead;
    
    public bool IsGrounded { get; private set; }
    public bool IsAttacking { get; set; }
    public float MaxHealth => stats.Get<float>(StatType.MaxHealth);
    public float CurrentHealth => stats.Get<float>(StatType.CurrentHealth);
    public float MoveSpeed => stats.Get<float>(StatType.MoveSpeed);
    public float JumpForce => stats.Get<float>(StatType.JumpForce);
    public float Damage => stats.Get<float>(StatType.Damage);

    private float acceleration = 20f; // 지면 가속도
    private float deceleration = 10f; // 지면 감속도
    
    private float airAcceleration = 30f; // 공중 가속도
    private float airDeceleration = 15f; // 공중 감속도
    
    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        stats = new RuntimeStats(statDataAsset);
        Renderer = GetComponentInChildren<SpriteRenderer>();
        
        Owner = this;
        Machine = new PlayerStateMachine(Owner);
        Machine.Init();
        
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

        int flip = inputX > 0 ? 1 : -1;
        float scale = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(scale * flip, scale, scale);
        
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

    public void Dash()
    {
        Debug.Log("[Dash] starting dash");
        Rb.linearVelocity = Vector2.zero;
        Rb.AddForce(Vector2.right * transform.localScale.x * 50f, ForceMode2D.Impulse);
    }
    
    public void Die()
    {
        if (isDead) 
            return;
        isDead = true;
        
        StateManager.Instance.Unregister(Machine);
        Debug.Log("Player: 사망");
    }

    public void TakeDamage(float damage)
    {
        stats.Set(StatType.CurrentHealth, CurrentHealth - damage);
        healthBarImage.fillAmount = CurrentHealth / MaxHealth;
        
        if (CurrentHealth <= 0)
            Die();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * transform.localScale.x * 0.5f);
        
        if (groundCheck != null)
        {
            if (IsGrounded) Gizmos.color = Color.green;
            else Gizmos.color = Color.gray2;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
        
    }
}