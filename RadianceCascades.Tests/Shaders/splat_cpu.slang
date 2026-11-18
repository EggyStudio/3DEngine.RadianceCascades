// A cascade of 32 cells a side splatted from the triangles given, a group a triangle, each cell
// keeping the least code any gives it and the least code of a face it lies straight behind, the
// work a GPU's workgroup does over a triangle's cells done here by one thread.
import rc_field_splat;

// The count of triangles first, then each triangle's three corners, nine floats, its instance,
// whether it is double-sided and its color, fourteen floats a triangle.
RWStructuredBuffer<float> triangles;
RWStructuredBuffer<uint> codes;
RWStructuredBuffer<uint> behind;

static const int Size = 32;

// The cells as the splat keeps them, a minimum in place, since the CPU runs the groups one at a
// time.
struct KeptCells : ISplatCells
{
    void keepLeast(uint index, uint code) { codes[index] = min(codes[index], code); }
    void keepBehind(uint index, uint code) { behind[index] = min(behind[index], code); }
};

SplatTriangle triangleAt(int t)
{
    int at = 1 + t * 14;
    SplatTriangle triangle;
    triangle.a = float3(triangles[at], triangles[at + 1], triangles[at + 2]);
    triangle.b = float3(triangles[at + 3], triangles[at + 4], triangles[at + 5]);
    triangle.c = float3(triangles[at + 6], triangles[at + 7], triangles[at + 8]);
    triangle.openEdges = bool3(false);
    triangle.doubleSided = triangles[at + 10] > 0.5;
    triangle.instance = uint(triangles[at + 9]);
    return triangle;
}

SplatCascade cascade()
{
    SplatCascade cascade;
    cascade.corner = float3(-Size * 0.25 * 0.5);
    cascade.cell = 0.25;
    cascade.size = Size;
    cascade.band = 4.0;
    cascade.first = int3(0);
    cascade.last = int3(Size - 1);
    return cascade;
}

[shader("compute")]
[numthreads(1, 1, 1)]
void test(uint3 group : SV_GroupID)
{
    KeptCells cells;
    if (int(group.x) < int(triangles[0]))
        splatCodes(cells, triangleAt(int(group.x)), cascade(), 0u, 1u);
}
