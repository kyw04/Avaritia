using UnityEngine;

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
        foreach (var hit in Physics2D.OverlapCircleAll(caster.position, radius, targetLayer))
        {
            if (hit.transform.IsChildOf(caster) || !hit.TryGetComponent<Rigidbody2D>(out var rb)) continue;
            var dir = ((Vector2)(hit.transform.position - caster.position)).normalized;
            if (dir == Vector2.zero) dir = Vector2.up;
            rb.linearVelocity = dir * force;
        }
    }
}
