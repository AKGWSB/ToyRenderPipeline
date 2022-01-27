Shader "ToyRP/blur1xN"
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
                /*
                float2 uv = i.uv;
                float shadow = 0;
                float r = 3;
                for(int j=-r; j<=r; j++)
                {
                    float2 offset = float2(0, j) / float2(_screenWidth, _screenWidth);
                    shadow += tex2D(_MainTex, uv+offset).r;
                }
                shadow /= (2*r+1);
                */
                float2 uv = i.uv;

                float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
                float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
                float4 worldPos = mul(_vpMatrixInv, float4(uv*2-1, d, 1));
                worldPos /= worldPos.w;

                float shadow = 0;
                float weight = 0;
                float r = 3;

                float dis = distance(_WorldSpaceCameraPos.xyz, worldPos.xyz);
                float radius = 1.0 / (pow(dis, 1.2)*0.01 + 0.01);
                //radius = 1;

               for(int i=-r; i<=r; i++)
                {
                    float2 offset = float2(0, i) / float2(_screenWidth/4, _screenWidth/4);                   
                    float2 uv_sample = uv + offset * radius;
                    shadow += tex2D(_MainTex, uv_sample).r;
                    weight += 1;
                }
                shadow /= weight;

                return shadow;
            }
            ENDCG
        }
    }
}
