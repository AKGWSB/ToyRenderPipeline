using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CsmSettings
{
    public bool usingShadowMask = false;
    public ShadowSettings level0;
    public ShadowSettings level1;
    public ShadowSettings level2;
    public ShadowSettings level3;

    public void Set()
    {
        ShadowSettings[] levels = {level0, level1, level2, level3};
        for(int i=0; i<4; i++)
        {
            Shader.SetGlobalFloat("_shadingPointNormalBias"+i, levels[i].shadingPointNormalBias);
            Shader.SetGlobalFloat("_depthNormalBias"+i, levels[i].depthNormalBias);
            Shader.SetGlobalFloat("_pcssSearchRadius"+i, levels[i].pcssSearchRadius);
            Shader.SetGlobalFloat("_pcssFilterRadius"+i, levels[i].pcssFilterRadius);
        }
        Shader.SetGlobalFloat("_usingShadowMask", usingShadowMask ? 1.0f : 0.0f);
    }
}
