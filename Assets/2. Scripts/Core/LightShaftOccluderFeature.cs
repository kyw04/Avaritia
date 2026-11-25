using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// order in layer가 기준보다 큰 렌더러만 빛줄기 가림막 마스크(_LightShaftOccluderMask)에 그림. 기준 이하 스프라이트는 빛줄기 뒤에 있는 것처럼 무시됨
public class LightShaftOccluderFeature : ScriptableRendererFeature
{
    // 포스트 프로세스 쪽에선 sorting order로 걸러낼 수 없어서, 가림막 렌더러에 이 렌더링 레이어 비트를 붙여 구분
    private const uint OccluderLayer = 1u << 31;

    [Tooltip("order in layer가 이 값 이하인 렌더러는 빛줄기를 가리지 않음")]
    [SerializeField] private int maxIgnoredOrder = 10;

    private MaskPass pass;

    // Mark는 게임 코드에서 정적으로 불리므로 기준값을 정적으로 보관(에셋 값이 Create에서 덮어씀)
    private static int ignoredOrderThreshold = 10;
    private static readonly List<Renderer> MarkBuffer = new();

    public override void Create()
    {
        ignoredOrderThreshold = maxIgnoredOrder;
        pass = new MaskPass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing) => pass?.Dispose();

    // 매 프레임 씬 전체를 찾으면 배열 할당으로 GC가 생기므로, 씬 로드 때와 오브젝트를 새로 만들 때만 표시한다.
    // 풀에서 재사용되는 인스턴스는 표시가 남아 있어 다시 할 필요 없음. sortingOrder는 Awake에서만 바뀌므로 Instantiate 직후에 부르면 된다.
    // 에디트 모드에서 바꾸면 씬에 저장되므로 플레이 중에만 표시한다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded += (scene, _) =>
        {
            foreach (var root in scene.GetRootGameObjects())
                Mark(root);
        };
    }

    public static void Mark(GameObject root)
    {
        if (!Application.isPlaying || root == null)
            return;

        root.GetComponentsInChildren(true, MarkBuffer);
        foreach (var r in MarkBuffer)
        {
            if (r.sortingOrder > ignoredOrderThreshold)
                r.renderingLayerMask |= OccluderLayer;
            else
                r.renderingLayerMask &= ~OccluderLayer;
        }
        MarkBuffer.Clear();
    }

    private class MaskPass : ScriptableRenderPass
    {
        private static readonly int MaskId = Shader.PropertyToID("_LightShaftOccluderMask");
        private static readonly List<ShaderTagId> ShaderTags = new()
        {
            new ShaderTagId("Universal2D"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("SRPDefaultUnlit"),
        };

        private RTHandle mask;

        private class PassData
        {
            public RendererListHandle rendererList;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var renderingData = frameData.Get<UniversalRenderingData>();
            var cameraData = frameData.Get<UniversalCameraData>();
            var lightData = frameData.Get<UniversalLightData>();

            var desc = cameraData.cameraTargetDescriptor;
            desc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm;
            desc.depthStencilFormat = GraphicsFormat.None;
            desc.msaaSamples = 1;
            RenderingUtils.ReAllocateHandleIfNeeded(ref mask, desc, FilterMode.Point, TextureWrapMode.Clamp, name: "_LightShaftOccluderMask");

            // 대체 셰이더로 그리면 2D 렌더러가 스프라이트 텍스처를 제대로 바인딩하지 않아 실루엣이 빠지므로,
            // 원래 머티리얼 그대로 그리고 알파 채널만 마스크로 사용
            var drawSettings = RenderingUtils.CreateDrawingSettings(ShaderTags, renderingData, cameraData, lightData, SortingCriteria.None);
            var filterSettings = new FilteringSettings(RenderQueueRange.all, -1, OccluderLayer);

            using var builder = renderGraph.AddRasterRenderPass<PassData>("LightShaftOccluderMask", out var passData);
            var maskHandle = renderGraph.ImportTexture(mask);
            passData.rendererList = renderGraph.CreateRendererList(new RendererListParams(renderingData.cullResults, drawSettings, filterSettings));

            builder.UseRendererList(passData.rendererList);
            builder.SetRenderAttachment(maskHandle, 0);
            builder.SetGlobalTextureAfterPass(maskHandle, MaskId);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc((PassData data, RasterGraphContext ctx) =>
            {
                ctx.cmd.ClearRenderTarget(false, true, Color.clear);
                ctx.cmd.DrawRendererList(data.rendererList);
            });
        }

        public void Dispose() => mask?.Release();
    }
}
