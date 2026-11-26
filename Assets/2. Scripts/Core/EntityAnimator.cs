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
    private static readonly int Idle = Animator.StringToHash("idle");
    private static readonly int Move = Animator.StringToHash("move");
    private static readonly int FallLoop = Animator.StringToHash("fall_loop");
    private static readonly int Land = Animator.StringToHash("land");
    private static readonly int Turn = Animator.StringToHash("turn");
    private static readonly int Dash = Animator.StringToHash("dash");
    private Entity self;
    private bool? hasSpeedParameter;

    protected override void Awake()
    {
        base.Awake();
        self = GetComponentInParent<Entity>();
    }

    private bool HasSpeedParameter
    {
        get
        {
            if (hasSpeedParameter == null)
            {
                hasSpeedParameter = false;
                foreach (var parameter in AnimatorComponent.parameters)
                    if (parameter.nameHash == Speed && parameter.type == AnimatorControllerParameterType.Float)
                        hasSpeedParameter = true;
            }
            return hasSpeedParameter.Value;
        }
    }

    // 자기 엔티티 이벤트만 쓰므로 전역 버스가 아니라 엔티티의 로컬 허브를 구독한다.
    private void OnEnable()
    {
        if (self == null) return;
        var hub = self.Events;
        hub.Subscribe<EntityIdleEvent>(this);
        hub.Subscribe<EntityMovedEvent>(this);
        hub.Subscribe<EntityJumpedEvent>(this);
        hub.Subscribe<EntityFallingEvent>(this);
        hub.Subscribe<EntityLandedEvent>(this);
        hub.Subscribe<EntityTurnEvent>(this);
        hub.Subscribe<EntityDashEvent>(this);
        hub.Subscribe<EntityAttackStartEvent>(this);
        hub.Subscribe<EntityAttackCanceledEvent>(this);
        hub.Subscribe<EntityAnimationEvent>(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (self == null) return;
        var hub = self.Events;
        hub.Unsubscribe<EntityIdleEvent>(this);
        hub.Unsubscribe<EntityMovedEvent>(this);
        hub.Unsubscribe<EntityJumpedEvent>(this);
        hub.Unsubscribe<EntityFallingEvent>(this);
        hub.Unsubscribe<EntityLandedEvent>(this);
        hub.Unsubscribe<EntityTurnEvent>(this);
        hub.Unsubscribe<EntityDashEvent>(this);
        hub.Unsubscribe<EntityAttackStartEvent>(this);
        hub.Unsubscribe<EntityAttackCanceledEvent>(this);
        hub.Unsubscribe<EntityAnimationEvent>(this);
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

    public void OnNotify(EntityIdleEvent e) { if (e.Source == self && !LocomotionLocked) PlayAnimation(Idle); }
    public void OnNotify(EntityMovedEvent e)
    {
        if (e.Source != self || LocomotionLocked)
            return;
        
        // 이동 속도 파라미터가 없는 애니메이터(보스 등)는 move 상태만 튼다.
        if (HasSpeedParameter)
        {
            float defaultSpeed = e.Source.GetDefaultStat<float>(StatType.MoveSpeed);
            AnimatorComponent.SetFloat(Speed, e.Speed / defaultSpeed);
        }
        PlayAnimation(Move);
    }
    public void OnNotify(EntityJumpedEvent e) { if (e.Source == self) PlayAnimation(ClipHash(e.Data.jump.animClip)); }
    // 사망 중에는 행동 취소(RestoreAnimation 등)가 보내는 낙하 애니메이션이 사망 모션을 덮지 않게 한다.
    public void OnNotify(EntityFallingEvent e) { if (e.Source == self && !self.IsDead) PlayAnimation(FallLoop); }
    public void OnNotify(EntityLandedEvent e) { if (e.Source == self) PlayAnimation(Land); }
    public void OnNotify(EntityTurnEvent e) { if (e.Source == self && !LocomotionLocked) PlayAnimation(Turn); }
    public void OnNotify(EntityDashEvent e) { if (e.Source == self) PlayAnimation(Dash); }
    public void OnNotify(EntityAnimationEvent e) { if (e.Source == self) PlayAnimation(ClipHash(e.Clip)); }

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

            PlayAnimation(ClipHash(readyClip));
            if (attackClip != null)
                animCoroutine = StartCoroutine(PlayAttackAfterReady(ClipHash(attackClip), readyClip.length));
        }
        else if (attackClip != null)
        {
            PlayAttackClip(ClipHash(attackClip));
        }
    }

    // 같은 공격을 연달아 할 때: 반복 클립(누르고 있는 연속 공격)은 끊지 않고 이어서 재생하고,
    // 한 번만 재생하는 클립(총 쏘기 등)은 매번 처음부터 다시 튼다. Animator.Play는 같은 상태면 처음으로 돌리지 않는다.
    private void PlayAttackClip(int stateHash)
    {
        var info = AnimatorComponent.GetCurrentAnimatorStateInfo(0);
        PlayAnimation(stateHash);
        if (info.shortNameHash == stateHash && !info.loop)
            AnimatorComponent.Play(stateHash, 0, 0f);
    }

    // 준비 동작은 Animator.speed로 느려지므로 공격 애니메이션 전환도 같은 배율로 늦춘다.
    private IEnumerator PlayAttackAfterReady(int stateHash, float length)
    {
        for (float t = 0f; t < length; t += Time.deltaTime * self.AttackSpeedMultiplier)
            yield return null;
        PlayAnimation(stateHash);
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
