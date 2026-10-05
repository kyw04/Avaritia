using UnityEngine;
using System.Collections;

public struct BulletMoveSettings
{
    public Vector3 dir;
    public float speed;

    public BulletMoveSettings(Vector3 dir, float speed)
    {
        this.dir = dir;
        this.speed = speed;
    }
}

public class BulletMover : MonoBehaviour, IPoolable
{
    public void Launch(BulletMoveSettings data, float lifeTime, float delay)
    {
        float angle = Mathf.Atan2(data.dir.y, data.dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        StartCoroutine(Move(data, lifeTime, delay));
    }

    private IEnumerator Move(BulletMoveSettings data, float lifeTime, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        float timer = 0f;
        while (true)
        {
            transform.position += data.dir * data.speed * Time.deltaTime;
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
