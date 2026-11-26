// The split cascades' store over buffers on the CPU, one pass a run picked by the setup's first
// float, so the tests read what the store keeps over the frames of a walk with no GPU. The CPU runs
// a dispatch's threads one after another, so the store's uints change in place with no atomics,
// which the CPU target has none of on a buffer, where an engine's change atomically as
// split_hand_kernel.slang's do.
//
// The setup holds the pass first, the eye from 1, the frame's count at 4, the store's slots,
// spacing, levels, cascades, reach and age from 5, the cascade a pass of the cascades asks above
// at 11, and the count of points at 12. A point is two float4, its place with 1 in w where the eye
// sees it, then its normal.
import rc_split;

RWStructuredBuffer<float> setup;
RWStructuredBuffer<float4> points;
RWStructuredBuffer<uint> uints;
RWStructuredBuffer<float4> light;
RWStructuredBuffer<float4> taken;

struct BufferPoints : IVisiblePoints
{
    float3 placeAt(uint index) { return points[index * 2u].xyz; }
    float3 normalAt(uint index) { return points[index * 2u + 1u].xyz; }
    void keepTaken(uint index, float3 value) { taken[index] = float4(value, 1.0); }
    bool seenAt(uint index) { return points[index * 2u].w > 0.0; }
}

struct PlainUints : IStoreUints
{
    uint load(uint index) { return uints[index]; }
    void store(uint index, uint value) { uints[index] = value; }

    uint add(uint index, uint value)
    {
        uint held = uints[index];
        uints[index] = held + value;
        return held;
    }

    uint compareExchange(uint index, uint expected, uint desired)
    {
        uint held = uints[index];
        if (held == expected)
            uints[index] = desired;
        return held;
    }
}

struct BufferLight : IStoreLight
{
    float4 lightAt(uint index) { return light[index]; }
    void keepLight(uint index, float4 value) { light[index] = value; }
}

[shader("compute")]
[numthreads(64, 1, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    SplitFrame frame;
    frame.eye = float3(setup[1], setup[2], setup[3]);
    frame.frame = uint(setup[4]);
    SplitStore shape;
    shape.slots = uint(setup[5]);
    shape.spacing = setup[6];
    shape.levels = uint(setup[7]);
    shape.cascades = uint(setup[8]);
    shape.reach = setup[9];
    shape.age = uint(setup[10]);
    uint pass = uint(setup[0]), index = id.x;
    BufferPoints seen;
    PlainUints store;
    BufferLight kept;
    if (pass == 0u)
        clearSplitTable(store, shape, index);
    else if (pass == 1u)
        keepSplitProbes(store, frame, shape, index);
    else if (pass == 2u && index < uint(setup[12]))
        askSeenProbes(seen, store, frame, shape, index);
    else if (pass == 3u)
        askCascadeProbes(store, frame, shape, uint(setup[11]), index);
    else if (pass == 4u && index < uint(setup[12]))
        gatherSurface(store, kept, frame, shape, seen, index);
    else if (pass == 5u && index == 0u)
    {
        // The layout, which the tests hold the library's sizes to.
        taken[0] = float4(float(splitUints(shape)), float(splitLights(shape)), float(splitEntries(shape)), float(splitTableSlots(shape)));
        taken[1] = float4(float(splitTableAt(shape)), float(splitPoolAt(shape)), float(splitFreeAt(shape)), 0.0);
    }
    else if (pass == 6u && index < uint(setup[12]))
    {
        // Each point's level, its share of the next, and the entries of its nearest probes of both.
        float next;
        float3 p = seen.placeAt(index);
        uint level = splitLevelAt(frame, shape, p, next);
        uint entry = findSplitProbe(store, shape, splitKey(level, 0u, splitNearestCell(shape, level, 0u, p)));
        uint above = level + 1u < shape.levels ? findSplitProbe(store, shape, splitKey(level + 1u, 0u, splitNearestCell(shape, level + 1u, 0u, p))) : SplitNone;
        taken[index] = float4(float(level), next, entry == SplitNone ? -1.0 : float(entry), above == SplitNone ? -1.0 : float(above));
    }
    else if (pass == 7u && index < splitEntries(shape))
    {
        // Each entry's probe, its place and its level and cascade, or -1 where it holds none.
        uint key = uints[splitPoolAt(shape) + 3u * index];
        uint level = splitLevelOf(key), cascade = splitCascadeOfKey(key);
        float3 place = float3(splitCellOf(frame, shape, key)) * splitSpacing(shape, level, cascade);
        taken[index] = float4(place, key == 0u ? -1.0 : float(level * 4u + cascade));
    }
    else if (pass == 8u && index < 160u)
    {
        // The ways of the two finest cascades, the finest's first.
        taken[index] = float4(index < 32u ? splitWay(0u, index) : splitWay(1u, index - 32u), 0.0);
    }
}
