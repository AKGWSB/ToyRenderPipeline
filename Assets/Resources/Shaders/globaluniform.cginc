sampler2D _gdepth;
sampler2D _GT0;
sampler2D _GT1;
sampler2D _GT2;
sampler2D _GT3;

samplerCUBE _diffuseIBL;
samplerCUBE _specularIBL;
sampler2D _brdfLut;
sampler2D _noiseTex;

sampler2D _shadowStrength;
sampler2D _shadowtex0;
sampler2D _shadowtex1;
sampler2D _shadowtex2;
sampler2D _shadowtex3;
sampler2D _shadoMask;

float _usingShadowMask;

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

float _screenWidth;
float _screenHeight;
float _noiseTexResolution;

float _shadingPointNormalBias0;
float _shadingPointNormalBias1;
float _shadingPointNormalBias2;
float _shadingPointNormalBias3;

float _depthNormalBias0;
float _depthNormalBias1;
float _depthNormalBias2;
float _depthNormalBias3;

float _pcssSearchRadius0;
float _pcssSearchRadius1;
float _pcssSearchRadius2;
float _pcssSearchRadius3;

float _pcssFilterRadius0;
float _pcssFilterRadius1;
float _pcssFilterRadius2;
float _pcssFilterRadius3;


