// The cells of one cascade of the field built from the triangles given, as field_code_cpu.slang
// builds them, a thread a cell, each with the distance it resolves to, the plane a double-sided
// face keeps in it and the share of light that passes a unit of the depth of the surface nearest
// it, the share of the triangle whose code it kept, so a probe's ray reads it where it meets that
// surface. A second pass over the built cells gives each cell the plane of a sheet passing over
// it where it lies in front of a solid's face (seamSheet), as a field's build runs it after the
// resolve.
import rc_field_read;

// The cascade's least corner, the width of a cell and the cells along each side, and the pass, 0
// to build the cells and 1 to read the seam's planes of the built ones.
RWStructuredBuffer<float> setup;

// How many triangles there are, then each triangle's three corners, its instance, whether it is
// double-sided and its share of light through, twelve floats a triangle.
RWStructuredBuffer<float> triangles;

// Each cell's distance, the plane it keeps and its share of light through, five floats a cell.
RWStructuredBuffer<float> cells;

// The plane each cell takes of the seam, three floats a cell, none where it takes none.
RWStructuredBuffer<float> seams;

static const float Band = 4.0;

// The built cells as the seam reads them, the one cascade of the setup.
struct BuiltCells : IFieldVolume
{
    int cascadeOf(float3 p, float margin) { return 0; }
    float4 cornerAndCell(int cascade) { return float4(setup[0], setup[1], setup[2], setup[3]); }
    int size() { return int(setup[4]); }
    float4 cellAt(int cascade, int3 at)
    {
        int i = ((at.z * size() + at.y) * size() + at.x) * 5;
        return float4(cells[i], cells[i + 1], cells[i + 2], cells[i + 3]);
    }
    float4 between(int cascade, float3 at) { return cellAt(cascade, int3(at)); }
    float3 secondAt(int cascade, int3 at) { return float3(0.0); }
};

[shader("compute")]
[numthreads(4, 4, 4)]
void test(uint3 id : SV_DispatchThreadID)
{
    int size = int(setup[4]);
    if (any(id >= uint(size)))
        return;
    float cell = setup[3];
    float3 p = float3(setup[0], setup[1], setup[2]) + (float3(id) + 0.5) * cell;
    float reach = Band * cell;
    uint code = EmptyCode, behind = EmptyCode;
    float through = 0.0;
    int count = int(triangles[0]);
    for (int t = 0; t < count; t++)
    {
        int at = 1 + t * 12;
        float3 a = float3(triangles[at], triangles[at + 1], triangles[at + 2]);
        float3 b = float3(triangles[at + 3], triangles[at + 4], triangles[at + 5]);
        float3 c = float3(triangles[at + 6], triangles[at + 7], triangles[at + 8]);
        // A triangle whose box grown by the band leaves the cell out gives it nothing, which the
        // test passes over before the code's work.
        if (any(p < min(a, min(b, c)) - reach) || any(p > max(a, max(b, c)) + reach) || !holdsSurface(a, b, c))
            continue;
        float d;
        uint back;
        uint given = cellCode(p, a, b, c, bool3(false), triangles[at + 10] > 0.5, uint(triangles[at + 9]), cell, d, back);
        if (d >= reach)
            continue;
        if (given < code)
        {
            code = given;
            through = triangles[at + 11];
        }
        behind = min(behind, back);
    }
    uint index = (id.z * uint(size) + id.y) * uint(size) + id.x;
    if (setup[5] > 0.5)
    {
        BuiltCells built;
        float3 seam = seamSheet(built, 0, int3(id), code);
        seams[index * 3] = seam.x;
        seams[index * 3 + 1] = seam.y;
        seams[index * 3 + 2] = seam.z;
        return;
    }
    float3 sheet = sheetOf(code);
    cells[index * 5] = resolvedDistance(code, behind, cell, Band, cell * 0.5);
    cells[index * 5 + 1] = sheet.x;
    cells[index * 5 + 2] = sheet.y;
    cells[index * 5 + 3] = sheet.z;
    cells[index * 5 + 4] = through;
}
