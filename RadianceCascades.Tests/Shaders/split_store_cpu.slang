// The split cascades' store over buffers on the CPU, one pass a run picked by the setup's first
// float, so the tests read what the store keeps over the frames of a walk with no GPU. The CPU runs
// a dispatch's threads one after another, so the store's uints change in place with no atomics,
// which the CPU target has none of on a buffer, where an engine's change atomically as
// split_hand_kernel.slang's do.
//
// The setup holds the pass first, the eye from 1, the frame's count at 4, the store's slots,
// spacing and cascades from 5, the cascade a pass of the cascades asks above at 11, the count of
// points at 12, the count of boxes at 13, the world's half width about its middle at 14 and the
// sky's light at 15. A point is two float4, its place with 1 in w where the eye sees it, then its
// normal, and a box two, its low corner with the light it sends in w, then its high corner.
import rc_split;

RWStructuredBuffer<float> setup;
RWStructuredBuffer<float4> points;
RWStructuredBuffer<uint> uints;
RWStructuredBuffer<float4> light;
RWStructuredBuffer<float4> taken;
RWStructuredBuffer<float4> boxes;

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

// The parts of a ray among the boxes, each meeting the nearest box it enters within its interval,
// or that it starts within, with the box's light, or leaving the world's cube where it leaves it
// first, or passing.
struct BoxHits : IRayHits
{
    ProbeRay along(float3 origin, float3 way, float start, float end)
    {
        float3 inv = 1.0 / select(abs(way) < 1e-8, float3(1e-8), way);
        float nearest = end, lit = 0.0;
        for (uint b = 0u; b < uint(setup[13]); b++)
        {
            float4 low = boxes[2u * b], high = boxes[2u * b + 1u];
            float3 t0 = (low.xyz - origin) * inv, t1 = (high.xyz - origin) * inv;
            float enter = max(max(min(t0.x, t1.x), min(t0.y, t1.y)), min(t0.z, t1.z));
            float leave = min(min(max(t0.x, t1.x), max(t0.y, t1.y)), max(t0.z, t1.z));
            float t = max(enter, start);
            if (enter <= leave && leave >= start && t < nearest)
            {
                nearest = t;
                lit = low.w;
            }
        }
        float3 wall = (select(way >= 0.0, float3(setup[14]), float3(-setup[14])) - origin) * inv;
        float leaves = min(wall.x, min(wall.y, wall.z));
        ProbeRay ray;
        ray.carried = 1.0;
        ray.light = float3(0.0);
        ray.t = end;
        ray.ended = RayPast;
        if (nearest < end && nearest <= leaves)
        {
            ray.ended = RayMet;
            ray.t = nearest;
            ray.light = float3(lit);
        }
        else if (leaves < end)
        {
            ray.ended = RayLeft;
            ray.t = max(leaves, start);
        }
        return ray;
    }
}

struct EvenSky : ISky
{
    float3 skyAlong(float3 way) { return float3(setup[15]); }
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
    shape.cascades = uint(setup[7]);
    uint pass = uint(setup[0]), index = id.x;
    BufferPoints seen;
    PlainUints store;
    BufferLight kept;
    BoxHits hits;
    EvenSky sky;
    if (pass == 0u)
        clearSplitTable(store, frame, shape, index);
    else if (pass == 1u)
        keepSplitProbes(store, frame, shape, index);
    else if (pass == 2u && index < uint(setup[12]))
        askSeenProbes(seen, hits, store, frame, shape, index);
    else if (pass == 3u)
        askCascadeProbes(store, frame, shape, uint(setup[11]), index);
    else if (pass == 4u && index < uint(setup[12]))
        gatherSurface(store, kept, frame, shape, seen, index);
    else if (pass == 5u && index == 0u)
    {
        // The layout, which the tests hold the library's sizes to.
        taken[0] = float4(float(splitUints(shape)), float(splitLights(shape)), float(splitEntries(shape)), float(splitTableSlots(shape)));
        taken[1] = float4(float(splitTableAt(shape)), float(splitPoolAt(shape)), float(splitFreeAt(shape)), float(splitSumsAt(shape)));
        taken[2] = float4(float(splitAgesAt(shape, 0u)), float(splitAgesAt(shape, 1u)), 0.0, 0.0);
    }
    else if (pass == 6u && index < uint(setup[12]))
    {
        // The place of the finest probe each point adds its rays to, and its entry, -1 where the
        // store holds none.
        float3 p = seen.placeAt(index);
        float3 origin = splitRayOrigin(frame, shape, p, seen.normalAt(index));
        int3 cell = splitSeenCell(hits, shape, p, origin);
        uint entry = findSplitProbe(store, shape, splitKey(0u, cell));
        taken[index] = float4(float3(cell) * shape.spacing, entry == SplitNone ? -1.0 : float(entry));
    }
    else if (pass == 7u && index < splitEntries(shape))
    {
        // Each entry's probe, its place and its cascade, or -1 where it holds none.
        uint key = uints[splitPoolAt(shape) + SplitEntryUints * index];
        uint cascade = splitCascadeOfKey(key);
        float3 place = float3(splitCellOf(frame, shape, key)) * splitSpacing(shape, cascade);
        taken[index] = float4(place, key == 0u ? -1.0 : float(cascade));
    }
    else if (pass == 8u && index < 160u)
    {
        // The ways of the two finest cascades, the finest's first.
        taken[index] = float4(index < 32u ? splitWay(0u, index) : splitWay(1u, index - 32u), 0.0);
    }
    else if (pass == 9u && index < uint(setup[12]))
        traceSeenRays(seen, hits, sky, store, frame, shape, index);
    else if (pass == 10u)
        keepSplitLight(store, kept, frame, shape, index);
    else if (pass == 14u)
        mergeSplitCascade(store, kept, sky, frame, shape, uint(setup[11]), index);
    else if (pass == 11u && index < uint(setup[12]))
    {
        // The first point's ray at the frame of the thread's count, and the finest way it falls in.
        float3 way = splitRayWay(seen.normalAt(0u), 0u, index);
        taken[index] = float4(way, float(splitWayOf(0u, way)));
    }
    else if (pass == 15u && index < uint(setup[12]))
    {
        // The way of the cascade at 11 each point's normal falls in, which the tests read the
        // merge's shares by.
        taken[index] = float4(float(splitWayOf(uint(setup[11]), seen.normalAt(index))), 0.0, 0.0, 0.0);
    }
    else if (pass == 16u && index < uint(setup[12]))
        taken[index] = splitGatherCause(store, kept, frame, shape, seen.placeAt(index), seen.normalAt(index));
    else if (pass == 12u && index < splitWays(uint(setup[11])))
        taken[index] = float4(float(splitWayOf(uint(setup[11]), splitWay(uint(setup[11]), index))), 0.0, 0.0, 0.0);
    else if (pass == 13u && index < uint(setup[12]))
    {
        // Each point's ray of the frame traced whole across every interval, 1 float4 and one a
        // cascade, its light and how it ended, then the entry and the way of each cascade's probe
        // its parts go to with the end of that cascade's interval, the finest's w where the ray
        // ended.
        float3 p = seen.placeAt(index), n = seen.normalAt(index);
        float3 way = splitRayWay(n, index, frame.frame);
        uint at = index * (1u + shape.cascades);
        float3 origin = splitRayOrigin(frame, shape, p, n);
        ProbeRay whole = hits.along(origin, way, 0.0, splitIntervalEnd(shape, shape.cascades - 1u));
        float3 light = whole.light + (whole.ended == RayLeft ? whole.carried * sky.skyAlong(way) : float3(0.0));
        taken[at] = float4(light, float(whole.ended));
        int3 cell = splitSeenCell(hits, shape, p, origin);
        for (uint c = 0u; c < shape.cascades; c++)
        {
            uint entry = findSplitProbe(store, shape, splitKey(c, cell));
            taken[at + 1u + c] = float4(entry == SplitNone ? -1.0 : float(entry), float(splitWayOf(c, way)), splitIntervalEnd(shape, c),
                c == 0u ? whole.t : 0.0);
            cell = (cell + 1) >> 1;
        }
    }
}
