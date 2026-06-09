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

    protected override IEnumerator Execute(IAttacker attacker, Transform target = null)
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
            var go = Object.Instantiate(bulletPrefab, attacker.Mono.transform.position, Quaternion.identity);
            var dealer = go.GetComponent<DamageDealer>();
            if (dealer == null)
            {
                Debug.LogError("BulletAttackData: bulletPrefab missing DamageDealer component");
                Object.Destroy(go);
                continue;
            }
            dealer.damage = attacker.Damage * damageMultiplier;
            attacker.Mono.StartCoroutine(MoveBullet(go.transform, attacker, target, angle));
        }
        yield return new WaitForSeconds(duration);
    }

    private IEnumerator MoveBullet(Transform bullet, IAttacker attacker, Transform target, float spreadDeg)
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
            if (target == null) { Object.Destroy(bullet.gameObject); yield break; }
            var dir = (target.position - bullet.position).normalized;
            bullet.position += dir * redirectSpeed * Time.deltaTime;
            yield return null;
        }
    }
}
