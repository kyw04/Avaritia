using UnityEngine;

public static class ContactFilters
{
    // OverlapCircleAll(..., layerMask)와 같은 대상을 고르는 필터: 레이어만 거르고, 트리거 포함 여부는 Physics2D 설정을 따른다.
    public static ContactFilter2D ForLayer(LayerMask layer)
    {
        var filter = new ContactFilter2D().NoFilter();
        filter.SetLayerMask(layer);
        filter.useTriggers = Physics2D.queriesHitTriggers;
        return filter;
    }
}
