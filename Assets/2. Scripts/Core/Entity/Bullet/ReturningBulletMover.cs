using System.Collections;
using UnityEngine;

// 총알처럼 발사돼 maxDistance까지 날아갔다가 시전자에게 돌아온다. 벽(wallLayer)에 맞으면 stuckDuration 동안
// 박혀 있다가 돌아온다. 시전자에게 닿으면 ObjectRetrieval로 회수를 알린다. 날아가는 동안 시전자와 선으로 이어진다.
// 시전자는 같은 오브젝트의 DamageDealer.attacker(RangedAttackStrategy가 발사 전에 채움)를 쓴다.
public class ReturningBulletMover : BulletMover
{
    public float maxDistance = 7f;
    public float returnSpeed = 22f;
    public float stuckDuration = 1.5f;
    public float catchDistance = 0.5f; // 시전자와 이 거리 안이면 회수된다
    public float wallCheckRadius = 0.3f;
    public LayerMask wallLayer;
    public float spinSpeed = 1080f;
    public Transform spinTarget;
    public LineRenderer line;
    public ActionFeedback stick;

    private Entity owner;
    private PiercingDamageDealer piercing;
    private bool retrieved;

    private bool OwnerAlive => owner != null && owner.gameObject.activeInHierarchy;

    public override void Launch(BulletMoveSettings setting, float lifeTime, float delay)
    {
        owner = TryGetComponent<DamageDealer>(out var dealer) ? dealer.attacker : null;
        piercing = GetComponent<PiercingDamageDealer>();
        retrieved = false;
        transform.rotation = Quaternion.identity;
        UpdateLine();
        StartCoroutine(Fly(setting.dir, setting.speed, delay));
    }

    private IEnumerator Fly(Vector2 dir, float speed, float delay)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        float travelled = 0f;
        while (travelled < maxDistance)
        {
            Vector2 from = transform.position;
            float step = Mathf.Min(speed * Time.deltaTime, maxDistance - travelled);
            var wall = Physics2D.CircleCast(from, wallCheckRadius, dir, step, wallLayer);
            float moved = wall ? wall.distance : step;
            transform.position = from + dir * moved;
            travelled += moved;

            if (wall)
            {
                stick.Play(transform.position);
                yield return new WaitForSeconds(stuckDuration);
                break;
            }
            yield return null;
        }

        // 돌아오는 길에도 같은 적을 한 번 더 맞힐 수 있게 하고, 벽은 무시하고 시전자에게 곧장 날아온다.
        piercing?.ResetHits();
        while (OwnerAlive)
        {
            Vector2 toOwner = owner.transform.position - transform.position;
            float distance = toOwner.magnitude;
            float step = returnSpeed * Time.deltaTime;
            if (distance <= catchDistance + step)
            {
                retrieved = true;
                ObjectRetrieval.Notify(owner, gameObject);
                break;
            }

            transform.position += (Vector3)(toOwner / distance * step);
            yield return null;
        }

        ObjectPoolManager.Instance.Despawn(gameObject);
    }

    private void LateUpdate()
    {
        if (spinTarget != null)
            spinTarget.Rotate(0f, 0f, -spinSpeed * Time.deltaTime);
        UpdateLine();
    }

    private void UpdateLine()
    {
        if (line == null) return;
        line.enabled = OwnerAlive;
        if (!line.enabled) return;
        line.positionCount = 2;
        line.SetPosition(0, owner.transform.position);
        line.SetPosition(1, transform.position);
    }

    public override void OnDespawn()
    {
        base.OnDespawn();
        // 회수되지 못하고 사라져도(방 정리 등) 시전자의 쿨타임 잠금이 남지 않게 회수된 것으로 알린다.
        if (!retrieved && owner != null)
            ObjectRetrieval.Notify(owner, gameObject);
        retrieved = true;
        owner = null;
    }
}
