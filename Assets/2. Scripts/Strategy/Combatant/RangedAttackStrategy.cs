using UnityEngine;
using System.Collections;

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
            var go = Object.Instantiate(bulletPrefab, attacker.Mono.transform.position, Quaternion.identity);
            var dealer = go.GetComponent<DamageDealer>();
            if (dealer == null)
            {
                Debug.LogError("BulletAttackData: bulletPrefab missing DamageDealer component");
                Object.Destroy(go);
                continue;
            }
            dealer.damage = attacker.Damage * damageMultiplier;
            var dir = (target.position - go.transform.position).normalized;
            attacker.Mono.StartCoroutine(MoveBullet(go.transform, attacker, dir, angle));
        }
        
    }
    
    private IEnumerator MoveBullet(Transform bullet, IAttacker attacker, Vector3 dir, float spreadDeg)
    {
        float timer = 0f;
        Vector3 upDir = Quaternion.Euler(0f, 0f, spreadDeg) * Vector3.up;
        float angle = Mathf.Atan2(upDir.y, upDir.x) * Mathf.Rad2Deg;
        bullet.rotation = Quaternion.Euler(0, 0, angle);
        while (timer < upDuration)
        {
            if (bullet == null)
                yield break;
            
            bullet.position += upDir * upSpeed * Time.fixedDeltaTime;
            timer += Time.fixedDeltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(Random.Range(0f, redirectDelay));
        angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        bullet.rotation = Quaternion.Euler(0, 0, angle);
        while (bullet != null)
        {
            bullet.position += dir * redirectSpeed * Time.fixedDeltaTime;
            yield return null;
        }
    }
}
