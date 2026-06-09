using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    public float duration;
    public AnimationClip animClip;
    public ContactFilter2D filter;
    public Vector2 hitboxPosition;
    public Vector2 hitboxSize;
    public float damageMultiplier = 1f;

    public virtual IEnumerator Execute(IAttacker attacker, Transform target = null)
    {
        float dmg = attacker.Damage * damageMultiplier;
        var hits = new List<Collider2D>();
        var pos = (Vector2)attacker.Mono.transform.position + hitboxPosition;
        Physics2D.OverlapBox(pos, hitboxSize, 0f, filter, hits);
        foreach (var hit in hits)
            if (hit.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(dmg);
        yield return new WaitForSeconds(duration);
    }
}
