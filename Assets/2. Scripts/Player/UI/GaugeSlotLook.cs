using System;
using UnityEngine;

// 게이지에서 특정 칸 하나만 다르게 칠하는 설정(강화 총알, 스턴 키트 등). 칸 모양 그대로 색과 무늬만 바뀐다.
[Serializable]
public class GaugeSlotLook
{
    public bool use;
    public Color color = Color.white;
    // 칸 안에 입힐 무늬. 가로는 원호 방향, 세로는 고리 두께 방향으로 칸에 맞춰 늘어난다. 비우면 단색.
    public Texture2D pattern;
}
