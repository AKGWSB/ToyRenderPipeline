#define N_SAMPLE 16
const float2 poissonDisk[16] = {
    float2( -0.94201624, -0.39906216 ),
    float2( 0.94558609, -0.76890725 ),
    float2( -0.094184101, -0.92938870 ),
    float2( 0.34495938, 0.29387760 ),
    float2( -0.91588581, 0.45771432 ),
    float2( -0.81544232, -0.87912464 ),
    float2( -0.38277543, 0.27676845 ),
    float2( 0.97484398, 0.75648379 ),
    float2( 0.44323325, -0.97511554 ),
    float2( 0.53742981, -0.47373420 ),
    float2( -0.26496911, -0.41893023 ),
    float2( 0.79197514, 0.19090188 ),
    float2( -0.24188840, 0.99706507 ),
    float2( -0.81409955, 0.91437590 ),
    float2( 0.19984126, 0.78641367 ),
    float2( 0.14383161, -0.14100790 )
};

float ShadowMap01(float4 worldPos, sampler2D _shadowtex, float4x4 _shadowVpMatrix)
{
    float4 shadowNdc = mul(_shadowVpMatrix, worldPos);
    shadowNdc /= shadowNdc.w;
    float2 uv = shadowNdc.xy * 0.5 + 0.5;

    if(uv.x<0 || uv.x>1 || uv.y<0 || uv.y>1) return 1.0f;

    float d = shadowNdc.z;
    float d_sample = tex2D(_shadowtex, uv).r;

#if defined (UNITY_REVERSED_Z)
    if(d_sample>d) return 0.0f;
#else
    if(d_sample<d) return 0.0f;
#endif

    return 1.0f;
}

float PCF3x3(float4 worldPos, sampler2D _shadowtex, float4x4 _shadowVpMatrix, float shadowMapResolution)
{
    for(int i=-1; i<=1; i++)
    {
        for(int j=-1; j<=1; j++)
        {

        }
    }

    return 1.0;
}

float2 AverageBlockerDepth(float4 shadowNdc, sampler2D _shadowtex, float d_shadingPoint, float lightSize, float shadowMapResolution)
{
    float2 uv = shadowNdc.xy * 0.5 + 0.5;
    float step = 2.0;
    float searchWidth = lightSize / shadowMapResolution;
    float d_average = 0.0;
    float count = 0.0005;   // 防止 ÷ 0

    /*
    for(int i=-step; i<=step; i++)
    {
        for(int j=-step; j<=step; j++)
        {
            float2 unitOffset = float2(i, j) / step;  // map to [-1, 1]
            float2 offset = unitOffset * searchWidth;
            float2 uvo = uv + offset;

            float d_sample = tex2D(_shadowtex, uvo).r;
            if(d_sample>d_shadingPoint)
            {
                count += 1;
                d_average += d_sample;
            }
        }
    }
    */

    for(int i=0; i<N_SAMPLE; i++)
    {
        float2 unitOffset = poissonDisk[i];
        float2 offset = unitOffset * searchWidth;
        float2 uvo = uv + offset;

        float d_sample = tex2D(_shadowtex, uvo).r;
        if(d_sample>d_shadingPoint)
        {
            count += 1;
            d_average += d_sample;
        }
    }

    return float2(d_average / count, count);
}

float ShadowMapPCSS(
    float4 worldPos, sampler2D _shadowtex, float4x4 _shadowVpMatrix, 
    float lightSize, float orthoWidth, float orthoDistance, float shadowMapResolution)
{
    float4 shadowNdc = mul(_shadowVpMatrix, worldPos);
    shadowNdc /= shadowNdc.w;
    float d_shadingPoint = shadowNdc.z;  // 着色点深度
    float2 uv = shadowNdc.xy * 0.5 + 0.5;
    float2 d_sample_d = tex2D(_shadowtex, uv).r;

    //if(uv.x<0 || uv.x>1 || uv.y<0 || uv.y>1) return 1.0f;
    float disPerPixel = orthoWidth / shadowMapResolution;
    
    // 计算平均遮挡深度
    float2 blocker = AverageBlockerDepth(shadowNdc, _shadowtex, d_shadingPoint, lightSize, shadowMapResolution);
    float d_average = blocker.x;
    float blockCnt = blocker.y;
    //return blockCnt / N_SAMPLE;
    if(blockCnt<0.1) return 1.0;    // 没有遮挡则直接返回

    // 世界空间下的距离, 计算 PCSS 用, 注意 Reverse Z
    float d_receiver = (1.0 - d_shadingPoint) * 2 * orthoDistance;
    float d_blocker = (1.0 - d_average) * 2 * orthoDistance;

    // 世界空间下的 filter 半径
    float w = (d_receiver - d_blocker) * lightSize / d_blocker;
    
    // 深度图上的 filter 半径
    float radius = w / disPerPixel;

    float shadow = 0.0f;
    float sum = 0.0;

    
    float step = 2;     // 半径为 step*2+1 像素的带洞卷积
    for(int i=-step; i<=step; i++)
    {
        for(int j=-step; j<=step; j++)
        {
            sum += 1;
            float2 offset = float2(i, j) / shadowMapResolution;
            float2 uvo = uv + offset * radius;
            //if(uvo.x<0 || uvo.x>1 || uvo.y<0 || uvo.y>1) continue;

            float d_sample = tex2D(_shadowtex, uvo).r;
            if(d_sample>d_shadingPoint) shadow += 1.0f;
        }
    }
      
    /*
    // PCF
    for(int i=0; i<N_SAMPLE; i++)
    {
        sum += 1;
        float2 offset = poissonDisk[i] / shadowMapResolution;
        float2 uvo = uv + offset * radius;
        //if(uvo.x<0 || uvo.x>1 || uvo.y<0 || uvo.y>1) continue;

        float d_sample = tex2D(_shadowtex, uvo).r;
        if(d_sample>d_shadingPoint) shadow += 1.0f;
    }*/

    return 1.0 - (shadow / sum);
    
    
}
