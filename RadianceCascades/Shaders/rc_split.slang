// What an engine hands the passes of split radiance cascades, the passes that keep their store's
// probes, and the gather of a seen point's light from them. The split cascades keep probes only
// near the surfaces the eye sees. Each seen point of a frame inserts the nearest probe of its level
// into a store keyed by place and level, and each probe inserts the nearest probe of the cascade
// above, so the store holds probes where light is read and none in the air of a narrow room or
// inside its walls. A probe stays in the store, its place and its light kept, while the eye moves,
// until no seen point has asked for it for some frames, or until the eye leaves it outside its
// level's reach. A ray is traced from each seen point, never from a probe, and its parts are split
// by how far along it they lie among the cascades' intervals. Each part's light, the share of light
// it lets through and a count are added to the nearest probe of that cascade and to its nearest
// way, and the store keeps each probe's light as their mean by count. A point's level is the
// base-two logarithm of its greatest distance along an axis from the eye, so the passes read the
// eye as a member of the hand, and the ways a point's rays take turn from frame to frame by the
// frame's count, so a probe's ways over frames cover what one frame's few rays cannot.
//
// An engine gives the seen points through IVisiblePoints, a part of a ray's trace through
// IRayHits, which FieldHits gives from the field's march, the store's memory through IStoreUints
// and IStoreLight, and each frame's eye and the store's shape by value, as it hands a
// ProbeCascade. The store's uints take only 32-bit atomics on a buffer, an add and a compare and
// exchange, which every target the module compiles to has, and a probe's light is added to them as
// a whole number, since an atomic add of a float is not on every target.
module rc_split;

__exported import rc_probe_passes;

// What the split cascades' passes read of a frame, which the engine fills each frame.
public struct SplitFrame
{
    // The eye's place in the world, which a seen point's level is read from.
    public float3 eye;

    // The frame's count from the engine's start, which the ways of a seen point's rays turn by.
    public uint frame;
}

// The store's shape, which the engine sets once and lays out its uints and its light by.
public struct SplitStore
{
    // The most probes the store holds, a power of two.
    public uint slots;

    // The spacing of the finest level's probes, in the engine's world unit.
    public float spacing;

    // The count of levels, each spaced twice the one below and reaching twice as far from the eye.
    public uint levels;

    // The count of cascades above a level's probes, each with four times the ways of the one
    // below and an interval four times as long, at most 4.
    public uint cascades;

    // How far the finest level reaches from the eye along each axis, past which a point is the
    // next level's, each level reaching twice as far as the one below.
    public float reach;

    // How many frames a probe no seen point asks for is kept, its light with it, before its entry
    // is freed for another.
    public uint age;
}

// The points of the surfaces the eye sees in a frame, a pixel's place read from its depth and its
// normal from the engine's normals, as the gather reads them (ISurfacePoints), and whether the
// pixel sees a surface at all.
public interface IVisiblePoints : ISurfacePoints
{
    // Whether the pixel at index sees a surface, false where its depth is the far plane's and it
    // sees the sky, so its point inserts no probe and traces no ray.
    bool seenAt(uint index);
}

// A part of a ray from a seen point, where it meets a surface and the light that surface sends
// back along it, which the field's march gives (FieldHits) and an engine that traces rays its own
// way gives as it finds them. The passes trace a ray one cascade's interval at a time and hand the
// next part on from where the last ended, so a part that meets nothing ends as RayPast at end and
// one that leaves the world as RayLeft, the sky's light then read through ISky.
public interface IRayHits
{
    // The part of the ray from origin along way, of unit length, between start and end, how it
    // ended, where along it, the share of light carried through it and the light it met.
    ProbeRay along(float3 origin, float3 way, float start, float end);
}

// The parts of a ray the field's march gives (traceProbeRay), the light of a surface it meets as
// the engine shades it (IProbeField.hitLight), each taking at most cap steps, 0 for no count.
public struct FieldHits<F : IProbeField> : IRayHits
{
    public F field;
    public uint cap;

    public ProbeRay along(float3 origin, float3 way, float start, float end)
    {
        NoSteps steps;
        return traceProbeRay(field, origin, way, start, end, cap, steps);
    }
}

// The store's uints, which an engine keeps in a buffer of its own and changes atomically. The
// passes lay out in them the table from each probe's key to its entry, each entry's key, the
// frames it was taken and last asked for and the shares of the probes above it that it sees, each
// cascade's free entries, the sums of light, of the
// share let through and of the count its rays add, and the count of probes standing in each
// cascade, which an engine may read as a dispatch's argument.
public interface IStoreUints
{
    // The uint at index.
    uint load(uint index);

    // Writes value to the uint at index.
    void store(uint index, uint value);

    // Adds value to the uint at index and gives what it held before, an atomic add.
    uint add(uint index, uint value);

    // Writes desired to the uint at index where it holds expected and gives what it held before,
    // an atomic compare and exchange, by which a probe's key takes a slot.
    uint compareExchange(uint index, uint expected, uint desired);
}

// The store's light, which an engine keeps in a buffer of float4 of its own. The passes lay out in
// it each probe's light along each of its ways, the share let through in w, kept and blended from
// frame to frame, and beside it the light merged from the cascades above, which the gather reads.
public interface IStoreLight
{
    // The light at index.
    float4 lightAt(uint index);

    // Keeps light at index.
    void keepLight(uint index, float4 light);
}

// The share of its reach a level's start is shortened to from the reach of the level below, so a
// point over the last tenth of a level's reach is gathered from it and from the next, blended
// from the one to the other across the overlap, and its light does not step where the eye moves
// it from one level to the next.
public static const float SplitLevelStart = 0.9;

// The ways of a probe of the finest cascade, each cascade above having four times as many.
public static const uint SplitBaseWays = 32u;

// The entry of a table's slot whose key no entry of the pools holds, where the pool was full or
// the thread that took the slot this frame has yet to give it one.
public static const uint SplitNone = 0xFFFFFFFFu;

// The uints ahead of the table, four for each cascade, its free entries' count, the count of those
// taken this frame and the count of its probes standing, and the fourth unused.
public static const uint SplitCounts = 16u;

// How many slots of the table a key's search passes along from its own before it gives up, which
// at a table four times the finest pool's slots is past any run a key meets but one built to meet
// it.
static const uint SplitSearch = 32u;

// The entries of cascade's pool, the store's slots for the finest and a quarter as many for each
// above, since a probe of the cascade above stands for the four below it along a surface.
public uint splitPoolProbes(SplitStore shape, uint cascade)
{
    return shape.slots >> (2u * cascade);
}

// The first entry of cascade's pool, the pools laid out one after another from the finest.
public uint splitPoolFirst(SplitStore shape, uint cascade)
{
    uint first = 0u;
    for (uint c = 0u; c < cascade; c++)
        first += splitPoolProbes(shape, c);
    return first;
}

// The entries of every pool.
public uint splitEntries(SplitStore shape)
{
    return splitPoolFirst(shape, shape.cascades);
}

// The cascade whose pool holds entry.
public uint splitCascadeOf(SplitStore shape, uint entry)
{
    uint cascade = 0u;
    while (cascade + 1u < shape.cascades && entry >= splitPoolFirst(shape, cascade + 1u))
        cascade++;
    return cascade;
}

// The slots of the table, four for each of the finest pool's entries, so the pools fill at most a
// third of them and a key's search meets short runs.
public uint splitTableSlots(SplitStore shape)
{
    return 4u * shape.slots;
}

// The first uint of the table, each slot two, its key and its entry.
public uint splitTableAt(SplitStore shape)
{
    return SplitCounts;
}

// The first uint of the pools, each entry four, its key, the frame it was last asked for, the
// frame it was taken, the light it keeps from the probe that held it before read as none, and the
// shares of the probes above about it that it sees (splitAboveSights).
public uint splitPoolAt(SplitStore shape)
{
    return splitTableAt(shape) + 2u * splitTableSlots(shape);
}

// The first uint of the free lists, each cascade's free entries from its pool's first.
public uint splitFreeAt(SplitStore shape)
{
    return splitPoolAt(shape) + 4u * splitEntries(shape);
}

// The first uint of the sums the seen points' rays add to each probe's ways, five a way laid out
// as the probes' own light is (splitLightAt), the light along it in red, green and blue, the share
// let through and the count of rays, each light and share a whole number of SplitSumScale. The
// count keeps the frame's rays in its low SplitCountBits and above them the rays the way's kept
// light is the mean of.
public uint splitSumsAt(SplitStore shape)
{
    return splitFreeAt(shape) + splitEntries(shape);
}

// The uints the store lays out.
public uint splitUints(SplitStore shape)
{
    return splitSumsAt(shape) + 5u * shape.cascades * SplitBaseWays * shape.slots;
}

// The ways of a probe of cascade.
public uint splitWays(uint cascade)
{
    return SplitBaseWays << (2u * cascade);
}

// Where the light of entry's probe along its way lies in the store's light, its own as its rays
// keep it or the light merged from the cascades above. Each cascade's pool keeps a quarter as many
// probes as the one below with four times the ways, so each cascade's light takes the same span,
// the base ways times the slots, the probes' own first and the merged after them.
public uint splitLightAt(SplitStore shape, uint entry, uint way, bool merged)
{
    uint cascade = splitCascadeOf(shape, entry);
    uint span = SplitBaseWays * shape.slots;
    return (merged ? shape.cascades : 0u) * span + cascade * span
        + (entry - splitPoolFirst(shape, cascade)) * splitWays(cascade) + way;
}

// The float4 of light the store lays out.
public uint splitLights(SplitStore shape)
{
    return 2u * shape.cascades * SplitBaseWays * shape.slots;
}

// The spacing of the probes of level's cascade, the finest level's spacing twice over for each
// level and each cascade above.
public float splitSpacing(SplitStore shape, uint level, uint cascade)
{
    return shape.spacing * exp2(float(level + cascade));
}

// The cell of the probe of level's cascade nearest p, the probes standing at whole multiples of
// their spacing.
public int3 splitNearestCell(SplitStore shape, uint level, uint cascade, float3 p)
{
    return int3(floor(p / splitSpacing(shape, level, cascade) + 0.5));
}

// The key of the probe at cell of level's cascade, the level one up in the top three bits so no
// key is 0, which marks a slot empty, the cascade in the next two, and the low nine bits of the
// cell along each axis, which the probes of a level within its reach of the eye tell apart.
public uint splitKey(uint level, uint cascade, int3 cell)
{
    uint3 low = uint3(cell) & 511u;
    return ((level + 1u) << 29) | (cascade << 27) | (low.x << 18) | (low.y << 9) | low.z;
}

// The level of the probe of key.
public uint splitLevelOf(uint key)
{
    return (key >> 29) - 1u;
}

// The cascade of the probe of key.
public uint splitCascadeOfKey(uint key)
{
    return (key >> 27) & 3u;
}

// The cell of the probe of key, the one whose low nine bits along each axis are the key's within
// 256 cells of the eye's, which every probe the store keeps is.
public int3 splitCellOf(SplitFrame frame, SplitStore shape, uint key)
{
    int3 eye = splitNearestCell(shape, splitLevelOf(key), splitCascadeOfKey(key), frame.eye);
    int3 low = int3((key >> 18) & 511u, (key >> 9) & 511u, key & 511u);
    int3 off = (low - eye) & 511;
    return eye + off - select(off >= 256, int3(512), int3(0));
}

// The level a point at p is gathered from and inserts its probe into, the finest whose reach holds
// its greatest distance along an axis from the eye, which is the base-two logarithm of that
// distance over the finest level's reach, and the coarsest past every reach, with the share of
// the next level it takes in next, rising from 0 to 1 over the last tenth of the level's reach.
public uint splitLevelAt(SplitFrame frame, SplitStore shape, float3 p, out float next)
{
    float3 d3 = abs(p - frame.eye);
    float d = max(d3.x, max(d3.y, d3.z));
    float reach = shape.reach;
    uint level = 0u;
    while (d >= reach && level + 1u < shape.levels)
    {
        reach *= 2.0;
        level++;
    }
    next = level + 1u < shape.levels ? saturate((d / reach - SplitLevelStart) / (1.0 - SplitLevelStart)) : 0.0;
    return level;
}

// Whether the store holds a probe for the point at p, every point within the coarsest level's
// reach and those past it within 254 of that level's spacings of the eye along each axis, past
// which a probe's key would read back as another cell. A point it does not hold asks
// for no probe, sends no ray and gathers no light, and the engine gives its pixel the far light it
// gives the sky, or raises the store's levels or spacing so the coarsest level holds its scene.
public bool splitHolds(SplitFrame frame, SplitStore shape, float3 p)
{
    float3 d3 = abs(p - frame.eye);
    return max(d3.x, max(d3.y, d3.z)) <= 254.0 * splitSpacing(shape, shape.levels - 1u, 0u);
}

// Whether the probe of key stands within its level's reach of the eye along each axis and a
// spacing of its cascade past it, or within 255 cells of the eye at the coarsest level, which
// reaches every point past the others, so its cell is read back from its key.
bool splitStandsNear(SplitFrame frame, SplitStore shape, uint key)
{
    uint level = splitLevelOf(key), cascade = splitCascadeOfKey(key);
    float spacing = splitSpacing(shape, level, cascade);
    float3 d3 = abs(float3(splitCellOf(frame, shape, key)) * spacing - frame.eye);
    float d = max(d3.x, max(d3.y, d3.z));
    return level + 1u < shape.levels ? d <= shape.reach * exp2(float(level)) + spacing : d <= 255.0 * spacing;
}

// A uint's bits mixed so that neighboring uints give unrelated ones, by which a key finds its slot
// and a point's rays their offset.
uint splitMix(uint h)
{
    h ^= h >> 16;
    h *= 0x7feb352du;
    h ^= h >> 15;
    h *= 0x846ca68bu;
    h ^= h >> 16;
    return h;
}

// The slot of the table a key's search passes at its kth step.
uint splitSlotAt(SplitStore shape, uint key, uint k)
{
    return splitTableAt(shape) + 2u * ((splitMix(key) + k) & (splitTableSlots(shape) - 1u));
}

// The entry of the probe of key, or SplitNone where the store holds none this frame.
public uint findSplitProbe<W : IStoreUints>(W uints, SplitStore shape, uint key)
{
    for (uint k = 0u; k < SplitSearch; k++)
    {
        uint slot = splitSlotAt(shape, key, k);
        uint held = uints.load(slot);
        if (held == key)
            return uints.load(slot + 1u);
        if (held == 0u)
            return SplitNone;
    }
    return SplitNone;
}

// Asks for the probe of key in a frame. Where the store holds it, it is marked asked for in the
// frame, and where it does not, the thread that takes its key's slot of the table takes an entry
// of its cascade's pool for it, the light that entry keeps read as none from the frame it is
// taken. Gives its entry, or SplitNone where the table or the pool is full or another thread took
// its slot this frame and has yet to give it one, which marks it asked for itself.
public uint askSplitProbe<W : IStoreUints>(W uints, SplitFrame frame, SplitStore shape, uint key)
{
    for (uint k = 0u; k < SplitSearch; k++)
    {
        uint slot = splitSlotAt(shape, key, k);
        uint held = uints.compareExchange(slot, 0u, key);
        if (held == 0u)
        {
            uint cascade = splitCascadeOfKey(key);
            uint taken = uints.add(4u * cascade + 1u, 1u);
            if (taken >= uints.load(4u * cascade))
                return SplitNone;
            uint entry = uints.load(splitFreeAt(shape) + splitPoolFirst(shape, cascade) + taken);
            uint at = splitPoolAt(shape) + 4u * entry;
            uints.store(at, key);
            uints.store(at + 1u, frame.frame);
            uints.store(at + 2u, frame.frame);
            uints.add(4u * cascade + 2u, 1u);
            uints.store(slot + 1u, entry);
            return entry;
        }
        if (held == key)
        {
            uint entry = uints.load(slot + 1u);
            if (entry != SplitNone)
                uints.store(splitPoolAt(shape) + 4u * entry + 1u, frame.frame);
            return entry;
        }
    }
    return SplitNone;
}

// Empties the table and the counts at a frame's start, a thread a slot of the table, ahead of
// keepSplitProbes.
public void clearSplitTable<W : IStoreUints>(W uints, SplitStore shape, uint index)
{
    if (index < SplitCounts)
        uints.store(index, 0u);
    if (index >= splitTableSlots(shape))
        return;
    uints.store(splitTableAt(shape) + 2u * index, 0u);
    uints.store(splitTableAt(shape) + 2u * index + 1u, SplitNone);
}

// Keeps in the table the probe of entry where a seen point asked for it within the store's age
// and it stands within its level's reach of the eye, a thread an entry of every pool after
// clearSplitTable, and frees its entry onto its cascade's free list where it does not, or where
// the table holds its key already or is full about it. A pool cleared to 0 frees every entry.
public void keepSplitProbes<W : IStoreUints>(W uints, SplitFrame frame, SplitStore shape, uint entry)
{
    if (entry >= splitEntries(shape))
        return;
    uint cascade = splitCascadeOf(shape, entry);
    uint at = splitPoolAt(shape) + 4u * entry;
    uint key = uints.load(at);
    if (key != 0u && frame.frame - uints.load(at + 1u) <= shape.age && splitStandsNear(frame, shape, key))
    {
        for (uint k = 0u; k < SplitSearch; k++)
        {
            uint slot = splitSlotAt(shape, key, k);
            uint held = uints.compareExchange(slot, 0u, key);
            if (held == 0u)
            {
                uints.store(slot + 1u, entry);
                uints.add(4u * cascade + 2u, 1u);
                return;
            }
            if (held == key)
                break;
        }
    }
    uints.store(at, 0u);
    uint free = uints.add(4u * cascade, 1u);
    uints.store(splitFreeAt(shape) + splitPoolFirst(shape, cascade) + free, entry);
}

// Asks for the probe of its level the point at index adds its rays to (splitSeenCell) and, over
// the overlap, that of the next, a thread a point after keepSplitProbes, for a point the eye sees.
public void askSeenProbes<P : IVisiblePoints, H : IRayHits, W : IStoreUints>(P points, H hits, W uints, SplitFrame frame, SplitStore shape,
    uint index)
{
    if (!points.seenAt(index) || !splitHolds(frame, shape, points.placeAt(index)))
        return;
    float3 p = points.placeAt(index);
    float3 origin = splitRayOrigin(frame, shape, p, points.normalAt(index));
    float next;
    uint level = splitLevelAt(frame, shape, p, next);
    askSplitProbe(uints, frame, shape, splitKey(level, 0u, splitSeenCell(hits, shape, level, p, origin)));
    if (next > 0.0)
        askSplitProbe(uints, frame, shape, splitKey(level + 1u, 0u, splitSeenCell(hits, shape, level + 1u, p, origin)));
}

// Asks for the probe of the cascade above nearest each probe of cascade asked for in the frame, a
// thread an entry of cascade's pool, after askSeenProbes for the finest cascade and after this
// pass of the cascade below for each above, so each probe asked for has its nearest probe above
// asked for in turn, and keeps in its entry the shares of the probes above about it that it sees
// (splitAboveSights), which the merge weighs them by. A probe of the cascade above stands at every
// other probe's place along each axis, the nearest of a probe between two the one past it. A
// binding hands it the hits its rays are traced by (IRayHits), the same as traceSeenRays takes.
public void askCascadeProbes<H : IRayHits, W : IStoreUints>(H hits, W uints, SplitFrame frame, SplitStore shape, uint cascade,
    uint index)
{
    if (cascade + 1u >= shape.cascades || index >= splitPoolProbes(shape, cascade))
        return;
    uint at = splitPoolAt(shape) + 4u * (splitPoolFirst(shape, cascade) + index);
    uint key = uints.load(at);
    if (key == 0u || uints.load(at + 1u) != frame.frame)
        return;
    int3 cell = splitCellOf(frame, shape, key);
    askSplitProbe(uints, frame, shape, splitKey(splitLevelOf(key), cascade + 1u, (cell + 1) >> 1));
    uints.store(at + 3u, splitAboveSights(hits, shape, splitLevelOf(key), cascade, cell));
}

// How much of the probe at place the probe at from sees, as splitProbeSight reads a point's sight
// of a probe, the march starting slack along it, so a probe on the plane of a face, or behind it
// by less than slack, sees past that face as the points before it do.
float splitAboveSight<H : IRayHits>(H hits, float3 from, float3 place, float slack)
{
    float3 toward = place - from;
    float distance = length(toward);
    ProbeRay ray = hits.along(from, toward / distance, slack, distance);
    if (ray.ended == RayPast || ray.ended == RayLeft)
        return 1.0;
    return ray.ended == RayMet ? saturate(2.0 * (ray.t - distance + slack) / slack) : 0.0;
}

// The shares of the eight probes of the cascade above about the probe at cell of level's cascade
// that it sees from its place (splitAboveSight, by SeeingSlack of its spacing), four bits each,
// the corner's offset along x, y and z its three bits from the lowest, and 15 for all. A probe
// above stands at half its cell along each axis, and where that half lies between two, the probe
// at it rounded up holds the next parts of its rays (askCascadeProbes) and is read as seen whole,
// and a corner whose offset leads along an axis where none lies between keeps 0, the merge
// leaving it out.
public uint splitAboveSights<H : IRayHits>(H hits, SplitStore shape, uint level, uint cascade, int3 cell)
{
    float spacing = splitSpacing(shape, level, cascade);
    float3 from = float3(cell) * spacing;
    int3 low = cell >> 1, high = (cell + 1) >> 1;
    uint sights = 0u;
    for (uint corner = 0u; corner < 8u; corner++)
    {
        int3 offset = int3(int(corner & 1u), int((corner >> 1u) & 1u), int(corner >> 2u));
        if (any(offset > high - low))
            continue;
        int3 above = low + offset;
        float share = all(above == high) ? 1.0 : splitAboveSight(hits, from, float3(above) * 2.0 * spacing, SeeingSlack * spacing);
        sights |= uint(round(15.0 * share)) << (4u * corner);
    }
    return sights;
}

// How much of the probe at place a point at p whose rays start at origin sees, all of it where its
// march toward the place meets no surface short of it by more than half of slack, none where it
// meets one short of it by slack or more, and a share rising evenly between. A probe of a level
// stands up to a spacing from a point along each axis, past a wall thinner than that, where it
// holds the rays of the points on the wall's far side nearest it, so a point that read it would
// take the light of the room beyond. A probe on the plane of the face the point lies on, or before
// it, or behind it by less than half of slack, is marched to lifted off the face as the origin is
// lifted off p, so the march runs beside the face. Marched to on the plane, it was met short of its
// place by the field's march where the march came in low, by more than half of slack below about
// a quarter of a right angle. A share that fell from all to none at one distance would move a
// probe's whole weight in or out of the gather as the march's end crossed it, which at a far
// level's slack a step of the eye or of the field's cells moves it across.
public float splitProbeSight<H : IRayHits>(H hits, float3 p, float3 origin, float3 place, float slack)
{
    float3 lift = origin - p;
    float height = length(lift);
    if (height > 0.0 && dot(place - p, lift) > -0.5 * slack * height)
        place += lift;
    float3 toward = place - origin;
    float distance = length(toward);
    if (distance <= slack)
        return 1.0;
    ProbeRay ray = hits.along(origin, toward / distance, 0.0, distance);
    if (ray.ended == RayPast || ray.ended == RayLeft)
        return 1.0;
    return ray.ended == RayMet ? saturate(2.0 * (ray.t - distance + slack) / slack) : 0.0;
}

// The cell of level's probe of the finest cascade a point at p whose rays start at origin asks for
// and adds its rays to, the nearest of the eight about it the point sees at least half of
// (splitProbeSight), or the nearest of all where it sees none so. The nearest of all stands behind
// the face a point lies on for half the points of a face, and past a wall thinner than its spacing,
// where the gather, which takes only the probes a point sees, would not read the point's own rays.
public int3 splitSeenCell<H : IRayHits>(H hits, SplitStore shape, uint level, float3 p, float3 origin)
{
    float spacing = splitSpacing(shape, level, 0u);
    float slack = SeeingSlack * spacing;
    int3 nearest = splitNearestCell(shape, level, 0u, p);
    if (splitProbeSight(hits, p, origin, float3(nearest) * spacing, slack) >= 0.5)
        return nearest;
    int3 base = int3(floor(p / spacing));
    int3 best = nearest;
    float least = 1e30;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 cell = base + int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        float distance = length(float3(cell) * spacing - p);
        if (all(cell == nearest) || distance >= least || splitProbeSight(hits, p, origin, float3(cell) * spacing, slack) < 0.5)
            continue;
        best = cell;
        least = distance;
    }
    return best;
}

// The light the point at p facing n takes from level's finest probes about it, each standing
// probe by its merged light along its ways weighed by the cosine of each way off n over their
// sum, a way merged to none (mergeSplitCascade) left out and the cosines taken over those kept,
// and by its trilinear weight times the share of it the point sees from origin
// (splitProbeSight, by SeeingSlack of the level's spacing) over the sum of those weights of the
// probes standing and seen that hold a way facing n, with the sum in weights, 0 where none of the
// eight does.
public float3 splitLevelLight<W : IStoreUints, L : IStoreLight, H : IRayHits>(W uints, L light, H hits, SplitStore shape, uint level,
    float3 p, float3 n, float3 origin, out float weights)
{
    float spacing = splitSpacing(shape, level, 0u);
    float3 q = p / spacing;
    int3 base = int3(floor(q));
    float3 f = q - float3(base);
    float3 sum = float3(0.0);
    weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z;
        uint entry = findSplitProbe(uints, shape, splitKey(level, 0u, base + offset));
        if (weight <= 0.0 || entry == SplitNone)
            continue;
        weight *= splitProbeSight(hits, p, origin, float3(base + offset) * spacing, SeeingSlack * spacing);
        if (weight <= 0.0)
            continue;
        float3 taken = float3(0.0);
        float facing = 0.0;
        for (uint way = 0u; way < SplitBaseWays; way++)
        {
            float4 merged = light.lightAt(splitLightAt(shape, entry, way, true));
            float cosine = max(dot(n, splitWay(0u, way)), 0.0);
            if (merged.w >= 0.0)
            {
                taken += merged.xyz * cosine;
                facing += cosine;
            }
        }
        if (facing <= 0.0)
            continue;
        sum += taken / facing * weight;
        weights += weight;
    }
    return weights > 0.0 ? sum / weights : float3(0.0);
}

// Gathers the light the point at index takes from the store's finest probes, a thread a point,
// from the level splitLevelAt picks for it and over the overlap from the next, blended by its
// share, each level's light from the probes standing about the point that it sees from where its
// rays start (splitLevelLight), and from the one level alone where the other has none it sees. A
// point that sees none of either takes none, as does a point the store does not hold
// (splitHolds), though the probe its own rays go to (splitSeenCell) is one it sees. The light kept
// is pi times the cosine-weighted mean, the irradiance, as the dense passes' gather keeps it, so a
// binding reads the same light whichever layout it takes. A binding hands it the hits its rays are
// traced by (IRayHits), the same as traceSeenRays takes.
public void gatherSurface<W : IStoreUints, L : IStoreLight, H : IRayHits, A : ISurfacePoints>(W uints, L light, H hits, SplitFrame frame,
    SplitStore shape, A surfaces, uint index)
{
    float3 p = surfaces.placeAt(index);
    if (!splitHolds(frame, shape, p))
    {
        surfaces.keepTaken(index, float3(0.0));
        return;
    }
    float3 n = surfaces.normalAt(index);
    float3 origin = splitRayOrigin(frame, shape, p, n);
    float next, weights, nextWeights = 0.0;
    uint level = splitLevelAt(frame, shape, p, next);
    float3 taken = splitLevelLight(uints, light, hits, shape, level, p, n, origin, weights);
    float3 nextTaken = float3(0.0);
    if (next > 0.0)
        nextTaken = splitLevelLight(uints, light, hits, shape, level + 1u, p, n, origin, nextWeights);
    float share = nextWeights <= 0.0 ? 0.0 : weights <= 0.0 ? 1.0 : next;
    surfaces.keepTaken(index, lerp(taken, nextTaken, share) * 3.14159265);
}

// The point of a disk of radius 1 a point of the square from -1 to 1 stands for, by the concentric
// map, which keeps each patch's share of the area.
float2 splitDisk(float2 uv)
{
    if (all(uv == 0.0))
        return float2(0.0);
    float r, phi;
    if (abs(uv.x) > abs(uv.y))
    {
        r = uv.x;
        phi = 0.785398163 * (uv.y / uv.x);
    }
    else
    {
        r = uv.y;
        phi = 1.570796327 - 0.785398163 * (uv.x / uv.y);
    }
    return r * float2(cos(phi), sin(phi));
}

// The way of index among the ways of a probe of cascade, the middle of its texel of a square of 4
// texels a side for the finest cascade and twice as many for each above, one square for each half
// of the sphere, laid out on the half by the concentric map onto a disk lifted to it by its equal
// area, so each way stands for an equal share of the sphere.
public float3 splitWay(uint cascade, uint index)
{
    uint side = 4u << cascade;
    uint texel = index % (side * side);
    float2 uv = (float2(float(texel % side), float(texel / side)) + 0.5) / float(side) * 2.0 - 1.0;
    float2 d = splitDisk(uv);
    float r2 = dot(d, d);
    float z = 1.0 - r2;
    return float3(d * sqrt(max(2.0 - r2, 0.0)), index < side * side ? z : -z);
}

// The index of the way of a probe of cascade that way falls in, the texel of splitWay's squares
// holding it, by the equal-area lift and the concentric map taken back.
public uint splitWayOf(uint cascade, float3 way)
{
    uint side = 4u << cascade;
    float az = abs(way.z);
    float2 d = way.xy / sqrt(1.0 + az);
    float r = length(d);
    float2 uv = float2(0.0);
    if (r > 0.0)
    {
        float phi = atan2(d.y, d.x);
        if (phi < -0.785398163)
            phi += 6.283185307;
        if (phi < 0.785398163)
            uv = float2(r, phi * r / 0.785398163);
        else if (phi < 2.356194490)
            uv = float2(-(phi - 1.570796327) * r / 0.785398163, r);
        else if (phi < 3.926990817)
            uv = float2(-r, -(phi - 3.141592654) * r / 0.785398163);
        else
            uv = float2((phi - 4.712388980) * r / 0.785398163, -r);
    }
    uint2 texel = uint2(clamp(int2(floor((uv + 1.0) * 0.5 * float(side))), int2(0), int2(int(side) - 1)));
    return (way.z >= 0.0 ? 0u : side * side) + texel.y * side + texel.x;
}

// The length of the finest cascade's interval as a share of its level's spacing, each cascade
// above reaching four times as far past where the one below ends.
public static const float SplitBaseInterval = 1.6;

// How far off the surface along its normal a seen point's ray starts, as a share of the finest
// level's spacing for each of the finest level's reaches the point lies from the eye, so its first
// step does not meet the surface it leaves.
public static const float SplitLift = 0.25;

// How many parts of one a ray's light and its share let through are added to the store's sums as,
// each part a whole number, so light below a 2048th of one is lost to rounding and a ray's light is
// held at 16384 at most, since a way's sums hold 2^32 parts.
public static const float SplitSumScale = 1024.0;

// The most rays a way's kept light is the mean of, past which each ray of a frame replaces one
// part in this of it, so a way's light settles as its rays come and then follows a change of
// light over this many of them.
public static const uint SplitKeptRays = 256u;

// The bits of a way's count of rays that count the frame's rays, the bits above them keeping how
// many rays its kept light is the mean of.
public static const uint SplitCountBits = 20u;

// Where along its interval of level's cascade a part of a ray ends, the finest cascade's interval
// SplitBaseInterval spacings long and each above reaching four times as far past the one below.
public float splitIntervalEnd(SplitStore shape, uint level, uint cascade)
{
    return SplitBaseInterval * splitSpacing(shape, level, 0u) * float((1u << (2u * (cascade + 1u))) - 1u) / 3.0;
}

// The way of the ray the point at index facing n sends in a frame. Each point steps along the
// plane's sequence of the plastic number from an offset of its own, one step a frame, so over
// frames its ways lie evenly over the half of the sphere its normal faces, and two points beside
// each other step from unrelated offsets. A step's point of the square is laid on the half by the
// concentric map and the equal-area lift, as the probes' ways are, and turned onto n.
public float3 splitRayWay(float3 n, uint index, uint frame)
{
    uint seed = splitMix(index * 2u + 1u);
    uint2 u = uint2(seed, splitMix(seed)) + frame * uint2(3242174889u, 2447445414u);
    float2 d = splitDisk(float2(u >> 8) / 16777216.0 * 2.0 - 1.0);
    float r2 = dot(d, d);
    float3 local = float3(d * sqrt(max(2.0 - r2, 0.0)), 1.0 - r2);
    float s = n.z >= 0.0 ? 1.0 : -1.0;
    float a = -1.0 / (s + n.z);
    float b = n.x * n.y * a;
    float3 t = float3(1.0 + s * n.x * n.x * a, s * b, -s * n.x);
    float3 bt = float3(b, s + n.y * n.y * a, -n.y);
    return normalize(local.x * t + local.y * bt + local.z * n);
}

// Where the ray of a seen point at p facing n starts, SplitLift of the finest level's spacing off
// the surface within the finest level's reach of the eye and past it that share times the point's
// greatest distance along an axis over that reach. The start grows with the distance as the
// levels' spacings and the field's cells do, from the share of its level's spacing a point takes
// at the level's far end, and over the overlap a point's rays of both levels start at one place.
// A start by the level's spacing would put the next level's start twice as far off, a unit off a
// wall at the fourth level, where its rays cross half a corridor before they begin or begin on
// the roof beside a wall's top, and the two levels would read the point apart by what they see
// from there.
public float3 splitRayOrigin(SplitFrame frame, SplitStore shape, float3 p, float3 n)
{
    float3 d3 = abs(p - frame.eye);
    return p + n * (SplitLift * shape.spacing * max(max(d3.x, max(d3.y, d3.z)) / shape.reach, 1.0));
}

// Adds a part of a ray to the sums of entry's probe along its way, its light, the share it lets
// through and a count of one.
void addSplitLight<W : IStoreUints>(W uints, SplitStore shape, uint entry, uint way, float3 light, float through)
{
    uint at = splitSumsAt(shape) + 5u * splitLightAt(shape, entry, way, false);
    uint3 parts = uint3(min(max(light, 0.0), 16384.0) * SplitSumScale + 0.5);
    uints.add(at, parts.x);
    uints.add(at + 1u, parts.y);
    uints.add(at + 2u, parts.z);
    uints.add(at + 3u, uint(saturate(through) * SplitSumScale + 0.5));
    uints.add(at + 4u, 1u);
}

// Traces the ray of a seen point at p from origin along way through level's cascades' intervals one
// after another, each part from where the last ended, and adds each part to the probe of its
// cascade the point's probe of the level (splitSeenCell) leads up to (askCascadeProbes), at the way
// it falls in. A part that meets a surface adds the light the surface sends back and lets none
// through, one that leaves the world adds the sky's light past what it crossed, and the ray ends
// there, so a cascade's probe takes only the rays that reach its interval. A part whose march was
// capped is added to nothing, and the ray ends.
void traceSplitLevel<H : IRayHits, K : ISky, W : IStoreUints>(H hits, K sky, W uints, SplitStore shape, uint level, float3 p,
    float3 origin, float3 way)
{
    int3 cell = splitSeenCell(hits, shape, level, p, origin);
    float start = 0.0;
    for (uint cascade = 0u; cascade < shape.cascades; cascade++)
    {
        float end = splitIntervalEnd(shape, level, cascade);
        ProbeRay part = hits.along(origin, way, start, end);
        if (part.ended == RayCapped)
            return;
        float3 light = part.light;
        if (part.ended == RayLeft)
            light += part.carried * sky.skyAlong(way);
        uint entry = findSplitProbe(uints, shape, splitKey(level, cascade, cell));
        if (entry != SplitNone)
            addSplitLight(uints, shape, entry, splitWayOf(cascade, way), light, part.ended == RayPast ? part.carried : 0.0);
        if (part.ended != RayPast)
            return;
        cell = (cell + 1) >> 1;
        start = end;
    }
}

// Sends the seen point at index its frame's ray (splitRayWay) and adds its parts to the probes of
// its level, a thread a point after askCascadeProbes, and to those of the next over the overlap,
// the same way traced again from the same start by the next level's longer intervals, so a point
// there costs two rays.
public void traceSeenRays<P : IVisiblePoints, H : IRayHits, K : ISky, W : IStoreUints>(P points, H hits, K sky, W uints,
    SplitFrame frame, SplitStore shape, uint index)
{
    if (!points.seenAt(index) || !splitHolds(frame, shape, points.placeAt(index)))
        return;
    float3 p = points.placeAt(index);
    float3 n = points.normalAt(index);
    float next;
    uint level = splitLevelAt(frame, shape, p, next);
    float3 way = splitRayWay(n, index, frame.frame);
    float3 origin = splitRayOrigin(frame, shape, p, n);
    traceSplitLevel(hits, sky, uints, shape, level, p, origin, way);
    if (next > 0.0)
        traceSplitLevel(hits, sky, uints, shape, level + 1u, p, origin, way);
}

// Keeps each way's light from the frame's sums, a thread a way of every probe's own light after
// traceSeenRays, and empties the sums. A way its rays reached takes the mean of its kept light
// over the rays it is the mean of and of the frame's rays, its light and the share let through,
// those rays counted up to SplitKeptRays, so it holds the mean of all its rays until that many
// and moves by a share of one over SplitKeptRays a ray after. A way no ray reached keeps its light
// and its count, and a probe taken this frame first reads none along each way, its w -1, of no
// rays, so the light its entry kept for the probe before is let go.
public void keepSplitLight<W : IStoreUints, L : IStoreLight>(W uints, L light, SplitFrame frame, SplitStore shape, uint index)
{
    uint span = SplitBaseWays * shape.slots;
    if (index >= shape.cascades * span)
        return;
    uint cascade = index / span;
    uint entry = splitPoolFirst(shape, cascade) + (index % span) / splitWays(cascade);
    uint at = splitPoolAt(shape) + 4u * entry;
    uint sums = splitSumsAt(shape) + 5u * index;
    uint counts = uints.load(sums + 4u);
    if (uints.load(at) == 0u)
    {
        if (counts > 0u)
            for (uint k = 0u; k < 5u; k++)
                uints.store(sums + k, 0u);
        return;
    }
    bool fresh = uints.load(at + 2u) == frame.frame;
    float4 kept = fresh ? float4(0.0, 0.0, 0.0, -1.0) : light.lightAt(index);
    uint count = counts & ((1u << SplitCountBits) - 1u);
    uint held = fresh ? 0u : counts >> SplitCountBits;
    if (count > 0u)
    {
        float4 mean = float4(float(uints.load(sums)), float(uints.load(sums + 1u)), float(uints.load(sums + 2u)),
            float(uints.load(sums + 3u))) / (SplitSumScale * float(count));
        held = min(held + count, SplitKeptRays);
        kept = lerp(kept, mean, min(float(count) / float(held), 1.0));
        for (uint k = 0u; k < 4u; k++)
            uints.store(sums + k, 0u);
    }
    uints.store(sums + 4u, held << SplitCountBits);
    light.keepLight(index, kept);
}

// The light merged above the way of cascade of the probe of key, the mean of the four ways of the
// cascade above that its texel holds, at twice its place along each side of its half, at the
// probes of that cascade about it, each weighed by the share of it the probe sees (sights, as
// splitAboveSights keeps them), the one its rays' next parts are added to (traceSplitLevel), at
// half its cell rounded up, by all of it. A way merged to none is left out and the mean is taken
// over those kept, w holds the share let through to the sky, and w is -1 where none is kept or no
// such probe stands. The probe at half the cell rounded up alone holds the rays of the probes at
// the cell and the one past it along each axis, so a probe between two read through it alone the
// rays of a patch reaching a probe past it along each axis, and the probes of a surface by turns
// took the light from past either side, a step of the eye moving a point between them stepping its
// light. The probes about it weighed by the share they see, by turns blocked by a wall, take the
// light from both sides where the surface is open and none from past a wall thinner than their
// spacing, whose rays are those of points on its far side.
float4 splitAboveLight<W : IStoreUints, L : IStoreLight>(W uints, L light, SplitFrame frame, SplitStore shape, uint key,
    uint sights, uint cascade, uint way)
{
    uint side = 4u << cascade;
    uint above = 2u * side;
    uint first = (way / (side * side)) * above * above + 2u * ((way / side) % side) * above + 2u * (way % side);
    int3 cell = splitCellOf(frame, shape, key);
    int3 low = cell >> 1, high = (cell + 1) >> 1;
    float4 sum = float4(0.0);
    float weights = 0.0;
    for (uint corner = 0u; corner < 8u; corner++)
    {
        int3 offset = int3(int(corner & 1u), int((corner >> 1u) & 1u), int(corner >> 2u));
        if (any(offset > high - low))
            continue;
        int3 place = low + offset;
        float weight = all(place == high) ? 1.0 : float((sights >> (4u * corner)) & 15u) / 15.0;
        uint entry = weight > 0.0 ? findSplitProbe(uints, shape, splitKey(splitLevelOf(key), cascade + 1u, place)) : SplitNone;
        for (uint k = 0u; entry != SplitNone && k < 4u; k++)
        {
            float4 merged = light.lightAt(splitLightAt(shape, entry, first + (k & 1u) + (k >> 1u) * above, true));
            if (merged.w >= 0.0)
            {
                sum += merged * weight;
                weights += weight;
            }
        }
    }
    return weights > 0.0 ? sum / weights : float4(0.0, 0.0, 0.0, -1.0);
}

// Merges each way of cascade's probes into their merged light, a thread a way, once for each
// cascade from the coarsest down after keepSplitLight. A way's merged light is its own light and,
// by the share its own light lets through, the light merged above it (splitAboveLight), the sky's
// along the way above the coarsest, with the share let through to the sky in w. A way no ray
// reached merges to none, its w -1, which the ways below and the gather leave out, so a way no ray
// tells of is not read as letting the sky through, and a way letting some through with none merged
// above it takes none from above.
public void mergeSplitCascade<W : IStoreUints, L : IStoreLight, K : ISky>(W uints, L light, K sky, SplitFrame frame,
    SplitStore shape, uint cascade, uint index)
{
    if (cascade >= shape.cascades || index >= SplitBaseWays * shape.slots)
        return;
    uint ways = splitWays(cascade);
    uint entry = splitPoolFirst(shape, cascade) + index / ways;
    uint way = index % ways;
    uint at = splitPoolAt(shape) + 4u * entry;
    uint key = uints.load(at);
    if (key == 0u)
        return;
    float4 own = light.lightAt(splitLightAt(shape, entry, way, false));
    float4 above = cascade + 1u < shape.cascades ? splitAboveLight(uints, light, frame, shape, key, uints.load(at + 3u), cascade, way)
        : float4(sky.skyAlong(splitWay(cascade, way)), 1.0);
    float4 merged = float4(0.0, 0.0, 0.0, -1.0);
    if (own.w >= 0.0)
        merged = above.w < 0.0 ? float4(own.xyz, 0.0) : float4(own.xyz + own.w * above.xyz, own.w * above.w);
    light.keepLight(splitLightAt(shape, entry, way, true), merged);
}
