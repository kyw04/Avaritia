using UnityEngine;
using System.Collections;

public class BulletMover : MonoBehaviour, IPoolable
{
    public void Launch(Vector3 dir, float spreadDeg, float upSpeed, float upDuration, float redirectSpeed, float redirectDelay, float lifeTime)
    {
        StartCoroutine(Move(dir, spreadDeg, upSpeed, upDuration, redirectSpeed, redirectDelay, lifeTime));
    }

    private IEnumerator Move(Vector3 dir, float spreadDeg, float upSpeed, float upDuration, float redirectSpeed, float redirectDelay, float lifeTime)
    {
        float timer = 0f;
        Vector3 upDir = Quaternion.Euler(0f, 0f, spreadDeg) * Vector3.up;
        float angle = Mathf.Atan2(upDir.y, upDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        while (timer < upDuration)
        {
            transform.position += upDir * upSpeed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(Random.Range(0f, redirectDelay));

        timer = 0f;
        angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        while (true)
        {
            transform.position += dir * redirectSpeed * Time.deltaTime;
            timer += Time.deltaTime;
            if (lifeTime != 0 && lifeTime <= timer)
                break;
            
            yield return null;
        }

        ObjectPoolManager.Instance.Despawn(gameObject);
    }

    public void OnSpawn() { }

    public void OnDespawn()
    {
        StopAllCoroutines();
    }
}
