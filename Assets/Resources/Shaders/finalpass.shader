Shader "ToyRP/finalpass"
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

            sampler2D _MainTex;
            sampler2D _gdepth;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float3 ACESToneMapping(float3 color, float adapted_lum)
            {
                const float A = 2.51f;
                const float B = 0.03f;
                const float C = 2.43f;
                const float D = 0.59f;
                const float E = 0.14f;

                color *= adapted_lum;
                return (color * (A * color + B)) / (color * (C * color + D) + E);
            }

            fixed4 frag (v2f i, out float depthOut : SV_Depth) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                //col.rgb = ACESToneMapping(col.rgb, 1.0);    // tone mapping
                //col.rgb = col.rgb / (col.rgb + 1.0);

                float inv22 = 1.0 / 2.2;
                //col.rgb = pow(col.rgb, float3(inv22, inv22, inv22));  

                depthOut = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, i.uv));
                return col;
            }
            ENDCG
        }
    }
}