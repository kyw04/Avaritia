using UnityEngine;

// 정보 패널(ItemInfoUI)에 보여 줄 아이콘, 이름, 설명. 무기, 스킬, 아이템과 그 픽업이 제공한다.
public interface IInfo
{
    Sprite Icon { get; }
    string DisplayName { get; }
    string Details { get; }
}
