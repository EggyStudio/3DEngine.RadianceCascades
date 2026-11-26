// What an engine hands the passes of split radiance cascades, written out ahead of the passes,
// which no kernel runs yet. The split cascades keep probes only near the surfaces the eye sees.
// Each seen point of a frame inserts the nearest probe of its level into a store keyed by place and
// level, and each probe inserts the nearest probe of the cascade above, so the store holds probes
// where light is read and none in the air of a narrow room or inside its walls. A probe stays in
// the store, its place and its light kept, while the eye moves, until no seen point has asked for
// it for some frames. A ray is traced from each seen point, never from a probe, and its parts are
// split by how far along it they lie among the cascades' intervals. Each part's light, the share of
// light it lets through and a count are added to the nearest probe of that cascade and to its
// nearest way, and the store keeps each probe's light as their mean by count. A point's level is
// the base-two logarithm of its greatest distance along an axis from the eye, so the passes read
// the eye as a member of the hand, and the ways a point's rays take turn from frame to frame by the
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
    // below and an interval four times as long.
    public uint cascades;
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
// passes lay out in them the key of each probe standing in each slot, the frame it was last asked
// for, the sums of light, of the share let through and of the count its rays add, and the count of
// probes standing at each level, which an engine may read as a dispatch's argument.
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
