using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 플랫폼 타일맵에서 가로로 이어진 타일 줄마다 얇은 BoxCollider2D를 하나씩 맞춰 둔다.
// 에디터에서 타일을 칠하거나 지우면 콜라이더 너비와 위치가 따라 바뀐다(플레이 중에는 저장된 콜라이더를 그대로 쓴다).
[ExecuteAlways]
[RequireComponent(typeof(Tilemap))]
public class PlatformTilemapCollider : MonoBehaviour
{
    [Tooltip("콜라이더 두께")]
    [SerializeField] private float height = 0.1f;
    [Tooltip("타일 아래쪽에서 콜라이더 중심까지의 높이")]
    [SerializeField] private float offsetY = 0.75f;

#if UNITY_EDITOR
    private Tilemap tilemap;

    private void OnEnable()
    {
        if (Application.isPlaying)
            return;

        tilemap = GetComponent<Tilemap>();
        Tilemap.tilemapTileChanged += OnTileChanged;
        Rebuild();
    }

    private void OnDisable() => Tilemap.tilemapTileChanged -= OnTileChanged;

    // OnValidate 안에서는 컴포넌트를 추가/삭제할 수 없어 다음 에디터 틱으로 미룬다.
    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && isActiveAndEnabled)
                Rebuild();
        };
    }

    private void OnTileChanged(Tilemap changed, Tilemap.SyncTile[] _)
    {
        if (changed == tilemap)
            Rebuild();
    }

    private void Rebuild()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        tilemap.CompressBounds();
        var bounds = tilemap.cellBounds;
        var runs = new List<(Vector3Int start, int length)>();
        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                if (!tilemap.HasTile(new Vector3Int(x, y, 0)))
                    continue;

                int start = x;
                while (x < bounds.xMax && tilemap.HasTile(new Vector3Int(x, y, 0)))
                    x++;
                runs.Add((new Vector3Int(start, y, 0), x - start));
            }
        }

        var boxes = new List<BoxCollider2D>(GetComponents<BoxCollider2D>());
        bool changed = false;
        while (boxes.Count < runs.Count)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            if (boxes.Count > 0)
                box.usedByEffector = boxes[0].usedByEffector;
            boxes.Add(box);
            changed = true;
        }
        for (int i = boxes.Count - 1; i >= runs.Count; i--)
        {
            DestroyImmediate(boxes[i]);
            boxes.RemoveAt(i);
            changed = true;
        }

        for (int i = 0; i < runs.Count; i++)
        {
            var min = tilemap.CellToLocal(runs[i].start);
            var max = tilemap.CellToLocal(runs[i].start + new Vector3Int(runs[i].length, 1, 0));
            var size = new Vector2(max.x - min.x, height);
            var offset = new Vector2((min.x + max.x) * 0.5f, min.y + offsetY);
            if (boxes[i].size == size && boxes[i].offset == offset)
                continue;

            boxes[i].size = size;
            boxes[i].offset = offset;
            changed = true;
        }

        if (changed)
            UnityEditor.EditorUtility.SetDirty(gameObject);
    }
#endif
}
