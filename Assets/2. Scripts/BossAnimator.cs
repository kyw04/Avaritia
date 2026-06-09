using UnityEngine;

public class BossAnimator : MonoBehaviour,
    IObserver<BossIdleEvent>,
    IObserver<BossMovedEvent>,
    IObserver<BossJumpedEvent>,
    IObserver<BossFallingEvent>,
    IObserver<BossLandedEvent>,
    IObserver<BossDeadEvent>,
    IObserver<BossAttackStartEvent>
{
    private Animator animator;
    private static readonly int Speed = Animator.StringToHash("Speed");

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<BossIdleEvent>(this);
        EventBus.Subscribe<BossMovedEvent>(this);
        EventBus.Subscribe<BossJumpedEvent>(this);
        EventBus.Subscribe<BossFallingEvent>(this);
        EventBus.Subscribe<BossLandedEvent>(this);
        EventBus.Subscribe<BossAttackStartEvent>(this);
    }

    private void OnDisable()
    {
        EventBus.UnsubscribeAll(this);
    }
    
    public void OnNotify(BossIdleEvent e) => animator.Play("idle");
    public void OnNotify(BossMovedEvent e)
    {
        animator.SetFloat(Speed, e.Speed);
        animator.Play("Move");
    }
    public void OnNotify(BossJumpedEvent e) => animator.Play("jump");
    public void OnNotify(BossFallingEvent e) => animator.Play("fall_loop");
    public void OnNotify(BossLandedEvent e) => animator.Play("land");
    public void OnNotify(BossDeadEvent e) => animator.Play("dead");
    
    public void OnNotify(BossAttackStartEvent e) => animator.Play(e.Data.animClip.name);

}