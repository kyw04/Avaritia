using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Press: 한 번 누르면 끝까지 진행. Hold: 발동시킨 키를 떼면 중단.
public enum ChannelInputMode { Press, Hold }

// duration 동안 작업(채널링)한 뒤 onComplete를 적용한다. 피격(cancelOnDamaged), keepWhile 조건 실패,
// 바인딩 해제(State.IsChanneling=false) 시 중단되며 중단되면 onComplete는 적용되지 않는다.
// cooldownOnComplete면 쿨다운도 완료 시에만 시작한다.
[System.Serializable]
public class ChannelEffect : IAbilityEffect
{
    public float duration = 2f;
    public ChannelInputMode inputMode = ChannelInputMode.Press;
    // 채널링 동안 막을 행동. 기본은 제자리 고정(이동/공격/스킬/점프/대시 불가).
    public ActionBlock blockedActions = ActionBlock.Move | ActionBlock.Attack | ActionBlock.Skill | ActionBlock.Jump | ActionBlock.Dash;
    public bool cancelOnDamaged = true;
    // 시작/완료/중단 시 재생할 애니메이션·파티클·소리. 완료/중단 애니메이션이 있으면 그 길이만큼 행동 제한을 유지한 뒤 풀린다.
    public ActionFeedback startFeedback;
    public ActionFeedback completeFeedback;
    public ActionFeedback cancelFeedback;
    [SerializeReference, SubclassSelector] public List<IAbilityCondition> keepWhile = new();
    [SerializeReference, SubclassSelector] public List<IAbilityEffect> onStart = new();
    [SerializeReference, SubclassSelector] public List<IAbilityEffect> onComplete = new();
    public bool cooldownOnComplete = true;

    public void Apply(AbilityContext context)
    {
        if (context.Caster?.Mono is not Entity caster) return;

        foreach (var effect in onStart)
            effect?.Apply(context);
        if (cooldownOnComplete)
            context.State.DeferCooldown = true;

        context.State.IsChanneling = true;
        context.State.IsHoldChannel = inputMode == ChannelInputMode.Hold;
        context.State.ChannelProgress = 0f;
        caster.Restrictions.Set(context.State, blockedActions);
        PlayFeedback(caster, startFeedback);
        caster.StartCoroutine(Run(caster, context));
    }

    // ChannelEffect는 공유 에셋 데이터라 진행 상태를 필드에 두지 않고 코루틴 지역 변수로만 관리한다.
    // 피격은 이벤트 구독 대신 LastDamagedTime 폴링으로 감지한다(채널링 중 풀 반납되면 구독 해제 기회가 없음).
    private IEnumerator Run(Entity caster, AbilityContext context)
    {
        float startTime = Time.time;
        float endTime = startTime + duration;
        bool completed = true;
        while (Time.time < endTime)
        {
            yield return null;
            // 시작 프레임에 이미 입은 데미지(발동 직전 피격)는 중단 사유로 치지 않는다.
            bool damaged = cancelOnDamaged && caster.LastDamagedTime > startTime;
            // 입력 없이 발동된 경우(IsInputHeld == null)는 누르고 있는 것으로 본다.
            bool released = inputMode == ChannelInputMode.Hold && context.IsInputHeld != null && !context.IsInputHeld();
            if (damaged || released || !context.State.IsChanneling || keepWhile.Exists(c => !c.IsMet(context)))
            {
                completed = false;
                break;
            }
            context.State.ChannelProgress = duration > 0f ? Mathf.Clamp01((Time.time - startTime) / duration) : 1f;
        }

        context.State.IsChanneling = false;
        var endFeedback = completed ? completeFeedback : cancelFeedback;
        PlayFeedback(caster, endFeedback);
        if (completed)
        {
            foreach (var effect in onComplete)
                effect?.Apply(context);
            if (cooldownOnComplete)
                context.State.StartCooldown?.Invoke();
        }

        if (endFeedback.animClip != null)
            yield return new WaitForSeconds(endFeedback.animClip.length);

        caster.Restrictions.Remove(context.State);
        if (blockedActions != ActionBlock.None || startFeedback.animClip != null || endFeedback.animClip != null)
            RestoreLocomotionAnimation(caster);
    }

    private static void PlayFeedback(Entity caster, ActionFeedback feedback)
    {
        feedback.Play(caster.transform.position);
        if (feedback.animClip != null)
            EventBus.Publish(new EntityAnimationEvent(caster, feedback.animClip));
    }

    // 채널링 애니메이션이 남지 않도록 현재 이동 상태의 애니메이션으로 되돌린다.
    // 움직이는 중이면 이동 상태가 매 프레임 애니메이션을 덮어쓰므로 건드리지 않는다.
    private static void RestoreLocomotionAnimation(Entity caster)
    {
        if (!caster.IsGrounded)
            EventBus.Publish(new EntityFallingEvent(caster));
        else if (Mathf.Abs(caster.Rb.linearVelocityX) < 0.01f)
            EventBus.Publish(new EntityIdleEvent(caster));
    }
}
