using UnityEngine;

public class BossAnimator : AnimatorBase,
    IObserver<BossIdleEvent>,
    IObserver<BossMovedEvent>,
    IObserver<BossJumpedEvent>,
    IObserver<BossFallingEvent>,
    IObserver<BossLandedEvent>,
    IObserver<BossDeadEvent>,
    IObserver<BossAttackStartEvent>
{
    private static readonly int Speed = Animator.StringToHash("Speed");
    
    private void OnEnable()
    {
        EventBus.Subscribe<BossIdleEvent>(this);
        EventBus.Subscribe<BossMovedEvent>(this);
        EventBus.Subscribe<BossJumpedEvent>(this);
        EventBus.Subscribe<BossFallingEvent>(this);
        EventBus.Subscribe<BossLandedEvent>(this);
        EventBus.Subscribe<BossAttackStartEvent>(this);
    }

    public void OnNotify(BossIdleEvent e) => PlayAnimation("idle");
    public void OnNotify(BossMovedEvent e)
    {
        animator.SetFloat(Speed, e.Speed);
        PlayAnimation("Move");
    }
    public void OnNotify(BossJumpedEvent e) => PlayAnimation("jump");
    public void OnNotify(BossFallingEvent e) => PlayAnimation("fall_loop");
    public void OnNotify(BossLandedEvent e) => PlayAnimation("land");
    public void OnNotify(BossDeadEvent e) => PlayAnimation("dead");

    public void OnNotify(BossAttackStartEvent e)
    {
        string attackAnimName = e.Data.attackAnimClip.name;
        var readyClip = e.Data.readyAnimClip;
        if (readyClip != null)
        {
            if (animCoroutine != null)
                StopCoroutine(animCoroutine);
            
            PlayAnimation(readyClip.name);
            animCoroutine = StartCoroutine(PlayAnimationAfterDelay(attackAnimName, readyClip.length));
        }
        else
        {
            PlayAnimation(attackAnimName);
        }
    }
}