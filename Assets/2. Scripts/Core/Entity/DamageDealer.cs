using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    public float damage;
    public Entity attacker;
    public bool isCritical;
    public LayerMask targetLayer;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((targetLayer & (1 << other.gameObject.layer)) == 0) return;
        if (other.TryGetComponent<IDamageable>(out var d))
            d.TakeDamage(damage, attacker, transform.position, isCritical);
        ObjectPoolManager.Instance.Despawn(gameObject);
    }
}
