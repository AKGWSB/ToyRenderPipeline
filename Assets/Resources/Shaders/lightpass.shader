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

            #include "globaluniform.cginc"
            #include "UnityCG.cginc"
            #include "BRDF.cginc"
            #include "shadow.cginc"
            #include "UnityLightingCommon.cginc"
            #include "random.cginc"   
            #include "Cluster.cginc"      

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

            fixed4 frag (v2f i, out float depthOut : SV_Depth) : SV_Target
            {
                float2 uv = i.uv;
                //return tex2Dlod(_hizBuffer, float4(uv.x, uv.y, 0, 7)); 
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

                // 计算阴影
                float shadow = tex2D(_shadowStrength, uv).r;
                color += direct * shadow;

                //return float4(color, 1);

                // 计算 Cluster Based Lighting
                uint x = floor(uv.x * _numClusterX);
                uint y = floor(uv.y * _numClusterY);
                uint z = floor((1-d_lin) * _numClusterZ);   // Z 是反的

                uint3 clusterId_3D = uint3(x, y, z);
                uint clusterId_1D = Index3DTo1D(clusterId_3D);
                LightIndex lightIndex = _assignTable[clusterId_1D];

                int start = lightIndex.start;                   // 灯光分配表起始，结束范围
                int end = lightIndex.start + lightIndex.count;
                for(int j=start; j<end; j++)
                {
                    uint lightId = _lightAssignBuffer[j];   // 灯光 id
                    PointLight lit = _lightBuffer[lightId]; // 根据 id 查灯光表

                    L = normalize(lit.position - worldPos.xyz);
                    radiance = lit.color;

                    // 灯光衰减
                    float dis = distance(lit.position, worldPos.xyz);
                    float d2 = dis * dis;
                    float r2 = lit.radius * lit.radius;
                    float dying = saturate(1 - (d2 / r2) * (d2 / r2));
                    dying *= dying;

                    color += PBR(N, V, L, albedo, radiance, roughness, metallic) * lit.intensity * dying;
                }


                //color.rgb = float3(shadow,shadow,shadow);

                /*
                // visualize shadow mask
                float mask = tex2D(_shadoMask, uv).r;
                if(0.0000005<mask && mask<0.9999995) return float4(1, 0, 0, 1);*/

                return float4(color, 1);
            }
            ENDCG
        }
    }
}
