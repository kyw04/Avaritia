using UnityEngine;
using UnityEngine.Rendering;

// 화면 기준이던 빛줄기 원점을 월드의 LightShaftSource 위치에 고정하고, 그 소스의 빛줄기 설정을 적용. 화면에서 멀어지면 서서히 끔
// 에디트 모드에서도 실행해서 LightShaftSource 값을 바꾸면 바로 반영되고, 에디터에서 선택한 LightShaftSource는 거리와 상관없이 보여줌
[ExecuteAlways]
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

    private void OnEnable()
    {
        if (shaftMaterial == null)
            return;
        original = new Material(shaftMaterial);
        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
#if UNITY_EDITOR
        UnityEditor.Selection.selectionChanged += RepaintGameView;
#endif
    }

    // 플레이 중이나 에디터 미리보기에서 바꾼 값이 머티리얼 에셋에 남지 않도록 복구
    private void OnDisable()
    {
        if (original == null)
            return;
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
#if UNITY_EDITOR
        UnityEditor.Selection.selectionChanged -= RepaintGameView;
#endif
#if UNITY_EDITOR
        bool wasDirty = UnityEditor.EditorUtility.IsDirty(shaftMaterial);
        shaftMaterial.CopyPropertiesFromMaterial(original);
        if (!Application.isPlaying && !wasDirty)
            UnityEditor.EditorUtility.ClearDirty(shaftMaterial);
#else
        shaftMaterial.CopyPropertiesFromMaterial(original);
#endif
        if (Application.isPlaying)
            Destroy(original);
        else
            DestroyImmediate(original);
    }

    // 카메라마다 렌더 직전에 그 카메라 기준으로 값을 넣어야 Game 뷰와 Scene 뷰 모두 빛줄기 위치가 맞음
    private void OnBeginCameraRendering(ScriptableRenderContext context, Camera target)
    {
        if (target != cam && target.cameraType != CameraType.SceneView)
            return;

#if UNITY_EDITOR
        // 에디터 미리보기로 바꾼 값이 Ctrl+S로 머티리얼 파일에 저장되지 않도록, 원래 저장할 게 없었으면 dirty 표시를 지움
        bool wasDirty = UnityEditor.EditorUtility.IsDirty(shaftMaterial);
        ApplyBestSource(target);
        if (!Application.isPlaying && !wasDirty)
            UnityEditor.EditorUtility.ClearDirty(shaftMaterial);
#else
        ApplyBestSource(target);
#endif
    }

    private void ApplyBestSource(Camera target)
    {
        LightShaftSource selected = SelectedSource();
        LightShaftSource best = null;
        Vector3 bestPos = Vector3.zero;
        float bestIntensity = 0f;

        foreach (var source in LightShaftSource.All)
        {
            if (selected != null && source != selected)
                continue;

            Vector3 vp = target.WorldToViewportPoint(source.transform.position);
            float dist = Mathf.Max(Mathf.Abs(vp.x - 0.5f), Mathf.Abs(vp.y - 0.5f));
            float fade = source == selected ? 1f : 1f - Mathf.InverseLerp(fadeStart, fadeEnd, dist);
            float value = source.intensity * fade;
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

    // 하이어라키에서 LightShaftSource를 선택하면 그 빛만 거리와 상관없이 보여줌
    private static LightShaftSource SelectedSource()
    {
#if UNITY_EDITOR
        var go = UnityEditor.Selection.activeGameObject;
        return go != null ? go.GetComponent<LightShaftSource>() : null;
#else
        return null;
#endif
    }

#if UNITY_EDITOR
    // 선택만 바뀌면 Game 뷰가 다시 그려지지 않아서 직접 갱신 요청
    private static void RepaintGameView() => UnityEditor.EditorApplication.QueuePlayerLoopUpdate();
#endif
}
