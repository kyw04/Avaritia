using System;
using UnityEngine;

// 개수(탄, 스택, 키트)를 게이지로 보여 준다.
public class ImageCountBar : MonoBehaviour
{
    [SerializeField] private ImageCountStyle style;
    [SerializeField] private int currentCount;
    [SerializeField] private int maxCount;

    private Func<int> getCurrentCount;
    private Func<int> getMaxCount;
    private Func<int, GaugeSlotLook> getSlotLook;
    private ImageCountGauge gauge;

    public bool IsActive { get; private set; }

    private void Awake()
    {
        gauge = GetComponentInChildren<ImageCountGauge>(true);
        Init(style, () => currentCount, () => maxCount);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
        if (gauge != null)
            gauge.Show(false);
    }

    // slotLookBridge: 칸 인덱스별 특별 모양(강화 총알 등). null이거나 use가 꺼져 있으면 기본 칸.
    public void Init(ImageCountStyle newStyle, Func<int> currentCountBridge, Func<int> maxCountBridge, Func<int, GaugeSlotLook> slotLookBridge = null)
    {
        style = newStyle;
        getCurrentCount = currentCountBridge;
        getMaxCount = maxCountBridge;
        getSlotLook = slotLookBridge;
        Refresh();
    }

    public void Refresh()
    {
        maxCount = getMaxCount();
        currentCount = getCurrentCount();
        currentCount = currentCount < 0 ? maxCount : Mathf.Clamp(currentCount, 0, maxCount);

        if (gauge == null)
            return;
        gauge.Show(style != null);
        if (style == null)
            return;
        gauge.Set(currentCount, maxCount, style.EvaluateFillColor(currentCount, maxCount), style.Gap, getSlotLook);
    }
}
