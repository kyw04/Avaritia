using System.Collections;
using UnityEngine;

public class EntityAnimator : AnimatorBase,
    IObserver<EntityIdleEvent>,
    IObserver<EntityMovedEvent>,
    IObserver<EntityJumpedEvent>,
    IObserver<EntityFallingEvent>,
    IObserver<EntityLandedEvent>,
    IObserver<EntityTurnEvent>,
    IObserver<EntityDashEvent>,
    IObserver<EntityAttackStartEvent>,
    IObserver<EntityAttackCanceledEvent>,
    IObserver<EntityAnimationEvent>
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
        EventBus.Subscribe<EntityAttackCanceledEvent>(this);
        EventBus.Subscribe<EntityAnimationEvent>(this);
    }

    // 공격 둔화 중에는 공격 애니메이션도 같은 배율로 느리게 재생한다.
    private void Update()
    {
        if (self != null)
            AnimatorComponent.speed = self.IsAttacking ? self.AttackSpeedMultiplier : 1f;
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
        
        float defaultSpeed = e.Source.GetDefaultStat<float>(StatType.MoveSpeed);
        AnimatorComponent.SetFloat(Speed, e.Speed / defaultSpeed);
        PlayAnimation("Move");
    }
    public void OnNotify(EntityJumpedEvent e) { if (e.Source == self) PlayAnimation(e.Data.jump.animClip.name); }
    public void OnNotify(EntityFallingEvent e) { if (e.Source == self) PlayAnimation("fall_loop"); }
    public void OnNotify(EntityLandedEvent e) { if (e.Source == self) PlayAnimation("land"); }
    public void OnNotify(EntityTurnEvent e) { if (e.Source == self) PlayAnimation("turn"); }
    public void OnNotify(EntityDashEvent e) { if (e.Source == self) PlayAnimation("dash"); }
    public void OnNotify(EntityAnimationEvent e) { if (e.Source == self) PlayAnimation(e.Clip.name); }

    public void OnNotify(EntityAttackStartEvent e)
    {
        if (e.Source != self)
            return;

        var attackClip = e.Data.attack.animClip;
        var readyClip = e.Data.ready.animClip;
        if (readyClip != null)
        {
            if (animCoroutine != null)
                StopCoroutine(animCoroutine);

            PlayAnimation(readyClip.name);
            if (attackClip != null)
                animCoroutine = StartCoroutine(PlayAttackAfterReady(attackClip.name, readyClip.length));
        }
        else if (attackClip != null)
        {
            PlayAnimation(attackClip.name);
        }
    }

    // 준비 동작은 Animator.speed로 느려지므로 공격 애니메이션 전환도 같은 배율로 늦춘다.
    private IEnumerator PlayAttackAfterReady(string animName, float length)
    {
        for (float t = 0f; t < length; t += Time.deltaTime * self.AttackSpeedMultiplier)
            yield return null;
        PlayAnimation(animName);
    }

    // 준비 동작 중 캔슬되면 이어질 공격 애니메이션을 재생하지 않는다.
    public void OnNotify(EntityAttackCanceledEvent e)
    {
        if (e.Source != self || animCoroutine == null)
            return;

        StopCoroutine(animCoroutine);
        animCoroutine = null;
    }
}
