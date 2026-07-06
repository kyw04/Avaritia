using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    public float duration;
    public AnimationClip readyAnimClip;
    public AnimationClip attackAnimClip;
    public float damageMultiplier = 1f;
    public ContactFilter2D filter;
    
    [SerializeReference, SubclassSelector] public IAttackStrategy attackStrategy;

    public void Attack(IAttacker attacker, Transform target = null)
    {
        attacker.IsAttacking = true;
        attacker.Mono.StartCoroutine(PlayAttack(attacker, target));
    }
    
    private IEnumerator PlayAttack(IAttacker attacker, Transform target = null)
    {
        if (readyAnimClip)
            yield return new WaitForSeconds(readyAnimClip.length);
        
        attacker.Mono.StartCoroutine(Execute(attacker, target));
    }
    
    private IEnumerator Execute(IAttacker attacker, Transform target = null)
    {
        attackStrategy?.Offensive(attacker, damageMultiplier, filter, target);

        yield return new WaitForSeconds(attackAnimClip.length);
        yield return new WaitForSeconds(duration);
        attacker.IsAttacking = false;
    }
}
