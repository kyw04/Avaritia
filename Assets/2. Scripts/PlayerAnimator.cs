using UnityEngine;

public class PlayerAnimator : MonoBehaviour,
    IObserver<PlayerIdleEvent>,
    IObserver<PlayerMovedEvent>,
    IObserver<PlayerJumpedEvent>,
    IObserver<PlayerFallingEvent>,
    IObserver<PlayerLandedEvent>,
    IObserver<PlayerTurnEvent>,
    IObserver<PlayerAttackEvent>
{
    private Animator animator;
    private static readonly int Speed = Animator.StringToHash("Speed");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerIdleEvent>(this);
        EventBus.Subscribe<PlayerMovedEvent>(this);
        EventBus.Subscribe<PlayerJumpedEvent>(this);
        EventBus.Subscribe<PlayerFallingEvent>(this);
        EventBus.Subscribe<PlayerLandedEvent>(this);
        EventBus.Subscribe<PlayerTurnEvent>(this);
        EventBus.Subscribe<PlayerAttackEvent>(this);
    }

    private void OnDisable()
    {
        EventBus.UnsubscribeAll(this);
    }

    public void EndAttacking()
    {
        EventBus.Publish(new PlayerEndAttackEvent());
    }
    
    public void OnNotify(PlayerIdleEvent e) => animator.Play("idle");
    public void OnNotify(PlayerMovedEvent e)
    {
        animator.SetFloat(Speed, e.Speed);
        animator.Play("Move");
    }
    public void OnNotify(PlayerJumpedEvent e) => animator.Play("jump");
    public void OnNotify(PlayerFallingEvent e) => animator.Play("fall_loop");
    public void OnNotify(PlayerLandedEvent e) => animator.Play("land");
    public void OnNotify(PlayerTurnEvent e) => animator.Play("turn");
    public void OnNotify(PlayerAttackEvent e) => animator.Play(e.Data.animClip.name);

}