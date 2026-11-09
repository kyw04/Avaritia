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

    // 행동 제한(채널링 등) 중 이동 입력이 없으면, 감속/정지로 생기는 이동 애니메이션이 작업 애니메이션을 덮어쓰지 않게 한다.
    private bool LocomotionLocked =>
        self != null && self.Restrictions.Blocked != ActionBlock.None && Mathf.Abs(self.MoveDirection.x) <= 0.2f;

    // 이동 애니메이션의 발 닿는 프레임에서 호출된다(애니메이션 이벤트).
    // 블렌드 트리에서 섞이는 클립들의 이벤트가 모두 오므로 우세한 클립의 이벤트만 재생한다.
    // 이벤트 문자열 파라미터가 "walk"면 걷기 피드백, 아니면 달리기 피드백을 쓴다.
    public void OnFootstep(AnimationEvent evt)
    {
        if (self == null || evt.animatorClipInfo.weight < 0.5f)
            return;

        var feedback = evt.stringParameter == "walk" ? self.Feedbacks.Walk : self.Feedbacks.Run;
        feedback.Play(self.FootPosition);
    }

    public void OnNotify(EntityIdleEvent e) { if (e.Source == self && !LocomotionLocked) PlayAnimation("idle"); }
    public void OnNotify(EntityMovedEvent e)
    {
        if (e.Source != self || LocomotionLocked)
            return;
        
        float defaultSpeed = e.Source.GetDefaultStat<float>(StatType.MoveSpeed);
        AnimatorComponent.SetFloat(Speed, e.Speed / defaultSpeed);
        PlayAnimation("Move");
    }
    public void OnNotify(EntityJumpedEvent e) { if (e.Source == self) PlayAnimation(e.Data.jump.animClip.name); }
    public void OnNotify(EntityFallingEvent e) { if (e.Source == self) PlayAnimation("fall_loop"); }
    public void OnNotify(EntityLandedEvent e) {
        if (e.Source == self)
        {
            Debug.Log("Land Start");
            PlayAnimation("land");
        }}
    public void OnNotify(EntityTurnEvent e) { if (e.Source == self && !LocomotionLocked) PlayAnimation("turn"); }
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
