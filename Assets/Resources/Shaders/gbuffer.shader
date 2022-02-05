Shader "ToyRP/gbuffer"
{
    Properties
    {
        _MainTex ("Albedo Map", 2D) = "white" {}
        [Space(25)]

        _Metallic_global ("Metallic", Range(0, 1)) = 0.5
        _Roughness_global ("Roughness", Range(0, 1)) = 0.5
        [Toggle] _Use_Metal_Map ("Use Metal Map", Float) = 1
        _MetallicGlossMap ("Metallic Map", 2D) = "white" {}
        //[Enum(Metallic Alpha,0,Albedo Alpha,1)] _SmoothnessTextureChannel ("Smoothness texture channel", Float) = 0
        [Space(25)]
        
        _EmissionMap ("Emission Map", 2D) = "black" {}
        [Space(25)]

        _OcclusionMap ("Occlusion Map", 2D) = "white" {}
        [Space(25)]

        [Toggle] _Use_Normal_Map ("Use Normal Map", Float) = 1
        [Normal] _BumpMap ("Normal Map", 2D) = "bump" {}
    }
    SubShader
    {
        Pass
        {
            Tags { "LightMode"="depthonly" }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 depth : TEXCOORD0;
            };

            v2f vert (appdata_base v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.depth = o.vertex.zw;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float d = i.depth.x / i.depth.y;
            #if defined (UNITY_REVERSED_Z)
                d = 1.0 - d;
            #endif
                fixed4 c = EncodeFloatRGBA(d);
                //return float4(d,0,0,1);   // for debug
                return c;
            }
            ENDCG 
        }

        Pass
        {
            Tags { "LightMode"="gbuffer" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
            };

            float4 _MainTex_ST;

            sampler2D _MainTex;
            sampler2D _MetallicGlossMap;
            sampler2D _EmissionMap;
            sampler2D _OcclusionMap;
            sampler2D _BumpMap;

            float _Use_Metal_Map;
            float _Use_Normal_Map;
            float _Metallic_global;
            float _Roughness_global;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            void frag (
                v2f i,
                out float4 GT0 : SV_Target0,
                out float4 GT1 : SV_Target1,
                out float4 GT2 : SV_Target2,
                out float4 GT3 : SV_Target3
            )
            {
                float4 color = tex2D(_MainTex, i.uv);
                float3 emission = tex2D(_EmissionMap, i.uv).rgb;
                float3 normal = i.normal;
                float metallic = _Metallic_global;
                float roughness = _Roughness_global;
                float ao = tex2D(_OcclusionMap, i.uv).g;

                if(_Use_Metal_Map)
                {
                    float4 metal = tex2D(_MetallicGlossMap, i.uv);
                    metallic = metal.r;
                    roughness = 1.0 - metal.a;
                }
                //if(_Use_Normal_Map) normal = UnpackNormal(tex2D(_BumpMap, i.uv));

                GT0 = color;
                GT1 = float4(normal*0.5+0.5, 0);
                GT2 = float4(0, 0, roughness,metallic);
                GT3 = float4(emission, ao);
            }
            ENDCG
        }
    }
}
