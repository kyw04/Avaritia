using System;
using UnityEngine;
using UnityEngine.UI;

// ImageCountBar를 고리 스프라이트 세 장(배경, 바닥, 게이지)으로 그린다. 모두 SegmentedArcGauge 머티리얼로 원호만 남긴다.
// 배경은 바닥보다 두껍고 양 끝도 borderDegrees만큼 길어서 테두리가 된다. 바닥은 게이지가 채워지는 자리를 칸별로 어둡게 깔고,
// 게이지는 칸 사이를 뚫고 채워진 칸만 보인다.
// 특별한 칸(강화 총알 등)은 칸 모양 그대로 다른 색·무늬로 칠한다.
public class ImageCountGauge : MonoBehaviour
{
    // 셰이더가 한 번에 다르게 칠할 수 있는 칸 수.
    private const int MaxSpecialSlots = 4;

    private static readonly int SegmentsId = Shader.PropertyToID("_Segments");
    private static readonly int FilledCountId = Shader.PropertyToID("_FilledCount");
    private static readonly int GapDegreesId = Shader.PropertyToID("_GapDegrees");
    private static readonly int CenterAngleId = Shader.PropertyToID("_CenterAngle");
    private static readonly int ArcAngleId = Shader.PropertyToID("_ArcAngle");
    private static readonly int ClockwiseId = Shader.PropertyToID("_Clockwise");
    private static readonly int SpecialSegmentsId = Shader.PropertyToID("_SpecialSegments");
    private static readonly int[] SpecialColorIds =
    {
        Shader.PropertyToID("_SpecialColor0"), Shader.PropertyToID("_SpecialColor1"),
        Shader.PropertyToID("_SpecialColor2"), Shader.PropertyToID("_SpecialColor3"),
    };
    private static readonly int[] SpecialTexIds =
    {
        Shader.PropertyToID("_SpecialTex0"), Shader.PropertyToID("_SpecialTex1"),
        Shader.PropertyToID("_SpecialTex2"), Shader.PropertyToID("_SpecialTex3"),
    };

    [SerializeField] private Image background;
    [SerializeField] private Image track;
    [SerializeField] private Image fill;
    // 원호 가운데가 향하는 각도. 0 = 오른쪽, 90 = 위, 180 = 왼쪽.
    [SerializeField] private float centerAngle = 180f;
    [SerializeField] private float arcAngle = 120f;
    // 켜면 시계 방향으로 채운다(왼쪽 원호라면 아래에서 위로).
    [SerializeField] private bool clockwise = true;
    // 배경 원호를 양 끝으로 더 늘리는 각도. 고리 두께 방향 테두리와 비슷한 두께가 되게 맞춘다.
    [SerializeField] private float borderDegrees = 4.2f;

    private Material backgroundMaterial;
    private Material trackMaterial;
    private Material fillMaterial;

    private void Awake()
    {
        // 무기마다 칸 수가 다르므로 공유 머티리얼을 건드리지 않게 복제한다.
        backgroundMaterial = new Material(background.material);
        trackMaterial = new Material(track.material);
        track.material = trackMaterial;
        fillMaterial = new Material(fill.material);
        background.material = backgroundMaterial;
        fill.material = fillMaterial;
    }

    private void OnDestroy()
    {
        Destroy(backgroundMaterial);
        Destroy(trackMaterial);
        Destroy(fillMaterial);
    }

    public void Show(bool show) => gameObject.SetActive(show);

    // gapDegrees: 칸 사이 틈(각도). 0이면 칸 구분 없이 이어진 띠.
    // getSlotLook: 칸 인덱스별 특별 모양. 채워진 칸 중 use가 켜진 칸만 그 색·무늬로 칠한다(최대 4칸).
    public void Set(int current, int max, Color color, float gapDegrees, Func<int, GaugeSlotLook> getSlotLook = null)
    {
        // 배경은 틈 0으로 칸 구분 없는 원호 하나, 바닥은 게이지와 같은 칸으로 나눈다.
        Apply(backgroundMaterial, max, max, 0f, arcAngle + borderDegrees * 2f);
        Apply(trackMaterial, max, max, gapDegrees, arcAngle);
        Apply(fillMaterial, max, current, gapDegrees, arcAngle);
        fill.color = color;

        var special = new Vector4(-1f, -1f, -1f, -1f);
        int count = 0;
        for (int i = 0; getSlotLook != null && i < current && count < MaxSpecialSlots; i++)
        {
            var look = getSlotLook(i);
            if (look == null || !look.use)
                continue;
            special[count] = i;
            fillMaterial.SetColor(SpecialColorIds[count], look.color);
            fillMaterial.SetTexture(SpecialTexIds[count], look.pattern ? look.pattern : Texture2D.whiteTexture);
            count++;
        }
        fillMaterial.SetVector(SpecialSegmentsId, special);
    }

    private void Apply(Material material, int segments, int filled, float gapDegrees, float arc)
    {
        material.SetFloat(SegmentsId, segments);
        material.SetFloat(FilledCountId, filled);
        material.SetFloat(GapDegreesId, gapDegrees);
        material.SetFloat(CenterAngleId, centerAngle);
        material.SetFloat(ArcAngleId, arc);
        material.SetFloat(ClockwiseId, clockwise ? 1f : 0f);
    }
}
