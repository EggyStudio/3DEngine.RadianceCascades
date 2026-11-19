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
// its ProbeCascade, the finest's at the reach its probes trace themselves, each coarser level's
// farther, and the coarsest's past the field's edge, so its rays end at a surface or leave the
// field. A level's reach map runs to its end too, so that end lies past the diagonal of the cell
// between its probes, the spacing times the root of 3, for the seeing of a point among them to be
// read within the map. A texel's index is its probe's, x fastest, times the texels of a probe,
// and its texel's along a row of the octahedron, x fastest (cascadeTexelIndex), and a reach
// texel's the same over ReachTexels a side.
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

// Traces the ray of the cascade's texel at index from its probe through its interval
// (traceProbeRay) and keeps it, a thread a texel, and nothing past the cascade's last texel.
public void traceTexel<F : IProbeField, R : IProbeRays>(F field, R rays, ProbeCascade cascade, uint index)
{
    if (index >= cascadeTexels(cascade))
        return;
    uint texels = cascade.texels * cascade.texels;
    uint2 texel = uint2((index % texels) % cascade.texels, (index % texels) / cascade.texels);
    float3 place = probePlace(cascade, probeAt(cascade, index / texels));
    rays.keepRay(index, traceProbeRay(field, place, texelWay(cascade.texels, texel), cascade.start, cascade.end));
}

// Traces how far the ray of the cascade's reach texel at index goes from its probe before it meets
// a surface, up to the cascade's end (reachAlong), and keeps it, a thread a reach texel.
public void reachTexel<F : IFieldVolume, R : IReachTexels>(F field, R reach, ProbeCascade cascade, uint index)
{
    if (index >= cascadeReachTexels(cascade))
        return;
    uint texels = ReachTexels * ReachTexels;
    uint2 texel = uint2((index % texels) % ReachTexels, (index % texels) / ReachTexels);
    float3 place = probePlace(cascade, probeAt(cascade, index / texels));
    reach.keepReach(index, reachAlong(field, place, texelWay(ReachTexels, texel), cascade.end));
}

// Keeps the light of the coarsest level's texel at index as merged, its ray's alone, the light of
// the surface it met or the sky's past the field (lastCascadeLight), since no level above holds
// more.
public void settleTexel<R : IProbeRays, M : IMergedLight>(R rays, M merged, ProbeCascade cascade, float3 sky, uint index)
{
    if (index < cascadeTexels(cascade))
        merged.keepMerged(index, lastCascadeLight(rays.rayAt(index), sky));
}

// Merges the texel at index of a level finer than the coarsest with the merged light of the level
// above, above's probes read through IFarProbes, by the agreed rule (mergedLight), and keeps it.
// Where no probe above sees the end of its ray the ray marches on itself to marchEnd, the coarsest
// level's end, each step handed to steps, since past the end of the level above no merged light
// along it is taken from where the ray stands, and in a pair marchEnd is the far cascade's end the
// rule names. It says whether the texel marched on itself.
public bool mergeTexel<F : IProbeField, R : IProbeRays, P : IFarProbes, M : IMergedLight, S : IMarchSteps>(F field, R rays, P above,
    M merged, ProbeCascade cascade, ProbeCascade aboveCascade, float marchEnd, float3 sky, inout S steps, uint index)
{
    if (index >= cascadeTexels(cascade))
        return false;
    uint texels = cascade.texels * cascade.texels;
    uint2 texel = uint2((index % texels) % cascade.texels, (index % texels) / cascade.texels);
    float3 place = probePlace(cascade, probeAt(cascade, index / texels));
    ProbeCascade marchedTo = aboveCascade;
    marchedTo.end = marchEnd;
    bool marched;
    float3 light = mergedLight(field, above, marchedTo, place, texelWay(cascade.texels, texel), rays.rayAt(index), sky, steps, marched);
    merged.keepMerged(index, light);
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
// probe's light taken at the point's normal (lightTaken) is weighed by its trilinear weight, and by
// nothing where it stands inside a mesh or does not see the point (farProbeSees), as the merge
// weighs the probes about a ray's end, normalized by their sum, and none where no probe counts. A
// point at a probe's place takes that probe's light alone.
public void gatherAt<F : IProbeField, P : IFarProbes, A : ISurfacePoints>(F field, P probes, ProbeCascade cascade, A surfaces, uint index)
{
    float3 p = surfaces.placeAt(index);
    float3 n = surfaces.normalAt(index);
    int3 base;
    float3 f;
    farProbesAbout(cascade, p, base, f);
    float3 sum = float3(0.0);
    float weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 probe = base + offset;
        if (any(probe < 0) || any(probe >= cascade.count))
            continue;
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z;
        float3 place = probePlace(cascade, probe);
        int c = field.cascadeOf(place, 1.0);
        if (weight <= 0.0 || (c >= 0 && fieldDistanceIn(field, c, place) < 0.0) || !farProbeSees(probes, probe, place, p, SeeingSlack * cascade.spacing))
            continue;
        ProbeTexels<P> texels;
        texels.probes = probes;
        texels.probe = probe;
        sum += lightTaken(texels, cascade.texels, n) * weight;
        weights += weight;
    }
    surfaces.keepTaken(index, weights > 0.0 ? sum / weights : float3(0.0));
}
