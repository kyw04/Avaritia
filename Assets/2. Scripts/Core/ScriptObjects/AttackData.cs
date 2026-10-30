using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    public float duration;
    public ActionFeedback ready;
    public ActionFeedback attack;
    public float damageMultiplier = 1f;
    public ContactFilter2D filter;
    public bool canTurn;
    public bool canMove;
    public float moveDuration;

    [SerializeReference, SubclassSelector] public IAttackStrategy attackStrategy;

    public void Attack(IAttacker attacker, Transform target = null, BulletEnhancement enhancement = null)
    {
        attacker.IsAttacking = true;
        int cancelVersion = attacker is Entity entity ? entity.AttackCancelVersion : 0;
        attacker.Mono.StartCoroutine(PlayAttack(attacker, cancelVersion, target, enhancement));
    }

    private IEnumerator PlayAttack(IAttacker attacker, int cancelVersion, Transform target = null, BulletEnhancement enhancement = null)
    {
        ready.Play(attacker.Mono.transform.position);
        if (ready.animClip)
            yield return WaitAttackTime(attacker, cancelVersion, ready.animClip.length);
        if (IsCanceled(attacker, cancelVersion)) yield break;

        attacker.Mono.StartCoroutine(Execute(attacker, cancelVersion, target, enhancement));
    }

    private IEnumerator Execute(IAttacker attacker, int cancelVersion, Transform target = null, BulletEnhancement enhancement = null)
    {
        var resolvedFilter = filter;
        if (filter.useLayerMask && attacker is Entity entity)
            resolvedFilter.SetLayerMask(entity.ResolveTargetLayer(filter.layerMask));
        attack.Play(attacker.Mono.transform.position);
        attackStrategy?.Offensive(attacker, damageMultiplier, resolvedFilter, target, enhancement);

        if (attack.animClip)
            yield return WaitAttackTime(attacker, cancelVersion, attack.animClip.length);
        yield return WaitAttackTime(attacker, cancelVersion, duration);
        // 캔슬된 뒤 새 공격이 시작됐을 수 있으므로 그 공격의 IsAttacking을 건드리지 않는다.
        if (IsCanceled(attacker, cancelVersion)) yield break;
        attacker.IsAttacking = false;
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
