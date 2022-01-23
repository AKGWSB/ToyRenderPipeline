using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

public class ToyRenderPipeline : RenderPipeline
{
    RenderTexture gdepth;                                               // depth attachment
    RenderTexture[] gbuffers = new RenderTexture[4];                    // color attachments 
    RenderTargetIdentifier[] gbufferID = new RenderTargetIdentifier[4]; // tex ID 
    RenderTexture lightPassTex;                                         // 存储 light pass 的结果

    // IBL 贴图
    public Cubemap diffuseIBL;
    public Cubemap specularIBL;
    public Texture brdfLut;

    // 阴影管理
    public int shadowMapResolution = 1024;
    public float orthoDistance = 500.0f;
    CSM csm;
    RenderTexture[] shadowTextures = new RenderTexture[4];   // 阴影贴图

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

        // 给纹理 ID 赋值
        for(int i=0; i<4; i++)
            gbufferID[i] = gbuffers[i];
        
        // 创建阴影贴图
        for(int i=0; i<4; i++)
            //shadowTextures[i] = new RenderTexture(shadowMapResolution, shadowMapResolution, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            shadowTextures[i] = new RenderTexture(shadowMapResolution, shadowMapResolution, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);

        csm = new CSM();
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)  
    {
        // 主相机
        Camera camera = cameras[0];

        // 全局变量设置
        //  gbuffer 
        Shader.SetGlobalTexture("_gdepth", gdepth);
        for(int i=0; i<4; i++) 
            Shader.SetGlobalTexture("_GT"+i, gbuffers[i]);

        // 设置相机矩阵
        Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 vpMatrix = projMatrix * viewMatrix;
        Matrix4x4 vpMatrixInv = vpMatrix.inverse;
        Shader.SetGlobalMatrix("_vpMatrix", vpMatrix);
        Shader.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);

        // 设置 IBL 贴图
        Shader.SetGlobalTexture("_diffuseIBL", diffuseIBL);
        Shader.SetGlobalTexture("_specularIBL", specularIBL);
        Shader.SetGlobalTexture("_brdfLut", brdfLut);

        // 设置 CSM 相关参数
        Shader.SetGlobalFloat("_orthoDistance", orthoDistance);
        Shader.SetGlobalFloat("_shadowMapResolution", shadowMapResolution);
        Shader.SetGlobalFloat("_lightSize", 5.0f);
        for(int i=0; i<4; i++)
        {
            Shader.SetGlobalTexture("_shadowtex"+i, shadowTextures[i]);
            Shader.SetGlobalFloat("_split"+i, csm.splts[i]);
        }

        // ------------------------ 管线各个 Pass ------------------------ //

        ShadowPass(context, camera);

        GbufferPass(context, camera);

        LightPass(context, camera);

        //FinalPass(context, camera);

        // ------------------------- Pass end -------------------------- //

        // skybox and Gizmos
        context.DrawSkybox(camera);
        if (Handles.ShouldRenderGizmos()) 
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }

        // 提交绘制命令
        context.Submit();
    }

    // 阴影贴图 pass
    void ShadowPass(ScriptableRenderContext context, Camera camera)
    {
        // 获取光源信息
        Light light = RenderSettings.sun;
        Vector3 lightDir = light.transform.rotation * Vector3.forward;

        // 更新 shadowmap 分割
        csm.Update(camera, lightDir);

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
            
            cmd.BeginSample("shadowDraw"+level);
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

            cmd.EndSample("shadowDraw"+level);
            context.ExecuteCommandBuffer(cmd);

            context.Submit();   // 每次 set camera 之后立即提交
        }
        csm.RevertMainCameraSettings(ref camera);
    }

    // Gbuffer Pass
    void GbufferPass(ScriptableRenderContext context, Camera camera)
    {
        context.SetupCameraProperties(camera);

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "gbuffer";
        
        // 清屏
        cmd.SetRenderTarget(gbufferID, gdepth);
        cmd.ClearRenderTarget(true, true, Color.clear);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        cmd.BeginSample("gbufferDraw");
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

        // 绘制
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        cmd.EndSample("gbufferDraw");
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
    }
}

