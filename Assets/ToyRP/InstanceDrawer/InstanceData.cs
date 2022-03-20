using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RenderPipeline/InstanceData")]
[System.Serializable]
public class InstanceData : ScriptableObject
{
    [HideInInspector] public Matrix4x4[] mats;                // 变换矩阵（持久保存）

    [HideInInspector] public ComputeBuffer matrixBuffer;      // 全部实体的变换矩阵             （运行时生成的 GPU buffer）
    [HideInInspector] public ComputeBuffer validMatrixBuffer; // 剔除后剩余 instance 的变换矩阵 （运行时生成的 GPU buffer）
    [HideInInspector] public ComputeBuffer argsBuffer;        // 绘制参数                      （运行时生成的 GPU buffer）

    [HideInInspector] public int subMeshIndex = 0;  // 子网格下标   （持久保存）
    [HideInInspector] public int instanceCount = 0; // instance 数目（持久保存）

    public Mesh instanceMesh;
    public Material instanceMaterial;

    public Vector3 center = new Vector3(0,0,0);
    public int randomInstanceNum = 5000;
    public float distanceMin = 5.0f;
    public float distanceMax = 50.0f;
    public float heightMin = -0.5f;
    public float heightMax = 0.5f;

    // 随机生成
    public void GenerateRandomData()
    {
        instanceCount = randomInstanceNum;

        // 生成变换矩阵
        mats = new Matrix4x4[instanceCount];
        for (int i = 0; i < instanceCount; i++) 
        {
            float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            float distance = Mathf.Sqrt(Random.Range(0.0f, 1.0f)) * (distanceMax - distanceMin) + distanceMin;
            float height = Random.Range(heightMin, heightMax);

            Vector3 pos = new Vector3(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance);
            Vector3 dir = pos - center;

            Quaternion q = new Quaternion();
            q.SetLookRotation(dir, new Vector3(0, 1, 0));

            Matrix4x4 m = Matrix4x4.Rotate(q);
            m.SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1));

            mats[i] = m;
        }
        
        matrixBuffer.Release(); matrixBuffer=null;
        validMatrixBuffer.Release(); validMatrixBuffer=null;
        argsBuffer.Release(); argsBuffer=null;

        Debug.Log("Instance Data Generate Success");
    }
}
