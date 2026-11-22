using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public struct KnockbackSettings
{
    public float knockbackForce;
    public float knockbackUpForce;
    [Min(0)] public float knockbackDuration;

    public static KnockbackSettings Default => new KnockbackSettings
    {
        knockbackForce = 2f,
        knockbackUpForce = 0f,
        knockbackDuration = 0.5f
    };
}

[CreateAssetMenu(menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    public float duration;
    public ActionFeedback ready;
    // 준비 단계의 최소 시간. 준비 시간은 이 값과 ready 애니메이션 길이 중 긴 쪽이다(애니메이션이 없을 때 쓴다).
    [Min(0)] public float readyDuration;
    public ActionFeedback attack;
    [FormerlySerializedAs("knockbackData")] public KnockbackSettings knockback = KnockbackSettings.Default;
    [FormerlySerializedAs("cameraData")] public HitCameraSettings hitCamera = HitCameraSettings.Default;
    public float damageMultiplier = 1f;
    public ContactFilter2D filter;
    public bool canTurn;
    public bool canMove;
    public float moveDuration;
    // canMove면 공격 준비가 끝날 때 이 속도로 앞으로 밀려나 moveDuration 동안 0까지 줄어든다.
    // 플레이어는 0이면 대신 moveDuration 동안 방향키로 움직인다.
    public float moveForce;

    [SerializeReference, SubclassSelector] public IAttackStrategy attackStrategy;

    [Header("Editor Preview")]
    [Tooltip("씬 뷰 미리보기에서 공격 스프라이트를 그릴 위치(엔티티 루트 기준). 프리팹의 스프라이트 자식 위치와 맞추면 총구·판정 위치가 게임과 일치한다. 게임 동작에는 쓰이지 않는다.")]
    public Vector2 previewSpriteOffset;

    // 준비 단계 길이(공격 속도 배율 적용 전).
    public float ReadyTime => Mathf.Max(readyDuration, ready.animClip ? ready.animClip.length : 0f);

    // 지금 위치에서 대상에게 공격이 닿을 수 있는지(좌우로만 쏘는 원거리 공격의 높이 판정).
    public bool CanReach(IAttacker attacker, Transform target) =>
        attackStrategy is not RangedAttackStrategy ranged || ranged.IsTargetInLine(attacker, target);

    public void Attack(IAttacker attacker, Transform target = null, BulletEnhancement enhancement = null)
    {
        attacker.IsAttacking = true;
        int cancelVersion = attacker is Entity entity ? entity.AttackCancelVersion : 0;
        attacker.Mono.StartCoroutine(PlayAttack(attacker, cancelVersion, target, enhancement));
    }

    private IEnumerator PlayAttack(IAttacker attacker, int cancelVersion, Transform target = null, BulletEnhancement enhancement = null)
    {
        ready.Play(attacker.Mono.transform.position);

        float readyTime = ReadyTime;

        // 원거리 조준: 궤도 프리팹이 있으면 준비 동안 방향을 고정하고 궤도를 보여 준 뒤 그 방향으로 쏜다.
        Vector2? aim = null;
        List<TrajectoryTelegraph> telegraphs = null;
        if (readyTime > 0f && attackStrategy is RangedAttackStrategy ranged && ranged.aimTelegraphPrefab != null)
        {
            aim = ranged.GetAimDirection(attacker, target);
            telegraphs = ranged.ShowAim(attacker, aim.Value, readyTime);
        }

        if (readyTime > 0f)
            yield return WaitAttackTime(attacker, cancelVersion, readyTime);

        if (telegraphs != null)
            foreach (var telegraph in telegraphs)
                telegraph.Hide();
        if (IsCanceled(attacker, cancelVersion)) yield break;

        attacker.Mono.StartCoroutine(Execute(attacker, cancelVersion, target, enhancement, aim));
    }

    private IEnumerator Execute(IAttacker attacker, int cancelVersion, Transform target = null, BulletEnhancement enhancement = null, Vector2? aim = null)
    {
        var resolvedFilter = filter;
        if (filter.useLayerMask && attacker is Entity entity)
            resolvedFilter.SetLayerMask(entity.ResolveTargetLayer(filter.layerMask));
        attack.Play(attacker.Mono.transform.position);
        // moveForce로 밀려나는 근접 공격은 이동하는 동안(moveDuration) 판정을 계속 검사한다. 대상마다 한 번만 맞는다.
        if (canMove && moveForce > 0f && moveDuration > 0f && attackStrategy is MeleeAttackStrategy melee)
            attacker.Mono.StartCoroutine(SweepMelee(melee, attacker, cancelVersion, resolvedFilter));
        else
            attackStrategy?.Offensive(attacker, knockback, hitCamera, damageMultiplier, resolvedFilter, target, enhancement, aim);

        if (attack.animClip)
            yield return WaitAttackTime(attacker, cancelVersion, attack.animClip.length);
        yield return WaitAttackTime(attacker, cancelVersion, duration);
        // 캔슬된 뒤 새 공격이 시작됐을 수 있으므로 그 공격의 IsAttacking을 건드리지 않는다.
        if (IsCanceled(attacker, cancelVersion)) yield break;
        attacker.IsAttacking = false;
    }

    // 이동 시간(공격 속도 배율 반영) 동안 매 프레임 근접 판정을 검사한다. 공격이 캔슬되면 멈춘다.
    private IEnumerator SweepMelee(MeleeAttackStrategy melee, IAttacker attacker, int cancelVersion, ContactFilter2D resolvedFilter)
    {
        var entity = attacker as Entity;
        var alreadyHit = new HashSet<IDamageable>();
        for (float t = 0f; t < moveDuration; t += Time.deltaTime * (entity != null ? entity.AttackSpeedMultiplier : 1f))
        {
            if (IsCanceled(attacker, cancelVersion)) yield break;
            melee.Offensive(attacker, knockback, hitCamera, damageMultiplier, resolvedFilter, alreadyHit);
            yield return null;
        }
    }

    // 대기 중에 공격 둔화가 걸려도 남은 시간이 늘어나도록 매 프레임 배율을 반영한다.
    private static IEnumerator WaitAttackTime(IAttacker attacker, int cancelVersion, float seconds)
    {
        var entity = attacker as Entity;
        for (float t = 0f; t < seconds; t += Time.deltaTime * (entity != null ? entity.AttackSpeedMultiplier : 1f))
        {
            if (IsCanceled(attacker, cancelVersion)) yield break;
            yield return null;
        }
    }

    private static bool IsCanceled(IAttacker attacker, int cancelVersion) =>
        attacker is Entity entity && entity.AttackCancelVersion != cancelVersion;
}
