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
// and cell, four floats each, at 124 where the surface points' shares of the sky begin in
// skyFloats, at 125 whether the finer levels' merges lend no sky, and from the float at 128 the
// count of surface points and each point's place, normal and albedo, seven floats each.
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

// Each texel's merged share of the sky, one float a texel, the levels one after another, then from
// the float the setup gives at 124 each surface point's share.
RWStructuredBuffer<float> skyFloats;

// The light held for each cell of the field's cascades one after another, three floats a cell,
// which pass 7 holds and the merge reads where the setup sets 126.
RWStructuredBuffer<float> heldFloats;

// The steps each texel's trace took, one float a texel, the levels one after another.
RWStructuredBuffer<float> traceStepFloats;

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
struct TestSky : ISkyShare
{
    // 1 where the sky is lent whole, 0 where the setup gates it off for a level, light and share
    // alike.
    float on;
    float3 skyAlong(float3 way)
    {
        float up = max(way.y, 0.0);
        return on * float3(setup[82] < 0.5 ? setup[5] : setup[5] * (0.6 + 0.4 * way.x + 1.5 * up * up));
    }
    float shareAlong(float3 way) { return on; }
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

struct LevelMerged : IMergedSkyLight
{
    uint first;
    void keepMerged(uint index, float3 light)
    {
        uint i = (first + index) * 3u;
        mergedFloats[i] = light.x;
        mergedFloats[i + 1] = light.y;
        mergedFloats[i + 2] = light.z;
    }
    void keepSkyShare(uint index, float share) { skyFloats[first + index] = share; }
};

// A level's merged light, its share of the sky and its reach map as traced, which the level below
// it and the gather read through TracedSkyMeans.
struct LevelProbes : IFarSkyProbes
{
    ProbeCascade cascade;
    uint merged;
    uint reach;
    float3 lightAt(int3 probe, uint2 texel)
    {
        uint i = (merged + cascadeTexelIndex(cascade, probe, texel)) * 3u;
        return float3(mergedFloats[i], mergedFloats[i + 1], mergedFloats[i + 2]);
    }
    float reachAt(int3 probe, uint2 texel) { return reachFloats[reach + reachTexelIndex(cascade, probe, texel)]; }
    float skyShareAt(int3 probe, uint2 texel) { return skyFloats[merged + cascadeTexelIndex(cascade, probe, texel)]; }
};

// The light held for the field's cells, as a binding holds it in a texture of its own.
struct HeldCells : IHeldLight
{
    ProbeCells field;
    float3 heldAt(int cascade, int3 cell)
    {
        uint i = uint(field.index(cascade, cell)) * 3u;
        return float3(heldFloats[i], heldFloats[i + 1u], heldFloats[i + 2u]);
    }
    void hold(int cascade, int3 cell, float3 light)
    {
        uint i = uint(field.index(cascade, cell)) * 3u;
        heldFloats[i] = light.x;
        heldFloats[i + 1u] = light.y;
        heldFloats[i + 2u] = light.z;
    }
};

struct SurfacePoints : ISurfaceSkyPoints
{
    void keepSkyShare(uint index, float share) { skyFloats[uint(setup[124]) + index] = share; }
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
// and 12 of its sixteen floats, its cap of steps at 13, the texels it keeps at 14 and at 15 those
// the level below's merge reads it at, 0 for those it keeps.
uint firstOf(int level, int which) { return uint(setup[Levels + level * 16 + which]); }

// A level's probes as the level below or the gather reads them, at read texels a side, the mean
// of the traced texels each holds, or as traced where read is 0 or not fewer, with their shares of
// the sky.
TracedSkyMeans<LevelProbes> meansOf(int level, uint read)
{
    TracedSkyMeans<LevelProbes> probes;
    probes.means.traced = probesOf(level);
    probes.means.texels = levelCascade(level).texels;
    probes.means.perRead = read > 0u && read < probes.means.texels ? probes.means.texels / read : 1u;
    return probes;
}

TestPlaces placesOf(int level)
{
    TestPlaces places = { levelCascade(level) };
    return places;
}

LevelProbes probesOf(int level)
{
    LevelProbes probes = { levelCascade(level), firstOf(level, 12), firstOf(level, 11) };
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
    // Where the setup sets 125, a finer level's merge lends no sky to a ray that leaves the
    // field, as a binding gates its sky before its field holds every mesh in reach, and the
    // coarsest level's settle lends it whole.
    sky.on = pass == 3u && setup[125] > 0.5 ? 0.0 : 1.0;
    int level = int(setup[7]);
    ProbeCascade cascade = levelCascade(level);
    LevelRays rays = { firstOf(level, 10) };
    LevelMerged merged = { firstOf(level, 12) };
    if (pass == 0u)
    {
        CountedSteps traced = { 0u };
        traceTexel(field, rays, placesOf(level), cascade, traced, index);
        if (index < cascadeTexels(cascade))
            traceStepFloats[firstOf(level, 10) + index] = float(traced.taken);
    }
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
        // The level above as the merge reads it, at the texels the setup asks for where it gives
        // them, as a binding that reads a level above at the side it traced it at does, and
        // otherwise at those it keeps.
        uint read = firstOf(level + 1, 15) > 0u ? firstOf(level + 1, 15) : firstOf(level + 1, 14);
        TracedSkyMeans<LevelProbes> above = meansOf(level + 1, read);
        // Where the setup sets 126, the merge reads at each hit the light held for the cell it lies
        // in (HeldHitLight), as a binding passes it to the merge in place of its own shading.
        ProbeCascade aboveCascade = readCascade(levelCascade(level + 1), above.means.perRead);
        bool marched;
        if (setup[126] > 0.5)
        {
            HeldHitLight<ProbeCells, HeldCells> heldField = { field, { field } };
            marched = mergeTexel(heldField, rays, placesOf(level), above, placesOf(level + 1), merged, cascade, aboveCascade, setup[8], sky,
                counted, index);
        }
        else
            marched = mergeTexel(field, rays, placesOf(level), above, placesOf(level + 1), merged, cascade, aboveCascade, setup[8], sky,
                counted, index);
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
    else if (pass == 7u)
    {
        // The light of every cell of the field held at once, the share 1.
        HeldCells held = { field };
        holdCellLight(field, held, uint(field.cascades()), 1u, 0u, index);
    }
    else if (pass == 6u)
    {
        // The surface points' own rays handed to the finest level, read at the texels the setup
        // asks for where it gives them, as the merge reads a level, and otherwise at those it
        // keeps.
        if (index >= uint(setup[Points]))
            return;
        SurfacePoints surfaces;
        uint read = firstOf(0, 15) > 0u ? firstOf(0, 15) : firstOf(0, 14);
        TracedSkyMeans<LevelProbes> finest = meansOf(0, read);
        NoSteps none;
        gatherTraced(field, finest, placesOf(0), readCascade(levelCascade(0), finest.means.perRead), setup[8], sky, surfaces, none, index);
    }
    else if (index < uint(setup[Points]))
    {
        SurfacePoints surfaces;
        TracedSkyMeans<LevelProbes> finest = meansOf(0, firstOf(0, 14));
        gatherAt(field, finest, placesOf(0), readCascade(levelCascade(0), finest.means.perRead), surfaces, index);
    }
}
