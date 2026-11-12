using UnityEngine;

// 구덩이처럼 떨어지는 곳. 플레이어가 들어오면 연결된 복귀 지점으로 옮기고 피해를 준다. 적은 떨어지면 죽는다.
// 씬 뷰에서 구역과 복귀 지점을 선으로 이어 보여 준다.
[RequireComponent(typeof(BoxCollider2D))]
public class FallZone : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float damage = 10f;

    private void Reset() => GetComponent<BoxCollider2D>().isTrigger = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var entity = other.GetComponentInParent<Entity>();
        if (entity == null || entity.IsDead)
            return;

        if (entity is Player player)
        {
            // 먼저 옮겨야 이 피해로 죽어도 구덩이 밖에서 사망 연출이 나온다.
            player.Rb.position = respawnPoint.position;
            player.Rb.linearVelocity = Vector2.zero;
            player.TakeDamage(new DamageInfo(damage,
                flags: DamageFlags.IgnoreArmor | DamageFlags.IgnoreEvasion | DamageFlags.NoKnockback));
        }
        else
        {
            entity.Die();
        }
    }

    private void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.35f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(box.offset, box.size);
        Gizmos.matrix = Matrix4x4.identity;

        if (respawnPoint == null)
            return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.TransformPoint(box.offset), respawnPoint.position);
        Gizmos.DrawWireSphere(respawnPoint.position, 0.3f);
    }
}
