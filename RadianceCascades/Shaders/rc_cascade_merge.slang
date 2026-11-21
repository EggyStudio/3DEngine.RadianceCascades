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

// How far past a far probe's reach toward a point the point may lie for the probe to see it, as a
// share of the far cascade's spacing, an eighth, a cell of a quarter unit at a spacing of 2. A
// probe's reach map keeps the ray of each texel's middle way, which passes up to half a texel off
// the way toward the point, so a point beside a surface that ray met a little short of it is seen
// within the slack, and a point behind a wall half a unit thick lies past it.
public static const float SeeingSlack = 0.125;

// Whether the far probe at place sees e, where the distance to e lies within its reach toward e
// (reachAlong), and slack more.
public bool farProbeSees<P : IFarProbes>(P probes, int3 probe, float3 place, float3 e, float slack)
{
    float3 toward = e - place;
    float distance = length(toward);
    return distance <= slack || distance <= probes.reachAt(probe, texelOfWay(ReachTexels, toward / distance)) + slack;
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
    uint k = far.texels / texels;
    if (k <= 1u || k * texels != far.texels)
        return probes.lightAt(probe, texelOfWay(far.texels, texelWay(texels, texel)));
    float3 sum = float3(0.0);
    float shares = 0.0;
    for (uint j = 0u; j < k; j++)
        for (uint i = 0u; i < k; i++)
        {
            uint2 covered = texel * k + uint2(i, j);
            float share = octahedronShare((float2(covered) + 0.5) / float(far.texels));
            sum += probes.lightAt(probe, covered) * share;
            shares += share;
        }
    return sum / shares;
}

// The far cascade's light over a near texel from the far probes about e, each at its trilinear
// weight on the far grid, a probe inside a mesh or not seeing e weighing nothing, and in weights
// the sum of the weights taken, which divides it.
float3 farLightAbout<F : IProbeField, P : IFarProbes, Q : IProbePlaces>(F field, P probes, Q places, ProbeCascade far, float3 e, uint texels,
    uint2 texel, out float weights)
{
    int3 base;
    float3 f;
    farProbesAbout(far, e, base, f);
    float3 sum = float3(0.0);
    weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 probe = base + offset;
        if (any(probe < 0) || any(probe >= far.count))
            continue;
        float3 place = places.placeOf(probe);
        int c = field.cascadeOf(place, 1.0);
        if ((c >= 0 && fieldDistanceIn(field, c, place) < 0.0) || !farProbeSees(probes, probe, place, e, SeeingSlack * far.spacing))
            continue;
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z;
        sum += farTexelsLight(probes, far, probe, texels, texel) * weight;
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

// Whether the far probes about e that count, those not inside a mesh that see e, disagree on where
// a surface lies along the near ray from origin, some meeting one along the way within their
// reach (reachAt) and some meeting none, or every one meeting one at points of the near ray past e
// that lie farther apart than HitSpread of the farther and two of the field's cells at e.
bool farProbesSplit<F : IProbeField, P : IFarProbes, Q : IProbePlaces>(F field, P probes, Q places, ProbeCascade far, float3 origin, float3 e,
    uint texels, uint2 texel)
{
    float3 way = texelWay(texels, texel);
    int3 base;
    float3 f;
    farProbesAbout(far, e, base, f);
    float handed = dot(e - origin, way), nearest = ReachOpen, farthest = 0.0;
    int open = 0, met = 0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 probe = base + int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        if (any(probe < 0) || any(probe >= far.count))
            continue;
        float3 place = places.placeOf(probe);
        int c = field.cascadeOf(place, 1.0);
        if ((c >= 0 && fieldDistanceIn(field, c, place) < 0.0) || !farProbeSees(probes, probe, place, e, SeeingSlack * far.spacing))
            continue;
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
    marched = false;
    float3 way = texelWay(texels, texel);
    if (near.ended != RayPast && near.ended != RayCapped)
        return lastCascadeLight(near, way, sky);
    // The light each march took on its way is added as it goes, each weighed by the share carried
    // to it.
    ProbeRay open = marchToOpenSpace(field, origin, way, near.t, marchEnd, far.spacing, far.stepCap, steps);
    if (open.ended == RayMet || open.ended == RayLeft || (open.ended == RayPast && open.t >= marchEnd))
        return near.light + lastCascadeLight(open, way, sky) * near.carried;
    float carried = near.carried * open.carried;
    float3 taken = near.light + open.light * near.carried;
    float handed = open.t;
    // Where the far probes about E disagree on where a surface lies along the way, the ray takes
    // none of them there and marches on itself across the far interval, the far cascade's own end
    // past E, and hands off where it ends, taking the surface it meets or the sky where it leaves.
    if (farProbesSplit(field, probes, places, far, origin, origin + way * open.t, texels, texel))
    {
        ProbeRay across = traceProbeRay(field, origin, way, open.t, min(open.t + far.end, marchEnd), far.stepCap, steps);
        if (across.ended == RayMet || across.ended == RayLeft || (across.ended == RayPast && across.t >= marchEnd))
            return taken + lastCascadeLight(across, way, sky) * carried;
        taken += across.light * carried;
        carried *= across.carried;
        handed = across.t;
    }
    float weights;
    float3 sum = farLightAbout(field, probes, places, far, origin + way * handed, texels, texel, weights);
    if (weights > 0.0)
        return taken + sum / weights * carried;
    marched = true;
    ProbeRay on = traceProbeRay(field, origin, way, handed, marchEnd, far.stepCap, steps);
    taken += on.light * carried;
    if (on.ended != RayCapped)
        return taken + (lastCascadeLight(on, way, sky) - on.light) * carried;
    // A march-on capped short of the far cascade's end is handed to the far probes about where it
    // stopped, and takes nothing past it where none of them counts.
    sum = farLightAbout(field, probes, places, far, origin + way * on.t, texels, texel, weights);
    return weights > 0.0 ? taken + sum / weights * (carried * on.carried) : taken;
}
