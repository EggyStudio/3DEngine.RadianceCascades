// The cells of one cascade of the field built from the triangles given, as field_code_cpu.slang
// builds them, a thread a cell, each with the distance it resolves to, the plane a double-sided
// face keeps in it and the share of light that passes a unit of the depth of the surface nearest
// it, the share of the triangle whose code it kept, so a probe's ray reads it where it meets that
// surface.
import rc_field_code;

// The cascade's least corner, the width of a cell and the cells along each side.
RWStructuredBuffer<float> setup;

// How many triangles there are, then each triangle's three corners, its instance, whether it is
// double-sided and its share of light through, twelve floats a triangle.
RWStructuredBuffer<float> triangles;

// Each cell's distance, the plane it keeps and its share of light through, five floats a cell.
RWStructuredBuffer<float> cells;

static const float Band = 4.0;

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
    float3 sheet = sheetOf(code);
    cells[index * 5] = resolvedDistance(code, behind, cell, Band, cell * 0.5);
    cells[index * 5 + 1] = sheet.x;
    cells[index * 5 + 2] = sheet.y;
    cells[index * 5 + 3] = sheet.z;
    cells[index * 5 + 4] = through;
}
