// The merge by sight and agreement, the sight read from the far probes' reach maps, a rung of the
// ladder the merges climb (IMerge). A near texel's ray that reaches its end with nothing met takes
// the far probes about its own probe's place, not about where the ray ended. It leaves out each
// that does not see that place by its reach map (farProbeSees), and each whose own hit along the
// texel's way lies off where the ray, marched on to the far cascade's end, meets a surface, by
// HitSpread of the way or two of the field's cells, or that meets a surface where the march meets
// none. The rest are weighed by place, each read toward the point of the ray nearest its own hit
// along the way, since a probe a spacing off the ray that is read along the ray's own way lends
// what it sees from where it stands and not what the ray would meet. Where none is left the ray
// takes its own march, the surface it met or the sky where it left the field, or marches on to
// marchEnd where it met neither. The module holds as well the merge about the near probe's own
// place (MergeAboutProbe), the same blend and read with no march to leave a far probe out.
module rc_reach_sight_merge;

__exported import rc_cascade_merge;

// The texel of an octahedron of `texels` along each side at t, a texel up to one past an edge
// folded onto the texel across that edge, as a block of texels about a way near an edge reads it.
public int2 foldedTexel(int2 t, int texels)
{
    if (t.x < 0)
        t = int2(0, texels - 1 - t.y);
    else if (t.x >= texels)
        t = int2(texels - 1, texels - 1 - t.y);
    if (t.y < 0)
        t = int2(texels - 1 - t.x, 0);
    else if (t.y >= texels)
        t = int2(texels - 1 - t.x, texels - 1);
    return t;
}

// The far cascade's light a far probe lends a near texel of an octahedron of `texels` along each
// side, read toward way, in skyShare its share of the sky at the same weights. Where the far probe
// keeps a whole number of texels across each near texel, they are as many far texels as the near
// texel covers in a block about way, each weighed by its solid angle (octahedronShare), folded
// across the octahedron's edges, and otherwise the one far texel way falls in. Toward the near
// texel's middle way it reads the far texels farTexelsLight reads.
float3 farTexelsToward<P : IFarSkyProbes>(P probes, ProbeCascade far, int3 probe, uint texels, float3 way, out float skyShare)
{
    uint k = far.texels / texels;
    if (k <= 1u || k * texels != far.texels)
    {
        uint2 at = texelOfWay(far.texels, way);
        skyShare = probes.skyShareAt(probe, at);
        return probes.lightAt(probe, at);
    }
    int2 first = int2(floor(octahedron(way) * float(far.texels) - float(k) * 0.5 + 0.5));
    float3 sum = float3(0.0);
    float shares = 0.0, sky = 0.0;
    for (uint j = 0u; j < k; j++)
        for (uint i = 0u; i < k; i++)
        {
            uint2 covered = uint2(foldedTexel(first + int2(int(i), int(j)), int(far.texels)));
            float share = octahedronShare((float2(covered) + 0.5) / float(far.texels));
            sum += probes.lightAt(probe, covered) * share;
            sky += probes.skyShareAt(probe, covered) * share;
            shares += share;
        }
    skyShare = sky / shares;
    return sum / shares;
}

// The far cascade's light over a near texel along way from the far probes about origin, the near
// probe's place, each at its trilinear weight about origin. A far probe that stands inside a mesh
// or outside the field, or does not see origin by its reach map (farProbeSees), weighs nothing. One
// outside the field keeps a reach map open along every way, its rays leaving every cascade where
// they begin (reachAlong), so it would seem to see a point behind a floor or a wall. Where agreeing
// is set so does one whose own hit along way (reachAt) lies at a point of the ray off occluder,
// where the ray's march on met a surface, by more than HitSpread of occluder and two of the field's
// cells there, or short of the far cascade's end where occluder is below zero, the march having met
// none. Each probe is read toward the point of the ray nearest its own hit (farTexelsToward), and
// along way where it met none. In skyShares is the sum of their shares of the sky at the same
// weights, and in weights the sum of the weights taken, which divides both.
float3 sightedFarLight<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces>(F field, P probes, Q places, ProbeCascade far, float3 origin,
    float3 way, uint texels, bool agreeing, float occluder, out float skyShares, out float weights)
{
    int3 base;
    float3 f;
    farProbesAbout(far, origin, base, f);
    int c = field.cascadeOf(origin + way * max(occluder, 0.0), 1.0);
    float cells = c >= 0 ? 2.0 * field.cornerAndCell(c).w : 0.0;
    uint2 reachTexel = texelOfWay(ReachTexels, way);
    float3 sum = float3(0.0);
    skyShares = 0.0;
    weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 probe = base + offset;
        if (any(probe < 0) || any(probe >= far.count))
            continue;
        float3 place = places.placeOf(probe);
        if (field.cascadeOf(place, 1.0) < 0 || !farProbeSees(probes, probe, place, origin, SeeingSlack * far.spacing))
            continue;
        float reach = probes.reachAt(probe, reachTexel);
        float along = reach >= ReachOpen ? ReachOpen : dot(place + way * reach - origin, way);
        if (agreeing && (occluder < 0.0 ? along < far.end * 0.999 : abs(along - occluder) > max(HitSpread * occluder, cells)))
            continue;
        float3 toward = way;
        if (reach < ReachOpen)
        {
            float3 off = origin + way * max(along, 0.0) - place;
            if (dot(off, off) > 1e-8)
                toward = normalize(off);
        }
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z;
        float skyShare;
        sum += farTexelsToward(probes, far, probe, texels, toward, skyShare) * weight;
        skyShares += skyShare * weight;
        weights += weight;
    }
    return sum;
}

// The light a near probe's texel of an octahedron of `texels` along each side brings along its way
// from origin by sight and agreement, with in skyShare the share of the sky in it, as mergedLight
// takes its inputs. A ray that met a surface takes nothing past it and one that left the field the
// sky's along the way. One that reached its interval's end, or stopped at its cap short of it,
// marches on to the far cascade's end, where it meets a surface at the occluder or none, and takes
// the far probes about origin that see origin and agree with the occluder (sightedFarLight), times
// the share it carries at its end. Where far is the coarsest cascade, its end at marchEnd or past
// it, the ray does not march first, since that march would run to the field's edge, and takes
// every far probe that sees origin. Where none is left it takes its own march, the light it took
// on its way and the surface it met or the sky where it left, and where that march met neither it
// marches on to marchEnd, marched set. Every march takes the far cascade's cap of steps
// (stepCap), and one that stops at it takes nothing past where it stopped. Every step past the
// ray's end is handed to steps. The marches are one call in a loop, since a driver inlines an
// engine's shading (hitLight) at every call of the march.
public float3 reachSightLight<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISkyShare, S : IMarchSteps>(F field, P probes,
    Q places, ProbeCascade far, float marchEnd, float3 origin, uint texels, uint2 texel, ProbeRay near, K sky, inout S steps,
    out bool marched, out float skyShare)
{
    marched = false;
    skyShare = 0.0;
    float3 way = texelWay(texels, texel);
    if (near.ended != RayPast && near.ended != RayCapped)
    {
        skyShare = lastCascadeSkyShare(near, way, sky);
        return lastCascadeLight(near, way, sky);
    }
    float3 taken = near.light;
    float carried = near.carried;
    float from = near.t;
    bool agreeing = far.end < marchEnd;
    bool blended = false;
    ProbeRay on = near;
    float occluder = -1.0;
    // Two marches at most, the march on to the far cascade's end and the march on to marchEnd,
    // with a return after the loop that is never reached, since naga refuses WGSL whose function
    // may end with no return.
    [loop]
    for (uint marches = 0u; marches < 3u; marches++)
    {
        if (!blended && (marches > 0u || !agreeing))
        {
            blended = true;
            float skyShares, weights;
            float3 sum = sightedFarLight(field, probes, places, far, origin, way, texels, agreeing, occluder, skyShares, weights);
            if (weights > 0.0)
            {
                skyShare = skyShares / weights * carried;
                return taken + sum / weights * carried;
            }
            // None left, the ray takes its own march where it met a surface, left the field or
            // stopped at its cap, and otherwise marches on from where it reached the far end.
            if (marches > 0u)
            {
                if (on.ended != RayPast)
                {
                    skyShare = lastCascadeSkyShare(on, way, sky) * carried;
                    return taken + lastCascadeLight(on, way, sky) * carried;
                }
                taken += on.light * carried;
                carried *= on.carried;
                from = on.t;
            }
            marched = true;
        }
        ProbeRay ray = traceProbeRay(field, origin, way, from, blended ? marchEnd : far.end, far.stepCap, steps);
        if (blended)
        {
            skyShare = lastCascadeSkyShare(ray, way, sky) * carried;
            return taken + lastCascadeLight(ray, way, sky) * carried;
        }
        on = ray;
        occluder = ray.ended == RayMet ? ray.t : -1.0;
    }
    return taken;
}

// The merge by sight and agreement with the sight read from the far probes' reach maps
// (reachSightLight), the ladder's second rung.
public struct MergeByReachSight : IMerge
{
    public static const bool readsStage = false;

    public float3 light<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISkyShare, S : IMarchSteps, V : IStageValues>(F field,
        P probes, Q places, ProbeCascade far, float marchEnd, float3 origin, uint texels, uint2 texel, ProbeRay near, K sky, inout S steps,
        V stage, int3 probe, out bool marched, out float skyShare)
    {
        return reachSightLight(field, probes, places, far, marchEnd, origin, texels, texel, near, sky, steps, marched, skyShare);
    }
}

// The far cascade's light over a near texel along way from the far probes about place, the near
// probe's own place and not where its ray ended, each at its trilinear weight about place and read
// toward the point of the ray from place nearest its own hit along way (sightedFarLight with no
// agreement). A far probe that stands inside a mesh or outside the field, or does not see place by
// its reach map, weighs nothing. In skyShares is the sum of their shares of the sky at the same
// weights, and in weights the sum of the weights taken, which divides both. The far probes about
// place stand up to a far spacing off the ray, and read along the ray's own way each would lend
// what it sees from where it stands and not what the ray would meet, so the blend about place and
// the read toward the ray go together.
public float3 farLightAbout<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces>(F field, P probes, Q places, ProbeCascade far, float3 place,
    float3 way, uint texels, out float skyShares, out float weights)
{
    return sightedFarLight(field, probes, places, far, place, way, texels, false, -1.0, skyShares, weights);
}

// The light a near probe's texel of an octahedron of `texels` along each side brings along its way
// from origin with what lies past its ray's end blended about origin, with in skyShare the share
// of the sky in it, as mergedLight takes its inputs. A ray that met a surface takes nothing past it
// and one that left the field the sky's along the way. One that reached its interval's end, or
// stopped at its cap short of it, takes the far probes about origin that see origin
// (farLightAbout), times the share it carries at its end, with no march past its end to test them
// against. Where none counts the ray marches on itself to marchEnd, marched set, and takes the
// surface it meets or the sky where it leaves, a march stopped at the far cascade's cap of steps
// (stepCap) taking nothing past where it stopped. Every step past the ray's end is handed to
// steps.
public float3 aboutProbeLight<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISkyShare, S : IMarchSteps>(F field, P probes,
    Q places, ProbeCascade far, float marchEnd, float3 origin, uint texels, uint2 texel, ProbeRay near, K sky, inout S steps,
    out bool marched, out float skyShare)
{
    marched = false;
    skyShare = 0.0;
    float3 way = texelWay(texels, texel);
    if (near.ended != RayPast && near.ended != RayCapped)
    {
        skyShare = lastCascadeSkyShare(near, way, sky);
        return lastCascadeLight(near, way, sky);
    }
    float skyShares, weights;
    float3 sum = farLightAbout(field, probes, places, far, origin, way, texels, skyShares, weights);
    if (weights > 0.0)
    {
        skyShare = skyShares / weights * near.carried;
        return near.light + sum / weights * near.carried;
    }
    marched = true;
    ProbeRay ray = traceProbeRay(field, origin, way, near.t, marchEnd, far.stepCap, steps);
    skyShare = lastCascadeSkyShare(ray, way, sky) * near.carried;
    return near.light + lastCascadeLight(ray, way, sky) * near.carried;
}

// The merge about the near probe's own place (aboutProbeLight), the ladder's fourth rung, the merge
// by reach sight with no march past the ray's end to leave out the far probes it disagrees with.
public struct MergeAboutProbe : IMerge
{
    public static const bool readsStage = false;

    public float3 light<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISkyShare, S : IMarchSteps, V : IStageValues>(F field,
        P probes, Q places, ProbeCascade far, float marchEnd, float3 origin, uint texels, uint2 texel, ProbeRay near, K sky, inout S steps,
        V stage, int3 probe, out bool marched, out float skyShare)
    {
        return aboutProbeLight(field, probes, places, far, marchEnd, origin, texels, texel, near, sky, steps, marched, skyShare);
    }
}
