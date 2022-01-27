Shader "ToyRP/blurNxN"
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
            #include "shadow.cginc"
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

            sampler2D _MainTex;

            float frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                float4 worldPos = mul(_vpMatrixInv, float4(uv*2-1, d, 1));
                worldPos /= worldPos.w;
                
                float weight = 0;
                float shadow = 0;
                float r = 1;

                for(int i=-r; i<=r; i++)
                {
                    for(int j=-r; j<=r; j++)
                    {
                        float2 offset = float2(i, j) / float2(_screenWidth, _screenWidth);
                        float2 uv_sample = uv + offset;

                        float3 normal_sample = tex2D(_GT1, uv_sample).rgb * 2 - 1;
                        float d_sample = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv_sample));
                        float4 worldPos_sample = mul(_vpMatrixInv, float4(uv_sample*2-1, d_sample, 1));
                        worldPos_sample /= worldPos_sample.w;

                        float w = 1.0 / (1.0 + distance(worldPos, worldPos_sample)*0.5);

                        shadow += w * tex2D(_MainTex, uv_sample).r;
                        weight += w;
                    }
                }
                shadow /= weight;

                return shadow;
            }
            ENDCG
        }
    }
}
