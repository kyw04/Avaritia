using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/BulletAttackData")]
public class BulletAttackData : AttackData
{
    public int bulletCount;
    public float spreadAngle;
    public float upSpeed;
    public float upDuration;
    public float redirectSpeed;
    public GameObject bulletPrefab;

    public override IEnumerator Execute(Boss boss)
    {
        if (bulletPrefab == null)
        {
            Debug.LogError("BulletAttackData: bulletPrefab not assigned");
            yield break;
        }

        for (int i = 0; i < bulletCount; i++)
        {
            float angle = bulletCount > 1
                ? Mathf.Lerp(-spreadAngle / 2f, spreadAngle / 2f, (float)i / (bulletCount - 1))
                : 0f;
            var go = Object.Instantiate(bulletPrefab, boss.transform.position, Quaternion.identity);
            var dealer = go.GetComponent<DamageDealer>();
            if (dealer == null)
            {
                Debug.LogError("BulletAttackData: bulletPrefab missing DamageDealer component");
                Object.Destroy(go);
                continue;
            }
            dealer.damage = boss.Damage * damageMultiplier;
            boss.StartCoroutine(MoveBullet(go.transform, boss, angle));
        }
        yield return new WaitForSeconds(duration);
    }

    private IEnumerator MoveBullet(Transform bullet, Boss boss, float spreadDeg)
    {
        float timer = 0f;
        Vector3 upDir = Quaternion.Euler(0f, 0f, spreadDeg) * Vector3.up;
        while (timer < upDuration)
        {
            if (bullet == null) yield break;
            bullet.position += upDir * upSpeed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }
        while (bullet != null)
        {
            if (boss.Target == null) { Object.Destroy(bullet.gameObject); yield break; }
            var dir = ((Vector3)boss.Target.position - bullet.position).normalized;
            bullet.position += dir * redirectSpeed * Time.deltaTime;
            yield return null;
        }
    }
}
