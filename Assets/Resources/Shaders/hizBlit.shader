Shader "ToyRP/hizBlit"
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

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }           

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float frag (v2f i) : SV_Target
            {   
                // 本级 mip 的 0.5 个 texel 对应上一级 mip 的 1 个 texel
                float2 offset = 0.5 * _MainTex_TexelSize.xy;          

                float4 d;
                d.x = tex2D(_MainTex, i.uv + offset * float2(-0.5, -0.5)).r;
                d.y = tex2D(_MainTex, i.uv + offset * float2(-0.5,  0.5)).r;
                d.z = tex2D(_MainTex, i.uv + offset * float2( 0.5, -0.5)).r;
                d.w = tex2D(_MainTex, i.uv + offset * float2( 0.5,  0.5)).r;

                float maximum = max(max(d.x, d.y), max(d.z, d.w));
                float minimum = min(min(d.x, d.y), min(d.z, d.w));

            #if defined (UNITY_REVERSED_Z)
                return minimum;
            #else
                return maximum;
            #endif
            }
            ENDCG
        }
    }
}
