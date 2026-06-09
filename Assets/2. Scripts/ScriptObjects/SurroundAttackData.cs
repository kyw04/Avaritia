using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/SurroundAttackData")]
public class SurroundAttackData : AttackData
{
    public float radius;
    private Collider2D[] hits = new Collider2D[10];

    public override IEnumerator Execute(IAttacker attacker, Transform target = null)
    {
        float dmg = attacker.Damage * damageMultiplier;
        Physics2D.OverlapCircle(attacker.Mono.transform.position, radius, filter, hits);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(dmg);
        
        yield return new WaitForSeconds(duration);
    }
}
