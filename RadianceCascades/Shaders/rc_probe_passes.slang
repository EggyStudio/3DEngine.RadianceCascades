// The passes over a chain of cascades of probes, each function the work of one thread of an
// engine's compute kernel at the index it hands it, as splatCodes takes a lane, every read and
// write through an interface the engine implements over its own buffers, which keeps its bindings
// and its threads. A pass reads only what an earlier dispatch wrote, so the engine runs them as
// dispatches in order. Each level's texels are traced (traceTexel) and its reach map traced
// (reachTexel), then the coarsest level's texels are settled (settleTexel) and each finer level's
// merged with the merged light of the level above (mergeTexel), from the coarsest down, and last
// the light the finest level gives each surface point is gathered (gatherAt).
//
// Each level's rays begin at its probe, its start 0, and run to its end, which the engine sets in
// its ProbeCascade, the coarsest's past the field's edge, so its rays end at a surface or leave
// the field. A finer level's end is where its rays are handed to the level above, whose probes
// about that point stand up to a spacing of theirs off the ray and meet an edge beyond it where
// their own parallel rays do, so the nearer the point lies to an edge the more the edge is smeared
// over the ways about it. The ends are kept short against the spacings above. The tests' middle
// level of probes a unit apart reaches 2, twice its spacing, and hands a ray from the floor to
// the far probes, 2 apart, 4 units out. A ray that reaches its end near a surface marches on to
// open space before it is handed on (mergedLight, marchToOpenSpace), which mends an edge within
// the field's band past the end and not one farther, and one whose far probes disagree on where a
// surface lies along its way marches across the far interval itself (HitSpread), which mends an
// opening's edge the far probes beside the ray see past. An open box's floor reads 0.3 percent
// under its share of the sky with the middle level reaching 2 or 4 alike, where with the far
// probes taken as they stand, reaching 4 handed the ray on within 2 of the rim and read the floor
// 12.4 percent under. A level's reach map runs to its end too, so that end lies past the diagonal
// of the cell between its probes, the spacing times the root of 3, for the seeing of a point among
// them to be read within the map, and keeps ReachOpen for a way that meets no surface within it
// (reachAlong). A texel's index
// is its probe's, x fastest, times the texels of a probe, and its texel's along a row of the
// octahedron, x fastest (cascadeTexelIndex), and a reach texel's the same over ReachTexels a side.
//
// The engine gives each level's probes their places (IProbePlaces, gridPlaces where they stand on
// the grid), the light of a surface a ray meets (IProbeField.hitLight) and the sky along a way
// (ISky, UniformSky where it is one light), and every pass reads them where it needs them.
module rc_probe_passes;

__exported import rc_cascade_merge;

// The rays a cascade's texels traced, as the trace keeps them and the merge reads them, which an
// engine implements over its buffers.
public interface IProbeRays
{
    // The ray the texel at index traced (cascadeTexelIndex).
    ProbeRay rayAt(uint index);

    // Keeps the ray the texel at index traced.
    void keepRay(uint index, ProbeRay ray);
}

// A cascade's reach maps as their trace keeps them, which an engine implements over its buffers and
// reads back through IFarProbes.
public interface IReachTexels
{
    // Keeps how far the reach texel at index went (reachTexelIndex).
    void keepReach(uint index, float reach);
}

// A cascade's merged light as the merge keeps it, which an engine implements over its buffers and
// reads back through IFarProbes for the level below and the gather.
public interface IMergedLight
{
    // Keeps the light the texel at index brings from every distance past its probe.
    void keepMerged(uint index, float3 light);
}

// A cascade's merged light with the share of the sky in it, as the merge keeps them, which the
// level below reads back through IFarSkyProbes.
public interface IMergedSkyLight : IMergedLight
{
    // Keeps the share of the sky in the light the texel at index brings, what its light would be
    // under a sky of 1 along every way with every surface unlit, from 0 to 1.
    void keepSkyShare(uint index, float share);
}

// A merge's kept light alone, its share of the sky let go, which the passes that keep no share
// keep through.
struct NoSkyMerged<M : IMergedLight> : IMergedSkyLight
{
    M merged;
    void keepMerged(uint index, float3 light) { merged.keepMerged(index, light); }
    void keepSkyShare(uint index, float share) {}
}

// The surface points the gather lights, which an engine implements over its buffers, as a pixel's
// point and normal read from its depth and normals.
public interface ISurfacePoints
{
    // The place in the world of the point at index.
    float3 placeAt(uint index);

    // The normal of the surface at the point at index, of unit length.
    float3 normalAt(uint index);

    // Keeps the light the point at index takes (lightTaken), which a diffuse surface reflects
    // albedo over pi of (reflectedLight).
    void keepTaken(uint index, float3 taken);
}

// Surface points that keep the share of the sky in the light each takes beside it.
public interface ISurfaceSkyPoints : ISurfacePoints
{
    // Keeps the share of the sky in the light the point at index takes, what it would take under
    // a sky of 1 along every way with every surface unlit over pi, from 0 to 1.
    void keepSkyShare(uint index, float share);
}

// Surface points that keep their light alone, their share of the sky let go.
struct NoSkyPoints<A : ISurfacePoints> : ISurfaceSkyPoints
{
    A surfaces;
    float3 placeAt(uint index) { return surfaces.placeAt(index); }
    float3 normalAt(uint index) { return surfaces.normalAt(index); }
    void keepTaken(uint index, float3 taken) { surfaces.keepTaken(index, taken); }
    void keepSkyShare(uint index, float share) {}
}

// How many probes a cascade holds.
public uint cascadeProbes(ProbeCascade cascade)
{
    return uint(cascade.count.x * cascade.count.y * cascade.count.z);
}

// How many texels a cascade's probes hold together, the threads of its trace and its merge.
public uint cascadeTexels(ProbeCascade cascade)
{
    return cascadeProbes(cascade) * cascade.texels * cascade.texels;
}

// How many reach texels a cascade's probes hold together, the threads of its reach map's trace.
public uint cascadeReachTexels(ProbeCascade cascade)
{
    return cascadeProbes(cascade) * ReachTexels * ReachTexels;
}

// The probe of a cascade at a probe's index, x fastest, then y.
public int3 probeAt(ProbeCascade cascade, uint probe)
{
    uint x = uint(cascade.count.x), y = uint(cascade.count.y);
    return int3(int(probe % x), int((probe / x) % y), int(probe / (x * y)));
}

// The index of a probe's texel among a cascade's texels.
public uint cascadeTexelIndex(ProbeCascade cascade, int3 probe, uint2 texel)
{
    uint at = uint((probe.z * cascade.count.y + probe.y) * cascade.count.x + probe.x);
    return (at * cascade.texels + texel.y) * cascade.texels + texel.x;
}

// The index of a probe's reach texel among a cascade's reach texels.
public uint reachTexelIndex(ProbeCascade cascade, int3 probe, uint2 texel)
{
    uint at = uint((probe.z * cascade.count.y + probe.y) * cascade.count.x + probe.x);
    return (at * ReachTexels + texel.y) * ReachTexels + texel.x;
}

// Traces the ray of the cascade's texel at index from its probe's place through its interval
// (traceProbeRay), within the cascade's cap of steps, and keeps it, a thread a texel, and nothing
// past the cascade's last texel.
public void traceTexel<F : IProbeField, R : IProbeRays, Q : IProbePlaces>(F field, R rays, Q places, ProbeCascade cascade, uint index)
{
    if (index >= cascadeTexels(cascade))
        return;
    uint texels = cascade.texels * cascade.texels;
    uint2 texel = uint2((index % texels) % cascade.texels, (index % texels) / cascade.texels);
    float3 place = places.placeOf(probeAt(cascade, index / texels));
    NoSteps none;
    rays.keepRay(index, traceProbeRay(field, place, texelWay(cascade.texels, texel), cascade.start, cascade.end, cascade.stepCap, none));
}

// Traces how far the ray of the cascade's reach texel at index goes from its probe's place before
// it meets a surface, up to the cascade's end (reachAlong), and keeps it, a thread a reach texel.
public void reachTexel<F : IFieldVolume, R : IReachTexels, Q : IProbePlaces>(F field, R reach, Q places, ProbeCascade cascade, uint index)
{
    if (index >= cascadeReachTexels(cascade))
        return;
    uint texels = ReachTexels * ReachTexels;
    uint2 texel = uint2((index % texels) % ReachTexels, (index % texels) / ReachTexels);
    float3 place = places.placeOf(probeAt(cascade, index / texels));
    reach.keepReach(index, reachAlong(field, place, texelWay(ReachTexels, texel), cascade.end));
}

// Keeps the light of the coarsest level's texel at index as merged, its ray's alone, the light of
// the surface it met or the sky's along its way past the field (lastCascadeLight), since no level
// above holds more.
public void settleTexel<R : IProbeRays, M : IMergedLight, K : ISky>(R rays, M merged, ProbeCascade cascade, K sky, uint index)
{
    NoSkyMerged<M> kept = { merged };
    settleTexel(rays, kept, cascade, sky, index);
}

// settleTexel keeping beside the light the share of the sky in it (lastCascadeSkyShare).
public void settleTexel<R : IProbeRays, M : IMergedSkyLight, K : ISky>(R rays, M merged, ProbeCascade cascade, K sky, uint index)
{
    if (index >= cascadeTexels(cascade))
        return;
    uint texels = cascade.texels * cascade.texels;
    uint2 texel = uint2((index % texels) % cascade.texels, (index % texels) / cascade.texels);
    ProbeRay ray = rays.rayAt(index);
    merged.keepMerged(index, lastCascadeLight(ray, texelWay(cascade.texels, texel), sky));
    merged.keepSkyShare(index, lastCascadeSkyShare(ray));
}

// Merges the texel at index of a level finer than the coarsest with the merged light of the level
// above, above's probes read through IFarProbes and their places through abovePlaces, by the
// agreed rule (mergedLight), and keeps it, the ray beginning at its own probe's place (places).
// Where the probes above disagree on where a surface lies along the ray it marches across
// aboveCascade's own interval before it is handed on. Where no probe above sees the end of its
// ray the ray marches on itself to marchEnd, the coarsest level's end, each step handed to steps,
// since past the end of the level above no merged light along it is taken from where the ray
// stands, and in a pair marchEnd is the far cascade's end. It says whether the texel marched on
// itself.
public bool mergeTexel<F : IProbeField, R : IProbeRays, Q : IProbePlaces, P : IFarProbes, U : IProbePlaces, M : IMergedLight, K : ISky,
    S : IMarchSteps>(F field, R rays, Q places, P above, U abovePlaces, M merged, ProbeCascade cascade, ProbeCascade aboveCascade,
    float marchEnd, K sky, inout S steps, uint index)
{
    NoSkyShares<P> lights = { above };
    NoSkyMerged<M> kept = { merged };
    return mergeTexel(field, rays, places, lights, abovePlaces, kept, cascade, aboveCascade, marchEnd, sky, steps, index);
}

// mergeTexel keeping beside the light the share of the sky in it, above's shares read through
// IFarSkyProbes (mergedLight).
public bool mergeTexel<F : IProbeField, R : IProbeRays, Q : IProbePlaces, P : IFarSkyProbes, U : IProbePlaces, M : IMergedSkyLight, K : ISky,
    S : IMarchSteps>(F field, R rays, Q places, P above, U abovePlaces, M merged, ProbeCascade cascade, ProbeCascade aboveCascade,
    float marchEnd, K sky, inout S steps, uint index)
{
    if (index >= cascadeTexels(cascade))
        return false;
    uint texels = cascade.texels * cascade.texels;
    uint2 texel = uint2((index % texels) % cascade.texels, (index % texels) / cascade.texels);
    float3 place = places.placeOf(probeAt(cascade, index / texels));
    bool marched;
    float skyShare;
    float3 light = mergedLight(field, above, abovePlaces, aboveCascade, marchEnd, place, cascade.texels, texel, rays.rayAt(index), sky, steps,
        marched, skyShare);
    merged.keepMerged(index, light);
    merged.keepSkyShare(index, skyShare);
    return marched;
}

// One probe's texels as lightTaken reads them, its merged light through IFarProbes.
struct ProbeTexels<P : IFarProbes> : IProbeLight
{
    P probes;
    int3 probe;
    float3 lightOf(uint2 texel) { return probes.lightAt(probe, texel); }
}

// Gathers the light the surface point at index takes from the finest level's probes about it, its
// merged light and its reach map read through IFarProbes, and keeps it, a thread a point. Each
// probe's light taken at the point's normal (lightTaken) is weighed by its trilinear weight on the
// grid, and by nothing where it stands inside a mesh or does not see the point from its place
// (places, farProbeSees), as the merge weighs the probes about a ray's end, normalized by their
// sum, and none where no probe counts. A point at a probe's grid place takes that probe's light
// alone. It gives a point the probes' light at their own places and corrects nothing for a probe
// that stands off the surface, so a floor a spacing below its probes takes what they see from that
// height, more of the sky in a box open above it than the floor sees. A surface that wants its own
// light gathers it with gatherTraced.
public void gatherAt<F : IProbeField, P : IFarProbes, Q : IProbePlaces, A : ISurfacePoints>(F field, P probes, Q places, ProbeCascade cascade,
    A surfaces, uint index)
{
    NoSkyShares<P> lights = { probes };
    NoSkyPoints<A> points = { surfaces };
    gatherAt(field, lights, places, cascade, points, index);
}

// The share of the sky in the light a probe's texels give a surface facing n, each texel's share
// (IFarSkyProbes) weighed as lightTaken weighs its light, over pi.
float skyShareTaken<P : IFarSkyProbes>(P probes, int3 probe, uint texels, float3 n)
{
    float shares = octahedronShares(texels);
    float taken = 0.0;
    for (uint k = 0u; k < texels * texels; k++)
    {
        uint2 texel = uint2(k % texels, k / texels);
        float facing = max(dot(n, texelWay(texels, texel)), 0.0);
        if (facing > 0.0)
            taken += probes.skyShareAt(probe, texel) * (facing * texelSolidAngle(texels, texel, shares));
    }
    return taken / 3.14159265;
}

// gatherAt keeping beside the point's light the share of the sky in it, each probe's share
// (skyShareTaken) weighed as its light is.
public void gatherAt<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, A : ISurfaceSkyPoints>(F field, P probes, Q places,
    ProbeCascade cascade, A surfaces, uint index)
{
    float3 p = surfaces.placeAt(index);
    float3 n = surfaces.normalAt(index);
    int3 base;
    float3 f;
    farProbesAbout(cascade, p, base, f);
    float3 sum = float3(0.0);
    float skyShares = 0.0, weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 probe = base + offset;
        if (any(probe < 0) || any(probe >= cascade.count))
            continue;
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z;
        float3 place = places.placeOf(probe);
        int c = field.cascadeOf(place, 1.0);
        if (weight <= 0.0 || (c >= 0 && fieldDistanceIn(field, c, place) < 0.0) || !farProbeSees(probes, probe, place, p, SeeingSlack * cascade.spacing))
            continue;
        ProbeTexels<P> texels;
        texels.probes = probes;
        texels.probe = probe;
        sum += lightTaken(texels, cascade.texels, n) * weight;
        skyShares += skyShareTaken(probes, probe, cascade.texels, n) * weight;
        weights += weight;
    }
    surfaces.keepTaken(index, weights > 0.0 ? sum / weights : float3(0.0));
    surfaces.keepSkyShare(index, weights > 0.0 ? skyShares / weights : 0.0);
}

// Gathers the light the surface point at index takes along its own rays, and keeps it, a thread a
// point, for a surface whose probes stand off it. The point sends a ray along each way of an
// octahedron of cascade's texels a side that faces its normal, from a cell of the field's finest
// cascade holding it off the surface along the normal, and hands each on through mergedLight to
// cascade's probes, read through IFarProbes at those texels, as a ray of a cascade finer than
// cascade that ended where it began: it marches to open space, meeting what lies between, and is
// handed to the probes about where it stops, or marches across their interval where they
// disagree, each step handed to steps. The light taken is their sum, each weighed by its texel's
// solid angle and the cosine of its way off the normal, scaled to pi over the weights, as
// lightTaken scales it. Read from probes 2 units over a floor in a box open to the sky, gatherAt
// gives the floor 1.26 times the light traced at it, what the probes see from their height, and
// gatherTraced 1.03, at 16 texels a side, 128 rays and about 1,000 steps a point.
public void gatherTraced<F : IProbeField, P : IFarProbes, Q : IProbePlaces, K : ISky, A : ISurfacePoints, S : IMarchSteps>(F field, P probes, Q places,
    ProbeCascade cascade, float marchEnd, K sky, A surfaces, inout S steps, uint index)
{
    NoSkyShares<P> lights = { probes };
    NoSkyPoints<A> points = { surfaces };
    gatherTraced(field, lights, places, cascade, marchEnd, sky, points, steps, index);
}

// gatherTraced keeping beside the point's light the share of the sky in it, each ray's share
// (mergedLight) weighed as its light is, over the weights.
public void gatherTraced<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISky, A : ISurfaceSkyPoints, S : IMarchSteps>(F field,
    P probes, Q places, ProbeCascade cascade, float marchEnd, K sky, A surfaces, inout S steps, uint index)
{
    float3 p = surfaces.placeAt(index);
    float3 n = surfaces.normalAt(index);
    int c = field.cascadeOf(p, 1.0);
    float3 origin = p + n * (c >= 0 ? field.cornerAndCell(c).w : 0.0);
    float shares = octahedronShares(cascade.texels);
    float3 taken = float3(0.0);
    float skyShares = 0.0, weights = 0.0;
    ProbeRay begun;
    begun.ended = RayPast;
    begun.t = 0.0;
    begun.carried = 1.0;
    begun.light = float3(0.0);
    [loop]
    for (uint k = 0u; k < cascade.texels * cascade.texels; k++)
    {
        uint2 texel = uint2(k % cascade.texels, k / cascade.texels);
        float facing = dot(n, texelWay(cascade.texels, texel));
        if (facing <= 0.0)
            continue;
        float weight = facing * texelSolidAngle(cascade.texels, texel, shares);
        bool marched;
        float skyShare;
        taken += mergedLight(field, probes, places, cascade, marchEnd, origin, cascade.texels, texel, begun, sky, steps, marched, skyShare) * weight;
        skyShares += skyShare * weight;
        weights += weight;
    }
    surfaces.keepTaken(index, weights > 0.0 ? taken * (3.14159265 / weights) : float3(0.0));
    surfaces.keepSkyShare(index, weights > 0.0 ? skyShares / weights : 0.0);
}
