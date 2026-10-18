using System;
using UnityEngine;

[Serializable]
public struct DamagePopupStyle
{
    public Color color;
    [Min(0)] public float scale;

    public DamagePopupStyle(Color color, float scale) { this.color = color; this.scale = scale; }
}

// 피격/회피 이벤트를 받아 대상 머리 위에 데미지 팝업을 띄운다.
public class DamagePopupManager : Singleton<DamagePopupManager>,
    IObserver<EntityDamagedEvent>,
    IObserver<EntityEvadedEvent>
{
    [SerializeField] private DamagePopup popupPrefab;
    [SerializeField] private Vector3 spawnOffset = new(0f, 1f, 0f);

    [Header("Styles")]
    [SerializeField] private DamagePopupStyle normalStyle = new(Color.white, 1f);
    [SerializeField] private DamagePopupStyle criticalStyle = new(new Color(1f, 0.25f, 0.15f), 1.4f);
    [SerializeField] private DamagePopupStyle evadeStyle = new(new Color(0.6f, 0.85f, 1f), 0.9f);
    [SerializeField] private string evadeText = "MISS";

    private void Start()
    {
        EventBus.Subscribe<EntityDamagedEvent>(this);
        EventBus.Subscribe<EntityEvadedEvent>(this);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<EntityDamagedEvent>(this);
        EventBus.Unsubscribe<EntityEvadedEvent>(this);
    }

    public void OnNotify(EntityDamagedEvent e) =>
        Spawn(e.Source, Mathf.RoundToInt(e.Amount).ToString(), e.IsCritical ? criticalStyle : normalStyle);

    public void OnNotify(EntityEvadedEvent e) => Spawn(e.Source, evadeText, evadeStyle);

    private void Spawn(Entity target, string content, DamagePopupStyle style)
    {
        if (popupPrefab == null || target == null) return;
        var popup = ObjectPoolManager.Instance.Spawn(popupPrefab, target.transform.position + spawnOffset, Quaternion.identity);
        popup.Show(content, style);
    }
}
