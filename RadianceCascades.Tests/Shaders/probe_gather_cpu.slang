// A probe of a near cascade gathering its light at a surface from the cells given, on one thread
// so each part runs after the one it reads. Each far probe traces its texels and its reach map
// first, then each of the near probe's texels traces its ray and merges with the far probes, by the
// library's rule and, for a comparison, by trilinear weights alone, and last the light its texels
// give the surface is gathered both ways.
import rc_probes;
import rc_cascade_merge;

// Each cell's distance, the plane it keeps and its share of light through, five floats a cell, as
// probe_field_cpu.slang builds them.
RWStructuredBuffer<float> cells;

// The field's least corner, its cell and its cells along each side, the sky's luminance, the near
// cascade and the far cascade as ProbeCascade holds them, ten floats each from the floats at 6 and
// at 16, and the surface's normal and albedo.
RWStructuredBuffer<float> setup;

// Each far probe's light along each of its texels, three floats a texel, and its reach map.
RWStructuredBuffer<float> farLight;
RWStructuredBuffer<float> farReach;

// Each near texel's light merged by the rule and by trilinear weights alone, three floats each,
// whether it marched on itself and the steps that march took, eight floats a texel.
RWStructuredBuffer<float> nearTexels;

// The light the surface takes and reflects by the rule, the same by trilinear weights alone, three
// floats each, then how many texels marched on themselves, their steps in all and the most one
// took, and how many rays the far probes traced for their light, their steps in all and the most
// one took.
RWStructuredBuffer<float> result;

struct ProbeCells : IProbeField
{
    float3 corner() { return float3(setup[0], setup[1], setup[2]); }
    float cell() { return setup[3]; }
    int size() { return int(setup[4]); }
    int index(int3 at) { return (at.z * size() + at.y) * size() + at.x; }
    int cascadeOf(float3 p, float margin)
    {
        float3 q = (p - corner()) / cell();
        return all(q >= margin) && all(q <= float(size()) - margin) ? 0 : -1;
    }
    float4 cornerAndCell(int cascade) { return float4(corner(), cell()); }
    float4 cellAt(int cascade, int3 at)
    {
        int i = index(at) * 5;
        return float4(cells[i], cells[i + 1], cells[i + 2], cells[i + 3]);
    }
    float4 between(int cascade, float3 at)
    {
        float3 q = at - 0.5;
        int3 base = int3(floor(q));
        float3 f = q - float3(base);
        float4 sum = float4(0.0);
        for (int corner = 0; corner < 8; corner++)
        {
            int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
            float3 lerps = lerp(1.0 - f, f, float3(offset));
            sum += cellAt(cascade, clamp(base + offset, int3(0), int3(size() - 1))) * (lerps.x * lerps.y * lerps.z);
        }
        return sum;
    }
    float3 secondAt(int cascade, int3 at) { return float3(0.0); }
    float throughAt(int cascade, int3 at) { return cells[index(at) * 5 + 4]; }
    // The walls and floors of these rooms send no light of their own.
    float3 hitLight(float3 place, float3 way, int cascade, int3 at) { return float3(0.0); }
};

struct FarProbes : IFarProbes
{
    ProbeCascade cascade;
    uint index(int3 probe) { return uint((probe.z * cascade.count.y + probe.y) * cascade.count.x + probe.x); }
    float3 lightAt(int3 probe, uint2 texel)
    {
        uint i = (index(probe) * cascade.texels * cascade.texels + texel.y * cascade.texels + texel.x) * 3u;
        return float3(farLight[i], farLight[i + 1], farLight[i + 2]);
    }
    float reachAt(int3 probe, uint2 texel)
    {
        return farReach[index(probe) * ReachTexels * ReachTexels + texel.y * ReachTexels + texel.x];
    }
};

struct CountedSteps : IMarchSteps
{
    uint taken;
    [mutating]
    void at(float t, float d) { taken++; }
};

struct NearLight : IProbeLight
{
    uint texels;
    uint offset;
    float3 lightOf(uint2 texel)
    {
        uint i = (texel.y * texels + texel.x) * 8u + offset;
        return float3(nearTexels[i], nearTexels[i + 1], nearTexels[i + 2]);
    }
};

ProbeCascade cascadeAt(int at)
{
    ProbeCascade cascade;
    cascade.corner = float3(setup[at], setup[at + 1], setup[at + 2]);
    cascade.spacing = setup[at + 3];
    cascade.count = int3(int(setup[at + 4]), int(setup[at + 5]), int(setup[at + 6]));
    cascade.texels = uint(setup[at + 7]);
    cascade.start = setup[at + 8];
    cascade.end = setup[at + 9];
    cascade.stepCap = 0u;
    return cascade;
}

// The light along way at e from the eight far probes about it, each at its trilinear weight alone,
// the merge the rule is measured against.
float3 trilinearLight(FarProbes probes, float3 e, float3 way)
{
    ProbeCascade far = probes.cascade;
    int3 base;
    float3 f;
    farProbesAbout(far, e, base, f);
    float3 sum = float3(0.0);
    float weights = 0.0;
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 probe = base + offset;
        if (any(probe < 0) || any(probe >= far.count))
            continue;
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        float weight = lerps.x * lerps.y * lerps.z;
        sum += probes.lightAt(probe, texelOfWay(far.texels, way)) * weight;
        weights += weight;
    }
    return weights > 0.0 ? sum / weights : float3(0.0);
}

[shader("compute")]
[numthreads(1, 1, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    ProbeCells field;
    float3 sky = float3(setup[5]);
    UniformSky skyLight = { sky };
    ProbeCascade near = cascadeAt(6);
    ProbeCascade far = cascadeAt(16);
    FarProbes probes = { far };
    float3 place = probePlace(near, int3(0));

    // Every far probe is traced, since a near ray that marches on to open space hands off past its
    // reach as far as the field's edge.
    uint farRays = 0u, farSteps = 0u, farMost = 0u;
    for (int z = 0; z < far.count.z; z++)
        for (int y = 0; y < far.count.y; y++)
            for (int x = 0; x < far.count.x; x++)
            {
                int3 probe = int3(x, y, z);
                float3 p = probePlace(far, probe);
                // A probe inside a mesh keeps ReachInside, as reachTexel keeps it.
                uint reachBase = probes.index(probe) * ReachTexels * ReachTexels;
                int c = field.cascadeOf(p, 1.0);
                bool inside = c >= 0 && fieldDistanceIn(field, c, p) < 0.0;
                for (uint k = 0u; k < ReachTexels * ReachTexels; k++)
                    farReach[reachBase + k] = inside ? ReachInside
                        : reachAlong(field, p, texelWay(ReachTexels, uint2(k % ReachTexels, k / ReachTexels)), far.end);
                uint lightBase = probes.index(probe) * far.texels * far.texels;
                for (uint k = 0u; k < far.texels * far.texels; k++)
                {
                    CountedSteps counted = { 0u };
                    ProbeRay ray = traceProbeRay(field, p, texelWay(far.texels, uint2(k % far.texels, k / far.texels)), far.start, far.end, counted);
                    farRays++;
                    farSteps += counted.taken;
                    farMost = max(farMost, counted.taken);
                    float3 light = lastCascadeLight(ray, texelWay(far.texels, uint2(k % far.texels, k / far.texels)), skyLight);
                    farLight[(lightBase + k) * 3u] = light.x;
                    farLight[(lightBase + k) * 3u + 1u] = light.y;
                    farLight[(lightBase + k) * 3u + 2u] = light.z;
                }
            }

    uint marched = 0u, steps = 0u, most = 0u;
    for (uint k = 0u; k < near.texels * near.texels; k++)
    {
        uint2 texel = uint2(k % near.texels, k / near.texels);
        float3 way = texelWay(near.texels, texel);
        ProbeRay ray = traceProbeRay(field, place, way, near.start, near.end);
        CountedSteps counted = { 0u };
        bool itself;
        float3 merged = mergedLight(field, probes, gridPlaces(far), far, far.end, place, near.texels, texel, ray, skyLight, counted, itself);
        float3 trilinear = ray.light + (ray.ended == RayPast ? trilinearLight(probes, place + way * ray.t, way) * ray.carried
            : ray.ended == RayMet ? float3(0.0) : sky * ray.carried);
        if (itself)
        {
            marched++;
            steps += counted.taken;
            most = max(most, counted.taken);
        }
        nearTexels[k * 8u] = merged.x;
        nearTexels[k * 8u + 1u] = merged.y;
        nearTexels[k * 8u + 2u] = merged.z;
        nearTexels[k * 8u + 3u] = trilinear.x;
        nearTexels[k * 8u + 4u] = trilinear.y;
        nearTexels[k * 8u + 5u] = trilinear.z;
        nearTexels[k * 8u + 6u] = itself ? 1.0 : 0.0;
        nearTexels[k * 8u + 7u] = float(counted.taken);
    }

    float3 n = float3(setup[26], setup[27], setup[28]);
    float3 albedo = float3(setup[29]);
    for (uint kind = 0u; kind < 2u; kind++)
    {
        NearLight light = { near.texels, kind * 3u };
        float3 taken = lightTaken(light, near.texels, n);
        float3 reflected = reflectedLight(albedo, taken);
        result[kind * 6u] = taken.x;
        result[kind * 6u + 1u] = taken.y;
        result[kind * 6u + 2u] = taken.z;
        result[kind * 6u + 3u] = reflected.x;
        result[kind * 6u + 4u] = reflected.y;
        result[kind * 6u + 5u] = reflected.z;
    }
    result[12] = float(marched);
    result[13] = float(steps);
    result[14] = float(most);
    result[15] = float(farRays);
    result[16] = float(farSteps);
    result[17] = float(farMost);
}
