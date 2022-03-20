Shader "ToyRP/shadowmappingpass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "globaluniform.cginc"
            #include "UnityCG.cginc"
            #include "BRDF.cginc"
            #include "shadow.cginc"
            #include "UnityLightingCommon.cginc"
            #include "random.cginc"            

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }           

            float frag (v2f i) : SV_Target
            {
                // 从 Gbuffer 解码数据
                float2 uv = i.uv; 
                float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                float d_lin = Linear01Depth(d);

                // 反投影重建世界坐标
                float4 ndcPos = float4(uv*2-1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;

                // 向着法线偏移采样点
                float4 worldPosOffset = worldPos;
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float NdotL = clamp(dot(lightDir, normal), 0, 1);

                if(NdotL < 0.005) return NdotL;

                // 随机旋转角度
                uint seed = RandomSeed(uv, float2(_screenWidth, _screenHeight));
                float2 uv_noi = uv * float2(_screenWidth, _screenHeight) / _noiseTexResolution;
                float rotateAngle = rand(seed) * 2.0 * 3.1415926;
                rotateAngle = tex2D(_noiseTex, uv_noi*0.5).r * 2.0 * 3.1415926;   // blue noise

                // 开启 Shadow Mask 优化
                if(_usingShadowMask)
                {
                    float mask = tex2D(_shadoMask, uv).r;
                    if(0.0000005>mask) return 0;
                    if(mask>0.9999995) return 1;
                }

                float shadow = 1.0;
                float csmLevel = d_lin * (_far - _near) / _csmMaxDistance;
                if(csmLevel<_split0) 
                {
                    worldPosOffset.xyz += normal * _shadingPointNormalBias0;
                    float bias = (1 * _orthoWidth0 / _shadowMapResolution) * _depthNormalBias0;
                    
                    //color *= float3(1.5, 0.5, 0.5);
                    //shadow *= ShadowMap01(worldPosOffset, _shadowtex0, _shadowVpMatrix0, bias);
                    //shadow *= PCF3x3(worldPosOffset, _shadowtex0, _shadowVpMatrix0, _shadowMapResolution, 0); 
                    shadow *= ShadowMapPCSS(worldPosOffset, _shadowtex0, _shadowVpMatrix0, _orthoWidth0, _orthoDistance, _shadowMapResolution, rotateAngle, _pcssSearchRadius0, _pcssFilterRadius0, bias);
                }
                else if(csmLevel<_split0+_split1)
                {
                    worldPosOffset.xyz += normal * _shadingPointNormalBias1;
                    float bias = (1 * _orthoWidth1 / _shadowMapResolution) * _depthNormalBias1;

                    //color *= float3(0.5, 1.5, 0.5);
                    //shadow *= ShadowMap01(worldPosOffset, _shadowtex1, _shadowVpMatrix1, bias);
                    //shadow *= PCF3x3(worldPos, _shadowtex1, _shadowVpMatrix1, _shadowMapResolution, bias);
                    shadow *= ShadowMapPCSS(worldPosOffset, _shadowtex1, _shadowVpMatrix1, _orthoWidth1, _orthoDistance, _shadowMapResolution, rotateAngle, _pcssSearchRadius1, _pcssFilterRadius1, bias);
                }
                else if(csmLevel<_split0+_split1+_split2) 
                {   
                    worldPosOffset.xyz += normal * _shadingPointNormalBias2;
                    float bias = (1 * _orthoWidth2 / _shadowMapResolution) * _depthNormalBias2;

                    //color *= float3(0.5, 0.5, 1.5);
                    shadow *= ShadowMap01(worldPosOffset, _shadowtex2, _shadowVpMatrix2, bias);
                    //shadow *= ShadowMapPCSS(worldPosOffset, _shadowtex3, _shadowVpMatrix3, _orthoWidth3, _orthoDistance, _shadowMapResolution, rotateAngle, _pcssSearchRadius3, _pcssFilterRadius3, bias);
                }
                else if(csmLevel<_split0+_split1+_split2+_split3)
                {
                    worldPosOffset.xyz += normal * _shadingPointNormalBias3;
                    float bias = (1 * _orthoWidth3 / _shadowMapResolution) * _depthNormalBias3;

                    shadow *= ShadowMap01(worldPosOffset, _shadowtex3, _shadowVpMatrix3, bias);
                    //shadow *= ShadowMapPCSS(worldPosOffset, _shadowtex3, _shadowVpMatrix3, _orthoWidth3, _orthoDistance, _shadowMapResolution);
                }

                return shadow;
            }
            ENDCG
        }
    }
}
