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

    public void Offensive(IAttacker attacker, float damageMultiplier, ContactFilter2D filter, Transform target = null, BulletEnhancement enhancement = null)
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
        if (enhancement != null)
        {
            damageMultiplier *= enhancement.damageMultiplier;
            speed *= enhancement.speedMultiplier;
        }
        bool useDelay = 1 < count;
        
        for (int i = 0; i < count; i++)
        {
            float angle = count > 1
                ? Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)i / (count - 1))
                : 0f;
            var prefab = enhancement != null && enhancement.bulletPrefab != null ? enhancement.bulletPrefab : bulletPrefab;
            BulletMover mover = ObjectPoolManager.Instance.Spawn(prefab, attacker.Mono.transform.position, Quaternion.identity);
            mover.SetScale(enhancement?.scaleMultiplier ?? 1f);
            
            float dmg = attacker.Damage * damageMultiplier;
            float delay = 0;
            bool isCritical = false;
            if (attacker is IStatReadable readable)
            {
                if (useDelay) delay = Random.Range(minDelay, maxDelay);
                readable.TryGetStat<float>(StatType.CritRate, out var critRate);
                isCritical = Random.Range(0f, 100f) < critRate;
                if (isCritical)
                    dmg *= CritConfig.Multiplier;
            }
            var dealer = mover.GetComponent<DamageDealer>();
            dealer.damage = dmg;
            dealer.attacker = attacker as Entity;
            dealer.isCritical = isCritical;
            // 풀링된 탄환은 아군/적이 같이 쓰므로 매번 프리팹 값에서 다시 정한다.
            var prefabTargetLayer = prefab.GetComponent<DamageDealer>().targetLayer;
            dealer.targetLayer = dealer.attacker != null ? dealer.attacker.ResolveTargetLayer(prefabTargetLayer) : prefabTargetLayer;
            
            var dir = Vector3.right * attacker.LookDirection;
            if (target != null)
                dir = (target.position - mover.transform.position).normalized;
            dir = Quaternion.Euler(0f, 0f, angle) * dir;
            var setting = new BulletMoveSettings(dir, speed);
            
            mover.Launch(setting, lifeTime, delay);
        }
    }
}
