using UnityEngine;

public class AreaDamageDealer : DamageDealer
{
    public float radius = 1.5f;

    protected override void Attack(Transform target)
    {
        // 탄환 크기 강화가 폭발 범위에도 적용되도록 스케일을 곱한다.
        float r = radius * transform.lossyScale.x;
        foreach (var hit in Physics2D.OverlapCircleAll(transform.position, r, targetLayer))
        {
            if (hit.TryGetComponent<IDamageable>(out var d))
                d.TakeDamage(damage, attacker, transform.position, isCritical);
        }
    }
}
