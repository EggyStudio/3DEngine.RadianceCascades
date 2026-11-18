// The cells splat_cpu.slang kept painted by the triangle whose code each kept, a group a triangle,
// with the triangle's color times its corners' colors blended at the cell's nearest point.
import rc_field_splat;

RWStructuredBuffer<float> triangles;
RWStructuredBuffer<uint> codes;
// For each cell, its paint, four floats, and its face's gloss.
RWStructuredBuffer<float> paints;
RWStructuredBuffer<uint> glosses;

static const int Size = 32;

struct PaintedCells : IPaintCells
{
    uint kept(uint index) { return codes[index]; }
    void paint(uint index, float4 paint, float3 emission, uint gloss)
    {
        paints[index * 4] = paint.r;
        paints[index * 4 + 1] = paint.g;
        paints[index * 4 + 2] = paint.b;
        paints[index * 4 + 3] = paint.a;
        glosses[index] = gloss;
    }
};

[shader("compute")]
[numthreads(1, 1, 1)]
void test(uint3 group : SV_GroupID)
{
    int t = int(group.x);
    if (t >= int(triangles[0]))
        return;
    int at = 1 + t * 14;
    SplatTriangle triangle;
    triangle.a = float3(triangles[at], triangles[at + 1], triangles[at + 2]);
    triangle.b = float3(triangles[at + 3], triangles[at + 4], triangles[at + 5]);
    triangle.c = float3(triangles[at + 6], triangles[at + 7], triangles[at + 8]);
    triangle.openEdges = bool3(false);
    triangle.doubleSided = triangles[at + 10] > 0.5;
    triangle.instance = uint(triangles[at + 9]);
    SplatCascade cascade;
    cascade.corner = float3(-Size * 0.25 * 0.5);
    cascade.cell = 0.25;
    cascade.size = Size;
    cascade.band = 4.0;
    cascade.first = int3(0);
    cascade.last = int3(Size - 1);
    SplatSurface surface;
    surface.color = float3(triangles[at + 11], triangles[at + 12], triangles[at + 13]);
    surface.corners = float3x3(float3(1.0), float3(1.0), float3(1.0));
    surface.lightThrough = 0.0;
    surface.emission = float3(0.0);
    surface.gloss = glossBits(0.4, 0.0, 0.5);
    PaintedCells cells;
    splatPaint(cells, triangle, cascade, surface, 0u, 1u);
}
