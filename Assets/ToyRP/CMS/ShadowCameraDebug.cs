using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 这个脚本挂相机上
[ExecuteAlways]
public class ShadowCameraDebug : MonoBehaviour
{
    CSM csm;

    void Update()
    {
        Camera mainCam = Camera.main;

        // 获取光源信息
        Light light = RenderSettings.sun;
        Vector3 lightDir = light.transform.rotation * Vector3.forward;

        // 更新 shadowmap
        if(csm==null) csm = new CSM();
        csm.Update(mainCam, lightDir);
        csm.DebugDraw();
    }
}
