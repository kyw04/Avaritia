using UnityEngine;

public class DamageDealer : MonoBehaviour
{
    [HideInInspector] public float damage;
    [HideInInspector] public Entity attacker;
    [HideInInspector] public bool isCritical;
    public LayerMask targetLayer;
    public PooledParticle hitParticle;
    public AudioClip hitSound;
    
    private void OnHit(Transform target)
    {
        if ((targetLayer & (1 << target.gameObject.layer)) == 0)
            return;
        
        if (hitParticle != null)
        {
            var pos = (transform.position  + target.position) / 2f;
            ObjectPoolManager.Instance.Spawn(hitParticle, pos, Quaternion.identity);
        }

        if (hitSound != null)
            SoundManager.Instance.PlayOneShotClip(hitSound, AudioType.Effect);
        
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
