// A cascade of 32 cells a side built from the triangles given, each cell keeping the least code any
// of them gives it, painted the color of the triangle whose code that is, with its face's way and
// gloss and the distance it resolves to, a thread a cell, for field_paint_read_cpu.slang to read.
import rc_field_code;
import rc_field_paint;

// The count of triangles first, then each triangle's three corners, nine floats, its instance,
// whether it is double-sided and its color, fourteen floats a triangle.
RWStructuredBuffer<float> triangles;

// For each cell, its paint, four floats, its face, and its distance.
RWStructuredBuffer<float> paints;
RWStructuredBuffer<uint> faces;
RWStructuredBuffer<float> distances;

static const int Size = 32;
static const float Cell = 0.25;
static const float Band = 4.0;

[shader("compute")]
[numthreads(4, 4, 4)]
void test(uint3 id : SV_DispatchThreadID)
{
    float3 p = float3(-Size * Cell * 0.5) + (float3(id) + 0.5) * Cell;
    uint code = EmptyCode, behind = EmptyCode;
    float3 color = float3(0.0);
    int count = int(triangles[0]);
    for (int t = 0; t < count; t++)
    {
        int at = 1 + t * 14;
        float3 a = float3(triangles[at], triangles[at + 1], triangles[at + 2]);
        float3 b = float3(triangles[at + 3], triangles[at + 4], triangles[at + 5]);
        float3 c = float3(triangles[at + 6], triangles[at + 7], triangles[at + 8]);
        if (!holdsSurface(a, b, c))
            continue;
        float d;
        uint back;
        uint given = cellCode(p, a, b, c, bool3(false), triangles[at + 10] > 0.5, uint(triangles[at + 9]), Cell, d, back);
        if (d >= Band * Cell)
            continue;
        if (given < code)
            color = float3(triangles[at + 11], triangles[at + 12], triangles[at + 13]);
        code = min(code, given);
        behind = min(behind, back);
    }
    uint index = (id.z * Size + id.y) * Size + id.x;
    bool painted = code != EmptyCode;
    paints[index * 4] = color.r;
    paints[index * 4 + 1] = color.g;
    paints[index * 4 + 2] = color.b;
    paints[index * 4 + 3] = painted ? 1.0 : 0.0;
    faces[index] = ((code >> WayShift) & 255u) | glossBits(0.5, 0.0, 0.5);
    distances[index] = resolvedDistance(code, behind, Cell, Band, Cell * 0.5);
}
