using UnityEngine;

public class EntitySpawnPoint : MonoBehaviour
{
    public Entity entityPrefab;
    [Tooltip("스폰 위치에서 순찰 범위 중심까지의 거리")]
    public Vector2 patrolOffset;
    [Tooltip("적의 순찰 범위. 폭이 0이면 순찰하지 않고 순찰 중심에 선다.")]
    public Vector2 patrolSize;

    public Vector2 PatrolCenter => (Vector2)transform.position + patrolOffset;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color32(0, 255, 0, 50);
        Gizmos.DrawCube(PatrolCenter, patrolSize);
    }
}
