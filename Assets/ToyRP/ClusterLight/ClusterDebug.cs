using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class ClusterDebug : MonoBehaviour
{
    ClusterLight clusterLight;

    void Update()
    {
        if(clusterLight==null) 
        {
            clusterLight = new ClusterLight();
        }

        // 更新光源
        var lights = FindObjectsOfType(typeof(Light)) as Light[];
        clusterLight.UpdateLightBuffer(lights);

        // 划分 cluster
        Camera camera = Camera.main;
        clusterLight.ClusterGenerate(camera);

        // 分配光源
        clusterLight.LightAssign();

        // Debug
        clusterLight.DebugCluster();
        clusterLight.DebugLightAssign();
    }
}
