// The field's distance at the points given and the march of the rays given through the cells
// given, one cascade of 32 cells of a quarter unit a side about the origin, read as a sampler reads
// an image between its texels, a thread a point and a ray.
import rc_field_read;

// Each cell's distance and the plane it keeps, four floats a cell, as the resolve wrote them.
RWStructuredBuffer<float> cells;

// Each point, three floats, and its distance by each read, two floats.
RWStructuredBuffer<float> points;
RWStructuredBuffer<float> distances;

// Each ray's origin, way, least and most distance, eight floats, where each read's march meets a
// surface, two floats, and the steps the second takes, 64 of a distance along and a distance read.
RWStructuredBuffer<float> rays;
RWStructuredBuffer<float> hits;
RWStructuredBuffer<float> steps;

static const int Size = 32;
static const float Cell = 0.25;

struct BuiltField : IFieldVolume
{
    int cascadeOf(float3 p, float margin)
    {
        float3 q = (p - corner()) / Cell;
        return all(q >= margin) && all(q <= float(Size) - margin) ? 0 : -1;
    }
    float3 corner() { return float3(-Size * Cell * 0.5); }
    float4 cornerAndCell(int cascade) { return float4(corner(), Cell); }
    int size() { return Size; }
    float4 cellAt(int cascade, int3 at)
    {
        int i = ((at.z * Size + at.y) * Size + at.x) * 4;
        return float4(cells[i], cells[i + 1], cells[i + 2], cells[i + 3]);
    }
    float4 between(int cascade, float3 at)
    {
        float3 q = at - 0.5;
        int3 base = int3(floor(q));
        float3 f = q - float3(base);
        float4 sum = float4(0.0);
        for (int corner = 0; corner < 8; corner++)
        {
            int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
            float3 lerps = lerp(1.0 - f, f, float3(offset));
            sum += cellAt(cascade, clamp(base + offset, int3(0), int3(Size - 1))) * (lerps.x * lerps.y * lerps.z);
        }
        return sum;
    }
    float3 secondAt(int cascade, int3 at) { return float3(0.0); }
};

// The steps of a ray's march, written into its row of steps.
struct KeptSteps : IMarchSteps
{
    int ray;
    int taken;
    [mutating]
    void at(float t, float d)
    {
        if (taken < 64)
        {
            steps[(ray * 64 + taken) * 2] = t;
            steps[(ray * 64 + taken) * 2 + 1] = d;
        }
        taken++;
    }
};

[shader("compute")]
[numthreads(1, 1, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    BuiltField field;
    int i = int(id.x);
    if (i < int(points[0]))
    {
        float3 p = float3(points[1 + i * 3], points[2 + i * 3], points[3 + i * 3]);
        distances[i * 2] = fieldDistanceIn(field, 0, p);
        distances[i * 2 + 1] = fieldDistanceSheetIn(field, 0, p);
    }
    if (i < int(rays[0]))
    {
        int at = 1 + i * 8;
        float3 origin = float3(rays[at], rays[at + 1], rays[at + 2]);
        float3 way = float3(rays[at + 3], rays[at + 4], rays[at + 5]);
        hits[i * 2] = traceField(field, origin, way, rays[at + 6], rays[at + 7]);
        KeptSteps kept = { i, 0 };
        hits[i * 2 + 1] = traceFieldSheets(field, origin, way, rays[at + 6], rays[at + 7], kept);
    }
}
