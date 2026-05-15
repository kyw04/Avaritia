using UnityEngine;

public class PlayerAnimator : MonoBehaviour,
    IObserver<PlayerIdleEvent>,
    IObserver<PlayerMovedEvent>,
    IObserver<PlayerJumpedEvent>,
    IObserver<PlayerFallingEvent>,
    IObserver<PlayerLandedEvent>
{
    private Animator animator;
    private static readonly int Speed = Animator.StringToHash("Speed");

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
    }

    void OnEnable()
    {
        EventBus.Subscribe<PlayerIdleEvent>(this);
        EventBus.Subscribe<PlayerMovedEvent>(this);
        EventBus.Subscribe<PlayerJumpedEvent>(this);
        EventBus.Subscribe<PlayerFallingEvent>(this);
        EventBus.Subscribe<PlayerLandedEvent>(this);
    }

    void OnDisable()
    {
        EventBus.UnsubscribeAll(this);
    }

    public void OnNotify(PlayerIdleEvent e) => animator.Play("idle");
    public void OnNotify(PlayerMovedEvent e) => animator.Play("run");
    public void OnNotify(PlayerJumpedEvent e) => animator.Play("jump");
    public void OnNotify(PlayerFallingEvent e) => animator.Play("fall_loop");
    public void OnNotify(PlayerLandedEvent e) => animator.Play("land");
}