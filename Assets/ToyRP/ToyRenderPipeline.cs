using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEngine.Profiling;

public class ToyRenderPipeline : RenderPipeline
{
    RenderTexture gdepth;                                               // depth attachment
    RenderTexture[] gbuffers = new RenderTexture[4];                    // color attachments
    RenderTargetIdentifier gdepthID; 
    RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[4]; // tex ID 
    RenderTexture lightPassTex;                                         // 存储 light pass 的结果
    RenderTexture hizBuffer;                                            // hi-z buffer

    Matrix4x4 vpMatrix;
    Matrix4x4 vpMatrixInv;
    Matrix4x4 vpMatrixPrev;     // 上一帧的 vp 矩阵
    Matrix4x4 vpMatrixInvPrev;

    // 噪声图
    public Texture blueNoiseTex;

    // IBL 贴图
    public Cubemap diffuseIBL;
    public Cubemap specularIBL;
    public Texture brdfLut;

    // 阴影管理
    public int shadowMapResolution = 1024;
    public float orthoDistance = 500.0f;
    public float lightSize = 2.0f;
    CSM csm;
    public CsmSettings csmSettings;
    RenderTexture[] shadowTextures = new RenderTexture[4];   // 阴影贴图
    RenderTexture shadowMask;
    RenderTexture shadowStrength;

    // 光照管理
    ClusterLight clusterLight;

    // instance data 数组
    public InstanceData[] instanceDatas;

    public ToyRenderPipeline()
    {
        QualitySettings.vSyncCount = 0;     // 关闭垂直同步
        Application.targetFrameRate = 60;   // 帧率

        // 创建纹理
        gdepth = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        gbuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
        gbuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        gbuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        lightPassTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

        // Hi-z buffer
        int hSize = Mathf.NextPowerOfTwo(Mathf.Max(Screen.width, Screen.height));   // 大小必须是 2 的次幂
        hizBuffer = new RenderTexture(hSize, hSize, 0, RenderTextureFormat.RHalf);
        hizBuffer.autoGenerateMips = false;
        hizBuffer.useMipMap = true;
        hizBuffer.filterMode = FilterMode.Point;

        // 给纹理 ID 赋值
        gdepthID = gdepth;
        for(int i=0; i<4; i++)
            gbufferID[i] = gbuffers[i];
        
        // 创建阴影贴图
        shadowMask = new RenderTexture(Screen.width/4, Screen.height/4, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        shadowStrength = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        for(int i=0; i<4; i++)
            shadowTextures[i] = new RenderTexture(shadowMapResolution, shadowMapResolution, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);

        csm = new CSM();

        clusterLight = new ClusterLight();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        // 主相机
        Camera camera = cameras[0];

        // 全局变量设置
        Shader.SetGlobalFloat("_far", camera.farClipPlane);
        Shader.SetGlobalFloat("_near", camera.nearClipPlane);
        Shader.SetGlobalFloat("_screenWidth", Screen.width);
        Shader.SetGlobalFloat("_screenHeight", Screen.height);
        Shader.SetGlobalTexture("_noiseTex", blueNoiseTex);
        Shader.SetGlobalFloat("_noiseTexResolution", blueNoiseTex.width);

        //  gbuffer 
        Shader.SetGlobalTexture("_gdepth", gdepth);
        Shader.SetGlobalTexture("_hizBuffer", hizBuffer);
        for(int i=0; i<4; i++) 
            Shader.SetGlobalTexture("_GT"+i, gbuffers[i]);

        // 设置相机矩阵
        Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        vpMatrix = projMatrix * viewMatrix;
        vpMatrixInv = vpMatrix.inverse;
        Shader.SetGlobalMatrix("_vpMatrix", vpMatrix);
        Shader.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);
        Shader.SetGlobalMatrix("_vpMatrixPrev", vpMatrixPrev);
        Shader.SetGlobalMatrix("_vpMatrixInvPrev", vpMatrixInvPrev);

        // 设置 IBL 贴图
        Shader.SetGlobalTexture("_diffuseIBL", diffuseIBL);
        Shader.SetGlobalTexture("_specularIBL", specularIBL);
        Shader.SetGlobalTexture("_brdfLut", brdfLut);

        // 设置 CSM 相关参数
        Shader.SetGlobalFloat("_orthoDistance", orthoDistance);
        Shader.SetGlobalFloat("_shadowMapResolution", shadowMapResolution);
        Shader.SetGlobalFloat("_lightSize", lightSize);
        Shader.SetGlobalTexture("_shadowStrength", shadowStrength);
        Shader.SetGlobalTexture("_shadoMask", shadowMask);
        for(int i=0; i<4; i++)
        {
            Shader.SetGlobalTexture("_shadowtex"+i, shadowTextures[i]);
            Shader.SetGlobalFloat("_split"+i, csm.splts[i]);
        }

        bool isEditor = Handles.ShouldRenderGizmos();

        // ------------------------ 管线各个 Pass ------------------------ //

        ClusterLightingPass(context, camera);

        ShadowCastingPass(context, camera);

        GbufferPass(context, camera);

        InstanceDrawPass(context, Camera.main);

        // only generate for main camera
        if(!isEditor)
        {
            HizPass(context, camera);
            vpMatrixPrev = vpMatrix;
        }

        ShadowMappingPass(context, camera);

        LightPass(context, camera);

        //FinalPass(context, camera);

        //

        // ------------------------- Pass end -------------------------- //

        // skybox and Gizmos
        context.DrawSkybox(camera);
        if (isEditor) 
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }

        // 提交绘制命令
        context.Submit();
    }

    void ClusterLightingPass(ScriptableRenderContext context, Camera camera)
    {
        // 裁剪光源
        camera.TryGetCullingParameters(out var cullingParameters);
        var cullingResults = context.Cull(ref cullingParameters);
        
        // 更新光源
        clusterLight.UpdateLightBuffer(cullingResults.visibleLights.ToArray());

        // 划分 cluster
        clusterLight.ClusterGenerate(camera);

        // 分配光源
        clusterLight.LightAssign();

        // 传递参数
        clusterLight.SetShaderParameters();
    }

    // 阴影贴图 pass
    void ShadowCastingPass(ScriptableRenderContext context, Camera camera)
    {
        Profiler.BeginSample("MyPieceOfCode");

        // 获取光源信息
        Light light = RenderSettings.sun;
        Vector3 lightDir = light.transform.rotation * Vector3.forward;

        // 更新 shadowmap 分割
        csm.Update(camera, lightDir, csmSettings);
        csmSettings.Set();

        csm.SaveMainCameraSettings(ref camera);
        for(int level=0; level<4; level++)
        {
            // 将相机移到光源方向
            csm.ConfigCameraToShadowSpace(ref camera, lightDir, level, orthoDistance, shadowMapResolution);

            // 设置阴影矩阵, 视锥分割参数
            Matrix4x4 v = camera.worldToCameraMatrix;
            Matrix4x4 p = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
            Shader.SetGlobalMatrix("_shadowVpMatrix"+level, p * v);
            Shader.SetGlobalFloat("_orthoWidth"+level, csm.orthoWidths[level]);

            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "shadowmap" + level;

            // 绘制前准备
            context.SetupCameraProperties(camera);
            cmd.SetRenderTarget(shadowTextures[level]);
            cmd.ClearRenderTarget(true, true, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            // 剔除
            camera.TryGetCullingParameters(out var cullingParameters);
            var cullingResults = context.Cull(ref cullingParameters);
            // config settings
            ShaderTagId shaderTagId = new ShaderTagId("depthonly");
            SortingSettings sortingSettings = new SortingSettings(camera);
            DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;

            // 绘制
            context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
            context.Submit();   // 每次 set camera 之后立即提交
        }
        csm.RevertMainCameraSettings(ref camera);

        Profiler.EndSample();
    }

    // Gbuffer Pass
    void GbufferPass(ScriptableRenderContext context, Camera camera)
    {
        Profiler.BeginSample("gbufferDraw");

        context.SetupCameraProperties(camera);
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "gbuffer";
        
        // 清屏
        cmd.SetRenderTarget(gbufferID, gdepth);
        cmd.ClearRenderTarget(true, true, Color.clear);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        // 剔除
        camera.TryGetCullingParameters(out var cullingParameters);
        var cullingResults = context.Cull(ref cullingParameters);

        // config settings
        ShaderTagId shaderTagId = new ShaderTagId("gbuffer");   // 使用 LightMode 为 gbuffer 的 shader
        SortingSettings sortingSettings = new SortingSettings(camera);
        DrawingSettings drawingSettings = new DrawingSettings(shaderTagId, sortingSettings);
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;

        // 绘制一般几何体
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
        context.Submit();

        Profiler.EndSample();
    }
    
    // 阴影计算 pass : 输出阴影强度 texture
    void ShadowMappingPass(ScriptableRenderContext context, Camera camera)
    {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "shadowmappingpass";

        RenderTexture tempTex1 = RenderTexture.GetTemporary(Screen.width/4, Screen.height/4, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        RenderTexture tempTex2 = RenderTexture.GetTemporary(Screen.width/4, Screen.height/4, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        RenderTexture tempTex3 = RenderTexture.GetTemporary(Screen.width, Screen.height, 0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);

        if(csmSettings.usingShadowMask)
        {
            // 生成 Mask, 模糊 Mask
            cmd.Blit(gbufferID[0], tempTex1, new Material(Shader.Find("ToyRP/preshadowmappingpass")));
            cmd.Blit(tempTex1, tempTex2, new Material(Shader.Find("ToyRP/blurNx1")));
            cmd.Blit(tempTex2, shadowMask, new Material(Shader.Find("ToyRP/blur1xN")));
        }    

        // 生成阴影, 模糊阴影
        cmd.Blit(gbufferID[0], tempTex3, new Material(Shader.Find("ToyRP/shadowmappingpass")));
        cmd.Blit(tempTex3, shadowStrength, new Material(Shader.Find("ToyRP/blurNxN")));
        
        RenderTexture.ReleaseTemporary(tempTex1);
        RenderTexture.ReleaseTemporary(tempTex2);
        RenderTexture.ReleaseTemporary(tempTex3);

        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }

    // 光照 Pass : 计算 PBR 光照并且存储到 lightPassTex 纹理
    void LightPass(ScriptableRenderContext context, Camera camera)
    {
        // 使用 Blit
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "lightpass";

        Material mat = new Material(Shader.Find("ToyRP/lightpass"));
        cmd.Blit(gbufferID[0], BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);

        context.Submit();
    }

    // 后处理和最终合成 Pass
    void FinalPass(ScriptableRenderContext context, Camera camera)
    {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "finalpass";

        Material mat = new Material(Shader.Find("ToyRP/finalpass"));
        cmd.Blit(lightPassTex, BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }

    // 绘制 instanceData 列表中的所有 instance
    void InstanceDrawPass(ScriptableRenderContext context, Camera camera)
    {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "instance gbuffer";
        cmd.SetRenderTarget(gbufferID, gdepth);

        Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 vp = projMatrix * viewMatrix;

        // 绘制 instance
        ComputeShader cullingCs = FindComputeShader("InstanceCulling"); 
        for(int i=0; i<instanceDatas.Length; i++)
        {
            InstanceDrawer.Draw(instanceDatas[i], Camera.main, cullingCs, vpMatrixPrev, hizBuffer, ref cmd);
        }
        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }

    // hiz pass
    void HizPass(ScriptableRenderContext context, Camera camera)
    {
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "hizpass";

        // 创建纹理
        int size = hizBuffer.width;
        int nMips = (int)Mathf.Log(size, 2);
        RenderTexture[] mips = new RenderTexture[nMips];
        for(int i=0; i<mips.Length; i++)
        {
            int mSize = size / (int)Mathf.Pow(2, i);
            mips[i] = RenderTexture.GetTemporary(mSize, mSize, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
            mips[i].filterMode = FilterMode.Point;
        }
            
        // 生成 mipmap
        Material mat = new Material(Shader.Find("ToyRP/hizBlit"));
        cmd.Blit(gdepth, mips[0]);
        for(int i=1; i<mips.Length; i++)
        {
            cmd.Blit(mips[i-1], mips[i], mat);
        }
            
        // 拷贝到 hizBuffer 的各个 mip
        for(int i=0; i<mips.Length; i++)
        {
            cmd.CopyTexture(mips[i], 0, 0, hizBuffer, 0, i);
            RenderTexture.ReleaseTemporary(mips[i]);
        }

        context.ExecuteCommandBuffer(cmd);
        context.Submit();
    }

    static ComputeShader FindComputeShader(string shaderName)
    {
        ComputeShader[] css = Resources.FindObjectsOfTypeAll(typeof(ComputeShader)) as ComputeShader[];
        for (int i = 0; i < css.Length; i++) 
        { 
            if (css[i].name == shaderName) 
                return css[i];
        }
        return null;
    }
}

