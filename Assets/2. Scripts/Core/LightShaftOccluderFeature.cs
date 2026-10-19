using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

// order in layer가 기준보다 큰 렌더러만 빛줄기 가림막 마스크(_LightShaftOccluderMask)에 그림. 기준 이하 스프라이트는 빛줄기 뒤에 있는 것처럼 무시됨
public class LightShaftOccluderFeature : ScriptableRendererFeature
{
    // 포스트 프로세스 쪽에선 sorting order로 걸러낼 수 없어서, 가림막 렌더러에 이 렌더링 레이어 비트를 붙여 구분
    private const uint OccluderLayer = 1u << 31;

    [Tooltip("order in layer가 이 값 이하인 렌더러는 빛줄기를 가리지 않음")]
    [SerializeField] private int maxIgnoredOrder = 10;

    private MaskPass pass;
    private int markedFrame = -1;

    public override void Create()
    {
        pass = new MaskPass { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        MarkOccluders();
        renderer.EnqueuePass(pass);
    }

    protected override void Dispose(bool disposing) => pass?.Dispose();

    // 에디트 모드에서 바꾸면 씬에 저장되므로 플레이 중에만, 프레임당 한 번 표시
    private void MarkOccluders()
    {
        if (!Application.isPlaying || markedFrame == Time.frameCount)
            return;
        markedFrame = Time.frameCount;

        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r.sortingOrder > maxIgnoredOrder)
                r.renderingLayerMask |= OccluderLayer;
            else
                r.renderingLayerMask &= ~OccluderLayer;
        }
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
