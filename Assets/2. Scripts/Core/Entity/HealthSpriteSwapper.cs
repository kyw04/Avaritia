using System;
using System.Collections.Generic;
using UnityEngine;

// 체력 비율에 따라 스프라이트를 바꾼다(포탑이 부서져 가는 모습 등).
// 단계는 minHealthRatio가 높은 순으로 보고, 현재 비율이 처음으로 그보다 큰 단계의 스프라이트를 쓴다.
[RequireComponent(typeof(Entity))]
public class HealthSpriteSwapper : MonoBehaviour, IObserver<EntityHealthChangedEvent>
{
    [Serializable]
    public class Stage
    {
        [Range(0f, 1f)] public float minHealthRatio;
        public Sprite sprite;
    }

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private List<Stage> stages = new();

    private Entity entity;
    private bool refreshPending;

    private void Awake() => entity = GetComponent<Entity>();

    // 풀에서 꺼낼 때는 OnEnable 뒤 OnSpawn에서 체력이 초기화되고 체력 변경 이벤트는 나오지 않으므로,
    // 같은 프레임 LateUpdate(그리기 전)까지 미뤄 초기화된 체력으로 계산한다.
    private void OnEnable()
    {
        EventBus.Subscribe<EntityHealthChangedEvent>(this);
        refreshPending = true;
    }

    private void LateUpdate()
    {
        if (!refreshPending)
            return;
        refreshPending = false;
        Refresh(entity.MaxHealth > 0f ? entity.CurrentHealth / entity.MaxHealth : 1f);
    }

    private void OnDisable() => EventBus.Unsubscribe<EntityHealthChangedEvent>(this);

    public void OnNotify(EntityHealthChangedEvent e)
    {
        if (e.Source == entity)
            Refresh(e.Max > 0f ? e.Current / e.Max : 0f);
    }

    private void Refresh(float ratio)
    {
        // 어느 단계보다도 낮으면(체력 0 등) 가장 낮은 단계를 쓴다.
        Stage best = null, lowest = null;
        foreach (var stage in stages)
        {
            if (ratio > stage.minHealthRatio && (best == null || stage.minHealthRatio > best.minHealthRatio))
                best = stage;
            if (lowest == null || stage.minHealthRatio < lowest.minHealthRatio)
                lowest = stage;
        }
        best ??= lowest;
        if (best != null && best.sprite != null)
            spriteRenderer.sprite = best.sprite;
    }
}
