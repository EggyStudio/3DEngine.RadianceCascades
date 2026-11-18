// A cascade of 32 cells a side built from the triangles given, each cell keeping the least code
// any of them gives it and the least code of a face it lies straight behind, as the splat's
// minimum keeps them, then resolved into distances, a thread a cell, so a run on the CPU says what
// the field holds. The last uint of the triangles' corners says how many triangles there are.
import rc_field_code;

// Each triangle's three corners, nine floats, then its instance and whether it is double-sided,
// eleven floats a triangle.
RWStructuredBuffer<float> triangles;

// For each cell, the distance it resolves to, then the code it kept as its low and its high 16
// bits, which a float holds whole, and its sheet's plane.
RWStructuredBuffer<float> distances;
RWStructuredBuffer<float> codes;
RWStructuredBuffer<float> sheets;

static const int Size = 32;
static const float Cell = 0.25;
static const float Band = 4.0;

[shader("compute")]
[numthreads(4, 4, 4)]
void test(uint3 id : SV_DispatchThreadID)
{
    float3 origin = float3(-Size * Cell * 0.5);
    float3 p = origin + (float3(id) + 0.5) * Cell;
    uint code = EmptyCode, behind = EmptyCode;
    int count = int(triangles[0]);
    for (int t = 0; t < count; t++)
    {
        int at = 1 + t * 11;
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
        code = min(code, given);
        behind = min(behind, back);
    }
    uint index = (id.z * Size + id.y) * Size + id.x;
    distances[index] = resolvedDistance(code, behind, Cell, Band, Cell * 0.5);
    codes[index * 2] = float(code & 0xFFFFu);
    codes[index * 2 + 1] = float(code >> 16u);
    float3 sheet = sheetOf(code);
    sheets[index * 3] = sheet.x;
    sheets[index * 3 + 1] = sheet.y;
    sheets[index * 3 + 2] = sheet.z;
}
