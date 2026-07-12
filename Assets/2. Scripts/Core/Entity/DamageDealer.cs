using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    public float damage;
    public LayerMask targetLayer;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if ((targetLayer & (1 << other.gameObject.layer)) == 0) return;
        if (other.TryGetComponent<IDamageable>(out var d))
            d.TakeDamage(damage);
        Destroy(gameObject);
    }
}
