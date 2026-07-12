using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MeleeAttackStrategy : IAttackStrategy
{
    public Vector2 hitboxPosition;
    public Vector2 hitboxSize;
    
    public void Offensive(IAttacker attacker, float damageMultiplier, ContactFilter2D filter, Transform target = null)
    {
        float dmg = attacker.Damage * damageMultiplier;
        var hits = new List<Collider2D>();
        var pos = hitboxPosition;
        if (attacker.LookDirection < 0)
            pos.x = -pos.x;
        pos += (Vector2)attacker.Mono.transform.position;
        
        Physics2D.OverlapBox(pos, hitboxSize, 0f, filter, hits);
        DebugExtension.DrawBox(pos, hitboxSize / 2f, Quaternion.identity, Color.green, 1.0f);
        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(attacker.Mono.transform)) continue;
            if (hit.TryGetComponent<IDamageable>(out var d))
            {
                Debug.Log($"[Take Damage] {hit.name}");
                d.TakeDamage(dmg);
            }
        }
    }
}
