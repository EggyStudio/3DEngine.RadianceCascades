// A cascade of 32 cells a side stamped with the shapes given over the still cells given, a thread
// a cell, each cell taking the least of its still distance and its distance to each shape, and
// the nearer surface's paint and face.
import rc_field_stamp;

// The count of shapes first, then each shape's StampFloats floats as FieldShape.WriteStamp writes
// them.
RWStructuredBuffer<float> shapes;

// For each cell, its still distance and paint, four floats, and what the stamp gives it.
RWStructuredBuffer<float> stillDistances;
RWStructuredBuffer<float> stillPaints;
RWStructuredBuffer<float> stampedDistances;
RWStructuredBuffer<float> stampedPaints;
RWStructuredBuffer<uint> stampedFaces;

static const int Size = 32;
static const float Cell = 0.25;
static const float Band = 4.0;

struct GivenShapes : IStampShapes
{
    uint count() { return uint(shapes[0]); }
    StampShape shape(uint i)
    {
        int at = 1 + int(i) * 32;
        StampShape s;
        s.row0 = float4(shapes[at], shapes[at + 1], shapes[at + 2], shapes[at + 3]);
        s.row1 = float4(shapes[at + 4], shapes[at + 5], shapes[at + 6], shapes[at + 7]);
        s.row2 = float4(shapes[at + 8], shapes[at + 9], shapes[at + 10], shapes[at + 11]);
        s.centerAndScale = float4(shapes[at + 12], shapes[at + 13], shapes[at + 14], shapes[at + 15]);
        s.extents = float4(shapes[at + 16], shapes[at + 17], shapes[at + 18], shapes[at + 19]);
        s.color = float4(shapes[at + 20], shapes[at + 21], shapes[at + 22], shapes[at + 23]);
        s.emission = float4(shapes[at + 24], shapes[at + 25], shapes[at + 26], shapes[at + 27]);
        s.gloss = float4(shapes[at + 28], shapes[at + 29], shapes[at + 30], shapes[at + 31]);
        return s;
    }
};

[shader("compute")]
[numthreads(4, 4, 4)]
void test(uint3 id : SV_DispatchThreadID)
{
    uint index = (id.z * Size + id.y) * Size + id.x;
    float3 p = float3(-Size * Cell * 0.5) + (float3(id) + 0.5) * Cell;
    FieldCell still;
    still.distance = stillDistances[index];
    still.sheet = float3(0.0);
    still.paint = float4(stillPaints[index * 4], stillPaints[index * 4 + 1], stillPaints[index * 4 + 2], stillPaints[index * 4 + 3]);
    still.glow = float4(0.0);
    still.face = 0u;
    GivenShapes given;
    FieldCell stamped = stampCell(given, p, Band * Cell, still);
    stampedDistances[index] = stamped.distance;
    stampedPaints[index * 4] = stamped.paint.r;
    stampedPaints[index * 4 + 1] = stamped.paint.g;
    stampedPaints[index * 4 + 2] = stamped.paint.b;
    stampedPaints[index * 4 + 3] = stamped.paint.a;
    stampedFaces[index] = stamped.face;
}
