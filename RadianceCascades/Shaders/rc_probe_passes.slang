// The passes over a chain of cascades of probes, each function the work of one thread of an
// engine's compute kernel at the index it hands it, as splatCodes takes a lane, every read and
// write through an interface the engine implements over its own buffers, which keeps its bindings
// and its threads. A pass reads only what an earlier dispatch wrote, so the engine runs them as
// dispatches in order. Each level's texels are traced (traceTexel) and its reach map traced
// (reachTexel), then the coarsest level's texels are settled (settleTexel) and each finer level's
// merged with the merged light of the level above (mergeTexel), from the coarsest down, and last
// the light the finest level gives each surface point is gathered (gatherAt). An engine that holds
// the light of the field's cells (IHeldLight) holds a share of them (holdCellLight), or lists them
// (listHeldCell) and holds the listed ones (holdListedCell), before the merges, which read it at
// their hits (HeldHitLight) in place of its shading.
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

// The light an engine holds for each cell of the field that lies within a cell of a surface, the
// light that surface sends from each of its sides as the engine shades it (holdCellLight), its own,
// its emission and the light of its sun and its lamps, apart from the light of the engine's probes
// it reflects, which a merge reads at the cell a march met a surface in (HeldHitLight) in place of
// shading the surface there, and the engine implements over its own buffers or textures. A cell
// holds two sides (heldSide), 0 facing along its normal, its sheet's plane's where it keeps a sheet
// thinner than a cell and the field's at its middle otherwise, and 1 the other way, a sheet's far
// side or the far face of a solid thinner than a cell, and side 0's light again where no far face
// lies within a cell of its middle, since a ray from a thin wall's dark side reads its lit side's
// light where one light stands for both, and the foot of a thin room's wall lit by a lamp outside
// read 2.11 times its light so.
public interface IHeldLight
{
    // The light the side of the surface nearest the cell sends, its own.
    float3 heldAt(int cascade, int3 cell, uint side);

    // The light of the engine's probes the same side reflects, taken whole.
    float3 heldProbeLightAt(int cascade, int3 cell, uint side);

    // The share of the probes' light a hit at place in the cell takes, as the engine's own hit
    // weighs each probe's, 1 where it takes it whole, since a held light that takes it whole keeps
    // a light that went out lingering where the engine's gather lets it go within two frames.
    float probeShare(float3 place, int cascade, int3 cell);

    // Holds the side's own light and its probes' light for the cell.
    void hold(int cascade, int3 cell, uint side, float3 own, float3 probes);
}

// The light an engine shades a held cell's surface with (holdCellLight), its own and its probes'
// apart, at place toward -way, as IProbeField.hitLight gives their sum where the engine weighs the
// probes' by their share.
public interface IHeldShading
{
    // The surface's own light, its emission and the light of the engine's sun and lamps.
    float3 ownLight(float3 place, float3 way, int cascade, int3 cell);

    // The light of the engine's probes the surface reflects, taken whole.
    float3 probeLight(float3 place, float3 way, int cascade, int3 cell);
}

// An engine's shading held whole as a surface's own light (IHeldShading), for an engine whose hit
// weighs no probe's share.
public struct WholeShading<F : IProbeField> : IHeldShading
{
    // The field whose hitLight the engine shades with.
    public F field;
    public float3 ownLight(float3 place, float3 way, int cascade, int3 cell) { return field.hitLight(place, way, cascade, cell); }
    public float3 probeLight(float3 place, float3 way, int cascade, int3 cell) { return float3(0.0); }
}

// The side of the surface nearest a cell of the field that a ray along way meets (IHeldLight), 1
// where the way runs along the cell's normal, its sheet's plane's where it keeps a sheet and the
// field's at its middle otherwise, and 0 where it runs against it. A solid's is read as the field's
// distance growing along the way across half a cell either side of the middle, two reads of the
// field where its normal (fieldNormalIn) takes six, since the six took one engine's merge from 0.56
// to 0.77 ms.
public uint heldSide<F : IFieldVolume>(F field, int cascade, int3 cell, float3 way)
{
    float3 sheet = field.cellAt(cascade, cell).yzw;
    if (dot(sheet, sheet) >= 0.25)
        return dot(way, sheet) > 0.0 ? 1u : 0u;
    float4 oc = field.cornerAndCell(cascade);
    float3 middle = oc.xyz + (float3(cell) + 0.5) * oc.w;
    float3 half = way * (0.5 * oc.w);
    return fieldDistanceIn(field, cascade, middle + half) > fieldDistanceIn(field, cascade, middle - half) ? 1u : 0u;
}

// The field as a merge marches through it where the engine holds the light of the field's cells
// (IHeldLight): a surface a ray meets sends back the light held for the side it meets of the cell
// it met it in (heldSide), its own and the probes' weighed by the share the engine gives at the hit
// (probeShare), whatever the ray's way, every other read the field's own. The engine's shading at a
// hit, its sun map, its lamps and its probes read at the surface, is most of what a merge costs,
// 89 and 94 percent of it in two scenes on one engine, and a merge that takes nothing at a hit
// reads a room's deep end under a lintel 0.51 of the light traced there against 0.82, and a sunlit
// wall a little past the near rays' reach 44 percent under. Read so, the merge costs what it costs
// taking nothing there, and the sunlit wall reads 0.6 percent under what the merge reads shading
// its hits. A held light gives up the gloss toward the eye a hit's own shading weighs by the ray's
// way. A binding takes it where its lamps fail under a cheaper hit of its own (mergeTexel), held
// whole each frame (holdCellLight).
public struct HeldHitLight<F : IProbeField, H : IHeldLight> : IProbeField
{
    // The field as the engine gives it.
    public F field;
    // The light the engine holds for the field's cells.
    public H held;
    public int cascadeOf(float3 p, float margin) { return field.cascadeOf(p, margin); }
    public float4 cornerAndCell(int cascade) { return field.cornerAndCell(cascade); }
    public int size() { return field.size(); }
    public float4 between(int cascade, float3 cells) { return field.between(cascade, cells); }
    public float4 cellAt(int cascade, int3 cell) { return field.cellAt(cascade, cell); }
    public float3 secondAt(int cascade, int3 cell) { return field.secondAt(cascade, cell); }
    public float3 hitLight(float3 place, float3 way, int cascade, int3 cell)
    {
        uint side = heldSide(field, cascade, cell, way);
        return held.heldAt(cascade, cell, side) + held.heldProbeLightAt(cascade, cell, side) * held.probeShare(place, cascade, cell);
    }
    public float throughAt(int cascade, int3 cell) { return field.throughAt(cascade, cell); }
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
    NoSteps none;
    traceTexel(field, rays, places, cascade, none, index);
}

// traceTexel with each step of the trace handed to steps, as a binding counts what its trace
// costs.
public void traceTexel<F : IProbeField, R : IProbeRays, Q : IProbePlaces, S : IMarchSteps>(F field, R rays, Q places, ProbeCascade cascade,
    inout S steps, uint index)
{
    if (index >= cascadeTexels(cascade))
        return;
    uint texels = cascade.texels * cascade.texels;
    uint2 texel = uint2((index % texels) % cascade.texels, (index % texels) / cascade.texels);
    float3 place = places.placeOf(probeAt(cascade, index / texels));
    rays.keepRay(index, traceProbeRay(field, place, texelWay(cascade.texels, texel), cascade.start, cascade.end, cascade.stepCap, steps));
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
    int c = field.cascadeOf(place, 1.0);
    reach.keepReach(index, c >= 0 && fieldDistanceIn(field, c, place) < 0.0 ? ReachInside
        : reachAlong(field, place, texelWay(ReachTexels, texel), cascade.end));
}

// Holds for the cell of the field at the thread's place in one share of the field's cells the light
// the surface nearest it sends from each of its sides (IHeldLight, heldSide), its own and its
// probes' apart as the engine shades them (IHeldShading, WholeShading where its hit weighs no
// probe's share), where a march toward that surface stops, a tenth of a cell short of it, a thread
// a cell, a sheet's two sides and a solid's thinner than a cell shading twice where a thicker
// solid's side 0 shades once. The cells of the field's cascades are counted one after another, each
// x fastest, and the thread at index takes the cell at index times share plus turn modulo share. A
// share above 1, with turn counting the frames, holds every cell once in share frames and lags a
// change by as many, so a binding whose lighting is to show a change in the next frame dispatches
// it each frame over every cell, share 1. A cell whose middle lies farther than a cell and a tenth
// from a surface holds nothing, since a march stops within a tenth of a cell of a surface, short of
// it or past it by less than a step, in a cell whose middle lies within a cell and a tenth of the
// surface, and so does one that lies whole where a march reads a finer cascade (cascadeOf), so that
// no thread shades a cell no march meets a surface in.
public void holdCellLight<F : IFieldVolume, G : IHeldShading, H : IHeldLight>(F field, G shading, H held, uint cascades, uint share,
    uint turn, uint index)
{
    int c;
    int3 cell;
    float d;
    if (heldCellAt(field, cascades, index * max(share, 1u) + turn % max(share, 1u), c, cell, d))
        holdHeldCell(field, shading, held, c, cell, d);
}

// The list of the field's cells whose light an engine holds (holdListedCell), which listHeldCell
// fills a cell a thread, each cell counted as holdCellLight counts them, and which the engine keeps
// over its own buffer, with a count it adds to atomically. The count is set to 0 among the frame's
// commands before the listing, by a clear of the buffer or a dispatch of one thread, since a 0
// written from the host that reaches the GPU after the listing's submit leaves the count going on
// from the frame before. The hold is dispatched after the listing among the same commands, over
// every cell with its threads past the count returning, or by a dispatch the GPU sizes from the
// count's buffer, and not over a count read back to the host, which either waits on the listing or
// is the count of the frame before.
public interface IHeldList
{
    // Adds the cell at to the list.
    void list(uint at);

    // The count of cells the list holds.
    uint count();

    // The cell the list holds at index.
    uint listedAt(uint index);
}

// Lists the cell of the field at the thread's place in one share of the field's cells, counted
// and chosen as holdCellLight chooses them, where holdCellLight would hold its light, so that a
// dispatch of holdListedCell a thread a listed cell shades no more than holdCellLight does over
// every cell. A thread a cell over the whole field leaves most threads with no cell to shade, in
// one engine 25,600 cells held of about a million on Sponza, and a workgroup whose one thread
// shades takes as long as one whose every thread does, so that engine's hold took 2.72 ms on Sponza
// and 2.95 on the Bistro over every cell, and 1.60 and 1.71 listed first, the listing 0.048 and
// 0.064 more.
public void listHeldCell<F : IFieldVolume, L : IHeldList>(F field, L list, uint cascades, uint share, uint turn, uint index)
{
    uint at = index * max(share, 1u) + turn % max(share, 1u);
    int c;
    int3 cell;
    float d;
    if (heldCellAt(field, cascades, at, c, cell, d))
        list.list(at);
}

// Holds the light of the cell the list holds at the thread's index (listHeldCell), as
// holdCellLight holds it, a thread a listed cell, the threads past the list's count holding
// nothing, so that a dispatch over as many threads as the field has cells holds every listed cell.
public void holdListedCell<F : IFieldVolume, G : IHeldShading, H : IHeldLight, L : IHeldList>(F field, G shading, H held, L list,
    uint index)
{
    if (index >= list.count())
        return;
    int c;
    int3 cell;
    heldCellOf(field, list.listedAt(index), c, cell);
    float4 oc = field.cornerAndCell(c);
    holdHeldCell(field, shading, held, c, cell, fieldDistanceSheetIn(field, c, oc.xyz + float3(cell) * oc.w + 0.5 * oc.w));
}

// The cascade and the cell of the field at at, its cells counted as holdCellLight counts them.
void heldCellOf<F : IFieldVolume>(F field, uint at, out int c, out int3 cell)
{
    uint size = uint(field.size());
    uint each = size * size * size;
    c = int(at / each);
    uint i = at % each;
    cell = int3(int(i % size), int((i / size) % size), int(i / (size * size)));
}

// Whether holdCellLight holds the light of the cell of the field at at, its cascade, its cell and
// the field's distance at its middle where it does.
bool heldCellAt<F : IFieldVolume>(F field, uint cascades, uint at, out int c, out int3 cell, out float d)
{
    uint size = uint(field.size());
    d = 0.0;
    heldCellOf(field, at, c, cell);
    if (at >= size * size * size * cascades)
        return false;
    float4 oc = field.cornerAndCell(c);
    float3 low = oc.xyz + float3(cell) * oc.w;
    for (int f = 0; f < c; f++)
    {
        float4 of = field.cornerAndCell(f);
        float3 a = (low - of.xyz) / of.w, b = (low + oc.w - of.xyz) / of.w;
        if (all(a >= 1.0) && all(b <= float(size) - 1.0))
            return false;
    }
    d = fieldDistanceSheetIn(field, c, low + 0.5 * oc.w);
    return abs(d) < oc.w * (1.0 + MarchHit);
}

// Holds the light of a cell holdCellLight holds, the field's distance at its middle d.
void holdHeldCell<F : IFieldVolume, G : IHeldShading, H : IHeldLight>(F field, G shading, H held, int c, int3 cell, float d)
{
    float4 oc = field.cornerAndCell(c);
    float3 middle = oc.xyz + float3(cell) * oc.w + 0.5 * oc.w;
    // A sheet's sides are shaded where a march toward its plane along its normal from either side
    // stops, SheetHalf and a tenth of a cell off the plane, a solid's side 0 where a march toward
    // it along the field's normal from the cell's middle stops, and its side 1 where a march from
    // beyond its far face stops, the face found within 1.2 cells of the middle against the normal,
    // the two sides one after another at one place of each shading.
    float3 sheet = field.cellAt(c, cell).yzw;
    float kept = length(sheet);
    float3 fieldNormal = fieldNormalIn(field, c, middle);
    float far = -1.0;
    if (kept < 0.5)
    {
        bool inside = false;
        [loop]
        for (uint k = 1u; k <= 8u; k++)
        {
            float along = float(k) * 0.15 * oc.w;
            float there = fieldDistanceSheetIn(field, c, middle - fieldNormal * along);
            if (there < 0.0)
                inside = true;
            else if (inside)
            {
                far = along - there;
                break;
            }
        }
    }
    float3 own = float3(0.0), probes = float3(0.0);
    [loop]
    for (uint side = 0u; side < 2u; side++)
    {
        float3 normal = fieldNormal;
        float3 place = middle - normal * (d - oc.w * MarchHit);
        if (kept >= 0.5)
        {
            normal = sheet / kept * (side == 0u ? 1.0 : -1.0);
            place = middle + sheet / kept * ((kept - SheetLength) * oc.w) + normal * ((SheetHalf + MarchHit) * oc.w);
        }
        else if (side == 1u)
        {
            // A solid with no far face within the cell's reach holds side 0's light again.
            if (far < 0.0)
            {
                held.hold(c, cell, 1u, own, probes);
                break;
            }
            normal = -fieldNormal;
            place = middle - fieldNormal * (far + oc.w * MarchHit);
        }
        own = shading.ownLight(place, -normal, c, cell);
        probes = shading.probeLight(place, -normal, c, cell);
        held.hold(c, cell, side, own, probes);
    }
}

// Keeps the light of the coarsest level's texel at index as merged, its ray's alone, the light of
// the surface it met or the sky's along its way past the field (lastCascadeLight), since no level
// above holds more.
public void settleTexel<R : IProbeRays, M : IMergedLight, K : ISky>(R rays, M merged, ProbeCascade cascade, K sky, uint index)
{
    NoSkyMerged<M> kept = { merged };
    WholeShare<K> whole = { sky };
    settleTexel(rays, kept, cascade, whole, index);
}

// settleTexel keeping beside the light the share of the sky in it, as the sky lends it
// (lastCascadeSkyShare).
public void settleTexel<R : IProbeRays, M : IMergedSkyLight, K : ISkyShare>(R rays, M merged, ProbeCascade cascade, K sky, uint index)
{
    if (index >= cascadeTexels(cascade))
        return;
    uint texels = cascade.texels * cascade.texels;
    uint2 texel = uint2((index % texels) % cascade.texels, (index % texels) / cascade.texels);
    ProbeRay ray = rays.rayAt(index);
    float3 way = texelWay(cascade.texels, texel);
    merged.keepMerged(index, lastCascadeLight(ray, way, sky));
    merged.keepSkyShare(index, lastCascadeSkyShare(ray, way, sky));
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
    WholeShare<K> whole = { sky };
    return mergeTexel(field, rays, places, lights, abovePlaces, kept, cascade, aboveCascade, marchEnd, whole, steps, index);
}

// mergeTexel keeping beside the light the share of the sky in it, above's shares read through
// IFarSkyProbes (mergedLight).
public bool mergeTexel<F : IProbeField, R : IProbeRays, Q : IProbePlaces, P : IFarSkyProbes, U : IProbePlaces, M : IMergedSkyLight, K : ISkyShare,
    S : IMarchSteps>(F field, R rays, Q places, P above, U abovePlaces, M merged, ProbeCascade cascade, ProbeCascade aboveCascade,
    float marchEnd, K sky, inout S steps, uint index)
{
    MergeAtRayEnd merge;
    NoStageValues none;
    return mergeTexel(merge, none, field, rays, places, above, abovePlaces, merged, cascade, aboveCascade, marchEnd, sky, steps, index);
}

// mergeTexel by the merge a binding names (IMerge), in place of the default the forms without one
// take, MergeAtRayEnd, with the values the per-probe stage kept for cascade's probes where the
// merge reads them (IMerge.readsStage), and NoStageValues where it reads none.
public bool mergeTexel<G : IMerge, V : IStageValues, F : IProbeField, R : IProbeRays, Q : IProbePlaces, P : IFarProbes, U : IProbePlaces,
    M : IMergedLight, K : ISky, S : IMarchSteps>(G merge, V stage, F field, R rays, Q places, P above, U abovePlaces, M merged,
    ProbeCascade cascade, ProbeCascade aboveCascade, float marchEnd, K sky, inout S steps, uint index)
{
    NoSkyShares<P> lights = { above };
    NoSkyMerged<M> kept = { merged };
    WholeShare<K> whole = { sky };
    return mergeTexel(merge, stage, field, rays, places, lights, abovePlaces, kept, cascade, aboveCascade, marchEnd, whole, steps, index);
}

// mergeTexel by the merge a binding names, keeping beside the light the share of the sky in it.
public bool mergeTexel<G : IMerge, V : IStageValues, F : IProbeField, R : IProbeRays, Q : IProbePlaces, P : IFarSkyProbes,
    U : IProbePlaces, M : IMergedSkyLight, K : ISkyShare, S : IMarchSteps>(G merge, V stage, F field, R rays, Q places, P above,
    U abovePlaces, M merged, ProbeCascade cascade, ProbeCascade aboveCascade, float marchEnd, K sky, inout S steps, uint index)
{
    if (index >= cascadeTexels(cascade))
        return false;
    uint texels = cascade.texels * cascade.texels;
    uint2 texel = uint2((index % texels) % cascade.texels, (index % texels) / cascade.texels);
    int3 probe = probeAt(cascade, index / texels);
    float3 place = places.placeOf(probe);
    // Set before the call, since slangc does not see through a merge taken by its shape (IMerge)
    // that it writes them, and refuses their reading as of a variable never set.
    bool marched = false;
    float skyShare = 0.0;
    float3 light = merge.light(field, above, abovePlaces, aboveCascade, marchEnd, place, cascade.texels, texel, rays.rayAt(index), sky, steps,
        stage, probe, marched, skyShare);
    merged.keepMerged(index, light);
    merged.keepSkyShare(index, skyShare);
    return marched;
}

// A value worked out once for each probe of a cascade from its place and the far cascade above it,
// which the per-probe stage (stageProbe) keeps for the cascade's merge to read (IStageValues),
// where every texel of the probe would otherwise work it out again.
public interface IProbeStage
{
    uint valueOf<F : IProbeField, Q : IProbePlaces, S : IMarchSteps>(F field, Q farPlaces, ProbeCascade far, float3 place, inout S steps);
}

// Keeps the value the per-probe stage worked out for the cascade's probe at index, in the order
// probeAt reads the cascade's probes, one uint a probe, which IStageValues reads back.
public interface IStageKeep
{
    void keepStage(uint index, uint value);
}

// The per-probe stage, a thread a probe of cascade, works out stage's value for the probe at index
// from its place (places) and far's probes (farPlaces) and keeps it, each step of a march it makes
// handed to steps. A binding runs it before cascade's merge where the merge reads it
// (IMerge.readsStage), in a pass of its own over cascadeProbes threads, since a value shared by a
// probe's texels is not held in groupshared memory, whose threads a dispatch does not group by
// probe at every count of texels.
public void stageProbe<T : IProbeStage, F : IProbeField, Q : IProbePlaces, U : IProbePlaces, W : IStageKeep, S : IMarchSteps>(T stage,
    F field, Q places, U farPlaces, ProbeCascade cascade, ProbeCascade far, W kept, inout S steps, uint index)
{
    if (index >= cascadeProbes(cascade))
        return;
    kept.keepStage(index, stage.valueOf(field, farPlaces, far, places.placeOf(probeAt(cascade, index)), steps));
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
    float3 sum;
    float skyShares;
    float weights = gatheredSums(probes, places, cascade, surfaces.placeAt(index), surfaces.normalAt(index), sum, skyShares);
    surfaces.keepTaken(index, weights > 0.0 ? sum / weights : float3(0.0));
    surfaces.keepSkyShare(index, weights > 0.0 ? skyShares / weights : 0.0);
}

// The sums gatherAt takes over cascade's probes about the point at p facing n, their light and
// their shares of the sky each times its weight, returning the weights' sum, 0 where no probe
// counts.
float gatheredSums<P : IFarSkyProbes, Q : IProbePlaces>(P probes, Q places, ProbeCascade cascade, float3 p, float3 n, out float3 sum,
    out float skyShares)
{
    int3 base;
    float3 f;
    farProbesAbout(cascade, p, base, f);
    sum = float3(0.0);
    skyShares = 0.0;
    float weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 probe = base + offset;
        if (any(probe < 0) || any(probe >= cascade.count))
            continue;
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z;
        float3 place = places.placeOf(probe);
        if (weight <= 0.0 || !farProbeSees(probes, probe, place, p, SeeingSlack * cascade.spacing))
            continue;
        ProbeTexels<P> texels;
        texels.probes = probes;
        texels.probe = probe;
        sum += lightTaken(texels, cascade.texels, n) * weight;
        skyShares += skyShareTaken(probes, probe, cascade.texels, n) * weight;
        weights += weight;
    }
    return weights;
}

// How far past a level's edge probes, in its spacing, a surface point is still gathered from that
// level (gatheredLevel). A wall a quarter of a unit past the finest level's last probes would
// otherwise be gathered from the level above, whose probes stand a spacing off it and see what the
// wall does not, as a court lit in a strip reads its high wall 2.367 of its trace without the rim
// and 1.240 with it. Half a spacing is the level's own cells, its edge probes the nearest to any
// point within it.
public static const float GatherRim = 0.5;

// The level a surface point at p is gathered from, the finest whose probes stand about it on every
// side or within rim of its spacing past its edge probes, and the coarsest where none does.
public int gatheredLevel<L : IGatherLevels>(L levels, float3 p, float rim)
{
    int count = levels.levels();
    for (int level = 0; level < count - 1; level++)
    {
        ProbeCascade cascade = levels.cascadeOf(level);
        float3 q = (p - cascade.corner) / cascade.spacing;
        if (all(q >= -rim) && all(q <= float3(cascade.count - 1) + rim))
            return level;
    }
    return count - 1;
}

// Gathers the light the surface point at index takes from the level gatheredLevel picks for it at
// GatherRim, by gatherAt's weights, and keeps it, a thread a point. Where no probe of that level
// counts, each standing inside a mesh or not seeing the point by its reach, the point takes the
// level above's, whose probes stand twice as far apart and so hold a crevice twice as wide. It goes
// no further, since a level further up holds a crevice narrower than its cells only by probes
// over it, in the open, whose texels' ways pass the sight check by their slack, an eighth of their
// spacing, and lend the crevice the open sky, so a point the level above sees by none of its probes
// stays dark. A binding gathers each pixel by it, handing its levels, its field and
// its points, so each engine takes one rule for which probes light a pixel.
public void gatherSurface<F : IProbeField, L : IGatherLevels, A : ISurfacePoints>(F field, L levels, A surfaces, uint index)
{
    NoSkyLevels<L> sky = { levels };
    NoSkyPoints<A> points = { surfaces };
    gatherSurface(field, sky, points, index);
}

// gatherSurface keeping beside the point's light the share of the sky in it.
public void gatherSurface<F : IProbeField, L : IGatherSkyLevels, A : ISurfaceSkyPoints>(F field, L levels, A surfaces, uint index)
{
    float3 p = surfaces.placeAt(index);
    float3 n = surfaces.normalAt(index);
    float3 sum = float3(0.0);
    float skyShares = 0.0, weights = 0.0;
    int first = gatheredLevel(levels, p, GatherRim);
    for (int level = first; level <= min(first + 1, levels.levels() - 1) && weights <= 0.0; level++)
    {
        GatherSkyLevelProbes<L> probes = { levels, level };
        weights = gatheredSums(probes, probes, levels.cascadeOf(level), p, n, sum, skyShares);
    }
    surfaces.keepTaken(index, weights > 0.0 ? sum / weights : float3(0.0));
    surfaces.keepSkyShare(index, weights > 0.0 ? skyShares / weights : 0.0);
}

// Levels whose texels' share of the sky is let go, which the gather that keeps no share reads
// them through.
struct NoSkyLevels<L : IGatherLevels> : IGatherSkyLevels
{
    L inner;
    int levels() { return inner.levels(); }
    ProbeCascade cascadeOf(int level) { return inner.cascadeOf(level); }
    float3 lightAt(int level, int3 probe, uint2 texel) { return inner.lightAt(level, probe, texel); }
    float reachAt(int level, int3 probe, uint2 texel) { return inner.reachAt(level, probe, texel); }
    float skyShareAt(int level, int3 probe, uint2 texel) { return 0.0; }
    float3 placeOf(int level, int3 probe) { return inner.placeOf(level, probe); }
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
    WholeShare<K> whole = { sky };
    gatherTraced(field, lights, places, cascade, marchEnd, whole, points, steps, index);
}

// gatherTraced keeping beside the point's light the share of the sky in it, each ray's share
// (mergedLight) weighed as its light is, over the weights.
public void gatherTraced<F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISkyShare, A : ISurfaceSkyPoints, S : IMarchSteps>(F field,
    P probes, Q places, ProbeCascade cascade, float marchEnd, K sky, A surfaces, inout S steps, uint index)
{
    MergeAtRayEnd merge;
    gatherTraced(merge, field, probes, places, cascade, marchEnd, sky, surfaces, steps, index);
}

// gatherTraced by the merge a binding names (IMerge), in place of the default the forms without one
// take, MergeAtRayEnd.
public void gatherTraced<G : IMerge, F : IProbeField, P : IFarProbes, Q : IProbePlaces, K : ISky, A : ISurfacePoints, S : IMarchSteps>(G merge,
    F field, P probes, Q places, ProbeCascade cascade, float marchEnd, K sky, A surfaces, inout S steps, uint index)
{
    NoSkyShares<P> lights = { probes };
    NoSkyPoints<A> points = { surfaces };
    WholeShare<K> whole = { sky };
    gatherTraced(merge, field, lights, places, cascade, marchEnd, whole, points, steps, index);
}

// gatherTraced by the merge a binding names, keeping beside the point's light the share of the sky
// in it.
public void gatherTraced<G : IMerge, F : IProbeField, P : IFarSkyProbes, Q : IProbePlaces, K : ISkyShare, A : ISurfaceSkyPoints,
    S : IMarchSteps>(G merge, F field, P probes, Q places, ProbeCascade cascade, float marchEnd, K sky, A surfaces, inout S steps, uint index)
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
    // A surface point is no probe's place, so a merge that reads the per-probe stage takes none
    // here.
    NoStageValues none;
    [loop]
    for (uint k = 0u; k < cascade.texels * cascade.texels; k++)
    {
        uint2 texel = uint2(k % cascade.texels, k / cascade.texels);
        float facing = dot(n, texelWay(cascade.texels, texel));
        if (facing <= 0.0)
            continue;
        float weight = facing * texelSolidAngle(cascade.texels, texel, shares);
        bool marched = false;
        float skyShare = 0.0;
        taken += merge.light(field, probes, places, cascade, marchEnd, origin, cascade.texels, texel, begun, sky, steps, none, int3(-1), marched,
            skyShare) * weight;
        skyShares += skyShare * weight;
        weights += weight;
    }
    surfaces.keepTaken(index, weights > 0.0 ? taken * (3.14159265 / weights) : float3(0.0));
    surfaces.keepSkyShare(index, weights > 0.0 ? skyShares / weights : 0.0);
}
