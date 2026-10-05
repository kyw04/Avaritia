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
    public void Launch(BulletMoveSettings setting, float lifeTime, float delay)
    {
        // 왼쪽으로 갈 땐 좌우 반전 후 회전시켜 스프라이트가 뒤집히지 않게 함
        float sign = setting.dir.x < 0f ? -1f : 1f;
        var scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * sign;
        transform.localScale = scale;
        float angle = Mathf.Atan2(setting.dir.y * sign, setting.dir.x * sign) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
        StartCoroutine(Move(setting, lifeTime, delay));
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
