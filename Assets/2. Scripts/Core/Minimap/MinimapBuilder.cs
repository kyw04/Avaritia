using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// 방의 타일맵을 "타일 1칸 = 1픽셀" 미니맵 텍스처로 만들고, 월드 좌표를 미니맵 비율 좌표로 바꾼다.
public static class MinimapBuilder
{
    // 타일이 있는 칸 범위를 모든 타일맵에 걸쳐 합친다. 타일이 하나도 없으면 false.
    public static bool TryGetCellBounds(IReadOnlyList<Tilemap> tilemaps, out BoundsInt bounds)
    {
        bounds = default;
        bool found = false;
        foreach (var map in tilemaps)
        {
            map.CompressBounds();
            if (map.GetUsedTilesCount() == 0)
                continue;

            var b = map.cellBounds;
            if (!found)
            {
                bounds = b;
                found = true;
                continue;
            }

            var min = Vector3Int.Min(bounds.min, b.min);
            var max = Vector3Int.Max(bounds.max, b.max);
            bounds.SetMinMax(min, max);
        }
        return found;
    }

    // 발판 타일맵(PlatformTilemapCollider)의 색이 지형 색보다 우선한다. 빈 칸은 투명.
    public static Texture2D BuildTexture(IReadOnlyList<Tilemap> tilemaps, BoundsInt bounds, Color groundColor, Color platformColor)
    {
        int w = bounds.size.x, h = bounds.size.y;
        var pixels = new Color[w * h];

        foreach (var map in tilemaps)
        {
            bool isPlatform = map.GetComponent<PlatformTilemapCollider>() != null;
            var color = isPlatform ? platformColor : groundColor;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var cell = new Vector3Int(bounds.xMin + x, bounds.yMin + y, bounds.zMin);
                    if (!map.HasTile(cell))
                        continue;
                    int i = y * w + x;
                    if (isPlatform || pixels[i].a == 0f)
                        pixels[i] = color;
                }
            }
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "Minimap",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // 타일맵이 없는 방(콜라이더로만 지형을 만든 방)용. 월드 1단위 = 1픽셀로 단단한 박스를 칠한다.
    public static bool TryBuildFromBoxes(IReadOnlyList<Bounds> solids, Color groundColor, out Texture2D texture, out Rect worldRect)
    {
        texture = null;
        worldRect = default;
        if (solids.Count == 0)
            return false;

        var min = new Vector2Int(int.MaxValue, int.MaxValue);
        var max = new Vector2Int(int.MinValue, int.MinValue);
        foreach (var b in solids)
        {
            min = Vector2Int.Min(min, new Vector2Int(Mathf.FloorToInt(b.min.x), Mathf.FloorToInt(b.min.y)));
            max = Vector2Int.Max(max, new Vector2Int(Mathf.CeilToInt(b.max.x), Mathf.CeilToInt(b.max.y)));
        }

        int w = max.x - min.x, h = max.y - min.y;
        if (w <= 0 || h <= 0)
            return false;

        var pixels = new Color[w * h];
        foreach (var b in solids)
        {
            int x0 = Mathf.FloorToInt(b.min.x) - min.x, x1 = Mathf.CeilToInt(b.max.x) - min.x;
            int y0 = Mathf.FloorToInt(b.min.y) - min.y, y1 = Mathf.CeilToInt(b.max.y) - min.y;
            for (int y = y0; y < y1; y++)
                for (int x = x0; x < x1; x++)
                    pixels[y * w + x] = groundColor;
        }

        texture = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            name = "Minimap",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels(pixels);
        texture.Apply();
        worldRect = new Rect(min.x, min.y, w, h);
        return true;
    }

    // 가장 바깥쪽 바닥·벽·천장이 maxThickness칸보다 두꺼우면 넘는 만큼 잘라낸다. worldRect도 같이 줄인다.
    // 변마다 가장자리부터 이어진 지형 칸 수 중 0보다 큰 최솟값을 두께로 본다(전부 지형이거나 전부 빈 줄은 제외).
    // 자를 게 없으면 src를 그대로 돌려주고, 잘랐으면 새 텍스처를 돌려준다(src는 호출한 쪽이 정리한다).
    public static Texture2D TrimOuterSolid(Texture2D src, int maxThickness, ref Rect worldRect)
    {
        int w = src.width, h = src.height;
        var px = src.GetPixels();
        bool Solid(int x, int y) => px[y * w + x].a > 0f;

        int left = Excess(h, w, (line, i) => Solid(i, line), maxThickness);
        int right = Excess(h, w, (line, i) => Solid(w - 1 - i, line), maxThickness);
        int bottom = Excess(w, h, (line, i) => Solid(line, i), maxThickness);
        int top = Excess(w, h, (line, i) => Solid(line, h - 1 - i), maxThickness);

        int nw = w - left - right, nh = h - bottom - top;
        if ((left | right | bottom | top) == 0 || nw <= 0 || nh <= 0)
            return src;

        var tex = new Texture2D(nw, nh, TextureFormat.RGBA32, false)
        {
            name = src.name,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
        };
        tex.SetPixels(src.GetPixels(left, bottom, nw, nh));
        tex.Apply();

        float cellW = worldRect.width / w, cellH = worldRect.height / h;
        worldRect = new Rect(worldRect.x + left * cellW, worldRect.y + bottom * cellH, nw * cellW, nh * cellH);
        return tex;
    }

    // lines개의 줄마다 가장자리에서 안쪽으로 length칸까지 이어진 지형 칸 수를 세고, 두께가 max를 넘는 만큼을 돌려준다.
    private static int Excess(int lines, int length, System.Func<int, int, bool> solidAt, int max)
    {
        int thickness = int.MaxValue;
        for (int line = 0; line < lines; line++)
        {
            int run = 0;
            while (run < length && solidAt(line, run))
                run++;
            if (run > 0 && run < length)
                thickness = Mathf.Min(thickness, run);
        }
        return thickness == int.MaxValue ? 0 : Mathf.Max(0, thickness - max);
    }

    public static Rect GetWorldRect(Tilemap anyTilemap, BoundsInt bounds)
    {
        Vector2 min = anyTilemap.CellToWorld(bounds.min);
        Vector2 max = anyTilemap.CellToWorld(bounds.max);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    public static Vector2 WorldToNormalized(Vector2 world, Rect worldRect)
    {
        return new Vector2(
            Mathf.Clamp01(Mathf.InverseLerp(worldRect.xMin, worldRect.xMax, world.x)),
            Mathf.Clamp01(Mathf.InverseLerp(worldRect.yMin, worldRect.yMax, world.y)));
    }

    // 비율을 유지해 maxSize 안에 맞춘다. 너무 얇으면 높이만 minHeight로 올린다.
    public static Vector2 FitSize(Vector2Int textureSize, Vector2 maxSize, float minHeight)
    {
        float scale = Mathf.Min(maxSize.x / textureSize.x, maxSize.y / textureSize.y);
        var size = new Vector2(textureSize.x * scale, textureSize.y * scale);
        if (size.y < minHeight)
            size.y = minHeight;
        return size;
    }
}
