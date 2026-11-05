using UnityEngine;

// 대장장이 팔의 스탯 조각. 플레이어가 닿으면 들고 있는 무기의 ForgeModule에 넣고 사라진다.
// 대장장이 팔을 들고 있지 않으면 먹지 않고 그 자리에 남는다.
public class ForgePickup : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other) => TryCollect(other);

    // 다른 무기를 들고 겹쳐 있다가 대장장이 팔로 바꿔도 먹을 수 있게 한다.
    // 가만히 선 플레이어는 리지드바디가 잠들어 Stay가 오지 않으므로 프리팹의 Kinematic 리지드바디를 NeverSleep으로 둔다.
    private void OnTriggerStay2D(Collider2D other) => TryCollect(other);

    private void TryCollect(Collider2D other)
    {
        var player = other.GetComponentInParent<Player>();
        if (player == null || player.Weapon == null || !player.Weapon.TryGetModule<ForgeModule>(out var forge))
            return;

        forge.Collect();
        ObjectPoolManager.Instance.Despawn(gameObject);
    }
}
