using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class InstanceDebug : MonoBehaviour
{
    public InstanceData idata;
    public ComputeShader cs;
    public Camera camera;

    public bool usingCulling = false;

    ComputeShader FindComputeShader(string shaderName)
    {
        ComputeShader[] css = Resources.FindObjectsOfTypeAll(typeof(ComputeShader)) as ComputeShader[];
        for (int i = 0; i < css.Length; i++) 
        { 
            if (css[i].name == shaderName) 
                return css[i];
        }
        return null;
    }

    // Update is called once per frame
    void Update()
    {
        if(camera==null) camera = Camera.main;
        if(cs==null) cs = FindComputeShader("InstanceCulling");

        if(usingCulling)
            InstanceDrawer.Draw(idata, camera, cs);
        else
            InstanceDrawer.Draw(idata);
    }
}
