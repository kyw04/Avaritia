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
        
        int count = bulletCount;
        float minDelay = bulletDelay;
        float maxDelay = bulletMaxDelay;
        float speed = bulletSpeed;
        if (attacker is IStatReadable r)
        {
            if (r.TryGetStat(StatType.BulletCount, out int statCount)) count = statCount;
            if (r.TryGetStat(StatType.BulletDelay, out float statDelay)) minDelay = statDelay;
            if (r.TryGetStat(StatType.BulletMaxDelay, out float statMaxDelay)) maxDelay = statMaxDelay;
            if (r.TryGetStat(StatType.BulletSpeed, out float statSpeed)) speed = statSpeed;
        }
        bool useDelay = 1 < count;
        
        for (int i = 0; i < count; i++)
        {
            float angle = count > 1
                ? Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)i / (count - 1))
                : 0f;
            BulletMover mover = ObjectPoolManager.Instance.Spawn(bulletPrefab, attacker.Mono.transform.position, Quaternion.identity);
            
            float dmg = attacker.Damage * damageMultiplier;
            float delay = 0;
            if (attacker is IStatReadable readable)
            {
                if (useDelay) delay = Random.Range(minDelay, maxDelay);
                readable.TryGetStat<float>(StatType.CritRate, out var critRate);
                if (Random.Range(0f, 100f) < critRate)
                    dmg *= CritConfig.Multiplier;
            }
            mover.GetComponent<DamageDealer>().damage = dmg;
            
            var dir = Vector3.right * attacker.LookDirection;
            if (target != null)
                dir = (target.position - mover.transform.position).normalized;
            dir = Quaternion.Euler(0f, 0f, angle) * dir;
            var setting = new BulletMoveSettings(dir, speed);
            
            mover.Launch(setting, lifeTime, delay);
        }
    }
}
