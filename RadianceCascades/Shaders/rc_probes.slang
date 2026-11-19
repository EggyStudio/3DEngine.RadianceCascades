// The probes of a cascade, where each stands, the way each of its texels looks along and how much
// of the sphere each spans, the trace of a texel's ray through the scene's distance field within
// the cascade's interval, how far a probe's ray goes before it meets a surface, and the light a
// probe's texels give a surface beside it.
module rc_probes;

__exported import rc_octahedron;
__exported import rc_field_read;

static const float Pi = 3.14159265358979;

// A cascade's probes on a grid, the place of its first probe in the world, the spacing between
// probes, how many stand along each axis, the texels along each side of a probe's octahedron, and
// the interval of distances from a probe along each of its texels' rays its trace takes, from
// start to end.
public struct ProbeCascade
{
    public float3 corner;
    public float spacing;
    public int3 count;
    public uint texels;
    public float start;
    public float end;
}

// Where a probe of the cascade stands.
public float3 probePlace(ProbeCascade cascade, int3 probe)
{
    return cascade.corner + float3(probe) * cascade.spacing;
}

// The way a texel of an octahedron of `texels` along each side looks along, the way of its middle,
// which its one ray takes.
public float3 texelWay(uint texels, uint2 texel)
{
    return fromOctahedron((float2(texel) + 0.5) / float(texels));
}

// The texel of an octahedron of `texels` along each side a way falls in.
public uint2 texelOfWay(uint texels, float3 way)
{
    return min(uint2(octahedron(way) * float(texels)), uint2(texels - 1u));
}

// The sum of octahedronShare over the middles of the texels of an octahedron of `texels` along each
// side, which texelSolidAngle divides by so the texels' solid angles sum to the sphere's 4 pi.
public float octahedronShares(uint texels)
{
    float sum = 0.0;
    for (uint k = 0u; k < texels * texels; k++)
        sum += octahedronShare((float2(float(k % texels), float(k / texels)) + 0.5) / float(texels));
    return sum;
}

// The solid angle a texel of an octahedron of `texels` along each side spans, its octahedronShare
// at its middle over `shares`, the sum octahedronShares gives, of the sphere's 4 pi.
public float texelSolidAngle(uint texels, uint2 texel, float shares)
{
    return octahedronShare((float2(texel) + 0.5) / float(texels)) * (4.0 * Pi) / shares;
}

// The field's cascades as a probe's ray reads them, the distances and planes IFieldVolume reads
// with what the surface nearest a cell gives a ray that meets it, which an engine implements over
// its images.
public interface IProbeField : IFieldVolume
{
    // The light the surface nearest a cell of a cascade sends of its own, its emission.
    float3 surfaceLightAt(int cascade, int3 cell);

    // The share of light that passes a unit of the depth of the surface nearest a cell of a
    // cascade, 0 for a surface that lets none through, which a ray crosses where it is above zero.
    float throughAt(int cascade, int3 cell);
}

// How a probe's ray ended, at a surface it met, at the end of its interval with nothing met, or
// where it left every cascade of the field.
public static const uint RayMet = 0u;
public static const uint RayPast = 1u;
public static const uint RayLeft = 2u;

// A probe's ray as its trace ended, how it ended (RayMet, RayPast or RayLeft), the distance along
// it where it did, the share of light it carries past the surfaces it crossed, and the light of the
// surface it met times that share, none where it met none.
public struct ProbeRay
{
    public uint ended;
    public float t;
    public float carried;
    public float3 light;
}

// The cell of cascade i that p lies in.
int3 cellOf<F : IFieldVolume>(F field, int i, float3 p)
{
    float4 oc = field.cornerAndCell(i);
    return clamp(int3(floor((p - oc.xyz) / oc.w)), int3(0), int3(field.size() - 1));
}

// The trace of a ray from origin along way through the field between start and end, as
// traceFieldSheets marches it, through the finest cascade that holds each point, each step the
// distance read or MarchStep of a cell at least and each handed to steps. It ends where it comes
// within MarchHit of a cell of a surface, at end, or where it leaves every cascade. A surface whose
// share of light through is above zero the ray crosses, stepping out of the solid by half a cell or
// the distance read below zero, whichever is more, and its carried share is multiplied by the
// surface's share to the power of the depth it crossed, from where the march met the surface, the
// distance read on, to where it came out, the distance read back, so a sheet of no depth passes
// its light whole. A cell met within it whose surface lets no light through stops the ray there.
// The march takes no count of steps at most, since each goes on by a quarter of a cell at least
// and the field ends, so a ray of an interval that runs to the field's edge takes every step to
// its end.
public ProbeRay traceProbeRay<F : IProbeField, S : IMarchSteps>(F field, float3 origin, float3 way, float start, float end, inout S steps)
{
    ProbeRay ray;
    ray.ended = RayPast;
    ray.t = end;
    ray.carried = 1.0;
    ray.light = float3(0.0);
    float t = start;
    // Where the ray met the surface it is crossing and that surface's share, or below zero where it
    // crosses none.
    float entered = -1.0, through = 0.0;
    [loop]
    while (t < end)
    {
        float3 p = origin + way * t;
        int c = field.cascadeOf(p, 1.0);
        if (c < 0)
        {
            ray.ended = RayLeft;
            ray.t = t;
            break;
        }
        float cell = field.cornerAndCell(c).w;
        float d = fieldDistanceSheetIn(field, c, p);
        steps.at(t, d);
        if (entered >= 0.0)
        {
            if (d < cell * MarchHit)
            {
                // A solid that lets no light through, met within the one the ray crosses, as a
                // floor under a glass wall, stops it, where marched on it would come out past the
                // floor's underside.
                if (field.throughAt(c, cellOf(field, c, p)) <= 0.0)
                {
                    ray.carried *= pow(through, max(t - entered, 0.0));
                    ray.ended = RayMet;
                    ray.t = t;
                    ray.light = field.surfaceLightAt(c, cellOf(field, c, p)) * ray.carried;
                    return ray;
                }
                t += max(cell * 0.5, -d);
                continue;
            }
            ray.carried *= pow(through, max(t - d - entered, 0.0));
            entered = -1.0;
        }
        else if (d < cell * MarchHit)
        {
            through = field.throughAt(c, cellOf(field, c, p));
            if (through <= 0.0)
            {
                ray.ended = RayMet;
                ray.t = t;
                ray.light = field.surfaceLightAt(c, cellOf(field, c, p)) * ray.carried;
                return ray;
            }
            entered = t + max(d, 0.0);
            t += max(cell * 0.5, -d);
            continue;
        }
        t += max(d, cell * MarchStep);
    }
    // A ray that ends within a surface it crosses passes the depth it crossed of it.
    if (entered >= 0.0)
        ray.carried *= pow(through, max(min(t, end) - entered, 0.0));
    return ray;
}

// The same with no steps kept.
public ProbeRay traceProbeRay<F : IProbeField>(F field, float3 origin, float3 way, float start, float end)
{
    NoSteps none;
    return traceProbeRay(field, origin, way, start, end, none);
}

// The light a ray of the last cascade brings, the light of the surface it met, or, past its
// interval or the field, where no cascade beyond holds the light, the sky's times the share it
// carries.
public float3 lastCascadeLight(ProbeRay ray, float3 sky)
{
    return ray.ended == RayMet ? ray.light : sky * ray.carried;
}

// The texels along each side of a far probe's reach map, how far its rays went in each way.
public static const uint ReachTexels = 16u;

// How far a ray from a probe's own place along way goes before it meets a surface, whether or not
// the surface lets light through, up to end or where it leaves every cascade, the reach a far
// probe's map keeps for a texel's middle way.
public float reachAlong<F : IFieldVolume>(F field, float3 place, float3 way, float end)
{
    float t = 0.0;
    [loop]
    while (t < end)
    {
        float3 p = place + way * t;
        int c = field.cascadeOf(p, 1.0);
        if (c < 0)
            return t;
        float cell = field.cornerAndCell(c).w;
        float d = fieldDistanceSheetIn(field, c, p);
        if (d < cell * MarchHit)
            return t;
        t += max(d, cell * MarchStep);
    }
    return end;
}

// A probe's texels as the gather reads them, each one's light merged from every distance.
public interface IProbeLight
{
    // The light a texel brings along its way.
    float3 lightOf(uint2 texel);
}

// The light a surface of normal n beside a probe takes from the probe's texels of an octahedron of
// `texels` along each side, each texel's light times the cosine of its way off n, none behind the
// surface, times its solid angle (texelSolidAngle).
public float3 lightTaken<L : IProbeLight>(L probe, uint texels, float3 n)
{
    float shares = octahedronShares(texels);
    float3 taken = float3(0.0);
    for (uint k = 0u; k < texels * texels; k++)
    {
        uint2 texel = uint2(k % texels, k / texels);
        float facing = max(dot(n, texelWay(texels, texel)), 0.0);
        if (facing > 0.0)
            taken += probe.lightOf(texel) * (facing * texelSolidAngle(texels, texel, shares));
    }
    return taken;
}

// The light a diffuse surface of albedo reflects of the light it takes (lightTaken), albedo over pi
// times it, so a floor under a uniform sky of every way reflects albedo times the sky.
public float3 reflectedLight(float3 albedo, float3 taken)
{
    return albedo / Pi * taken;
}
