using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    public float duration;
    public AnimationClip readyAnimClip;
    public AnimationClip attackAnimClip;
    public ContactFilter2D filter;
    public Vector2 hitboxPosition;
    public Vector2 hitboxSize;
    public float damageMultiplier = 1f;

    public void Attack(IAttacker attacker, Transform target = null)
    {
        attacker.IsAttacking = true;
        attacker.Mono.StartCoroutine(PlayAttack(attacker, target));
    }
    
    protected virtual IEnumerator PlayAttack(IAttacker attacker, Transform target = null)
    {
        if (readyAnimClip)
            yield return new WaitForSeconds(readyAnimClip.length);
        
        attacker.Mono.StartCoroutine(Execute(attacker, target));
    }
    
    protected virtual IEnumerator Execute(IAttacker attacker, Transform target = null)
    {
        float dmg = attacker.Damage * damageMultiplier;
        var hits = new List<Collider2D>();
        var pos = hitboxPosition;
        if (attacker.Mono.transform.localScale.x < 0)
            pos.x = -pos.x;
        pos += (Vector2)attacker.Mono.transform.position;
        
        Physics2D.OverlapBox(pos, hitboxSize, 0f, filter, hits);
        DebugExtension.DrawBox(pos, hitboxSize / 2f, Quaternion.identity, Color.green, 1.0f);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var d))
            {
                Debug.Log($"[Take Damage] {hit.name}");
                d.TakeDamage(dmg);
            }

        }        

        yield return new WaitForSeconds(attackAnimClip.length);
        yield return new WaitForSeconds(duration);
        attacker.IsAttacking = false;
    }
}
