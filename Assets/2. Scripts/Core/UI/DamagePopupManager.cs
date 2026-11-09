using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct DamagePopupStyle
{
    public Color color;
    [Min(0)] public float scale;

    public DamagePopupStyle(Color color, float scale) { this.color = color; this.scale = scale; }
}

// 피격/회피/피격 무효화 이벤트를 받아 대상 머리 위에 데미지 팝업을 띄운다.
public class DamagePopupManager : Singleton<DamagePopupManager>,
    IObserver<EntityDamagedEvent>,
    IObserver<EntityEvadedEvent>,
    IObserver<EntityHitBlockedEvent>
{
    [SerializeField] private DamagePopup popupPrefab;
    [SerializeField] private Vector3 spawnOffset = new(0f, 1f, 0f);

    [Header("Stacking")]
    [SerializeField, Min(0)] private float stackWindow = 0.3f;      // 이 시간 안에 생긴 팝업끼리만 겹침을 피한다
    [SerializeField] private Vector2 stackOverlapSize = new(0.8f, 0.4f); // 이 범위 안이면 겹친 것으로 본다
    [SerializeField, Min(0.05f)] private float stackSpacing = 0.4f;    // 겹치면 위로 올리는 간격

    [Header("Styles")]
    [SerializeField] private DamagePopupStyle normalStyle = new(Color.white, 1f);
    [SerializeField] private DamagePopupStyle criticalStyle = new(new Color(1f, 0.25f, 0.15f), 1.4f);
    [SerializeField] private DamagePopupStyle evadeStyle = new(new Color(0.6f, 0.85f, 1f), 0.9f);
    [SerializeField] private string evadeText = "MISS";
    [SerializeField] private DamagePopupStyle blockStyle = new(new Color(1f, 0.85f, 0.3f), 0.9f);
    [SerializeField] private string blockText = "BLOCK";

    private void Start()
    {
        EventBus.Subscribe<EntityDamagedEvent>(this);
        EventBus.Subscribe<EntityEvadedEvent>(this);
        EventBus.Subscribe<EntityHitBlockedEvent>(this);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<EntityDamagedEvent>(this);
        EventBus.Unsubscribe<EntityEvadedEvent>(this);
        EventBus.Unsubscribe<EntityHitBlockedEvent>(this);
    }

    public void OnNotify(EntityDamagedEvent e) =>
        Spawn(e.Source, Mathf.RoundToInt(e.Amount).ToString(), e.IsCritical ? criticalStyle : normalStyle);

    public void OnNotify(EntityEvadedEvent e) => Spawn(e.Source, evadeText, evadeStyle);

    public void OnNotify(EntityHitBlockedEvent e) => Spawn(e.Source, blockText, blockStyle);

    private void Spawn(Entity target, string content, DamagePopupStyle style)
    {
        if (popupPrefab == null || target == null) return;
        var popup = ObjectPoolManager.Instance.Spawn(popupPrefab, GetStackedPosition(target.transform.position + spawnOffset), Quaternion.identity);
        popup.Show(content, style);
    }

    private readonly List<(Vector3 pos, float time)> recentSpawns = new();

    // 최근 팝업과 겹치면 겹치지 않을 때까지 위로 올린다.
    private Vector3 GetStackedPosition(Vector3 pos)
    {
        recentSpawns.RemoveAll(s => Time.time - s.time > stackWindow);

        bool overlapped = true;
        while (overlapped)
        {
            overlapped = false;
            foreach (var s in recentSpawns)
            {
                if (Mathf.Abs(s.pos.x - pos.x) < stackOverlapSize.x && Mathf.Abs(s.pos.y - pos.y) < stackOverlapSize.y)
                {
                    pos.y += stackSpacing;
                    overlapped = true;
                    break;
                }
            }
        }

        recentSpawns.Add((pos, Time.time));
        return pos;
    }
}
