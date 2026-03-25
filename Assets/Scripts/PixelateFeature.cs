// Cách dùng:
// 1. Chọn file PC_Renderer trong Assets/Settings
// 2. Inspector → Add Renderer Feature → Pixelate Feature
// 3. Chỉnh Pixel Size và Strength
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class PixelateFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Tooltip("Mỗi art pixel chiếm bao nhiêu screen pixel. 1 = không đổi, 4 = vuông to")]
        [Min(1)] public int pixelSize = 4;

        [Tooltip("0 = không đổi, 1 = full pixel art")]
        [Range(0f, 1f)] public float strength = 1f;

        public RenderPassEvent passEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public Settings settings = new Settings();

    PixelatePass _pass;
    Material     _material;

    public override void Create()
    {
        var shader = Shader.Find("Hidden/Pixelate");
        if (shader == null)
        {
            Debug.LogWarning("[PixelateFeature] Không tìm thấy shader 'Hidden/Pixelate'.");
            return;
        }
        _material = CoreUtils.CreateEngineMaterial(shader);
        _pass     = new PixelatePass(_material) { renderPassEvent = settings.passEvent };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
                                         ref RenderingData  renderingData)
    {
        if (_material == null || _pass == null) return;
        if (renderingData.cameraData.cameraType == CameraType.Preview) return;

        _pass.Setup(settings.pixelSize, settings.strength);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
class PixelatePass : ScriptableRenderPass
{
    static readonly int s_PixelSize = Shader.PropertyToID("_PixelSize");
    static readonly int s_Strength  = Shader.PropertyToID("_Strength");

    readonly Material _mat;

    public PixelatePass(Material mat)
    {
        _mat             = mat;
        profilingSampler = new ProfilingSampler("Pixelate");
    }

    public void Setup(int pixelSize, float strength)
    {
        _mat.SetFloat(s_PixelSize, pixelSize);
        _mat.SetFloat(s_Strength,  strength);
    }

    // ── RenderGraph (URP 17 / Unity 6) ───────────────────────────────────
    class PassData
    {
        internal TextureHandle src;
        internal TextureHandle dst;
        internal Material      material;
    }

    public override void RecordRenderGraph(RenderGraph      renderGraph,
                                           ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();

        if (resourceData.isActiveTargetBackBuffer) return;

        var srcTexture = resourceData.activeColorTexture;

        // tạo temp texture cùng kích thước
        var desc         = renderGraph.GetTextureDesc(srcTexture);
        desc.name        = "_PixelateTempTex";
        desc.clearBuffer = false;
        var dstTexture   = renderGraph.CreateTexture(desc);

        // Pass 1: pixelate source → temp
        using (var builder = renderGraph.AddUnsafePass<PassData>("Pixelate", out var passData))
        {
            passData.src      = srcTexture;
            passData.dst      = dstTexture;
            passData.material = _mat;

            builder.UseTexture(srcTexture);
            builder.UseTexture(dstTexture, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                Blitter.BlitCameraTexture(cmd, data.src, data.dst, data.material, 0);
            });
        }

        // Pass 2: copy temp → source (ping-pong vì activeColorTexture là read-only)
        using (var builder = renderGraph.AddUnsafePass<PassData>("Pixelate CopyBack", out var passData))
        {
            passData.src      = dstTexture;
            passData.dst      = srcTexture;
            passData.material = null;

            builder.UseTexture(dstTexture);
            builder.UseTexture(srcTexture, AccessFlags.Write);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc((PassData data, UnsafeGraphContext ctx) =>
            {
                var cmd = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
                Blitter.BlitCameraTexture(cmd, data.src, data.dst);
            });
        }
    }
}
