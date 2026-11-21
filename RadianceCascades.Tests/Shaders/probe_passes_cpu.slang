// The passes over a chain of cascades of probes, a thread a texel or a surface point, as an
// engine's compute kernels run them, one pass a dispatch. A level's texels are traced, its reach
// map traced, the coarsest level's texels settled, a finer level's texels merged with the level
// above, and the light the finest level gives each surface point gathered. One kernel holds the
// passes, picked by a value of the setup, so each target compiles every function.
import rc_probe_passes;

// Each cell's distance, the plane it keeps and its share of light through, five floats a cell, as
// probe_field_cpu.slang builds them, the field's cascades one after another, finest first.
RWStructuredBuffer<float> cells;

// The field's least corner, its cell and its cells along each side, the sky's luminance, the pass,
// the level it runs on, the coarsest level's end and the count of levels, then from the float at
// 16 each level's cascade as ProbeCascade holds them, where its texels' rays, its reach texels and
// its texels' merged light begin in their buffers, its cap of steps and the texels along a side the
// level below and the gather read it at, sixteen floats a level, up to four. From the float at 80
// come what the test's binding sets, whether it moves probes off surfaces and the margin it moves
// them to, whether the sky varies with the way, the way toward the sun times its irradiance, the
// albedo of the surfaces it lights and whether a hit takes its light from cellLight. From the float
// at 96 come the count of the field's cascades, none for the one above, and each one's least corner
// and cell, four floats each, and from the float at 128 the count of surface points and each
// point's place, normal and albedo, seven floats each.
RWStructuredBuffer<float> setup;

// Each texel's ray as its trace ended, how, where, its carried share and its light, six floats a
// texel, each reach texel's reach, and each texel's merged light, three floats a texel.
RWStructuredBuffer<float> rayFloats;
RWStructuredBuffer<float> reachFloats;
RWStructuredBuffer<float> mergedFloats;

// Whether each texel's merge marched on itself and the steps that march took, two floats a texel.
RWStructuredBuffer<float> marches;

// The light each surface point takes and reflects, three floats each.
RWStructuredBuffer<float> surfaceFloats;

// The light a surface sends from each cell of the field, as the test's binding shades it ahead.
RWStructuredBuffer<float> cellLight;

static const int Levels = 16;
static const int Field = 96;
static const int Points = 128;

struct ProbeCells : IProbeField
{
    int cascades() { return max(int(setup[Field]), 1); }
    int size() { return int(setup[4]); }
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
        int at = setup[Field] < 0.5 ? 0 : Field + 1 + cascade * 4;
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
    // The walls and floors of these rooms send no light of their own, and where the setup gives a
    // sun they reflect its light as a diffuse surface at the field's normal does, and a sheet,
    // whose plane the cell keeps, as a leaf sends the light it takes on either side of it. Where
    // the setup asks for it, a hit takes the light cellLight holds for its cell.
    float3 hitLight(float3 place, float3 way, int cascade, int3 at)
    {
        if (setup[87] > 0.5)
            return float3(cellLight[index(cascade, at)]);
        float3 sun = float3(setup[83], setup[84], setup[85]);
        if (dot(sun, sun) <= 0.0)
            return float3(0.0);
        float lit = dot(fieldNormalIn(this, cascade, place), sun);
        float3 plane = cellAt(cascade, at).yzw;
        return float3(setup[86] / 3.14159265 * (dot(plane, plane) >= 0.25 ? abs(lit) : max(lit, 0.0)));
    }
};

// The sky the setup asks for, its light along every way, or brightest toward the top and toward x.
struct TestSky : ISky
{
    float3 skyAlong(float3 way)
    {
        float up = max(way.y, 0.0);
        return float3(setup[82] < 0.5 ? setup[5] : setup[5] * (0.6 + 0.4 * way.x + 1.5 * up * up));
    }
};

// The test's own rule for where a probe stands, at its grid place, or where the setup asks for it
// moved out along the field's normal to the margin off a surface it stands within the margin of.
struct TestPlaces : IProbePlaces
{
    ProbeCascade cascade;
    float3 placeOf(int3 probe)
    {
        float3 p = probePlace(cascade, probe);
        ProbeCells field;
        int c = field.cascadeOf(p, 1.0);
        if (setup[80] < 0.5 || c < 0)
            return p;
        float d = fieldDistanceIn(field, c, p);
        return d < setup[81] ? p + fieldNormalIn(field, c, p) * (setup[81] - d) : p;
    }
};

struct LevelRays : IProbeRays
{
    uint first;
    ProbeRay rayAt(uint index)
    {
        uint i = (first + index) * 6u;
        ProbeRay ray;
        ray.ended = uint(rayFloats[i]);
        ray.t = rayFloats[i + 1];
        ray.carried = rayFloats[i + 2];
        ray.light = float3(rayFloats[i + 3], rayFloats[i + 4], rayFloats[i + 5]);
        return ray;
    }
    void keepRay(uint index, ProbeRay ray)
    {
        uint i = (first + index) * 6u;
        rayFloats[i] = float(ray.ended);
        rayFloats[i + 1] = ray.t;
        rayFloats[i + 2] = ray.carried;
        rayFloats[i + 3] = ray.light.x;
        rayFloats[i + 4] = ray.light.y;
        rayFloats[i + 5] = ray.light.z;
    }
};

struct LevelReach : IReachTexels
{
    uint first;
    void keepReach(uint index, float reach) { reachFloats[first + index] = reach; }
};

struct LevelMerged : IMergedLight
{
    uint first;
    void keepMerged(uint index, float3 light)
    {
        uint i = (first + index) * 3u;
        mergedFloats[i] = light.x;
        mergedFloats[i + 1] = light.y;
        mergedFloats[i + 2] = light.z;
    }
};

// A level's merged light and reach map as the level below it, or the gather, reads them, at the
// texels it keeps, where fewer than it traces each the mean of the traced texels it covers.
struct LevelProbes : IFarProbes
{
    ProbeCascade cascade;
    uint merged;
    uint reach;
    uint kept;
    float3 tracedAt(int3 probe, uint2 texel)
    {
        uint i = (merged + cascadeTexelIndex(cascade, probe, texel)) * 3u;
        return float3(mergedFloats[i], mergedFloats[i + 1], mergedFloats[i + 2]);
    }
    float3 lightAt(int3 probe, uint2 texel)
    {
        if (kept == 0u || kept >= cascade.texels)
            return tracedAt(probe, texel);
        uint k = cascade.texels / kept;
        float3 sum = float3(0.0);
        for (uint j = 0u; j < k; j++)
            for (uint i = 0u; i < k; i++)
                sum += tracedAt(probe, texel * k + uint2(i, j));
        return sum / float(k * k);
    }
    float reachAt(int3 probe, uint2 texel) { return reachFloats[reach + reachTexelIndex(cascade, probe, texel)]; }
};

struct SurfacePoints : ISurfacePoints
{
    float3 placeAt(uint index) { uint i = uint(Points) + 1u + index * 7u; return float3(setup[i], setup[i + 1], setup[i + 2]); }
    float3 normalAt(uint index) { uint i = uint(Points) + 4u + index * 7u; return float3(setup[i], setup[i + 1], setup[i + 2]); }
    void keepTaken(uint index, float3 taken)
    {
        float3 reflected = reflectedLight(float3(setup[uint(Points) + 7u + index * 7u]), taken);
        surfaceFloats[index * 6u] = taken.x;
        surfaceFloats[index * 6u + 1u] = taken.y;
        surfaceFloats[index * 6u + 2u] = taken.z;
        surfaceFloats[index * 6u + 3u] = reflected.x;
        surfaceFloats[index * 6u + 4u] = reflected.y;
        surfaceFloats[index * 6u + 5u] = reflected.z;
    }
};

struct CountedSteps : IMarchSteps
{
    uint taken;
    [mutating]
    void at(float t, float d) { taken++; }
};

ProbeCascade levelCascade(int level)
{
    int at = Levels + level * 16;
    ProbeCascade cascade;
    cascade.corner = float3(setup[at], setup[at + 1], setup[at + 2]);
    cascade.spacing = setup[at + 3];
    cascade.count = int3(int(setup[at + 4]), int(setup[at + 5]), int(setup[at + 6]));
    cascade.texels = uint(setup[at + 7]);
    cascade.start = setup[at + 8];
    cascade.end = setup[at + 9];
    cascade.stepCap = uint(setup[at + 13]);
    return cascade;
}

// Where a level's rays, its reach texels or its merged light begin in their buffers, at 10, 11
// and 12 of its sixteen floats, its cap of steps at 13 and the texels it keeps at 14.
uint firstOf(int level, int which) { return uint(setup[Levels + level * 16 + which]); }

// A level's cascade as the level below and the gather read it, at the texels it keeps.
ProbeCascade keptCascade(int level)
{
    ProbeCascade cascade = levelCascade(level);
    uint kept = firstOf(level, 14);
    if (kept > 0u)
        cascade.texels = kept;
    return cascade;
}

TestPlaces placesOf(int level)
{
    TestPlaces places = { levelCascade(level) };
    return places;
}

LevelProbes probesOf(int level)
{
    LevelProbes probes = { levelCascade(level), firstOf(level, 12), firstOf(level, 11), firstOf(level, 14) };
    return probes;
}

[shader("compute")]
[numthreads(64, 1, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    uint index = id.x;
    ProbeCells field;
    TestSky sky;
    uint pass = uint(setup[6]);
    int level = int(setup[7]);
    ProbeCascade cascade = levelCascade(level);
    LevelRays rays = { firstOf(level, 10) };
    LevelMerged merged = { firstOf(level, 12) };
    if (pass == 0u)
        traceTexel(field, rays, placesOf(level), cascade, index);
    else if (pass == 1u)
    {
        LevelReach reach = { firstOf(level, 11) };
        reachTexel(field, reach, placesOf(level), cascade, index);
    }
    else if (pass == 2u)
        settleTexel(rays, merged, cascade, sky, index);
    else if (pass == 3u)
    {
        CountedSteps counted = { 0u };
        bool marched = mergeTexel(field, rays, placesOf(level), probesOf(level + 1), placesOf(level + 1), merged, cascade, keptCascade(level + 1),
            setup[8], sky, counted, index);
        if (index < cascadeTexels(cascade))
        {
            marches[(firstOf(level, 10) + index) * 2u] = marched ? 1.0 : 0.0;
            marches[(firstOf(level, 10) + index) * 2u + 1u] = float(counted.taken);
        }
    }
    else if (pass == 5u)
    {
        // The finest level's texel marched on itself from its probe to the coarsest level's end,
        // the reference a merge is read against.
        if (index >= cascadeTexels(cascade))
            return;
        uint texels = cascade.texels * cascade.texels;
        float3 way = texelWay(cascade.texels, uint2((index % texels) % cascade.texels, (index % texels) / cascade.texels));
        ProbeRay ray = traceProbeRay(field, placesOf(level).placeOf(probeAt(cascade, index / texels)), way, 0.0, setup[8]);
        merged.keepMerged(index, lastCascadeLight(ray, way, sky));
    }
    else if (index < uint(setup[Points]))
    {
        SurfacePoints surfaces;
        gatherAt(field, probesOf(0), placesOf(0), keptCascade(0), surfaces, index);
    }
}
