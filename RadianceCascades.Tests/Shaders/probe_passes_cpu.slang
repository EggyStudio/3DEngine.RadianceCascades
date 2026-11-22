// The passes over a chain of cascades of probes, a thread a texel or a surface point, as an
// engine's compute kernels run them, one pass a dispatch. A level's texels are traced, its reach
// map traced, the coarsest level's texels settled, a finer level's texels merged with the level
// above, and the light the finest level gives each surface point gathered. One kernel holds the
// passes, picked by a value of the setup, so each target compiles every function.
import rc_probe_passes;
import rc_reach_sight_merge;
import rc_marched_sight_merge;
import rc_cone_read;

// Each cell's distance, the plane it keeps and its share of light through, five floats a cell, as
// probe_field_cpu.slang builds them, the field's cascades one after another, finest first.
RWStructuredBuffer<float> cells;

// The field's least corner, its cell and its cells along each side, the sky's luminance, the pass,
// the level it runs on, the coarsest level's end and the count of levels, at 10 where the bounce a
// hit takes begins in cellLight, none where 0, and at 11 its grid's cells along a side, then from
// the float at 16 each level's cascade as ProbeCascade holds them, where its texels' rays, its
// reach texels and its texels' merged light begin in their buffers, its cap of steps and the texels
// along a side the level below and the gather read it at, sixteen floats a level, the first four
// there and any past them after the surface points. From the float at 80 come what the test's
// binding sets, whether it moves probes off surfaces and the margin it moves them to, whether the
// sky varies with the way, the way toward the sun times its irradiance, the albedo of the surfaces
// it lights and whether a hit takes its light from cellLight, at 91 the most a moved probe moves as
// a share of its spacing, none where 0, and from 92 the least corner of the bounce's grid and its
// cell. From the float at 96 come the count of the field's cascades, none for the one above, and
// each one's least corner and cell, four floats each, at 121 the count of cones pass 11 reads, at
// 122 whether each surface point is gathered from the finest level about it, at 123 whether a hit's
// light is read from cellLight by its place in the cascade the float at 0 begins, at 124 where the
// surface points' shares of the sky begin in skyFloats, at 125 whether the finer levels' merges
// lend no sky, at 127 the merge the passes take, 0 the merge at the ray's end, 1 by reach sight and
// 2 by marched sight, and from the float at 128 the count of surface points and each point's place,
// normal and albedo, seven floats each.
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

// The light held for each cell of the field's cascades one after another, twelve floats a cell,
// each of its two sides' own light and then its probes' light, which pass 7 holds and the merge
// reads where the setup sets 126.
RWStructuredBuffer<float> heldFloats;

// The steps each texel's trace took, one float a texel, the levels one after another.
RWStructuredBuffer<float> traceStepFloats;

// The cells whose light pass 8 lists for pass 9 to hold, their count first and then each cell as
// holdCellLight counts them.
RWStructuredBuffer<uint> heldList;

// The reads of a level's probes, of a texel's light, its share of the sky or a reach texel, each
// texel's merge made, one float a texel, the levels one after another, then from the float the
// setup gives at 124 each surface point's gather, the cost of a merge beside its steps.
RWStructuredBuffer<float> readFloats;

// Each probe's value of the per-probe stage and the steps its marches took, two a probe, the
// levels one after another, which pass 10 keeps for the merge by marched sight to read.
RWStructuredBuffer<uint> stageValues;

// Each cone pass 11 reads, nine floats a cone, the surface point it is read at, the way toward the
// eye and the roughness, which the test gives, and the light it reflects and its reads, which the
// pass keeps.
RWStructuredBuffer<float> coneFloats;

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
    // A hit's light as the test's binding shades it, its own and, where the setup gives the
    // probes' light at 90, that light weighed by the share at 89.
    float3 hitLight(float3 place, float3 way, int cascade, int3 at)
    {
        return ownLight(place, way, cascade, at) + float3(setup[89] * setup[90]) + float3(bounceAt(place, way));
    }

    // The light the round before lends a hit at place along way by the bounce the setup holds, the
    // light the surface reflects of the probes in the cell of its grid a cell back along the ray,
    // and none where the setup holds none or that point lies outside the grid. A march stops at a
    // sheet's plane inside a wall one cell thick, whose cell lies between the wall's two sides, and
    // a cell back along the ray lies on the side the ray came from.
    float bounceAt(float3 place, float3 way)
    {
        if (setup[10] < 0.5)
            return 0.0;
        int n = int(setup[11]);
        int3 q = int3(floor((place - way * setup[95] - float3(setup[92], setup[93], setup[94])) / setup[95]));
        if (any(q < 0) || any(q >= n))
            return 0.0;
        return cellLight[uint(setup[10]) + uint((q.z * n + q.y) * n + q.x)];
    }

    // A hit's own light, the cell's where the setup gives the cells' light, or the sun's on a
    // surface of albedo 86, a sheet lit from either side, or where the setup sets 88 on the face
    // the ray meets alone, by the normal of the sheet's plane on that side.
    float3 ownLight(float3 place, float3 way, int cascade, int3 at)
    {
        if (setup[87] > 0.5 && setup[123] > 0.5)
        {
            // The cell of the test's one cascade at the setup's corner the hit lies in, whatever
            // cascade of the field holds it, as the light traced to a point reads it.
            int3 q = clamp(int3(floor((place - float3(setup[0], setup[1], setup[2])) / setup[3])), int3(0), int3(size() - 1));
            return float3(cellLight[(q.z * size() + q.y) * size() + q.x]);
        }
        if (setup[87] > 0.5)
            return float3(cellLight[index(cascade, at)]);
        float3 sun = float3(setup[83], setup[84], setup[85]);
        if (dot(sun, sun) <= 0.0)
            return float3(0.0);
        float lit = dot(fieldNormalIn(this, cascade, place), sun);
        float3 plane = cellAt(cascade, at).yzw;
        if (dot(plane, plane) >= 0.25)
            lit = setup[88] > 0.5 ? dot(dot(plane, way) < 0.0 ? normalize(plane) : -normalize(plane), sun) : abs(lit);
        return float3(setup[86] / 3.14159265 * max(lit, 0.0));
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
        // Where the setup sets 91, a probe moves no more than that share of its spacing.
        float most = setup[91] > 0.0 ? setup[91] * cascade.spacing : 1e30;
        return d < setup[81] ? p + fieldNormalIn(field, c, p) * min(setup[81] - d, most) : p;
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
// Each read is counted in readFloats at slot, the thread's own.
struct LevelProbes : IFarSkyProbes
{
    ProbeCascade cascade;
    uint merged;
    uint reach;
    uint slot;
    float3 lightAt(int3 probe, uint2 texel)
    {
        readFloats[slot] += 1.0;
        uint i = (merged + cascadeTexelIndex(cascade, probe, texel)) * 3u;
        return float3(mergedFloats[i], mergedFloats[i + 1], mergedFloats[i + 2]);
    }
    float reachAt(int3 probe, uint2 texel)
    {
        readFloats[slot] += 1.0;
        return reachFloats[reach + reachTexelIndex(cascade, probe, texel)];
    }
    float skyShareAt(int3 probe, uint2 texel)
    {
        readFloats[slot] += 1.0;
        return skyFloats[merged + cascadeTexelIndex(cascade, probe, texel)];
    }
};

// The light held for the field's cells, as a binding holds it in a texture of its own.
struct HeldCells : IHeldLight
{
    ProbeCells field;
    float3 read(uint i) { return float3(heldFloats[i], heldFloats[i + 1u], heldFloats[i + 2u]); }
    float3 heldAt(int cascade, int3 cell, uint side) { return read(uint(field.index(cascade, cell)) * 12u + side * 6u); }
    float3 heldProbeLightAt(int cascade, int3 cell, uint side) { return read(uint(field.index(cascade, cell)) * 12u + side * 6u + 3u); }
    // The share of the probes' light a hit takes, as the setup gives it at 89 when the merges run.
    float probeShare(float3 place, int cascade, int3 cell) { return setup[89]; }
    void hold(int cascade, int3 cell, uint side, float3 own, float3 probes)
    {
        uint i = uint(field.index(cascade, cell)) * 12u + side * 6u;
        heldFloats[i] = own.x;
        heldFloats[i + 1u] = own.y;
        heldFloats[i + 2u] = own.z;
        heldFloats[i + 3u] = probes.x;
        heldFloats[i + 4u] = probes.y;
        heldFloats[i + 5u] = probes.z;
    }
};

// The list of held cells as a binding keeps it, its count added to with no atomic, since the CPU
// runs the kernel's threads one after another.
struct HeldList : IHeldList
{
    void list(uint at)
    {
        heldList[1u + heldList[0]] = at;
        heldList[0] = heldList[0] + 1u;
    }
    uint count() { return heldList[0]; }
    uint listedAt(uint index) { return heldList[1u + index]; }
};

// The test's binding's shading of a held cell, its own light and the probes' light the setup gives
// at 90 apart.
struct HeldShading : IHeldShading
{
    ProbeCells field;
    float3 ownLight(float3 place, float3 way, int cascade, int3 cell) { return field.ownLight(place, way, cascade, cell); }
    float3 probeLight(float3 place, float3 way, int cascade, int3 cell) { return float3(setup[90]); }
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

// Where a level's sixteen floats begin in the setup, the first four's from the float at 16 and any
// past them after the surface points.
int levelAt(int level)
{
    return level < 4 ? Levels + level * 16 : Points + 1 + int(setup[Points]) * 7 + (level - 4) * 16;
}

ProbeCascade levelCascade(int level)
{
    int at = levelAt(level);
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
uint firstOf(int level, int which) { return uint(setup[levelAt(level) + which]); }

// The level a surface point at p is gathered from, the finest whose probes stand about it on every
// side where the setup sets 122, as a binding gathers each pixel from the finest cascade that holds
// it, and otherwise the finest level.
int gatherLevel(float3 p)
{
    if (setup[122] < 0.5)
        return 0;
    int levels = int(setup[9]);
    for (int l = 0; l < levels - 1; l++)
    {
        ProbeCascade cascade = levelCascade(l);
        float3 q = (p - cascade.corner) / cascade.spacing;
        if (all(q >= 0.0) && all(q <= float3(cascade.count - 1)))
            return l;
    }
    return levels - 1;
}

// Where a level's probes begin in stageValues, the probes of the levels below it before them.
uint firstProbe(int level)
{
    uint first = 0u;
    for (int l = 0; l < level; l++)
        first += cascadeProbes(levelCascade(l));
    return first;
}

// The values the per-probe stage kept for a level's probes, read in the order probeAt reads them.
struct LevelStage : IStageValues
{
    ProbeCascade cascade;
    uint first;
    uint valueAt(int3 probe)
    {
        return stageValues[(first + uint((probe.z * cascade.count.y + probe.y) * cascade.count.x + probe.x)) * 2u];
    }
};

// Keeps a level's per-probe values where LevelStage reads them.
struct LevelStageKeep : IStageKeep
{
    uint first;
    void keepStage(uint index, uint value) { stageValues[(first + index) * 2u] = value; }
};

// A level's probes as the level below or the gather reads them, at read texels a side, the mean
// of the traced texels each holds, or as traced where read is 0 or not fewer, with their shares of
// the sky, their reads counted at slot.
TracedSkyMeans<LevelProbes> meansOf(int level, uint read, uint slot)
{
    TracedSkyMeans<LevelProbes> probes;
    probes.means.traced = probesOf(level, slot);
    probes.means.texels = levelCascade(level).texels;
    probes.means.perRead = read > 0u && read < probes.means.texels ? probes.means.texels / read : 1u;
    return probes;
}

TestPlaces placesOf(int level)
{
    TestPlaces places = { levelCascade(level) };
    return places;
}

LevelProbes probesOf(int level, uint slot)
{
    LevelProbes probes = { levelCascade(level), firstOf(level, 12), firstOf(level, 11), slot };
    return probes;
}

// mergeTexel by the merge the setup names at 127.
bool mergeBy<F : IProbeField>(F field, LevelRays rays, TracedSkyMeans<LevelProbes> above, LevelMerged merged, int level,
    ProbeCascade aboveCascade, TestSky sky, inout CountedSteps counted, uint index)
{
    ProbeCascade cascade = levelCascade(level);
    NoStageValues none;
    if (setup[127] > 1.5)
    {
        MergeByMarchedSight merge;
        LevelStage stage = { cascade, firstProbe(level) };
        return mergeTexel(merge, stage, field, rays, placesOf(level), above, placesOf(level + 1), merged, cascade, aboveCascade, setup[8],
            sky, counted, index);
    }
    if (setup[127] > 0.5)
    {
        MergeByReachSight merge;
        return mergeTexel(merge, none, field, rays, placesOf(level), above, placesOf(level + 1), merged, cascade, aboveCascade, setup[8],
            sky, counted, index);
    }
    MergeAtRayEnd merge;
    return mergeTexel(merge, none, field, rays, placesOf(level), above, placesOf(level + 1), merged, cascade, aboveCascade, setup[8], sky,
        counted, index);
}

// The levels as a cone's read takes them, each read at the texels it keeps as its gather reads it,
// each read of a texel or a reach texel counted at slot of coneFloats.
struct ConeLevels : IConeLevels
{
    uint slot;
    int levels() { return int(setup[9]); }
    TracedSkyMeans<LevelProbes> kept(int level) { return meansOf(level, firstOf(level, 14), uint(setup[124] + setup[Points])); }
    ProbeCascade cascadeOf(int level) { return readCascade(levelCascade(level), kept(level).means.perRead); }
    float3 lightAt(int level, int3 probe, uint2 texel)
    {
        coneFloats[slot] += 1.0;
        return kept(level).lightAt(probe, texel);
    }
    float reachAt(int level, int3 probe, uint2 texel)
    {
        coneFloats[slot] += 1.0;
        return kept(level).reachAt(probe, texel);
    }
    float3 placeOf(int level, int3 probe) { return placesOf(level).placeOf(probe); }
};

// The cones as coneFloats holds them, each at its surface point, read from the level the point's
// gather reads.
struct ConePoints : IConePoints
{
    SurfacePoints surfaces;
    uint pointOf(uint index) { return uint(coneFloats[index * 9u]); }
    float3 placeAt(uint index) { return surfaces.placeAt(pointOf(index)); }
    float3 normalAt(uint index) { return surfaces.normalAt(pointOf(index)); }
    float3 viewAt(uint index) { uint i = index * 9u; return float3(coneFloats[i + 1u], coneFloats[i + 2u], coneFloats[i + 3u]); }
    float roughnessAt(uint index) { return coneFloats[index * 9u + 4u]; }
    int levelAt(uint index) { return gatherLevel(placeAt(index)); }
    void keepReflected(uint index, float3 light)
    {
        coneFloats[index * 9u + 5u] = light.x;
        coneFloats[index * 9u + 6u] = light.y;
        coneFloats[index * 9u + 7u] = light.z;
    }
};

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
        TracedSkyMeans<LevelProbes> above = meansOf(level + 1, read, firstOf(level, 10) + index);
        // Where the setup sets 126, the merge reads at each hit the light held for the cell it lies
        // in (HeldHitLight), as a binding passes it to the merge in place of its own shading.
        ProbeCascade aboveCascade = readCascade(levelCascade(level + 1), above.means.perRead);
        bool marched;
        if (setup[126] > 0.5)
        {
            HeldHitLight<ProbeCells, HeldCells> heldField = { field, { field } };
            marched = mergeBy(heldField, rays, above, merged, level, aboveCascade, sky, counted, index);
        }
        else
            marched = mergeBy(field, rays, above, merged, level, aboveCascade, sky, counted, index);
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
    else if (pass == 10u)
    {
        // The per-probe stage of the merge by marched sight, a thread a probe of the level, its
        // lines of sight to the level above's probes about each, the steps they took kept beside.
        CountedSteps counted = { 0u };
        FarSightLines lines;
        LevelStageKeep kept = { firstProbe(level) };
        stageProbe(lines, field, placesOf(level), placesOf(level + 1), cascade, levelCascade(level + 1), kept, counted, index);
        if (index < cascadeProbes(cascade))
            stageValues[(firstProbe(level) + index) * 2u + 1u] = counted.taken;
    }
    else if (pass == 7u)
    {
        // The light of every cell of the field held at once, the share 1.
        HeldCells held = { field };
        HeldShading shading = { field };
        holdCellLight(field, shading, held, uint(field.cascades()), 1u, 0u, index);
    }
    else if (pass == 8u)
    {
        // The cells whose light pass 9 holds listed, every cell of the field at once.
        HeldList list;
        listHeldCell(field, list, uint(field.cascades()), 1u, 0u, index);
    }
    else if (pass == 9u)
    {
        // The light of the listed cells held, a thread a listed cell.
        HeldCells held = { field };
        HeldShading shading = { field };
        HeldList list;
        holdListedCell(field, shading, held, list, index);
    }
    else if (pass == 6u)
    {
        // The surface points' own rays handed to the finest level, read at the texels the setup
        // asks for where it gives them, as the merge reads a level, and otherwise at those it
        // keeps.
        if (index >= uint(setup[Points]))
            return;
        SurfacePoints surfaces;
        int g = gatherLevel(surfaces.placeAt(index));
        uint read = firstOf(g, 15) > 0u ? firstOf(g, 15) : firstOf(g, 14);
        TracedSkyMeans<LevelProbes> finest = meansOf(g, read, uint(setup[124]) + index);
        NoSteps none;
        ProbeCascade gathered = readCascade(levelCascade(g), finest.means.perRead);
        if (setup[127] > 1.5)
        {
            MergeByMarchedSight merge;
            gatherTraced(merge, field, finest, placesOf(g), gathered, setup[8], sky, surfaces, none, index);
        }
        else if (setup[127] > 0.5)
        {
            MergeByReachSight merge;
            gatherTraced(merge, field, finest, placesOf(g), gathered, setup[8], sky, surfaces, none, index);
        }
        else
        {
            MergeAtRayEnd merge;
            gatherTraced(merge, field, finest, placesOf(g), gathered, setup[8], sky, surfaces, none, index);
        }
    }
    else if (pass == 11u)
    {
        // Each cone a surface point reflects toward its eye, read from the merged levels.
        if (index >= uint(setup[121]))
            return;
        ConePoints points;
        ConeLevels levels = { index * 9u + 8u };
        ConeAtProbes cone;
        readCone(cone, levels, points, index);
    }
    else if (index < uint(setup[Points]))
    {
        SurfacePoints surfaces;
        int g = gatherLevel(surfaces.placeAt(index));
        TracedSkyMeans<LevelProbes> finest = meansOf(g, firstOf(g, 14), uint(setup[124]) + index);
        gatherAt(field, finest, placesOf(g), readCascade(levelCascade(g), finest.means.perRead), surfaces, index);
    }
}
