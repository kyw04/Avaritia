using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/SurroundAttackData")]
public class SurroundAttackData : AttackData
{
    public float radius;

    public override IEnumerator Execute(Boss boss)
    {
        float dmg = boss.Damage * damageMultiplier;
        var hits = Physics2D.OverlapCircleAll(boss.transform.position, radius, filter.layerMask);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(dmg);
        yield return new WaitForSeconds(duration);
    }
}
