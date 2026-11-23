using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// 화면 구석에 현재 방 전체를 보여주는 미니맵. 지형은 방이 바뀔 때 한 번 만들고, 아이콘 위치만 매 프레임 갱신한다.
public class MinimapUI : MonoBehaviour, IObserver<StageNodeChangedEvent>
{
    [SerializeField] private RoomContentSpawner spawner;
    [SerializeField] private Player player;
    [SerializeField] private RawImage terrainImage;
    [SerializeField] private RectTransform iconLayer;
    [SerializeField] private Image iconPrefab;

    [SerializeField] private Vector2 maxSize = new(320f, 140f);
    [SerializeField] private float minHeight = 20f;
    // 가장 바깥쪽 바닥·벽을 이 칸 수까지만 보여준다.
    [SerializeField, Min(0)] private int maxBorderThickness = 3;

    [Header("Colors")]
    [SerializeField] private Color groundColor = new(0.85f, 0.85f, 0.85f, 1f);
    [SerializeField] private Color platformColor = new(0.55f, 0.75f, 1f, 1f);
    [SerializeField] private Color playerColor = new(0.3f, 1f, 0.4f, 1f);
    [SerializeField] private Color enemyColor = new(1f, 0.35f, 0.3f, 1f);
    [SerializeField] private Color bossColor = new(1f, 0.1f, 0.6f, 1f);
    [SerializeField] private Color closedDoorColor = new(0.6f, 0.6f, 0.6f, 1f);
    [SerializeField] private Color openDoorColor = new(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color rewardColor = new(1f, 1f, 0.4f, 1f);

    [Header("Icon Sizes (px)")]
    [SerializeField] private float playerSize = 8f;
    [SerializeField] private float enemySize = 6f;
    [SerializeField] private float bossSize = 12f;
    [SerializeField] private float doorSize = 8f;
    [SerializeField] private float rewardSize = 6f;

    private readonly List<Image> iconPool = new();
    private readonly List<Tilemap> tilemaps = new();
    private InteractableDoor[] doors = System.Array.Empty<InteractableDoor>();
    private Texture2D texture;
    private Rect worldRect;
    private bool hasMap;
    private bool dirty = true;
    private int usedIcons;

    private void Awake() => EventBus.Subscribe<StageNodeChangedEvent>(this);

    private void OnDestroy()
    {
        EventBus.UnsubscribeAll(this);
        if (texture != null)
            Destroy(texture);
    }

    // 같은 이벤트를 받는 RoomContentSpawner보다 먼저 불려 방이 아직 없을 수 있어 다음 LateUpdate에서 만든다.
    public void OnNotify(StageNodeChangedEvent e) => dirty = true;

    private void LateUpdate()
    {
        if (dirty)
        {
            dirty = false;
            Rebuild();
        }

        if (hasMap)
            UpdateIcons();
    }

    private void Rebuild()
    {
        if (texture != null)
        {
            Destroy(texture);
            texture = null;
        }

        var room = spawner.CurrentRoom;
        tilemaps.Clear();
        if (room != null)
            room.GetComponentsInChildren(tilemaps);

        hasMap = false;
        if (room != null)
        {
            if (MinimapBuilder.TryGetCellBounds(tilemaps, out var bounds))
            {
                texture = MinimapBuilder.BuildTexture(tilemaps, bounds, groundColor, platformColor);
                worldRect = MinimapBuilder.GetWorldRect(tilemaps[0], bounds);
                hasMap = true;
            }
            else
            {
                // 타일맵이 없는 방(상점 등)은 단단한 박스 콜라이더로 지형을 그린다.
                hasMap = MinimapBuilder.TryBuildFromBoxes(CollectSolidBoxes(room), groundColor, out texture, out worldRect);
            }
        }

        if (hasMap)
        {
            var trimmed = MinimapBuilder.TrimOuterSolid(texture, maxBorderThickness, ref worldRect);
            if (trimmed != texture)
            {
                Destroy(texture);
                texture = trimmed;
            }
        }

        terrainImage.texture = texture;
        if (hasMap)
            SetUp(room);
        SetVisible(hasMap);
    }

    private static List<Bounds> CollectSolidBoxes(GameObject room)
    {
        var solids = new List<Bounds>();
        foreach (var box in room.GetComponentsInChildren<BoxCollider2D>())
            if (!box.isTrigger && box.GetComponentInParent<InteractableDoor>() == null)
                solids.Add(box.bounds);
        return solids;
    }

    private void SetUp(GameObject room)
    {
        var size = MinimapBuilder.FitSize(new Vector2Int(texture.width, texture.height), maxSize, minHeight);
        ((RectTransform)transform).sizeDelta = size;
        terrainImage.rectTransform.sizeDelta = size;
        iconLayer.sizeDelta = size;

        doors = room.GetComponentsInChildren<InteractableDoor>();
    }

    private void SetVisible(bool visible)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(visible);
        if (TryGetComponent<Image>(out var background))
            background.enabled = visible;
    }

    private void UpdateIcons()
    {
        usedIcons = 0;

        var doorColor = StageManager.Instance.IsCurrentRoomCleared ? openDoorColor : closedDoorColor;
        foreach (var door in doors)
            if (door != null)
                PlaceIcon(door.transform.position, doorColor, doorSize);

        foreach (var box in spawner.RoomBoxes)
            if (box != null && box.gameObject.activeInHierarchy && !box.IsOpened)
                PlaceIcon(box.transform.position, rewardColor, rewardSize);

        foreach (var pickup in spawner.RoomPickups)
            if (pickup != null && pickup.gameObject.activeInHierarchy)
                PlaceIcon(pickup.transform.position, rewardColor, rewardSize);

        foreach (var actor in spawner.AliveActors)
        {
            if (actor == null || !actor.gameObject.activeInHierarchy || actor.CurrentHealth <= 0)
                continue;
            bool isBoss = actor is Boss;
            PlaceIcon(actor.transform.position, isBoss ? bossColor : enemyColor, isBoss ? bossSize : enemySize);
        }

        // 플레이어는 다른 아이콘 위에 그린다.
        PlaceIcon(player.transform.position, playerColor, playerSize);

        for (int i = usedIcons; i < iconPool.Count; i++)
            iconPool[i].gameObject.SetActive(false);
    }

    private void PlaceIcon(Vector2 world, Color color, float size)
    {
        if (usedIcons == iconPool.Count)
            iconPool.Add(Instantiate(iconPrefab, iconLayer));

        var icon = iconPool[usedIcons++];
        icon.gameObject.SetActive(true);
        icon.color = color;

        var rt = icon.rectTransform;
        rt.anchorMin = rt.anchorMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(size, size);
        rt.anchoredPosition = Vector2.Scale(MinimapBuilder.WorldToNormalized(world, worldRect), iconLayer.rect.size);
    }
}
