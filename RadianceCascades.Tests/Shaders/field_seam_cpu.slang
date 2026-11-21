// The seam pass over the cells field_code_cpu.slang resolves, a thread a cell, each cell in front
// of a solid's face taking the plane of a sheet a cell beside it keeps where the sheet passes over
// it (seamSheet), as a sheet resting on a wall's top passes over the cells by the wall's face.
import rc_field_read;

// Each cell's distance and the plane it keeps, four floats a cell, as the resolve wrote them.
RWStructuredBuffer<float> cells;

// Each cell's code as its low and its high 16 bits, two floats a cell.
RWStructuredBuffer<float> codes;

// The plane each cell takes of the seam, three floats a cell, none where it takes none.
RWStructuredBuffer<float> seams;

static const int Size = 32;
static const float Cell = 0.25;

struct BuiltField : IFieldVolume
{
    int cascadeOf(float3 p, float margin) { return 0; }
    float4 cornerAndCell(int cascade) { return float4(float3(-Size * Cell * 0.5), Cell); }
    int size() { return Size; }
    float4 cellAt(int cascade, int3 at)
    {
        int i = ((at.z * Size + at.y) * Size + at.x) * 4;
        return float4(cells[i], cells[i + 1], cells[i + 2], cells[i + 3]);
    }
    float4 between(int cascade, float3 at) { return cellAt(cascade, int3(at)); }
    float3 secondAt(int cascade, int3 at) { return float3(0.0); }
};

[shader("compute")]
[numthreads(4, 4, 4)]
void test(uint3 id : SV_DispatchThreadID)
{
    uint index = (id.z * Size + id.y) * Size + id.x;
    uint code = uint(codes[index * 2]) | (uint(codes[index * 2 + 1]) << 16u);
    BuiltField field;
    float3 seam = seamSheet(field, 0, int3(id), code);
    seams[index * 3] = seam.x;
    seams[index * 3 + 1] = seam.y;
    seams[index * 3 + 2] = seam.z;
}
