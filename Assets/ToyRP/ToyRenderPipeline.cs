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

    public ToyRenderPipeline()
    {
        // 创建纹理
        gdepth  = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        gbuffers[0] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[1] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
        gbuffers[2] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        gbuffers[3] = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
        lightPassTex = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

        // 给纹理 ID 赋值
        for(int i=0; i<4; i++)
            gbufferID[i] = gbuffers[i];
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)  
    {
        // 主相机
        Camera camera = cameras[0];
        context.SetupCameraProperties(camera);

        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "gbuffer";

        // 清屏
        cmd.SetRenderTarget(gbufferID, gdepth);
        cmd.ClearRenderTarget(true, true, Color.clear);

        // 设置 gbuffer 为全局纹理
        cmd.SetGlobalTexture("_gdepth", gdepth);
        for(int i=0; i<4; i++) 
            cmd.SetGlobalTexture("_GT"+i, gbuffers[i]);

        // 设置相机矩阵
        Matrix4x4 viewMatrix = camera.worldToCameraMatrix;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false);
        Matrix4x4 vpMatrix = projMatrix * viewMatrix;
        Matrix4x4 vpMatrixInv = vpMatrix.inverse;
        cmd.SetGlobalMatrix("_vpMatrix", vpMatrix);
        cmd.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);

        // 设置 IBL 贴图
        cmd.SetGlobalTexture("_diffuseIBL", diffuseIBL);
        cmd.SetGlobalTexture("_specularIBL", specularIBL);
        cmd.SetGlobalTexture("_brdfLut", brdfLut);

        context.ExecuteCommandBuffer(cmd);

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

        LightPass(context, camera);
        //FinalPass(context, camera);

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

    // 光照 Pass : 计算 PBR 光照并且存储到 lightPassTex 纹理
    void LightPass(ScriptableRenderContext context, Camera camera)
    {
        // 使用 Blit
        CommandBuffer cmd = new CommandBuffer();
        cmd.name = "lightpass";

        Material mat = new Material(Shader.Find("ToyRP/lightpass"));
        cmd.Blit(gbufferID[0], BuiltinRenderTextureType.CameraTarget, mat);
        context.ExecuteCommandBuffer(cmd);
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

