using UnityEngine;

public class PlayerAnimator : AnimatorBase,
    IObserver<PlayerIdleEvent>,
    IObserver<PlayerMovedEvent>,
    IObserver<PlayerJumpedEvent>,
    IObserver<PlayerFallingEvent>,
    IObserver<PlayerLandedEvent>,
    IObserver<PlayerTurnEvent>,
    IObserver<PlayerDashEvent>,
    IObserver<PlayerAttackStartEvent>
{
    private static readonly int Speed = Animator.StringToHash("Speed");

    private void OnEnable()
    {
        EventBus.Subscribe<PlayerIdleEvent>(this);
        EventBus.Subscribe<PlayerMovedEvent>(this);
        EventBus.Subscribe<PlayerJumpedEvent>(this);
        EventBus.Subscribe<PlayerFallingEvent>(this);
        EventBus.Subscribe<PlayerLandedEvent>(this);
        EventBus.Subscribe<PlayerTurnEvent>(this);
        EventBus.Subscribe<PlayerDashEvent>(this);
        EventBus.Subscribe<PlayerAttackStartEvent>(this);
    }

    public void EndAttacking()
    {
        EventBus.Publish(new PlayerAttackEndEvent());
    }
    
    public void OnNotify(PlayerIdleEvent e) => PlayAnimation("idle");
    public void OnNotify(PlayerMovedEvent e)
    {
        animator.SetFloat(Speed, e.Speed);
        PlayAnimation("Move");
    }
    public void OnNotify(PlayerJumpedEvent e) => PlayAnimation("jump");
    public void OnNotify(PlayerFallingEvent e) => PlayAnimation("fall_loop");
    public void OnNotify(PlayerLandedEvent e) => PlayAnimation("land");
    public void OnNotify(PlayerTurnEvent e) => PlayAnimation("turn");
    public void OnNotify(PlayerDashEvent e) => PlayAnimation("dash");

    public void OnNotify(PlayerAttackStartEvent e)
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