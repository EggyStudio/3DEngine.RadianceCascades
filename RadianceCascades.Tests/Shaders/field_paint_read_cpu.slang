// The color each hit given reads from the cells field_paint_build_cpu.slang built, at its point on
// a surface facing its normal, a thread a hit.
import rc_field_paint;

RWStructuredBuffer<float> paints;
RWStructuredBuffer<uint> faces;
RWStructuredBuffer<float> distances;

// Each hit's point and its surface's normal, six floats a hit, and the color it reads, three.
RWStructuredBuffer<float> hits;
RWStructuredBuffer<float> colors;

static const int Size = 32;
static const float Cell = 0.25;

// The built cells as the read takes them.
struct BuiltCells : IFieldCells
{
    int size() { return Size; }
    float3 corner() { return float3(-Size * Cell * 0.5); }
    float cell() { return Cell; }
    uint index(int3 at) { return uint((at.z * Size + at.y) * Size + at.x); }
    float4 paint(int3 at)
    {
        uint i = index(at);
        return float4(paints[i * 4], paints[i * 4 + 1], paints[i * 4 + 2], paints[i * 4 + 3]);
    }
    uint face(int3 at) { return faces[index(at)]; }
    float distance(int3 at) { return distances[index(at)]; }
};

[shader("compute")]
[numthreads(1, 1, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    BuiltCells cells;
    int h = int(id.x);
    float3 p = float3(hits[h * 6], hits[h * 6 + 1], hits[h * 6 + 2]);
    float3 n = float3(hits[h * 6 + 3], hits[h * 6 + 4], hits[h * 6 + 5]);
    float3 gloss;
    float3 color = paintFacing(cells, p, n, gloss);
    colors[h * 3] = color.r;
    colors[h * 3 + 1] = color.g;
    colors[h * 3 + 2] = color.b;
}
