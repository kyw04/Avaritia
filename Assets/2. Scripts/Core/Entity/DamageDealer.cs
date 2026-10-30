using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [HideInInspector] public float damage;
    [HideInInspector] public Entity attacker;
    [HideInInspector] public bool isCritical;
    public LayerMask targetLayer;
    public ActionFeedback hit;
    
    protected bool IsTarget(Transform target) => (targetLayer & (1 << target.gameObject.layer)) != 0;

    protected virtual void OnHit(Transform target)
    {
        if (!IsTarget(target))
            return;
        
        hit.Play((transform.position + target.position) / 2f);
        
        Attack(target);

        ObjectPoolManager.Instance.Despawn(gameObject);
    }

    protected virtual void Attack(Transform target)
    {
        if (target.TryGetComponent<IDamageable>(out var d))
            d.TakeDamage(damage, attacker, transform.position, isCritical);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        OnHit(other.transform);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        OnHit(other.transform);
    }
    
}
