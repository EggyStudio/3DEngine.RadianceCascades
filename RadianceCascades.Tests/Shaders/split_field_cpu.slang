// The split cascades' passes over a scene's field on the CPU, as an engine's compute kernels run
// them, one pass a dispatch, their rays traced through the field's march (FieldHits) and lit by
// the light each cell of the test's one cascade holds by the hit's place, or the field's own
// cell's, and by the bounce the setup holds, so the corridor's walk and the ladder's scenes read
// the split store beside the dense levels on the same field. One kernel holds the passes, picked
// by a value of the setup, so each target compiles every function.
import rc_split;

// Each cell's distance, the plane it keeps and its share of light through, five floats a cell, as
// probe_field_cpu.slang builds them, the field's cascades one after another, finest first.
RWStructuredBuffer<float> cells;

// The pass at 0, the eye from 1, the frame's count at 4, the store's slots, spacing and cascades
// from 5, the cascade a pass of the cascades runs on at 11, the counts of seen points and of points
// gathered at 12 and 13, the field's cells along a side at 14 and the count of its cascades at 15,
// each one's least corner and cell from 16, four floats each, the sky's light at 48, the least
// corner, the cell and the cells along a side of the grid cellLight holds from 49, the cap of steps
// a ray's part takes at 54, none where 0, at 57 whether a hit takes the light of the field's cell
// it lies in rather than the light of its place, at 58 where the bounce a hit takes begins in
// cellLight, none where 0, and from 59 the least corner of the bounce's grid, its cell and its
// cells along a side.
RWStructuredBuffer<float> setup;

// The light of each cell of the grid the setup gives from 49, x fastest, or of each cell of the
// field as cells holds them where the setup asks for it, then the bounce's where it holds one.
RWStructuredBuffer<float> cellLight;

// The seen points, each two float4, its place with 1 in w where the eye sees it, then its normal.
RWStructuredBuffer<float4> seen;

// The points gathered, each two float4, its place, then its normal.
RWStructuredBuffer<float4> gathered;

RWStructuredBuffer<uint> uints;
RWStructuredBuffer<float4> light;

// The light each point gathered takes.
RWStructuredBuffer<float4> taken;

struct FieldCells : IProbeField
{
    int cascades() { return int(setup[15]); }
    int size() { return int(setup[14]); }
    int index(int cascade, int3 at) { return ((cascade * size() + at.z) * size() + at.y) * size() + at.x; }
    int cascadeOf(float3 p, float margin)
    {
        for (int c = 0; c < cascades(); c++)
        {
            float4 oc = cornerAndCell(c);
            float3 q = (p - oc.xyz) / oc.w;
            if (all(q >= margin) && all(q <= float(size()) - margin))
                return c;
        }
        return -1;
    }
    float4 cornerAndCell(int cascade)
    {
        int at = 16 + cascade * 4;
        return float4(setup[at], setup[at + 1], setup[at + 2], setup[at + 3]);
    }
    float4 cellAt(int cascade, int3 at)
    {
        int i = index(cascade, at) * 5;
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
            sum += cellAt(cascade, clamp(base + offset, int3(0), int3(size() - 1))) * (lerps.x * lerps.y * lerps.z);
        }
        return sum;
    }
    float3 secondAt(int cascade, int3 at) { return float3(0.0); }
    float throughAt(int cascade, int3 at) { return cells[index(cascade, at) * 5 + 4]; }

    // The light of the cell of cellLight's grid the hit lies in, whatever cascade of the field
    // holds it, as the light traced to a point reads it, or the light of the field's cell where the
    // setup asks for it, and the bounce the setup holds.
    float3 hitLight(float3 place, float3 way, int cascade, int3 at)
    {
        if (setup[57] > 0.5)
            return float3(cellLight[index(cascade, at)] + bounceAt(place, way));
        int n = int(setup[53]);
        int3 q = clamp(int3(floor((place - float3(setup[49], setup[50], setup[51])) / setup[52])), int3(0), int3(n - 1));
        return float3(cellLight[(q.z * n + q.y) * n + q.x] + bounceAt(place, way));
    }

    // The light the round before lends a hit at place along way, the light the surface reflects of
    // the probes in the cell of the bounce's grid a cell back along the ray, as the dense passes
    // read it, and none where the setup holds none or that point lies outside the grid.
    float bounceAt(float3 place, float3 way)
    {
        if (setup[58] < 0.5)
            return 0.0;
        int n = int(setup[63]);
        int3 q = int3(floor((place - way * setup[62] - float3(setup[59], setup[60], setup[61])) / setup[62]));
        if (any(q < 0) || any(q >= n))
            return 0.0;
        return cellLight[uint(setup[58]) + uint((q.z * n + q.y) * n + q.x)];
    }
};

struct EvenSky : ISky
{
    float3 skyAlong(float3 way) { return float3(setup[48]); }
};

struct SeenPoints : IVisiblePoints
{
    float3 placeAt(uint index) { return seen[index * 2u].xyz; }
    float3 normalAt(uint index) { return seen[index * 2u + 1u].xyz; }
    void keepTaken(uint index, float3 value) {}
    bool seenAt(uint index) { return seen[index * 2u].w > 0.0; }
};

struct GatheredPoints : ISurfacePoints
{
    float3 placeAt(uint index) { return gathered[index * 2u].xyz; }
    float3 normalAt(uint index) { return gathered[index * 2u + 1u].xyz; }
    void keepTaken(uint index, float3 value) { taken[index] = float4(value, 1.0); }
};

// The store's uints changed plainly, which the CPU's dispatch, running its threads one after
// another, leaves as an atomic change would.
struct PlainUints : IStoreUints
{
    uint load(uint index) { return uints[index]; }
    void store(uint index, uint value) { uints[index] = value; }

    uint add(uint index, uint value)
    {
        uint held = uints[index];
        uints[index] = held + value;
        return held;
    }

    uint compareExchange(uint index, uint expected, uint desired)
    {
        uint held = uints[index];
        if (held == expected)
            uints[index] = desired;
        return held;
    }
};

struct BufferLight : IStoreLight
{
    float4 lightAt(uint index) { return light[index]; }
    void keepLight(uint index, float4 value) { light[index] = value; }
};

[shader("compute")]
[numthreads(64, 1, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    uint index = id.x;
    uint pass = uint(setup[0]);
    SplitFrame frame;
    frame.eye = float3(setup[1], setup[2], setup[3]);
    frame.frame = uint(setup[4]);
    SplitStore shape;
    shape.slots = uint(setup[5]);
    shape.spacing = setup[6];
    shape.cascades = uint(setup[7]);
    uint cascade = uint(setup[11]);
    SeenPoints points;
    GatheredPoints surfaces;
    PlainUints store;
    BufferLight kept;
    EvenSky sky;
    FieldHits<FieldCells> hits;
    hits.cap = uint(setup[54]);
    if (pass == 0u)
        clearSplitTable(store, frame, shape, index);
    else if (pass == 1u)
        keepSplitProbes(store, frame, shape, index);
    else if (pass == 2u && index < uint(setup[12]))
        askSeenProbes(points, hits, store, frame, shape, index);
    else if (pass == 3u)
        askCascadeProbes(hits, store, frame, shape, cascade, index);
    else if (pass == 4u && index < uint(setup[12]))
        traceSeenRays(points, hits, sky, store, frame, shape, index);
    else if (pass == 5u)
        keepSplitLight(store, kept, frame, shape, index);
    else if (pass == 6u)
        mergeSplitCascade(store, kept, sky, frame, shape, cascade, index);
    else if (pass == 7u && index < uint(setup[13]))
        gatherSurface(store, kept, hits, frame, shape, surfaces, index);
    else if (pass == 8u && index < uint(setup[13]))
    {
        // Each point's light from the probes about it it sees and from those standing by their
        // trilinear weights alone, and the weights of each.
        float3 p = surfaces.placeAt(index);
        float weights, plainWeights;
        float3 n = surfaces.normalAt(index);
        float3 origin = splitRayOrigin(frame, shape, p, n);
        float3 seenLight = splitFinestLight(store, kept, hits, shape, p, n, origin, true, weights);
        float3 plain = splitFinestLight(store, kept, hits, shape, p, n, origin, false, plainWeights);
        taken[index] = float4(seenLight.x, plain.x, weights, plainWeights);
    }
}
