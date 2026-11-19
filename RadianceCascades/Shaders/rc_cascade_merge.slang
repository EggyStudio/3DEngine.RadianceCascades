// The merge of a near cascade's texel with the far cascade past it. A texel's ray that reaches the
// end of its interval with nothing met takes the far cascade's light along its own way from the
// eight far probes about the point where it ended, and where none of them counts it marches on
// itself through the field.
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

// The light a near probe's texel brings along way from origin, its ray traced as near, the light of
// the surface it met, the sky's where it left the field, and where it reached its interval's end E
// the far cascade's along the same way from the far probes about E, each at its trilinear weight,
// normalized by their sum, all times the share the ray carries. A far probe that stands inside a
// mesh, the field's distance at it below zero, or that does not see E (farProbeSees) weighs
// nothing, not a small share, since its light along the way is of another room than E's, and one
// weighed a little lights a closed room from the sky outside its walls. Where none counts, the ray
// marches on itself from E to the far cascade's end along it, each step handed to steps and marched
// set, and takes the sky past the field. The far probes' own rays begin at the probes, the far
// cascade's interval starting at 0, since a probe about E whose rays began 4 units out would leave
// the near ray's next 4 units past E untraced, and a floor in a box open to the sky would take the
// sky's light along every way whose ray meets a wall there, 3.6 times what it should.
public float3 mergedLight<F : IProbeField, P : IFarProbes, S : IMarchSteps>(F field, P probes, ProbeCascade far, float3 origin, float3 way,
    ProbeRay near, float3 sky, inout S steps, out bool marched)
{
    marched = false;
    if (near.ended != RayPast)
        return lastCascadeLight(near, sky);
    float3 e = origin + way * near.t;
    int3 base;
    float3 f;
    farProbesAbout(far, e, base, f);
    uint2 texel = texelOfWay(far.texels, way);
    float3 sum = float3(0.0);
    float weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 probe = base + offset;
        if (any(probe < 0) || any(probe >= far.count))
            continue;
        float3 place = probePlace(far, probe);
        int c = field.cascadeOf(place, 1.0);
        if ((c >= 0 && fieldDistanceIn(field, c, place) < 0.0) || !farProbeSees(probes, probe, place, e, SeeingSlack * far.spacing))
            continue;
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z;
        sum += probes.lightAt(probe, texel) * weight;
        weights += weight;
    }
    if (weights > 0.0)
        return sum / weights * near.carried;
    marched = true;
    ProbeRay on = traceProbeRay(field, origin, way, near.t, far.end, steps);
    return lastCascadeLight(on, sky) * near.carried;
}
