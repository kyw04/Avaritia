using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RangedAttackStrategy : IAttackStrategy
{
    public BulletMover bulletPrefab;
    public float spreadAngle;
    public float lifeTime;
    [Tooltip("총알이 나가고 궤도가 시작되는 위치. 공격자 기준이며 x는 바라보는 방향에 맞춰 뒤집힌다.")]
    public Vector2 muzzleOffset;
    
    [Header("Dynamic Stat")]
    [Tooltip("If the attacker has custom stats, they will be applied instead.")]
    public int bulletCount = 1;
    public float bulletDelay = 0;
    public float bulletMaxDelay = 0;
    public float bulletSpeed = 15f;

    [Header("Aim")]
    [Tooltip("지정하면 공격 준비(ready) 동안 궤도를 보여 주고, 준비를 시작할 때 정한 방향으로 쏜다.")]
    public TrajectoryTelegraph aimTelegraphPrefab;
    [Min(0)] public float aimLength = 8f;

    public Vector3 GetMuzzlePosition(IAttacker attacker) =>
        attacker.Mono.transform.position + new Vector3(muzzleOffset.x * attacker.LookDirection, muzzleOffset.y, 0f);

    public Vector2 GetAimDirection(IAttacker attacker, Transform target) =>
        target != null
            ? ((Vector2)(target.position - GetMuzzlePosition(attacker))).normalized
            : Vector2.right * attacker.LookDirection;

    // 쏠 총알마다(퍼짐 각도 포함) 궤도를 띄운다. 궤도는 duration 동안 가늘어진다.
    public List<TrajectoryTelegraph> ShowAim(IAttacker attacker, Vector2 aimDirection, float duration)
    {
        var telegraphs = new List<TrajectoryTelegraph>();
        if (aimTelegraphPrefab == null)
            return telegraphs;

        var origin = GetMuzzlePosition(attacker);
        int count = ResolveCount(attacker);
        for (int i = 0; i < count; i++)
        {
            var dir = Quaternion.Euler(0f, 0f, SpreadAngle(i, count)) * aimDirection;
            var telegraph = ObjectPoolManager.Instance.Spawn(aimTelegraphPrefab, origin, Quaternion.identity);
            telegraph.Show(attacker.Mono, origin, dir, aimLength, duration);
            telegraphs.Add(telegraph);
        }
        return telegraphs;
    }

    private int ResolveCount(IAttacker attacker) =>
        attacker is IStatReadable r && r.TryGetStat(StatType.BulletCount, out int statCount) ? statCount : bulletCount;

    private float SpreadAngle(int index, int count) =>
        count > 1 ? Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)index / (count - 1)) : 0f;

    public void Offensive(IAttacker attacker, float damageMultiplier, ContactFilter2D filter, Transform target = null, BulletEnhancement enhancement = null, Vector2? aimDirection = null)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletAttackData: bulletPrefab not assigned");
            return;
        }
        
        int count = ResolveCount(attacker);
        float minDelay = bulletDelay;
        float maxDelay = bulletMaxDelay;
        float speed = bulletSpeed;
        if (attacker is IStatReadable r)
        {
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
            float angle = SpreadAngle(i, count);
            var prefab = enhancement != null && enhancement.bulletPrefab != null ? enhancement.bulletPrefab : bulletPrefab;
            BulletMover mover = ObjectPoolManager.Instance.Spawn(prefab, GetMuzzlePosition(attacker), Quaternion.identity);
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
            if (aimDirection.HasValue)
                dir = aimDirection.Value;
            else if (target != null)
                dir = (target.position - mover.transform.position).normalized;
            dir = Quaternion.Euler(0f, 0f, angle) * dir;
            var setting = new BulletMoveSettings(dir, speed);
            
            mover.Launch(setting, lifeTime, delay);
        }
    }
}
