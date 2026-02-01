using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CRTRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class CRTSettings
    {
        public Material material;
        [Range(0, 0.5f)] public float distortion = 0.1f;
        public float scanlineCount = 500f;
        public float scanlineSpeed = 2f;
        [Range(0, 1f)] public float scanlineIntensity = 0.5f;

        [Range(0, 1f)] public float flashIntensity = 0;
        [Range(0, 0.1f)] public float jitter = 0;
    }

    public CRTSettings settings = new CRTSettings();
    CRTPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new CRTPass(settings);
        // 设置渲染时机：在所有透明物体渲染完之后
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.material != null)
        {
            // 将当前的 renderer 传给 Pass 使用，修复你遇到的报错
            m_ScriptablePass.SetRenderer(renderer);
            renderer.EnqueuePass(m_ScriptablePass);
        }
    }

    class CRTPass : ScriptableRenderPass
    {
        private CRTSettings settings;
        private ScriptableRenderer renderer; // 增加一个变量来存储引用
        private RenderTargetHandle tempTexture;

        public CRTPass(CRTSettings settings)
        {
            this.settings = settings;
            tempTexture.Init("_TempCRTTexture");
        }

        // 供 Feature 调用来传递 renderer
        public void SetRenderer(ScriptableRenderer renderer)
        {
            this.renderer = renderer;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // 确保在这里能通过 renderer 获取颜色缓冲
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (renderer == null || settings.material == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("CRT Effect");

            // 获取摄像机的渲染目标（颜色）
            var source = renderer.cameraColorTarget;

            // 更新 Shader 参数
            settings.material.SetFloat("_Distortion", settings.distortion);
            settings.material.SetFloat("_ScanlineCount", settings.scanlineCount);
            settings.material.SetFloat("_ScanlineSpeed", settings.scanlineSpeed);
            settings.material.SetFloat("_ScanlineIntensity", settings.scanlineIntensity);
            settings.material.SetFloat("_FlashIntensity", settings.flashIntensity);
            settings.material.SetFloat("_Jitter", settings.jitter);

            // 获取屏幕描述符，准备临时纹理
            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;
            cmd.GetTemporaryRT(tempTexture.id, opaqueDesc, FilterMode.Bilinear);

            // 后处理核心：Source -> Temp (应用材质) -> Source
            Blit(cmd, source, tempTexture.Identifier(), settings.material);
            Blit(cmd, tempTexture.Identifier(), source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempTexture.id);
        }
    }
}