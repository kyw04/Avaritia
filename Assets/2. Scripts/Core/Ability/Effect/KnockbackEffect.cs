using UnityEngine;
using UnityEngine.Pool;

// 시전자 주변 radius 안의 대상(targetLayer)을 바깥 방향으로 밀어낸다.
[System.Serializable]
public class KnockbackEffect : IAbilityEffect
{
    public float radius = 3f;
    public float force = 15f;
    public LayerMask targetLayer;

    public void Apply(AbilityContext context)
    {
        var caster = context.Caster.Mono.transform;
        using (ListPool<Collider2D>.Get(out var hits))
        {
            Physics2D.OverlapCircle(caster.position, radius, ContactFilters.ForLayer(targetLayer), hits);
            foreach (var hit in hits)
            {
                if (hit.transform.IsChildOf(caster) || !hit.TryGetComponent<Rigidbody2D>(out var rb)) continue;
                var dir = ((Vector2)(hit.transform.position - caster.position)).normalized;
                if (dir == Vector2.zero) dir = Vector2.up;
                rb.linearVelocity = dir * force;
            }
        }
    }
}
