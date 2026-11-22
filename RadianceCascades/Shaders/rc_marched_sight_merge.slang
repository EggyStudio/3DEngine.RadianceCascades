// The merge by sight and agreement with each line of sight marched through the field, a rung of the
// ladder the merges climb (IMerge). A near texel's ray that reaches its end with nothing met takes
// the far probes about its own probe's place, each clamped onto the far grid, and a far probe the
// near probe has no clear line to through the field, past surfaces that let light through
// (seesAcross), or that stands inside a mesh, its reach map marked so (ReachInside), weighs
// nothing. The eight lines are marched once for each near probe by the per-probe stage
// (FarSightLines), and for a surface point, which is no probe's place, by each texel. The ray
// marches on past its end to the far cascade's end where that end lies short of two and a half
// times the ray's, and a far probe whose own hit along the way lies off where that march meets a
// surface, by a quarter of that distance or two of the field's cells at the far probe, or that
// meets one where the march meets none, is left out. Each probe left is weighed by place and read
// toward the point of the ray nearest its own hit, over the far texels the near texel covers,
// weighed alike. Where none agrees and the march met a surface the light from beyond is none, where
// none agrees and the march met none every far probe seen is taken, and where none is seen the ray
// takes the light it took on its way alone.
module rc_marched_sight_merge;

__exported import rc_reach_sight_merge;
import rc_probe_passes;

// The lines of sight from a near probe's place to each of the eight far probes about it, clamped
// onto the far grid, a bit for each in the order of their corners, set where the near probe sees
// the far probe past surfaces that let light through (seesAcross), half a cell of the field at the
// near probe kept clear at either end. A near probe outside the field sees none.
public struct FarSightLines : IProbeStage
{
    public uint valueOf<F : IProbeField, Q : IProbePlaces, S : IMarchSteps>(F field, Q farPlaces, ProbeCascade far, float3 place,
        inout S steps)
    {
        int c = field.cascadeOf(place, 1.0);
        if (c < 0)
            return 0u;
        float clearance = field.cornerAndCell(c).w * 0.5;
        int3 base;
        float3 f;
        farProbesAbout(far, place, base, f);
        uint seen = 0u;
        [loop]
        for (int corner = 0; corner < 8; corner++)
        {
            int3 probe = clamp(base + int3(corner & 1, (corner >> 1) & 1, corner >> 2), int3(0), far.count - 1);
            if (seesAcross(field, place, farPlaces.placeOf(probe), clearance, steps))
                seen |= 1u << uint(corner);
        }
        return seen;
    }
}

// The light a near probe's texel of an octahedron of `texels` along each side brings along its way
// from origin by marched sight and agreement, with in skyShare the share of the sky in it, as
// mergedLight takes its inputs, the lines of sight read from stage at probe, or marched here where
// probe is below zero. A ray that met a surface takes nothing past it and one that left the field
// the sky's along the way. Each step of the march on and of a line marched here is handed to steps,
// and the ray never marches on to marchEnd itself.
public float3 marchedSightLight<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISkyShare, S : IMarchSteps, V : IStageValues>(
    F field, P probes, Q places, ProbeCascade far, float marchEnd, float3 origin, uint texels, uint2 texel, ProbeRay near, K sky,
    inout S steps, V stage, int3 probe, out bool marched, out float skyShare)
{
    marched = false;
    skyShare = 0.0;
    float3 way = texelWay(texels, texel);
    if (near.ended != RayPast && near.ended != RayCapped)
    {
        skyShare = lastCascadeSkyShare(near, way, sky);
        return lastCascadeLight(near, way, sky);
    }
    FarSightLines lines;
    uint seen = probe.x >= 0 ? stage.valueAt(probe) : lines.valueOf(field, places, far, origin, steps);
    // The march on past the ray's end, which reads where a surface lies along the way for the far
    // probes to agree with, and which the coarsest far cascade's end, far past the ray's, is not
    // worth.
    bool agreeing = far.end < 2.5 * near.t;
    float occluder = agreeing ? opaqueAlong(field, origin, way, near.t, far.end, steps) : -1.0;
    int3 base;
    float3 f;
    farProbesAbout(far, origin, base, f);
    uint k = max(far.texels / texels, 1u);
    uint2 reachTexel = texelOfWay(ReachTexels, way);
    float3 sum = float3(0.0), agreedSum = float3(0.0);
    float skies = 0.0, agreedSkies = 0.0, weights = 0.0, agreedWeights = 0.0;
    [loop]
    for (int corner = 0; corner < 8; corner++)
    {
        if ((seen & (1u << uint(corner))) == 0u)
            continue;
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 at = clamp(base + offset, int3(0), far.count - 1);
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z / float(k * k);
        float reach = probes.reachAt(at, reachTexel);
        if (reach < 0.0)
            continue;
        float3 place = places.placeOf(at);
        bool met = reach > 0.0 && reach < far.end * 0.999;
        float along = met ? dot(place + way * reach - origin, way) : 1e30;
        float3 toward = way;
        if (met)
        {
            float3 off = origin + way * max(along, 0.0) - place;
            if (dot(off, off) > 1e-8)
                toward = normalize(off);
        }
        int c = field.cascadeOf(place, 1.0);
        float cells = c >= 0 ? 2.0 * field.cornerAndCell(c).w : 0.0;
        bool agrees = !agreeing || (occluder < 0.0 ? along >= far.end * 0.999 : abs(along - occluder) <= max(cells, 0.25 * occluder));
        int2 first = int2(floor(octahedron(toward) * float(far.texels) - float(k) * 0.5 + 0.5));
        for (uint v = 0u; v < k; v++)
            for (uint u = 0u; u < k; u++)
            {
                uint2 covered = uint2(foldedTexel(first + int2(int(u), int(v)), int(far.texels)));
                float3 light = probes.lightAt(at, covered) * weight;
                float itsSky = probes.skyShareAt(at, covered) * weight;
                sum += light;
                skies += itsSky;
                weights += weight;
                if (agrees)
                {
                    agreedSum += light;
                    agreedSkies += itsSky;
                    agreedWeights += weight;
                }
            }
    }
    if (agreedWeights > 1e-4)
    {
        sum = agreedSum;
        skies = agreedSkies;
        weights = agreedWeights;
    }
    else if (occluder >= 0.0)
    {
        sum = float3(0.0);
        skies = 0.0;
    }
    if (weights <= 1e-4)
        return near.light;
    skyShare = skies / weights * near.carried;
    return near.light + sum / weights * near.carried;
}

// The merge by marched sight and agreement (marchedSightLight), the ladder's third rung, which
// reads the per-probe stage's lines of sight (FarSightLines).
public struct MergeByMarchedSight : IMerge
{
    public static const bool readsStage = true;

    public float3 light<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISkyShare, S : IMarchSteps, V : IStageValues>(F field,
        P probes, Q places, ProbeCascade far, float marchEnd, float3 origin, uint texels, uint2 texel, ProbeRay near, K sky, inout S steps,
        V stage, int3 probe, out bool marched, out float skyShare)
    {
        return marchedSightLight(field, probes, places, far, marchEnd, origin, texels, texel, near, sky, steps, stage, probe, marched,
            skyShare);
    }
}
