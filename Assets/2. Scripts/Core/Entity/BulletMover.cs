using UnityEngine;
using System.Collections;

public class BulletMover : MonoBehaviour, IPoolable
{
    public void Launch(Vector3 dir, float spreadDeg, float upSpeed, float upDuration, float redirectSpeed, float redirectDelay)
    {
        StartCoroutine(Move(dir, spreadDeg, upSpeed, upDuration, redirectSpeed, redirectDelay));
    }

    private IEnumerator Move(Vector3 dir, float spreadDeg, float upSpeed, float upDuration, float redirectSpeed, float redirectDelay)
    {
        float timer = 0f;
        Vector3 upDir = Quaternion.Euler(0f, 0f, spreadDeg) * Vector3.up;
        float angle = Mathf.Atan2(upDir.y, upDir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        while (timer < upDuration)
        {
            transform.position += upDir * upSpeed * Time.fixedDeltaTime;
            timer += Time.fixedDeltaTime;
            yield return null;
        }

        yield return new WaitForSeconds(Random.Range(0f, redirectDelay));

        angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
        while (true)
        {
            transform.position += dir * redirectSpeed * Time.fixedDeltaTime;
            yield return null;
        }
    }

    public void OnSpawn() { }

    public void OnDespawn()
    {
        StopAllCoroutines();
    }
}
