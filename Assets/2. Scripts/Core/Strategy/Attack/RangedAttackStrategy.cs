using UnityEngine;

[System.Serializable]
public class RangedAttackStrategy : IAttackStrategy
{
    public GameObject bulletPrefab;
    public int bulletCount;
    public float spreadAngle;

    public float upSpeed;
    public float upDuration;
    public float redirectSpeed;
    public float redirectDelay;

    public void Offensive(IAttacker attacker, float damageMultiplier, ContactFilter2D filter, Transform target = null)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletAttackData: bulletPrefab not assigned");
            return;
        }

        if (target == null)
        {
            Debug.LogError("BulletAttackData: target not assigned");
            return;
        }

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = bulletCount > 1
                ? Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)i / (bulletCount - 1))
                : 0f;
            var go = ObjectPoolManager.Instance.Spawn(bulletPrefab, attacker.Mono.transform.position, Quaternion.identity);
            var dealer = go.GetComponent<DamageDealer>();
            var mover = go.GetComponent<BulletMover>();
            if (dealer == null || mover == null)
            {
                Debug.LogError("BulletAttackData: bulletPrefab missing DamageDealer or BulletMover component");
                ObjectPoolManager.Instance.Despawn(go);
                continue;
            }
            dealer.damage = attacker.Damage * damageMultiplier;
            var dir = (target.position - go.transform.position).normalized;
            mover.Launch(dir, angle, upSpeed, upDuration, redirectSpeed, redirectDelay);
        }
    }
}
