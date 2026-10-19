using System.Collections.Generic;
using UnityEngine;

// 빛줄기가 뻗어 나오는 월드 위치와 그 빛줄기의 모양. LightShaftController가 화면에 가장 가까운 하나를 골라 사용
public class LightShaftSource : MonoBehaviour
{
    public static readonly List<LightShaftSource> All = new();

    public Color color = new(1f, 0.86f, 0.62f, 1f);
    [Range(0f, 3f)] public float intensity = 1.3f;
    [Tooltip("0 = 바로 아래, + = 오른쪽으로 기울어짐")]
    [Range(-90f, 90f)] public float direction = 25f;
    [Range(1f, 120f)] public float spread = 70f;
    [Range(1f, 80f)] public float density = 10f;
    [Range(0.5f, 8f)] public float sharpness = 1.8f;
    [Range(0f, 2f)] public float swaySpeed = 0.38f;
    [Range(0f, 4f)] public float falloff = 0.6f;
    [Range(0f, 1f)] public float flicker = 0.361f;
    [Tooltip("가림막에 막힌 곳에 남는 은은한 빛의 밝기 (0 = 완전히 어두움)")]
    [Range(0f, 1f)] public float shadowScatter = 0f;

    private void OnEnable() => All.Add(this);
    private void OnDisable() => All.Remove(this);
}
