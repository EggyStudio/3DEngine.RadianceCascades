// The probes of a cascade, where each stands, the way each of its texels looks along and how much
// of the sphere each spans, the trace of a texel's ray through the scene's distance field within
// the cascade's interval, how far a probe's ray goes before it meets a surface, and the light a
// probe's texels give a surface beside it.
module rc_probes;

__exported import rc_octahedron;
__exported import rc_field_read;

static const float Pi = 3.14159265358979;

// A cascade's probes on a grid, the place of its first probe in the world, the spacing between
// probes, how many stand along each axis, the texels along each side of a probe's octahedron, the
// interval of distances from a probe along each of its texels' rays its trace takes, from start to
// end, and the most steps a march of its rays takes, 0 for no count (stepCap). A binding sets
// every field, stepCap 0 where it caps nothing.
public struct ProbeCascade
{
    public float3 corner;
    public float spacing;
    public int3 count;
    public uint texels;
    public float start;
    public float end;
    public uint stepCap;
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
// its images and its shading.
public interface IProbeField : IFieldVolume
{
    // The light the surface a ray met at place sends back along the ray, toward -way, as the
    // engine shades it, its emission and the light it reflects of its sun, its local lights and
    // the probes' own, with the cascade and the cell of the field the hit lies in, which a binding
    // reads the surface's paint and gloss from and the field's normal at place (fieldNormalIn).
    // The way lets it weigh a gloss toward the eye the ray stands for, as a split sum does.
    float3 hitLight(float3 place, float3 way, int cascade, int3 cell);

    // The share of light that passes a unit of the depth of the surface nearest a cell of a
    // cascade, 0 for a surface that lets none through, which a ray crosses where it is above zero.
    float throughAt(int cascade, int3 cell);
}

// How a probe's ray ended, at a surface it met, at the end of its interval with nothing met, where
// it left every cascade of the field, or where its march took the most steps its cascade allows
// (stepCap) with nothing met, short of its end.
public static const uint RayMet = 0u;
public static const uint RayPast = 1u;
public static const uint RayLeft = 2u;
public static const uint RayCapped = 3u;

// A probe's ray as its trace ended, how it ended (RayMet, RayPast, RayLeft or RayCapped), the
// distance along it where it did, the share of light it carries past the surfaces it crossed, and
// the light it took on its way, each surface it crossed sending its own light weighed by what that
// surface keeps of the light and the share carried to it, and the surface it met its light times
// the share carried to it. What lies past where it ended, the level above's light or the sky's,
// is weighed by the share it carries and added.
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
// the distance read below zero, whichever is more, and its carried share is multiplied over the
// depth it crossed, from where the march met the surface, the distance read on, to where it came
// out, the distance read back, by the share of the cell each step within began in to the power of
// that step's depth, and a sheet thinner than a cell, which has no depth to cross, passes its share
// once, as a unit of depth would. The ray takes the light the surface sends (hitLight) where the
// crossing has stopped half the light carried into it, or at its last point within where it passes
// more, weighed by what the surface keeps, one less what it passes, and the share carried to it, so
// a lit canopy of leaves reads lit from below as well as dimming what lies past it. A cell met
// within it whose surface lets no light through stops the ray there. The march takes no count of
// steps at most, since each goes on by a quarter of a cell at least and the field ends, so a ray of
// an interval that runs to the field's edge takes every step to its end.
public ProbeRay traceProbeRay<F : IProbeField, S : IMarchSteps>(F field, float3 origin, float3 way, float start, float end, inout S steps)
{
    return marchProbeRay(field, origin, way, start, end, 0.0, 0u, steps);
}

// The same, ending as RayCapped where the march has taken cap steps short of its end, cap 0 for
// no count, as a cascade's stepCap gives it.
public ProbeRay traceProbeRay<F : IProbeField, S : IMarchSteps>(F field, float3 origin, float3 way, float start, float end, uint cap,
    inout S steps)
{
    return marchProbeRay(field, origin, way, start, end, 0.0, cap, steps);
}

// How far the distance a march reads must reach for the ray to stand in open space, the lesser of
// clearance and the field's band at the point (FieldBand cells), a hundredth short of it, since
// past the band the field reads the band's distance whatever lies farther, and the blend of cells
// that each read it comes out a rounding under it.
float openAt(float clearance, float cell)
{
    return 0.99 * min(clearance, FieldBand * cell);
}

// The march of a ray past the end of its interval to open space, from start along way through the
// field as traceProbeRay marches it, crossing surfaces that let light through, until it meets a
// surface, reaches end, leaves every cascade, or stands where the distance read reaches clearance
// or the field's band, whichever is less (openAt), outside any surface it crosses, where it ends
// as RayPast at that distance along it, or as RayCapped where it has taken cap steps, cap 0 for no
// count. A ray that starts in open space ends there with no step taken. Clearance 0 marches as
// traceProbeRay does, to end, which mergedLight takes for every leg of its march past a ray's end
// at one call. Each step on is handed to steps.
public ProbeRay marchToOpenSpace<F : IProbeField, S : IMarchSteps>(F field, float3 origin, float3 way, float start, float end, float clearance,
    uint cap, inout S steps)
{
    return marchProbeRay(field, origin, way, start, end, clearance, cap, steps);
}

// Whether the cell of cascade i keeps the plane of a double-sided face (sheetOf), a sheet thinner
// than a cell, which a ray crosses with no depth.
bool keepsSheet<F : IFieldVolume>(F field, int i, int3 at)
{
    float3 plane = field.cellAt(i, at).yzw;
    float3 second = field.secondAt(i, at);
    return dot(plane, plane) >= 0.25 || dot(second, second) >= 0.25;
}

// Ends a ray's crossing of a surface that passed the share passed of the light it carried in, the
// surface's own light at the crossing, light, weighed by what it keeps and the share carried to
// it, and the share carried on by what it passes.
void crossSurface(inout ProbeRay ray, float passed, float3 light)
{
    ray.light += light * (ray.carried * (1.0 - passed));
    ray.carried *= passed;
}

// The share a crossing passes, passed over the steps it took and the share through of the cell
// its last step began in over the depth since, or passed alone where it crossed a sheet, which
// passes its share once.
float crossedShare(float passed, float through, float depth, bool sheet)
{
    return sheet ? passed : passed * pow(through, max(depth, 0.0));
}

// What the march takes the light of a surface for (marchProbeRay): none, a stop at a surface that
// lets no light through, a crossing where it has stopped half the light it carried in, and a
// crossing at its last point where it ends past a step, at a solid met within it or where the
// march ends, each of which passes more.
static const uint ShadeNone = 0u;
static const uint ShadeStop = 1u;
static const uint ShadeHalf = 2u;
static const uint ShadeLeft = 3u;
static const uint ShadeBeforeSolid = 4u;
static const uint ShadeEnded = 5u;

// The march traceProbeRay and marchToOpenSpace share, which ends in open space as well where
// clearance is above zero (openAt), and as RayCapped where cap is above zero and it has taken cap
// steps.
ProbeRay marchProbeRay<F : IProbeField, S : IMarchSteps>(F field, float3 origin, float3 way, float start, float end, float clearance,
    uint cap, inout S steps)
{
    ProbeRay ray;
    ray.ended = RayPast;
    ray.t = end;
    ray.carried = 1.0;
    ray.light = float3(0.0);
    float t = start;
    // Where the ray met the surface it is crossing, or below zero where it crosses none, the share
    // it passed over the steps it took within it, where its last step began and the share of the
    // cell it began in, whether it is a sheet, whether its light is taken and that light, and the
    // last point within it and that point's cascade.
    float entered = -1.0, passed = 1.0, from = 0.0, through = 0.0;
    bool sheet = false, lit = false;
    float3 crossed = float3(0.0), last = origin;
    int lastCascade = 0;
    // Whether the step at t is taken again, its count kept, once a crossing that met a solid within
    // it has taken its light at its last point.
    bool again = false;
    uint taken = 0u;
    [loop]
    while (true)
    {
        // Whether the march ends here, at its end, at its cap or where it leaves every cascade.
        bool ending = false;
        float3 p = origin + way * t;
        int c = -1;
        if (t >= end)
        {
            ending = true;
            ray.ended = RayPast;
            ray.t = end;
        }
        else if (cap > 0u && taken >= cap)
        {
            ending = true;
            ray.ended = RayCapped;
            ray.t = t;
        }
        else
        {
            c = field.cascadeOf(p, 1.0);
            if (c < 0)
            {
                ending = true;
                ray.ended = RayLeft;
                ray.t = t;
            }
        }
        // What the light at q in cascade qc is taken for, where any is (Shading).
        uint shading = ShadeNone;
        float3 q = p;
        int qc = c;
        float cell = 0.0, d = 0.0;
        if (ending)
        {
            // A crossing whose light is still to take takes it at its last point.
            if (entered < 0.0 || lit)
                break;
            shading = ShadeEnded;
            q = last;
            qc = lastCascade;
        }
        else
        {
            cell = field.cornerAndCell(c).w;
            d = fieldDistanceSheetIn(field, c, p);
            if (clearance > 0.0 && entered < 0.0 && d >= openAt(clearance, cell))
            {
                ray.t = t;
                return ray;
            }
            if (!again)
            {
                steps.at(t, d);
                taken++;
            }
            again = false;
            if (entered >= 0.0)
            {
                if (d < cell * MarchHit)
                {
                    // A solid that lets no light through, met within the one the ray crosses, as a
                    // floor under a glass wall, stops it, where marched on it would come out past
                    // the floor's underside. Each step within passes the share of the cell it began
                    // in, as a crown's leaves pass more at its edge than at its core, and a crown 6
                    // units deep passing 0.9 a unit within a unit of its faces and 0.3 within read
                    // the sky a probe before it takes through it 0.99 of its trace, where with the
                    // share it was met at taken for its whole depth it read 1.23 times it.
                    float share = field.throughAt(c, cellOf(field, c, p));
                    if (share > 0.0)
                    {
                        passed = crossedShare(passed, through, t - from, sheet);
                        from = t;
                        through = sheet ? through : share;
                        if (lit || passed > 0.5)
                        {
                            last = p;
                            lastCascade = c;
                            t += max(cell * 0.5, -d);
                            continue;
                        }
                        shading = ShadeHalf;
                    }
                    else if (!lit)
                    {
                        shading = ShadeBeforeSolid;
                        q = last;
                        qc = lastCascade;
                    }
                    else
                    {
                        crossSurface(ray, crossedShare(passed, through, t - from, sheet), crossed);
                        entered = -1.0;
                        shading = ShadeStop;
                    }
                }
                else if (!lit)
                {
                    shading = ShadeLeft;
                    q = last;
                    qc = lastCascade;
                }
                else
                {
                    crossSurface(ray, crossedShare(passed, through, t - d - from, sheet), crossed);
                    entered = -1.0;
                }
            }
            else if (d < cell * MarchHit)
            {
                through = field.throughAt(c, cellOf(field, c, p));
                if (through <= 0.0)
                    shading = ShadeStop;
                else
                {
                    entered = t + max(d, 0.0);
                    from = entered;
                    sheet = keepsSheet(field, c, cellOf(field, c, p));
                    passed = sheet ? through : 1.0;
                    lit = false;
                    last = p;
                    lastCascade = c;
                    if (passed > 0.5)
                    {
                        t += max(cell * 0.5, -d);
                        continue;
                    }
                    shading = ShadeHalf;
                }
            }
        }
        if (shading != ShadeNone)
        {
            // hitLight is called at this one place of the march, since a driver inlines an
            // engine's shading at every call, and with a call at each of the three ways a surface
            // is met a merge took 2.6 minutes to compile on one engine. A crossing takes its light
            // where it has stopped half the light it carried in, or at its last point within where
            // it passes more, its light deferred a step past that point, since the light of the
            // place a crown is met at, taken for all the crossing stops, reads its sunlit front for
            // the shaded inside behind it: a crown lit from before it read a probe there 1.58
            // times its trace so.
            float3 light = field.hitLight(q, way, qc, cellOf(field, qc, q));
            if (shading == ShadeStop)
            {
                ray.ended = RayMet;
                ray.t = t;
                ray.light += light * ray.carried;
                return ray;
            }
            crossed = light;
            lit = true;
            if (shading == ShadeEnded)
                break;
            if (shading == ShadeBeforeSolid)
            {
                crossSurface(ray, crossedShare(passed, through, t - from, sheet), crossed);
                entered = -1.0;
                again = true;
                continue;
            }
            if (shading == ShadeHalf)
            {
                last = p;
                lastCascade = c;
                t += max(cell * 0.5, -d);
                continue;
            }
            crossSurface(ray, crossedShare(passed, through, t - d - from, sheet), crossed);
            entered = -1.0;
        }
        t += max(d, cell * MarchStep);
    }
    // A ray that ends within a surface it crosses passes the depth it crossed of it.
    if (entered >= 0.0)
        crossSurface(ray, crossedShare(passed, through, ray.t - from, sheet), crossed);
    return ray;
}

// The same with no steps kept.
public ProbeRay traceProbeRay<F : IProbeField>(F field, float3 origin, float3 way, float start, float end)
{
    NoSteps none;
    return traceProbeRay(field, origin, way, start, end, none);
}

// The sky a ray that leaves the field takes along its way, which an engine implements over its
// own sky, and UniformSky gives the same along every way.
public interface ISky
{
    // The light the sky sends along way, toward -way, to a ray that leaves the field along it.
    float3 skyAlong(float3 way);
}

// The sky as a merge that keeps the sky's share reads it, its light along a way and the share of
// a sky of 1 a ray leaving the field along it takes, which a binding that lends a cascade no sky,
// as one does before its field holds every mesh in reach, gives as 0 beside its light.
public interface ISkyShare : ISky
{
    // The share of a sky of 1 along every way that a ray leaving the field along way takes, 1 for
    // a sky lent whole.
    float shareAlong(float3 way);
}

// A sky read through ISky alone, lent whole, its share 1 along every way, which the passes that
// keep no share read it through.
public struct WholeShare<K : ISky> : ISkyShare
{
    public K sky;

    public float3 skyAlong(float3 way) { return sky.skyAlong(way); }

    public float shareAlong(float3 way) { return 1.0; }
}

// A sky of one light along every way, lent whole.
public struct UniformSky : ISkyShare
{
    public float3 light;

    public float3 skyAlong(float3 way) { return light; }

    public float shareAlong(float3 way) { return 1.0; }
}

// The light a ray of the last cascade along way brings, the light it took on its way, and past its
// interval or the field, where no cascade beyond holds the light, the sky's along its way times
// the share it carries. A ray that met a surface or stopped at its cap takes nothing past where it
// ended, since no cascade above holds the light past a capped ray's stop, and a ray out of steps
// mostly grazes a surface inside the field, where the sky would light a closed room through it.
public float3 lastCascadeLight<K : ISky>(ProbeRay ray, float3 way, K sky)
{
    return ray.ended == RayMet || ray.ended == RayCapped ? ray.light : ray.light + sky.skyAlong(way) * ray.carried;
}

// The share of the sky in what a ray of the last cascade along way brings, as lastCascadeLight
// takes the sky, the share the ray carries where it takes the sky times the share the sky lends
// along way (ISkyShare), and none where it met a surface or stopped at its cap, its light to the
// sky's as a sky of 1 along every way would give it with every surface unlit.
public float lastCascadeSkyShare<K : ISkyShare>(ProbeRay ray, float3 way, K sky)
{
    return ray.ended == RayMet || ray.ended == RayCapped ? 0.0 : ray.carried * sky.shareAlong(way);
}

// Where a cascade's probes stand, which an engine implements over its own rule where it moves a
// probe that would stand in or against a surface off it, and GridPlaces gives the grid's places.
// The merge and the gather weigh probes by their places on the grid alone and read their places
// here for the rest, a probe's rays, its reach map and whether it stands inside a mesh or sees a
// point.
public interface IProbePlaces
{
    // Where the probe of the cascade stands.
    float3 placeOf(int3 probe);
}

// A cascade's probes at their places on its grid (probePlace).
public struct GridPlaces : IProbePlaces
{
    public ProbeCascade cascade;

    public float3 placeOf(int3 probe) { return probePlace(cascade, probe); }
}

// The grid's places of a cascade's probes.
public GridPlaces gridPlaces(ProbeCascade cascade)
{
    GridPlaces places;
    places.cascade = cascade;
    return places;
}

// The texels along each side of a far probe's reach map, how far its rays went in each way.
public static const uint ReachTexels = 16u;

// The reach of a ray that meets no surface within its end or leaves the field first, past any
// distance a ray goes, so a point at any distance lies within it and a reach short of it says a
// surface was met.
public static const float ReachOpen = 1e30;

// The reach a far probe keeps along every way of its reach map where it stands inside a mesh, the
// field's distance at its place below zero (reachTexel), so the merge and the gather pass it over
// by its reach alone (farProbeSees). Read for every ray at each of the eight far probes' places,
// twice where the merge tests for a split, the field's distance came to two in three of the
// merge's reads of the field.
public static const float ReachInside = -1.0;

// How far a ray from a probe's own place along way goes before it meets a surface, whether or not
// the surface lets light through, or ReachOpen where it meets none before end or before it leaves
// every cascade, the reach a far probe's map keeps for a texel's middle way.
public float reachAlong<F : IFieldVolume>(F field, float3 place, float3 way, float end)
{
    NoSteps none;
    return reachAlong(field, place, way, end, none);
}

// The same, each step handed to steps.
public float reachAlong<F : IFieldVolume, S : IMarchSteps>(F field, float3 place, float3 way, float end, inout S steps)
{
    float t = 0.0;
    [loop]
    while (t < end)
    {
        float3 p = place + way * t;
        int c = field.cascadeOf(p, 1.0);
        if (c < 0)
            return ReachOpen;
        float cell = field.cornerAndCell(c).w;
        float d = fieldDistanceSheetIn(field, c, p);
        steps.at(t, d);
        if (d < cell * MarchHit)
            return t;
        t += max(d, cell * MarchStep);
    }
    return ReachOpen;
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
