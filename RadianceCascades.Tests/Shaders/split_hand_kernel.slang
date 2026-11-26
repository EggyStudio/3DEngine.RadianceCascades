// The split cascades' hand implemented over buffers as an engine would, each function of its
// interfaces called once and each pass of the store run over it, so a compile of this kernel holds
// that the hand is given and the store kept on every target the tests compile to with 32-bit
// atomics on a buffer alone. Its entry is test, as every kernel's
// of the tests is, since the compile names it and Metal reserves main.
import rc_split;

RWStructuredBuffer<float4> points;
RWStructuredBuffer<float4> taken;
RWStructuredBuffer<Atomic<uint>> uints;
RWStructuredBuffer<float4> light;
RWStructuredBuffer<float4> written;

// A pixel's place in xyz and its depth in w, its normal in the next, and w 0 where it sees the sky.
struct BufferPoints : IVisiblePoints
{
    float3 placeAt(uint index) { return points[index * 2u].xyz; }
    float3 normalAt(uint index) { return points[index * 2u + 1u].xyz; }
    void keepTaken(uint index, float3 light) { taken[index] = float4(light, 1.0); }
    bool seenAt(uint index) { return points[index * 2u].w > 0.0; }
}

// A ray part that meets nothing, as one with no world to meet would end.
struct NoHits : IRayHits
{
    ProbeRay along(float3 origin, float3 way, float start, float end)
    {
        ProbeRay ray;
        ray.ended = RayPast;
        ray.t = end;
        ray.carried = 1.0;
        ray.light = float3(0.0);
        return ray;
    }
}

// The uints as a buffer of atomics, since WGSL changes a uint atomically only in an array of
// atomics, and a plain buffer's uint taken atomically compiles to an atomic add on a plain uint,
// which WGSL does not allow.
struct BufferUints : IStoreUints
{
    uint load(uint index) { return uints[index].load(); }
    void store(uint index, uint value) { uints[index].store(value); }
    uint add(uint index, uint value) { return uints[index].add(value); }
    uint compareExchange(uint index, uint expected, uint desired) { return uints[index].compareExchange(expected, desired); }
}

struct OneSky : ISky
{
    float3 skyAlong(float3 way) { return float3(1.0); }
}

struct BufferLight : IStoreLight
{
    float4 lightAt(uint index) { return light[index]; }
    void keepLight(uint index, float4 value) { light[index] = value; }
}

float4 handed<P : IVisiblePoints, H : IRayHits, W : IStoreUints, L : IStoreLight>(P seen, H hits, W store, L kept,
    SplitFrame frame, SplitStore shape, uint index)
{
    if (!seen.seenAt(index))
        return float4(0.0);
    float3 place = seen.placeAt(index);
    ProbeRay ray = hits.along(place, seen.normalAt(index), 0.0, shape.spacing);
    uint slot = store.compareExchange(index % shape.slots, 0u, frame.frame + 1u);
    store.store(shape.slots + slot % shape.slots, store.add(index, 1u) + store.load(index));
    kept.keepLight(index, kept.lightAt(index) + float4(ray.light, ray.carried));
    seen.keepTaken(index, ray.light);
    clearSplitTable(store, shape, index);
    keepSplitProbes(store, frame, shape, index);
    askSeenProbes(seen, hits, store, frame, shape, index);
    askCascadeProbes(hits, store, frame, shape, 0u, index);
    OneSky sky;
    traceSeenRays(seen, hits, sky, store, frame, shape, index);
    keepSplitLight(store, kept, frame, shape, index);
    mergeSplitCascade(store, kept, sky, frame, shape, 0u, index);
    gatherSurface(store, kept, hits, frame, shape, seen, index);
    return float4(place - frame.eye, float(shape.levels + shape.cascades));
}

[shader("compute")]
[numthreads(64, 1, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    BufferPoints seen;
    NoHits hits;
    BufferUints store;
    BufferLight kept;
    SplitFrame frame;
    frame.eye = float3(0.0, 1.5, 0.0);
    frame.frame = id.y;
    SplitStore shape;
    shape.slots = 1024u;
    shape.spacing = 0.25;
    shape.levels = 4u;
    shape.cascades = 4u;
    shape.reach = 1.0;
    shape.age = 32u;
    written[id.x] = handed(seen, hits, store, kept, frame, shape, id.x);
}
