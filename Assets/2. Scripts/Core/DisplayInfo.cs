using System;
using UnityEngine;

// 인스펙터에서 아이콘·이름·설명을 한 묶음으로 편집하는 IInfo 구현.
[Serializable]
public class DisplayInfo : IInfo
{
    [SerializeField] private Sprite icon;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;

    public Sprite Icon => icon;
    public string DisplayName => displayName;
    public string Details => description;
}
