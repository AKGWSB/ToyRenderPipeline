struct PointLight
{
    float3 color;
    float intensity;
    float3 position;
    float radius;
};

struct LightIndex
{
    int count;
    int start;
};

StructuredBuffer<PointLight> _lightBuffer;
StructuredBuffer<uint> _lightAssignBuffer;
StructuredBuffer<LightIndex> _assignTable;

float _numClusterX;
float _numClusterY;
float _numClusterZ;

uint Index3DTo1D(uint3 i)
{
    return i.z * _numClusterX * _numClusterY
        + i.y * _numClusterX
        + i.x;
}

