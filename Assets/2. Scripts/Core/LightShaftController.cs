using UnityEngine;

// 화면 기준이던 빛줄기 원점을 월드의 LightShaftSource 위치에 고정하고, 그 소스의 빛줄기 설정을 적용. 화면에서 멀어지면 서서히 끔
[RequireComponent(typeof(Camera))]
public class LightShaftController : MonoBehaviour
{
    private static readonly int LightPosId = Shader.PropertyToID("_LightPos");
    private static readonly int LightColorId = Shader.PropertyToID("_LightColor");
    private static readonly int LightIntensityId = Shader.PropertyToID("_LightIntensity");
    private static readonly int RayDirectionId = Shader.PropertyToID("_RayDirection");
    private static readonly int RaySpreadId = Shader.PropertyToID("_RaySpread");
    private static readonly int RayDensityId = Shader.PropertyToID("_RayDensity");
    private static readonly int RaySharpnessId = Shader.PropertyToID("_RaySharpness");
    private static readonly int RaySpeedId = Shader.PropertyToID("_RaySpeed");
    private static readonly int RayFalloffId = Shader.PropertyToID("_RayFalloff");
    private static readonly int FlickerId = Shader.PropertyToID("_Flicker");
    private static readonly int ShadowBrightnessId = Shader.PropertyToID("_ShadowBrightness");

    [SerializeField] private Material shaftMaterial;
    [Tooltip("화면 중심에서 이 거리(뷰포트 단위, 0.5 = 화면 끝)부터 어두워지기 시작")]
    [SerializeField] private float fadeStart = 0.9f;
    [Tooltip("이 거리에서 완전히 꺼짐")]
    [SerializeField] private float fadeEnd = 1.4f;

    private Camera cam;
    private Material original;

    private void Awake() => cam = GetComponent<Camera>();

    private void OnEnable() => original = new Material(shaftMaterial);

    // 플레이 중 바꾼 값이 머티리얼 에셋에 남지 않도록 복구
    private void OnDisable()
    {
        shaftMaterial.CopyPropertiesFromMaterial(original);
        Destroy(original);
    }

    private void LateUpdate()
    {
        LightShaftSource best = null;
        Vector3 bestPos = Vector3.zero;
        float bestIntensity = 0f;

        foreach (var source in LightShaftSource.All)
        {
            Vector3 vp = cam.WorldToViewportPoint(source.transform.position);
            float dist = Mathf.Max(Mathf.Abs(vp.x - 0.5f), Mathf.Abs(vp.y - 0.5f));
            float value = source.intensity * (1f - Mathf.InverseLerp(fadeStart, fadeEnd, dist));
            if (value > bestIntensity)
            {
                best = source;
                bestIntensity = value;
                bestPos = vp;
            }
        }

        shaftMaterial.SetFloat(LightIntensityId, bestIntensity);
        if (best == null)
            return;

        shaftMaterial.SetVector(LightPosId, new Vector4(bestPos.x, bestPos.y, 0f, 0f));
        shaftMaterial.SetColor(LightColorId, best.color);
        shaftMaterial.SetFloat(RayDirectionId, best.direction);
        shaftMaterial.SetFloat(RaySpreadId, best.spread);
        shaftMaterial.SetFloat(RayDensityId, best.density);
        shaftMaterial.SetFloat(RaySharpnessId, best.sharpness);
        shaftMaterial.SetFloat(RaySpeedId, best.swaySpeed);
        shaftMaterial.SetFloat(RayFalloffId, best.falloff);
        shaftMaterial.SetFloat(FlickerId, best.flicker);
        shaftMaterial.SetFloat(ShadowBrightnessId, best.shadowScatter);
    }
}
