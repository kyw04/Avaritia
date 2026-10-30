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
        attacker.Mono.StartCoroutine(PlayAttack(attacker, target, enhancement));
    }

    private IEnumerator PlayAttack(IAttacker attacker, Transform target = null, BulletEnhancement enhancement = null)
    {
        ready.Play(attacker.Mono.transform.position);
        if (ready.animClip)
            yield return new WaitForSeconds(ready.animClip.length);

        attacker.Mono.StartCoroutine(Execute(attacker, target, enhancement));
    }

    private IEnumerator Execute(IAttacker attacker, Transform target = null, BulletEnhancement enhancement = null)
    {
        var resolvedFilter = filter;
        if (filter.useLayerMask && attacker is Entity entity)
            resolvedFilter.SetLayerMask(entity.ResolveTargetLayer(filter.layerMask));
        attack.Play(attacker.Mono.transform.position);
        attackStrategy?.Offensive(attacker, damageMultiplier, resolvedFilter, target, enhancement);

        if (attack.animClip)
            yield return new WaitForSeconds(attack.animClip.length);
        yield return new WaitForSeconds(duration);
        attacker.IsAttacking = false;
    }
}
