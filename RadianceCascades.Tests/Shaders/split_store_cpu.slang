// The split cascades' store over buffers on the CPU, one pass a run picked by the setup's first
// float, so the tests read what the store keeps over the frames of a walk with no GPU. The CPU runs
// a dispatch's threads one after another, so the store's uints change in place with no atomics,
// which the CPU target has none of on a buffer, where an engine's change atomically as
// split_hand_kernel.slang's do.
//
// The setup holds the pass first, the eye from 1, the frame's count at 4, the store's slots,
// spacing, levels, cascades, reach and age from 5, the cascade a pass of the cascades asks above
// at 11, the count of points at 12, the count of boxes at 13, the world's half width about its
// middle at 14 and the sky's light at 15. A point is two float4, its place with 1 in w where the
// eye sees it, then its normal, and a box two, its low corner with the light it sends in w, then
// its high corner.
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
    shape.levels = uint(setup[7]);
    shape.cascades = uint(setup[8]);
    shape.reach = setup[9];
    shape.age = uint(setup[10]);
    uint pass = uint(setup[0]), index = id.x;
    BufferPoints seen;
    PlainUints store;
    BufferLight kept;
    BoxHits hits;
    EvenSky sky;
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
        taken[1] = float4(float(splitTableAt(shape)), float(splitPoolAt(shape)), float(splitFreeAt(shape)), float(splitSumsAt(shape)));
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
    else if (pass == 9u && index < uint(setup[12]))
        traceSeenRays(seen, hits, sky, store, frame, shape, index);
    else if (pass == 10u)
        keepSplitLight(store, kept, frame, shape, index);
    else if (pass == 11u && index < uint(setup[12]))
    {
        // The first point's ray at the frame of the thread's count, and the finest way it falls in.
        float3 way = splitRayWay(seen.normalAt(0u), 0u, index);
        taken[index] = float4(way, float(splitWayOf(0u, way)));
    }
    else if (pass == 12u && index < splitWays(uint(setup[11])))
        taken[index] = float4(float(splitWayOf(uint(setup[11]), splitWay(uint(setup[11]), index))), 0.0, 0.0, 0.0);
    else if (pass == 13u && index < uint(setup[12]))
    {
        // Each point's ray of the frame traced whole across every interval of its level and of
        // the next where it stands in their overlap, five float4 a level, its light and how it
        // ended, then the entry and the way of each cascade's probe its parts go to with the end of
        // that cascade's interval, the finest's w where the ray ended, the next level's entries -2
        // where it takes none.
        float next;
        float3 p = seen.placeAt(index), n = seen.normalAt(index);
        uint level = splitLevelAt(frame, shape, p, next);
        float3 way = splitRayWay(n, index, frame.frame);
        for (uint j = 0u; j < 2u; j++)
        {
            uint at = index * 10u + j * 5u;
            uint l = level + j;
            float far = splitIntervalEnd(shape, l, shape.cascades - 1u);
            ProbeRay whole = hits.along(splitRayOrigin(shape, l, p, n), way, 0.0, far);
            float3 light = whole.light + (whole.ended == RayLeft ? whole.carried * sky.skyAlong(way) : float3(0.0));
            taken[at] = float4(light, float(whole.ended));
            int3 cell = splitNearestCell(shape, l, 0u, p);
            for (uint c = 0u; c < shape.cascades; c++)
            {
                uint entry = findSplitProbe(store, shape, splitKey(l, c, cell));
                float name = j == 1u && !(next > 0.0) ? -2.0 : entry == SplitNone ? -1.0 : float(entry);
                taken[at + 1u + c] = float4(name, float(splitWayOf(c, way)), splitIntervalEnd(shape, l, c), c == 0u ? whole.t : 0.0);
                cell = (cell + 1) >> 1;
            }
        }
    }
}
