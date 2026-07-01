using System.Collections;
using UnityEngine;
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

    private RuntimeStats stats;
    private bool wasGroundCheckerChanged;
    private bool isTurning;
    private bool isDead;
    
    public bool IsGrounded { get; private set; }
    public bool IsAttacking { get; set; }
    public float MaxHealth => stats.Get<float>(StatType.MaxHealth);
    public float CurrentHealth => stats.Get<float>(StatType.CurrentHealth);
    public float MoveSpeed => stats.Get<float>(StatType.MoveSpeed);
    public int MaxJumpCount => statDataAsset.TryGetValue<int>(StatType.JumpCount);
    public int JumpCount => stats.Get<int>(StatType.JumpCount);
    public float JumpForce => stats.Get<float>(StatType.JumpForce);
    public float Damage => stats.Get<float>(StatType.Damage);
    public int MaxDashCount => statDataAsset.TryGetValue<int>(StatType.DashCount);
    public int DashCount => stats.Get<int>(StatType.DashCount);
    public float DashCooldown => stats.Get<float>(StatType.DashCooldown);
    
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
        Owner.stats.Set(StatType.JumpCount, 0);
        Owner.stats.Set(StatType.DashCount, 0);
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
                Owner.stats.Set(StatType.JumpCount, 0);
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
        stats.Set(StatType.JumpCount, JumpCount + 1);
        Rb.linearVelocity = new Vector2(Rb.linearVelocity.x, JumpForce);
    }

    public void Dash()
    {
        stats.Set(StatType.DashCount, DashCount + 1);
        Rb.linearVelocity = Vector2.zero;
        Rb.AddForce(Vector2.right * transform.localScale.x * 50f, ForceMode2D.Impulse);

        StartCoroutine(DashCooldownRoutine());
    }

    private IEnumerator DashCooldownRoutine()
    {
        yield return new WaitForSeconds(DashCooldown);
        stats.Set(StatType.DashCount, DashCount - 1);
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
        PlayerHpUI.UpdateHealthBar(CurrentHealth /  MaxHealth);
        
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