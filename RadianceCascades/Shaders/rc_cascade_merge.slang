// The merge of a near cascade's texel with the far cascade past it. A texel's ray that reaches the
// end of its interval with nothing met takes the far cascade's light along its own way from the
// eight far probes about the point where it ended, where they disagree on where a surface lies
// along the way it first marches across the far interval itself, and where none of them counts it
// marches on itself through the field.
module rc_cascade_merge;

__exported import rc_probes;

// The far cascade's probes as the merge reads them, which an engine implements over its images.
public interface IFarProbes
{
    // The light a far probe's texel brought, its ray's within the far cascade's interval with what
    // lies past it.
    float3 lightAt(int3 probe, uint2 texel);

    // How far a far probe's ray went along a texel of its reach map of ReachTexels along each side,
    // from the probe itself (reachAlong).
    float reachAt(int3 probe, uint2 texel);
}

// The far cascade's probes as a merge that keeps the sky's share beside the light reads them, the
// share of the sky in each texel's light, which a binding keeps from the level's own merge
// (IMergedSkyLight). A binding that reads the share this way merges once where a second merge of
// every texel, with the sky's light 1 along every way and every surface unlit, gave it the share.
public interface IFarSkyProbes : IFarProbes
{
    // The share of the sky in a far probe's texel's light, what its light would be under a sky of
    // 1 along every way with every surface unlit, from 0 to 1.
    float skyShareAt(int3 probe, uint2 texel);
}

// Far probes read through IFarProbes alone, their texels' share of the sky none, which the merge
// and the passes that keep no share read them through, so each has one form that keeps both.
public struct NoSkyShares<P : IFarProbes> : IFarSkyProbes
{
    public P probes;
    public float3 lightAt(int3 probe, uint2 texel) { return probes.lightAt(probe, texel); }
    public float reachAt(int3 probe, uint2 texel) { return probes.reachAt(probe, texel); }
    public float skyShareAt(int3 probe, uint2 texel) { return 0.0; }
}

// How far past a far probe's reach toward a point the point may lie for the probe to see it, as a
// share of the far cascade's spacing, an eighth, a cell of a quarter unit at a spacing of 2. A
// probe's reach map keeps the ray of each texel's middle way, which passes up to half a texel off
// the way toward the point, so a point beside a surface that ray met a little short of it is seen
// within the slack, and a point behind a wall half a unit thick lies past it.
public static const float SeeingSlack = 0.125;

// Whether the far probe at place sees e, where the distance to e lies within its reach toward e
// (reachAlong), and slack more, and none where it stands inside a mesh (ReachInside).
public bool farProbeSees<P : IFarProbes>(P probes, int3 probe, float3 place, float3 e, float slack)
{
    float3 toward = e - place;
    float distance = length(toward);
    float reach = probes.reachAt(probe, texelOfWay(ReachTexels, distance > 0.0 ? toward / distance : float3(0.0, 1.0, 0.0)));
    return reach >= 0.0 && (distance <= slack || distance <= reach + slack);
}

// The far probes about e, the least of the eight in base and where e lies between them in f, from 0
// to 1 along each axis, which a probe's trilinear weight is made of.
public void farProbesAbout(ProbeCascade far, float3 e, out int3 base, out float3 f)
{
    float3 q = (e - far.corner) / far.spacing;
    base = int3(floor(q));
    f = q - float3(base);
}

// The far cascade's light a far probe lends a near texel of an octahedron of `texels` along each
// side. Where the far probe keeps a whole number of texels across each near texel, as 16 a side
// against 8, they are the far texels the near texel's solid angle covers, each weighed by its
// solid angle (octahedronShare), and otherwise the one far texel the near texel's middle way
// falls in, which the same count gives alike. Taken at the middle way alone where the far texels
// are finer, the light leans toward one corner of the near texel, the corner its middle way
// touches, and under a sky that grows toward the top a near texel of 8 a side read 10.5 percent
// off its own ray marched on.
public float3 farTexelsLight<P : IFarProbes>(P probes, ProbeCascade far, int3 probe, uint texels, uint2 texel)
{
    NoSkyShares<P> lights = { probes };
    float skyShare;
    return farTexelsLight(lights, far, probe, texels, texel, skyShare);
}

// farTexelsLight with in skyShare the share of the sky in that light, taken from the same far
// texels at the same weights.
public float3 farTexelsLight<P : IFarSkyProbes>(P probes, ProbeCascade far, int3 probe, uint texels, uint2 texel, out float skyShare)
{
    uint k = far.texels / texels;
    if (k <= 1u || k * texels != far.texels)
    {
        uint2 at = texelOfWay(far.texels, texelWay(texels, texel));
        skyShare = probes.skyShareAt(probe, at);
        return probes.lightAt(probe, at);
    }
    float3 sum = float3(0.0);
    float shares = 0.0, sky = 0.0;
    for (uint j = 0u; j < k; j++)
        for (uint i = 0u; i < k; i++)
        {
            uint2 covered = texel * k + uint2(i, j);
            float share = octahedronShare((float2(covered) + 0.5) / float(far.texels));
            sum += probes.lightAt(probe, covered) * share;
            sky += probes.skyShareAt(probe, covered) * share;
            shares += share;
        }
    skyShare = sky / shares;
    return sum / shares;
}

// A cascade's probes read at fewer texels a side than they are traced at, perRead traced texels a
// side in each read texel, 1 or 2 as a binding chooses for the level below, and more for a gather
// that reads the finest coarser still. Each read texel is the mean of the traced texels it holds,
// weighed by their solid angle (octahedronShare), and so takes the light of perRead squared ways
// spread over it, where a texel traced at the side it is read at takes its middle way's alone and
// reads a thin strip of sky it holds part of as all of it or none. A binding wraps its own reads of
// the traced texels in it and passes readCascade's cascade beside it to the merge and the gather.
// In a room under a lintel lit by the sky alone, each level read at 16 a side and the finest
// gathered at 8, the face toward the opening read 0.83 of the light traced there at 2, for four
// times the rays and steps, and 0.57 at 1. An even mean read it 0.94 at 2, counting the texels by
// the octahedron's corner toward the opening, which see the sky and span the least solid angle,
// as whole ones.
public struct TracedMeans<P : IFarProbes> : IFarProbes
{
    // The probes as traced, read at their traced texels.
    public P traced;
    // The traced texels along a side.
    public uint texels;
    // The traced texels along a side of each read texel, 1 where they are read as traced.
    public uint perRead;

    public float3 lightAt(int3 probe, uint2 texel)
    {
        if (perRead <= 1u)
            return traced.lightAt(probe, texel);
        float3 sum = float3(0.0);
        float shares = 0.0;
        for (uint j = 0u; j < perRead; j++)
            for (uint i = 0u; i < perRead; i++)
            {
                uint2 at = texel * perRead + uint2(i, j);
                float share = octahedronShare((float2(at) + 0.5) / float(texels));
                sum += traced.lightAt(probe, at) * share;
                shares += share;
            }
        return sum / shares;
    }

    // The reach maps keep their own texels (ReachTexels), read as traced.
    public float reachAt(int3 probe, uint2 texel) { return traced.reachAt(probe, texel); }
}

// TracedMeans of probes that keep the sky's share, each read texel's share the mean of the traced
// texels' it holds at the weights its light takes.
public struct TracedSkyMeans<P : IFarSkyProbes> : IFarSkyProbes
{
    // The probes as traced, read at their traced texels, the traced texels along a side and those
    // of each read texel, as TracedMeans holds them.
    public TracedMeans<P> means;

    public float3 lightAt(int3 probe, uint2 texel) { return means.lightAt(probe, texel); }

    public float reachAt(int3 probe, uint2 texel) { return means.reachAt(probe, texel); }

    public float skyShareAt(int3 probe, uint2 texel)
    {
        if (means.perRead <= 1u)
            return means.traced.skyShareAt(probe, texel);
        float sum = 0.0, shares = 0.0;
        for (uint j = 0u; j < means.perRead; j++)
            for (uint i = 0u; i < means.perRead; i++)
            {
                uint2 at = texel * means.perRead + uint2(i, j);
                float share = octahedronShare((float2(at) + 0.5) / float(means.texels));
                sum += means.traced.skyShareAt(probe, at) * share;
                shares += share;
            }
        return sum / shares;
    }
}

// A cascade traced at its texels as the level below and the gather read it, at perRead traced
// texels along a side of each read texel (TracedMeans).
public ProbeCascade readCascade(ProbeCascade cascade, uint perRead)
{
    cascade.texels /= max(perRead, 1u);
    return cascade;
}

// The far cascade's light over a near texel from the far probes about e, each at its trilinear
// weight on the far grid, a probe inside a mesh or not seeing e weighing nothing (farProbeSees),
// in skyShares the sum of their shares of the sky at the same weights, and in weights the sum of
// the weights taken, which divides both.
float3 farLightAbout<P : IFarSkyProbes, Q : IProbePlaces>(P probes, Q places, ProbeCascade far, float3 e, uint texels, uint2 texel,
    out float skyShares, out float weights)
{
    int3 base;
    float3 f;
    farProbesAbout(far, e, base, f);
    float3 sum = float3(0.0);
    skyShares = 0.0;
    weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 probe = base + offset;
        if (any(probe < 0) || any(probe >= far.count))
            continue;
        if (!farProbeSees(probes, probe, places.placeOf(probe), e, SeeingSlack * far.spacing))
            continue;
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z;
        float skyShare;
        sum += farTexelsLight(probes, far, probe, texels, texel, skyShare) * weight;
        skyShares += skyShare * weight;
        weights += weight;
    }
    return sum;
}

// farLightAbout over the corners counted marks (farProbesSplit), which it does not test again, so
// a hand-off where the split was tested reads each far probe's reach toward e once. Tested again,
// each ray read 16 reach texels where it handed off past its end, and read with the test of each
// probe's place, as many reads of the field.
float3 farCountedLight<P : IFarSkyProbes>(P probes, ProbeCascade far, float3 e, uint texels, uint2 texel, uint counted, out float skyShares,
    out float weights)
{
    int3 base;
    float3 f;
    farProbesAbout(far, e, base, f);
    float3 sum = float3(0.0);
    skyShares = 0.0;
    weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        if ((counted & (1u << uint(corner))) == 0u)
            continue;
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z;
        float skyShare;
        sum += farTexelsLight(probes, far, base + offset, texels, texel, skyShare) * weight;
        skyShares += skyShare * weight;
        weights += weight;
    }
    return sum;
}

// How far apart the points of the near ray nearest the far probes' own hits along its way may
// lie, as a share of the farther, for the probes to agree on where a surface lies
// (farProbesSplit). A quarter, since a tenth and a quarter read a room's deep end under a lintel
// alike, 0.990 and 0.957 of the light traced there, where a half takes hits that far apart as
// agreeing and reads it 1.080 and 0.993.
public static const float HitSpread = 0.25;

// Whether the far probes about e that count, those not inside a mesh that see e (farProbeSees),
// disagree on where a surface lies along the near ray from origin, some meeting one along the way
// within their reach (reachAt) and some meeting none, or every one meeting one at points of the
// near ray past e that lie farther apart than HitSpread of the farther and two of the field's
// cells at e, and in counted a bit for each corner that counts (farCountedLight).
bool farProbesSplit<F : IProbeField, P : IFarProbes, Q : IProbePlaces>(F field, P probes, Q places, ProbeCascade far, float3 origin, float3 e,
    uint texels, uint2 texel, out uint counted)
{
    float3 way = texelWay(texels, texel);
    int3 base;
    float3 f;
    farProbesAbout(far, e, base, f);
    float handed = dot(e - origin, way), nearest = ReachOpen, farthest = 0.0;
    int open = 0, met = 0;
    counted = 0u;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 probe = base + int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        if (any(probe < 0) || any(probe >= far.count))
            continue;
        float3 place = places.placeOf(probe);
        if (!farProbeSees(probes, probe, place, e, SeeingSlack * far.spacing))
            continue;
        counted |= 1u << uint(corner);
        float reach = probes.reachAt(probe, texelOfWay(ReachTexels, way));
        if (reach >= ReachOpen)
        {
            open++;
            continue;
        }
        float along = max(dot(place + way * reach - origin, way) - handed, 0.0);
        nearest = min(nearest, along);
        farthest = max(farthest, along);
        met++;
    }
    int c = field.cascadeOf(e, 1.0);
    float cells = c >= 0 ? 2.0 * field.cornerAndCell(c).w : 0.0;
    return met > 0 && (open > 0 || farthest - nearest > max(HitSpread * farthest, cells));
}

// The light a near probe's texel of an octahedron of `texels` along each side brings along its
// way from origin, its ray traced as near, the light it took on its way (ProbeRay) with what lies
// past it weighed by the share it carries. far is the far cascade with its own end, to which its
// probes' rays and reach maps run, and marchEnd the coarsest cascade's end, to which a march past
// every far interval runs. A ray that met a surface takes nothing past it and one that left the
// field the sky's along the way. One that reached its interval's end, or stopped at its cascade's
// cap of steps short of it, marches on itself first to open space (marchToOpenSpace), where the
// field's distance reads a far spacing or the field's band, whichever is less, and hands off
// there, at E, or brings the light of the surface it met on the way. A hand-off where a surface
// lies nearer E than a far spacing lets the far probes about E, standing up to a spacing off the
// ray, meet that surface's edge where their own rays do, and a wall lit by a sun 0.4 units past a
// probe's reach read 37 percent under its share of light. Where the far probes about E that count
// disagree on where a surface lies along the way (farProbesSplit), the ray takes none of them and
// marches on itself across the far interval, to the far cascade's own end past E, then hands off
// where that march ends, or takes the surface it meets or the sky where it leaves. Far probes
// beside a ray that ends short of an opening's edge see past the edge where the ray meets it, as
// probes under a lintel see the sky where the ray meets the lintel's inner face, and a room's
// deep end read 1.4 times the light traced there when they were taken; marched across, it reads
// 0.96 to 0.99 of it, for 1.15 times the steps. At its hand-off the ray takes the far cascade's
// light over the texel from the far probes about the hand-off (farTexelsLight), each at its
// trilinear weight on the far grid, normalized by their sum, all times the share the ray carries.
// A far probe that stands inside a mesh, the field's distance at its place (places) below zero,
// or that does not see the hand-off from there (farProbeSees) weighs nothing, not a small share,
// since its light along the way is of another room than the ray's, and one weighed a little
// lights a closed room from the sky outside its walls. Where none counts, the ray marches on
// itself to marchEnd and takes the sky along the way past the field, marched set. Every march
// past the ray's end takes the far cascade's cap of steps (stepCap), none where it is 0, and one
// that stops at it hands off where it stopped, taking nothing past it where no far probe counts
// there. Every step past the ray's end is handed to steps. The far probes' own rays begin at the
// probes, the far cascade's interval starting at 0, since a probe about E whose rays began 4 units
// out would leave the near ray's next 4 units past E untraced, and a floor in a box open to the
// sky would take the sky's light along every way whose ray meets a wall there, 3.6 times what it
// should.
public float3 mergedLight<F : IProbeField, P : IFarProbes, Q : IProbePlaces, K : ISky, S : IMarchSteps>(F field, P probes, Q places,
    ProbeCascade far, float marchEnd, float3 origin, uint texels, uint2 texel, ProbeRay near, K sky, inout S steps, out bool marched)
{
    NoSkyShares<P> lights = { probes };
    WholeShare<K> whole = { sky };
    float skyShare;
    return mergedLight(field, lights, places, far, marchEnd, origin, texels, texel, near, whole, steps, marched, skyShare);
}

// mergedLight with in skyShare the share of the sky in the light, what it would be under a sky of
// 1 along every way with every surface unlit, tracked beside the light: the share the ray carries
// where it takes the sky, times the share the sky lends along the way (lastCascadeSkyShare), and at
// a hand-off the far probes' shares (IFarSkyProbes) weighed as their light is, times the share
// carried there.
public float3 mergedLight<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISkyShare, S : IMarchSteps>(F field, P probes, Q places,
    ProbeCascade far, float marchEnd, float3 origin, uint texels, uint2 texel, ProbeRay near, K sky, inout S steps, out bool marched,
    out float skyShare)
{
    marched = false;
    skyShare = 0.0;
    float3 way = texelWay(texels, texel);
    if (near.ended != RayPast && near.ended != RayCapped)
    {
        skyShare = lastCascadeSkyShare(near, way, sky);
        return lastCascadeLight(near, way, sky);
    }
    // The light each march took on its way is added as it goes, each weighed by the share carried
    // to it. The marches past the ray's end are legs of one loop with one call of the march, since
    // a driver inlines an engine's shading (hitLight) at every call of the march, and with a call
    // a leg a merge took 2.6 minutes to compile on one engine: leg 0 to open space, leg 1 across
    // the far interval where the far probes about E disagree, and leg 2 on to marchEnd where no
    // far probe counts.
    float3 taken = near.light;
    float carried = near.carried;
    float from = near.t;
    uint leg = 0u;
    // Three marches at most, since leg 2 returns, with a return after them that is never reached,
    // since naga refuses WGSL whose function may end with no return, and the loop endless with no
    // return after it kept one engine's merge from being made.
    [loop]
    for (uint marches = 0u; marches < 3u; marches++)
    {
        float end = leg == 1u ? min(from + far.end, marchEnd) : marchEnd;
        ProbeRay ray = marchToOpenSpace(field, origin, way, from, end, leg == 0u ? far.spacing : 0.0, far.stepCap, steps);
        if (ray.ended == RayMet || ray.ended == RayLeft || (ray.ended == RayPast && ray.t >= marchEnd))
        {
            skyShare = lastCascadeSkyShare(ray, way, sky) * carried;
            return taken + lastCascadeLight(ray, way, sky) * carried;
        }
        taken += ray.light * carried;
        carried *= ray.carried;
        from = ray.t;
        // Where the far probes about E disagree on where a surface lies along the way, the ray
        // takes none of them there and marches on itself across the far interval, the far
        // cascade's own end past E, and hands off where it ends, taking the surface it meets or
        // the sky where it leaves.
        uint counted = 0u;
        if (leg == 0u && farProbesSplit(field, probes, places, far, origin, origin + way * from, texels, texel, counted))
        {
            leg = 1u;
            continue;
        }
        // The hand-off where the split was tested takes the corners it counted, and one past a
        // later leg tests them where that leg ended.
        float skyShares, weights;
        float3 sum = leg == 0u ? farCountedLight(probes, far, origin + way * from, texels, texel, counted, skyShares, weights)
            : farLightAbout(probes, places, far, origin + way * from, texels, texel, skyShares, weights);
        if (weights > 0.0)
        {
            skyShare = skyShares / weights * carried;
            return taken + sum / weights * carried;
        }
        // A march on capped short of marchEnd takes nothing past where it stopped where none of
        // the far probes about it counts.
        if (leg == 2u)
            return taken;
        marched = true;
        leg = 2u;
    }
    return taken;
}

// The shape every merge takes, one kernel behind one name, which a binding selects by naming the
// merge's struct where it calls the passes (mergeTexel, gatherTraced), so that a merge is added as
// a struct of its own and no pass or other merge changes. A merge brings the light a near texel of
// an octahedron of `texels` along each side brings along its way from origin, its ray traced as
// near, with what lies past the ray's end taken from far's probes read through probes at their
// places, in skyShare the share of the sky in that light, and in marched whether the ray marched on
// itself to marchEnd, as mergedLight takes them. The merges stand on a ladder the module's tests
// keep, each read against every other over a set of scenes, and the one on top is the default the
// passes take where a binding names none.
public interface IMerge
{
    float3 light<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISkyShare, S : IMarchSteps>(F field, P probes, Q places,
        ProbeCascade far, float marchEnd, float3 origin, uint texels, uint2 texel, ProbeRay near, K sky, inout S steps, out bool marched,
        out float skyShare);
}

// The merge at the ray's end (mergedLight), the ladder's first rung. The ray marches on to open
// space and hands off to the far probes about where it stopped, marching across the far interval
// where they disagree on where a surface lies along the way.
public struct MergeAtRayEnd : IMerge
{
    public float3 light<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISkyShare, S : IMarchSteps>(F field, P probes, Q places,
        ProbeCascade far, float marchEnd, float3 origin, uint texels, uint2 texel, ProbeRay near, K sky, inout S steps, out bool marched,
        out float skyShare)
    {
        return mergedLight(field, probes, places, far, marchEnd, origin, texels, texel, near, sky, steps, marched, skyShare);
    }
}
