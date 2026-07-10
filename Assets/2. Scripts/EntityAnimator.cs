using UnityEngine;

public class EntityAnimator : AnimatorBase,
    IObserver<EntityIdleEvent>,
    IObserver<EntityMovedEvent>,
    IObserver<EntityJumpedEvent>,
    IObserver<EntityFallingEvent>,
    IObserver<EntityLandedEvent>,
    IObserver<EntityTurnEvent>,
    IObserver<EntityDashEvent>,
    IObserver<EntityAttackStartEvent>
{
    private static readonly int Speed = Animator.StringToHash("Speed");
    private Entity self;

    protected override void Awake()
    {
        base.Awake();
        self = GetComponentInParent<Entity>();
    }

    private void OnEnable()
    {
        EventBus.Subscribe<EntityIdleEvent>(this);
        EventBus.Subscribe<EntityMovedEvent>(this);
        EventBus.Subscribe<EntityJumpedEvent>(this);
        EventBus.Subscribe<EntityFallingEvent>(this);
        EventBus.Subscribe<EntityLandedEvent>(this);
        EventBus.Subscribe<EntityTurnEvent>(this);
        EventBus.Subscribe<EntityDashEvent>(this);
        EventBus.Subscribe<EntityAttackStartEvent>(this);
    }

    public void EndAttacking()
    {
        EventBus.Publish(new EntityAttackEndEvent());
    }

    public void OnNotify(EntityIdleEvent e) { if (e.Source == self) PlayAnimation("idle"); }
    public void OnNotify(EntityMovedEvent e)
    {
        if (e.Source != self)
            return;
        animator.SetFloat(Speed, e.Speed);
        PlayAnimation("Move");
    }
    public void OnNotify(EntityJumpedEvent e) { if (e.Source == self) PlayAnimation(e.Data.jumpClip.name); }
    public void OnNotify(EntityFallingEvent e) { if (e.Source == self) PlayAnimation("fall_loop"); }
    public void OnNotify(EntityLandedEvent e) { if (e.Source == self) PlayAnimation("land"); }
    public void OnNotify(EntityTurnEvent e) { if (e.Source == self) PlayAnimation("turn"); }
    public void OnNotify(EntityDashEvent e) { if (e.Source == self) PlayAnimation("dash"); }

    public void OnNotify(EntityAttackStartEvent e)
    {
        if (e.Source != self)
            return;

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