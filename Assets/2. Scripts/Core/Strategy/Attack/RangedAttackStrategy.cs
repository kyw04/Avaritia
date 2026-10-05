using UnityEngine;

[System.Serializable]
public class RangedAttackStrategy : IAttackStrategy
{
    public BulletMover bulletPrefab;
    public float spreadAngle;
    public float lifeTime;
    
    [Header("Dynamic Stat")]
    [Tooltip("If the attacker has custom stats, they will be applied instead.")]
    public int bulletCount = 1;
    public float bulletDelay = 0;
    public float bulletMaxDelay = 0;
    public float bulletSpeed = 15f;

    public void Offensive(IAttacker attacker, float damageMultiplier, ContactFilter2D filter, Transform target = null)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletAttackData: bulletPrefab not assigned");
            return;
        }
        var dealer = bulletPrefab.GetComponent<DamageDealer>();
        if (dealer == null)
        {
            Debug.LogError("BulletAttackData: bulletPrefab missing DamageDealer component");
            return;
        }
        
        if (attacker is IStatReadable r)
        {
            if (r.TryGetStat(StatType.BulletCount, out int count)) bulletCount = count;
            if (r.TryGetStat(StatType.BulletDelay, out float delay)) bulletDelay = delay;
            if (r.TryGetStat(StatType.BulletMaxDelay, out float maxDelay)) bulletMaxDelay = maxDelay;
            if (r.TryGetStat(StatType.BulletSpeed, out float speed)) bulletSpeed = speed;
        }
        bool useDelay = 1 < bulletCount;
        
        for (int i = 0; i < bulletCount; i++)
        {
            float angle = bulletCount > 1
                ? Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)i / (bulletCount - 1))
                : 0f;
            BulletMover mover = ObjectPoolManager.Instance.Spawn(bulletPrefab, attacker.Mono.transform.position, Quaternion.identity);
            
            float dmg = attacker.Damage * damageMultiplier;
            float delay = 0;
            if (attacker is IStatReadable readable)
            {
                if (useDelay) delay = Random.Range(bulletDelay, bulletMaxDelay);
                readable.TryGetStat<float>(StatType.CritRate, out var critRate);
                if (Random.Range(0f, 100f) < critRate)
                    dmg *= CritConfig.Multiplier;
            }
            dealer.damage = dmg;
            
            var dir = Vector3.right * attacker.LookDirection;
            if (target != null)
                dir = (target.position - mover.transform.position).normalized;
            var setting = new BulletMoveSettings(dir, bulletSpeed);
            
            mover.Launch(setting, lifeTime, delay);
        }
    }
}
