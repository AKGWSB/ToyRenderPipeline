using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class InstanceDrawer
{
    // 如果 GPU buffer 未被创建，那么创建它
    public static void CheckAndInit(InstanceData idata)
    {
        if(idata.matrixBuffer!=null && idata.validMatrixBuffer!=null && idata.argsBuffer!=null) return;

        int sizeofMatrix4x4 = 4 * 4 * 4;
        idata.matrixBuffer = new ComputeBuffer(idata.instanceCount, sizeofMatrix4x4);
        idata.validMatrixBuffer = new ComputeBuffer(idata.instanceCount, sizeofMatrix4x4, ComputeBufferType.Append);
        idata.argsBuffer = new ComputeBuffer(5, sizeof(uint), ComputeBufferType.IndirectArguments);

        // 传变换矩阵到 GPU
        idata.matrixBuffer.SetData(idata.mats);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 }; // 绘制参数
        if (idata.instanceMesh != null) 
        {
            args[0] = (uint)idata.instanceMesh.GetIndexCount(idata.subMeshIndex);
            args[1] = (uint)0;
            args[2] = (uint)idata.instanceMesh.GetIndexStart(idata.subMeshIndex);
            args[3] = (uint)idata.instanceMesh.GetBaseVertex(idata.subMeshIndex);
        }
        idata.argsBuffer.SetData(args);
    }

    // All-in drawing
    public static void Draw(InstanceData idata)
    {
        if(idata==null) return;
        CheckAndInit(idata);

        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        idata.argsBuffer.GetData(args);
        args[1] = (uint)idata.instanceCount;
        idata.argsBuffer.SetData(args);

        idata.instanceMaterial.SetBuffer("_validMatrixBuffer", idata.matrixBuffer);

        Graphics.DrawMeshInstancedIndirect(
            idata.instanceMesh, 
            idata.subMeshIndex, 
            idata.instanceMaterial, 
            new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)),
            idata.argsBuffer);
    }

    public static Vector4[] BoundToPoint(Bounds b)
    {
        Vector4[] boundingBox = new Vector4[8];
        boundingBox[0] = new Vector4(b.min.x, b.min.y, b.min.z, 1);
        boundingBox[1] = new Vector4(b.max.x, b.max.y, b.max.z, 1);
        boundingBox[2] = new Vector4(boundingBox[0].x, boundingBox[0].y, boundingBox[1].z, 1);
        boundingBox[3] = new Vector4(boundingBox[0].x, boundingBox[1].y, boundingBox[0].z, 1);
        boundingBox[4] = new Vector4(boundingBox[1].x, boundingBox[0].y, boundingBox[0].z, 1);
        boundingBox[5] = new Vector4(boundingBox[0].x, boundingBox[1].y, boundingBox[1].z, 1);
        boundingBox[6] = new Vector4(boundingBox[1].x, boundingBox[0].y, boundingBox[1].z, 1);
        boundingBox[7] = new Vector4(boundingBox[1].x, boundingBox[1].y, boundingBox[0].z, 1);
        return boundingBox;
    }

    // frustum culling
    public static void Draw(InstanceData idata, Camera camera, ComputeShader cs)
    {
        if(idata==null || camera==null || cs==null) return;
        CheckAndInit(idata);

        // 清空绘制计数
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        idata.argsBuffer.GetData(args);
        args[1] = 0;
        idata.argsBuffer.SetData(args);
        idata.validMatrixBuffer.SetCounterValue(0);  
        
        // 计算视锥体平面
        Plane[] ps = GeometryUtility.CalculateFrustumPlanes(camera);
        Vector4[] planes = new Vector4[6];
        for(int i=0; i<6; i++)
        {
            // Ax+By+Cz+D --> Vec4(A,B,C,D)
            planes[i] = new Vector4(ps[i].normal.x, ps[i].normal.y, ps[i].normal.z, ps[i].distance);
        }

        // 计算 bounding box
        Vector4[] bounds = BoundToPoint(idata.instanceMesh.bounds);
        
        // 传送参数到 compute shader
        int kid = cs.FindKernel("CSMain");
        cs.SetVectorArray("_bounds", bounds);
        cs.SetVectorArray("_planes", planes);
        cs.SetInt("_instanceCount", idata.instanceCount);
        cs.SetBuffer(kid, "_matrixBuffer", idata.matrixBuffer);
        cs.SetBuffer(kid, "_validMatrixBuffer", idata.validMatrixBuffer);
        cs.SetBuffer(kid, "_argsBuffer", idata.argsBuffer);

        // 视锥剔除
        int nDispatch = (idata.instanceCount / 128) + 1; // 128 个 instance 一组线程
        cs.Dispatch(kid, nDispatch, 1, 1);

        idata.instanceMaterial.SetBuffer("_validMatrixBuffer", idata.validMatrixBuffer);

        Graphics.DrawMeshInstancedIndirect(
            idata.instanceMesh, 
            idata.subMeshIndex, 
            idata.instanceMaterial, 
            new Bounds(Vector3.zero, new Vector3(100.0f, 100.0f, 100.0f)),
            idata.argsBuffer);
    }

    public static void Draw(InstanceData idata, Camera camera, ComputeShader cs, Matrix4x4 vpMatrix, RenderTexture hizBuffer, ref CommandBuffer cmd)
    {
        if(idata==null || camera==null || cs==null) return;
        CheckAndInit(idata);

        // 清空绘制计数
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        idata.argsBuffer.GetData(args);
        args[1] = 0;
        idata.argsBuffer.SetData(args);
        idata.validMatrixBuffer.SetCounterValue(0);  

        Plane[] ps = GeometryUtility.CalculateFrustumPlanes(camera);
        Vector4[] planes = new Vector4[6];
        for(int i=0; i<6; i++)
        {
            // Ax+By+Cz+D --> Vec4(A,B,C,D)
            planes[i] = new Vector4(ps[i].normal.x, ps[i].normal.y, ps[i].normal.z, ps[i].distance);
        }
        
        // 计算 bounding box
        Vector4[] bounds = BoundToPoint(idata.instanceMesh.bounds);
        
        // 传送参数到 shader
        int kid = cs.FindKernel("CSMain");
        cs.SetMatrix("_vpMatrix", vpMatrix);
        cs.SetVectorArray("_bounds", bounds);
        cs.SetVectorArray("_planes", planes);
        cs.SetInt("_size", hizBuffer.width);
        cs.SetInt("_instanceCount", idata.instanceCount);
        cs.SetBuffer(kid, "_matrixBuffer", idata.matrixBuffer);
        cs.SetBuffer(kid, "_validMatrixBuffer", idata.validMatrixBuffer);
        cs.SetBuffer(kid, "_argsBuffer", idata.argsBuffer);
        cs.SetTexture(kid, "_hizBuffer", hizBuffer);
        idata.instanceMaterial.SetBuffer("_validMatrixBuffer", idata.validMatrixBuffer);

        // 剔除
        int nDispatch = (int)Mathf.Ceil((float)idata.instanceCount / 128); // 128 个 instance 一组线程
        cs.Dispatch(kid, nDispatch, 1, 1);

        cmd.DrawMeshInstancedIndirect(
            idata.instanceMesh, 
            idata.subMeshIndex, 
            idata.instanceMaterial, 
            -1,
            idata.argsBuffer);
    }

    /*
    public static void Draw(InstanceData idata, Camera camera, ComputeShader cs, Matrix4x4 vpMatrix, RenderTexture hizBuffer, ref CommandBuffer cmd)
    {
        if(idata==null || camera==null || cs==null) return;
        CheckAndInit(idata);

        // 清空绘制计数
        uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
        idata.argsBuffer.GetData(args);
        args[1] = 0;
        idata.argsBuffer.SetData(args);
        idata.validMatrixBuffer.SetCounterValue(0);  
        
        // 计算视锥体平面
        Plane[] ps = GeometryUtility.CalculateFrustumPlanes(camera);
        Vector4[] planes = new Vector4[6];
        for(int i=0; i<6; i++)
        {
            // Ax+By+Cz+D --> Vec4(A,B,C,D)
            planes[i] = new Vector4(ps[i].normal.x, ps[i].normal.y, ps[i].normal.z, ps[i].distance);
        }

        // 计算 bounding box
        Vector4[] bounds = BoundToPoint(idata.instanceMesh.bounds);
        
        // 传送参数到 shader
        int kid = cs.FindKernel("CSMain");
        cs.SetMatrix("_vpMatrix", vpMatrix);
        cs.SetVectorArray("_bounds", bounds);
        cs.SetVectorArray("_planes", planes);
        cs.SetInt("_size", hizBuffer.width);
        cs.SetInt("_instanceCount", idata.instanceCount);
        cs.SetBuffer(kid, "_matrixBuffer", idata.matrixBuffer);
        cs.SetBuffer(kid, "_validMatrixBuffer", idata.validMatrixBuffer);
        cs.SetBuffer(kid, "_argsBuffer", idata.argsBuffer);
        cs.SetTexture(kid, "_hizBuffer", hizBuffer);
        idata.instanceMaterial.SetBuffer("_validMatrixBuffer", idata.validMatrixBuffer);

        // 剔除
        int nDispatch = (idata.instanceCount / 128) + 1; // 128 个 instance 一组线程
        cs.Dispatch(kid, nDispatch, 1, 1);

        cmd.DrawMeshInstancedIndirect(
            idata.instanceMesh, 
            idata.subMeshIndex, 
            idata.instanceMaterial, 
            -1,
            idata.argsBuffer);
    }
    */
}
