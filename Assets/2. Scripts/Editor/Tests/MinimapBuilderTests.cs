using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MinimapBuilderTests
{
    static readonly Color Ground = Color.white;
    static readonly Color Platform = Color.blue;

    GameObject root;
    Tile tile;
    Tilemap ground;
    Tilemap platform;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("MinimapTestGrid", typeof(Grid));
        ground = new GameObject("Ground", typeof(Tilemap)).GetComponent<Tilemap>();
        ground.transform.SetParent(root.transform);
        platform = new GameObject("Platform", typeof(Tilemap)).GetComponent<Tilemap>();
        platform.transform.SetParent(root.transform);
        platform.gameObject.AddComponent<PlatformTilemapCollider>();
        tile = ScriptableObject.CreateInstance<Tile>();
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(tile);
    }

    List<Tilemap> PaintSample()
    {
        ground.SetTile(new Vector3Int(0, 0, 0), tile);
        ground.SetTile(new Vector3Int(4, 0, 0), tile);
        platform.SetTile(new Vector3Int(2, 3, 0), tile);
        return new List<Tilemap> { ground, platform };
    }

    [Test]
    public void TryGetCellBounds_NoTiles_ReturnsFalse()
    {
        Assert.IsFalse(MinimapBuilder.TryGetCellBounds(new List<Tilemap> { ground, platform }, out _));
    }

    [Test]
    public void TryGetCellBounds_UnionsTilemaps()
    {
        Assert.IsTrue(MinimapBuilder.TryGetCellBounds(PaintSample(), out var bounds));
        Assert.AreEqual(new Vector3Int(0, 0, 0), bounds.min);
        Assert.AreEqual(new Vector3Int(5, 4, 1), bounds.size);
    }

    [Test]
    public void BuildTexture_ColorsGroundPlatformAndEmpty()
    {
        var maps = PaintSample();
        MinimapBuilder.TryGetCellBounds(maps, out var bounds);
        var tex = MinimapBuilder.BuildTexture(maps, bounds, Ground, Platform);
        try
        {
            Assert.AreEqual(5, tex.width);
            Assert.AreEqual(4, tex.height);
            Assert.AreEqual(FilterMode.Point, tex.filterMode);
            Assert.AreEqual(Ground, tex.GetPixel(0, 0));
            Assert.AreEqual(Ground, tex.GetPixel(4, 0));
            Assert.AreEqual(Platform, tex.GetPixel(2, 3));
            Assert.AreEqual(Color.clear, tex.GetPixel(1, 1));
        }
        finally
        {
            Object.DestroyImmediate(tex);
        }
    }

    [Test]
    public void WorldToNormalized_MapsCornersAndClamps()
    {
        var rect = new Rect(0, 0, 10, 5);
        Assert.AreEqual(new Vector2(0, 0), MinimapBuilder.WorldToNormalized(new Vector2(0, 0), rect));
        Assert.AreEqual(new Vector2(1, 1), MinimapBuilder.WorldToNormalized(new Vector2(10, 5), rect));
        Assert.AreEqual(new Vector2(0.5f, 0.5f), MinimapBuilder.WorldToNormalized(new Vector2(5, 2.5f), rect));
        Assert.AreEqual(new Vector2(0, 1), MinimapBuilder.WorldToNormalized(new Vector2(-3, 9), rect));
    }

    [Test]
    public void BuildFromBoxes_RasterizesSolidsAtOnePixelPerUnit()
    {
        // 바닥 (0..10, 0..1), 왼쪽 벽 (0..1, 0..4)
        var solids = new List<Bounds>
        {
            new Bounds(new Vector3(5f, 0.5f), new Vector3(10f, 1f)),
            new Bounds(new Vector3(0.5f, 2f), new Vector3(1f, 4f)),
        };
        Assert.IsTrue(MinimapBuilder.TryBuildFromBoxes(solids, Ground, out var tex, out var rect));
        try
        {
            Assert.AreEqual(new Rect(0, 0, 10, 4), rect);
            Assert.AreEqual(10, tex.width);
            Assert.AreEqual(4, tex.height);
            Assert.AreEqual(Ground, tex.GetPixel(9, 0));
            Assert.AreEqual(Ground, tex.GetPixel(0, 3));
            Assert.AreEqual(Color.clear, tex.GetPixel(5, 2));
        }
        finally
        {
            Object.DestroyImmediate(tex);
        }
    }

    [Test]
    public void BuildFromBoxes_Empty_ReturnsFalse()
    {
        Assert.IsFalse(MinimapBuilder.TryBuildFromBoxes(new List<Bounds>(), Ground, out _, out _));
    }

    // 30x12: 바닥 6칸(0..5행), 왼쪽 벽 10칸(0..9열, 6..10행), 오른쪽 벽 10칸(20..29열, 6..11행). 맨 윗줄 왼쪽은 비어 있다.
    static Texture2D MakeRoom(int wall, int floor)
    {
        int w = 30, h = 12;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        var px = new Color[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool solid = y < floor
                    || (x < wall && y < h - 1)
                    || x >= w - wall;
                px[y * w + x] = solid ? Ground : Color.clear;
            }
        tex.SetPixels(px);
        tex.Apply();
        return tex;
    }

    [Test]
    public void TrimOuterSolid_ClampsThickWallsAndFloor()
    {
        var src = MakeRoom(10, 6);
        var rect = new Rect(0, 0, 30, 12);
        var tex = MinimapBuilder.TrimOuterSolid(src, 3, ref rect);
        try
        {
            Assert.AreEqual(16, tex.width);   // 30 - 7 - 7
            Assert.AreEqual(9, tex.height);   // 12 - 3
            Assert.AreEqual(new Rect(7, 3, 16, 9), rect);
            Assert.AreEqual(Ground, tex.GetPixel(0, 0));
            Assert.AreEqual(Ground, tex.GetPixel(2, 3));       // 남은 왼쪽 벽 3칸
            Assert.AreEqual(Color.clear, tex.GetPixel(3, 3));  // 원래 (10,6): 방 안쪽
            Assert.AreEqual(Ground, tex.GetPixel(5, 2));       // 남은 바닥 3칸
            Assert.AreEqual(FilterMode.Point, tex.filterMode);
        }
        finally
        {
            Object.DestroyImmediate(src);
            Object.DestroyImmediate(tex);
        }
    }

    [Test]
    public void TrimOuterSolid_ThinBordersUnchanged()
    {
        var src = MakeRoom(2, 2);
        var rect = new Rect(0, 0, 30, 12);
        var tex = MinimapBuilder.TrimOuterSolid(src, 3, ref rect);
        try
        {
            Assert.AreEqual(30, tex.width);
            Assert.AreEqual(12, tex.height);
            Assert.AreEqual(new Rect(0, 0, 30, 12), rect);
        }
        finally
        {
            Object.DestroyImmediate(src);
            if (tex != src) Object.DestroyImmediate(tex);
        }
    }

    [Test]
    public void FitSize_KeepsAspectAndMinHeight()
    {
        var a = MinimapBuilder.FitSize(new Vector2Int(60, 10), new Vector2(320, 140), 20);
        Assert.AreEqual(320f, a.x, 0.01f);
        Assert.AreEqual(53.33f, a.y, 0.01f);

        var b = MinimapBuilder.FitSize(new Vector2Int(400, 5), new Vector2(320, 140), 20);
        Assert.AreEqual(320f, b.x, 0.01f);
        Assert.AreEqual(20f, b.y, 0.01f);
    }
}
