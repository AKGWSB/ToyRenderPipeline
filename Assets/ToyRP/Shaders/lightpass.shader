Shader "ToyRP/lightpass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull Off ZWrite On ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "BRDF.cginc"
            #include "shadow.cginc"
            #include "UnityLightingCommon.cginc"

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

            sampler2D _gdepth;
            sampler2D _GT0;
            sampler2D _GT1;
            sampler2D _GT2;
            sampler2D _GT3;

            samplerCUBE _diffuseIBL;
            samplerCUBE _specularIBL;
            sampler2D _brdfLut;

            sampler2D _shadowtex0;
            sampler2D _shadowtex1;
            sampler2D _shadowtex2;
            sampler2D _shadowtex3;

            float4x4 _vpMatrix;
            float4x4 _vpMatrixInv;

            float4x4 _shadowVpMatrix0;
            float4x4 _shadowVpMatrix1;
            float4x4 _shadowVpMatrix2;
            float4x4 _shadowVpMatrix3;

            float _split0;
            float _split1;
            float _split2;
            float _split3;

            float _orthoWidth0;
            float _orthoWidth1;
            float _orthoWidth2;
            float _orthoWidth3;

            float _orthoDistance;
            float _shadowMapResolution;
            float _lightSize;

            fixed4 frag (v2f i, out float depthOut : SV_Depth) : SV_Target
            {
                float2 uv = i.uv;
                float4 GT2 = tex2D(_GT2, uv);
                float4 GT3 = tex2D(_GT3, uv);

                // 从 Gbuffer 解码数据
                float3 albedo = tex2D(_GT0, uv).rgb;
                float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                float2 motionVec = GT2.rg;
                float roughness = GT2.b;
                float metallic = GT2.a;
                float3 emission = GT3.rgb;
                float occlusion = GT3.a;

                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                float d_lin = Linear01Depth(d);
                depthOut = d;

                // 反投影重建世界坐标
                float4 ndcPos = float4(uv*2-1, d, 1);
                float4 worldPos = mul(_vpMatrixInv, ndcPos);
                worldPos /= worldPos.w;

                // 计算参数
                float3 color = float3(0, 0, 0);
                float3 N = normalize(normal);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos.xyz);
                float3 radiance = _LightColor0.rgb;

                // 计算直接光照
                float3 direct = PBR(N, V, L, albedo, radiance, roughness, metallic);

                // 计算环境光照
                float3 ambient = IBL(
                    N, V,
                    albedo, roughness, metallic,
                    _diffuseIBL, _specularIBL, _brdfLut
                );

                color += ambient * occlusion;
                color += emission;

                //return float4(color, 1.0);

                // 阴影
                //float bias = max(0.0002 * (1.0 - dot(N, L)), 0.0001);
                //bias = 0.0001*tan(acos(abs(dot(N, L))));

                // 向着法线偏移采样点
                float4 worldPosOffset = worldPos;
                worldPosOffset.xyz += normal * 0.1;

                /*
                float shadow0 = ShadowMap01(worldPosOffset, _shadowtex0, _shadowVpMatrix0);
                float shadow1 = ShadowMap01(worldPosOffset, _shadowtex1, _shadowVpMatrix1);
                float shadow2 = ShadowMap01(worldPosOffset, _shadowtex2, _shadowVpMatrix2);
                float shadow3 = ShadowMap01(worldPosOffset, _shadowtex3, _shadowVpMatrix3);
                
                
                shadow0 = ShadowMap01_3x3(worldPosOffset, _shadowtex0, _shadowVpMatrix0, 0, 1024);
                shadow1 = ShadowMap01_3x3(worldPosOffset, _shadowtex1, _shadowVpMatrix1, 0, 1024);
                shadow2 = ShadowMap01_3x3(worldPosOffset, _shadowtex2, _shadowVpMatrix2, 0, 1024);
                shadow3 = ShadowMap01_3x3(worldPosOffset, _shadowtex3, _shadowVpMatrix3, 0, 1024);
                */

                float shadow = 1.0;
                if(d_lin<_split0) 
                {
                    //color *= float3(1.5, 0.5, 0.5);
                    shadow *= ShadowMap01(worldPosOffset, _shadowtex0, _shadowVpMatrix0);
                    //shadow *= ShadowMapPCSS(worldPosOffset, _shadowtex0, _shadowVpMatrix0, _lightSize, _orthoWidth0, _orthoDistance, _shadowMapResolution);
                }
                else if(d_lin<_split0+_split1) 
                {
                    //color *= float3(0.5, 1.5, 0.5);
                    shadow *= ShadowMap01(worldPosOffset, _shadowtex1, _shadowVpMatrix1);
                    //shadow *= ShadowMapPCSS(worldPosOffset, _shadowtex1, _shadowVpMatrix1, _lightSize, _orthoWidth1, _orthoDistance, _shadowMapResolution);
                }
                else if(d_lin<_split0+_split1+_split2) 
                {   
                    //color *= float3(0.5, 0.5, 1.5);
                    shadow *= ShadowMap01(worldPosOffset, _shadowtex2, _shadowVpMatrix2);
                    //shadow *= ShadowMapPCSS(worldPosOffset, _shadowtex2, _shadowVpMatrix2, _lightSize, _orthoWidth2, _orthoDistance, _shadowMapResolution);
                }
                else if(d_lin<_split0+_split1+_split2+_split3)
                {
                    shadow *= ShadowMap01(worldPosOffset, _shadowtex3, _shadowVpMatrix3);
                    //shadow *= ShadowMapPCSS(worldPosOffset, _shadowtex3, _shadowVpMatrix3, _lightSize, _orthoWidth3, _orthoDistance, _shadowMapResolution);
                }

                // 受阴影影响的直接光照
                color += direct * shadow;
                //color.rgb = float3(shadow, shadow, shadow);   // for debug

                return float4(color, 1);
            }
            ENDCG
        }
    }
}
