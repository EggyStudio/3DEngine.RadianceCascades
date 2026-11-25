// The level each point given is gathered from (gatheredLevel), over the levels given, at the
// library's rim (GatherRim) and with none, a thread a point.
import rc_probe_passes;

// The count of levels, then each level's corner, spacing and probes along each axis, seven floats a
// level.
RWStructuredBuffer<float> levelFloats;
// Each point's place, three floats a point.
RWStructuredBuffer<float> points;
// The level each point is gathered from at the library's rim, then with none, two a point.
RWStructuredBuffer<uint> chosen;

struct GivenLevels : IGatherLevels
{
    int levels() { return int(levelFloats[0]); }
    ProbeCascade cascadeOf(int level)
    {
        int at = 1 + level * 7;
        ProbeCascade cascade;
        cascade.corner = float3(levelFloats[at], levelFloats[at + 1], levelFloats[at + 2]);
        cascade.spacing = levelFloats[at + 3];
        cascade.count = int3(int(levelFloats[at + 4]), int(levelFloats[at + 5]), int(levelFloats[at + 6]));
        cascade.texels = 8u;
        cascade.start = 0.0;
        cascade.end = 0.0;
        cascade.stepCap = 0u;
        return cascade;
    }
    float3 lightAt(int level, int3 probe, uint2 texel) { return float3(0.0); }
    float reachAt(int level, int3 probe, uint2 texel) { return 0.0; }
    float3 placeOf(int level, int3 probe) { return probePlace(cascadeOf(level), probe); }
};

[shader("compute")]
[numthreads(1, 1, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    GivenLevels levels;
    float3 p = float3(points[id.x * 3u], points[id.x * 3u + 1u], points[id.x * 3u + 2u]);
    chosen[id.x * 2u] = uint(gatheredLevel(levels, p, GatherRim));
    chosen[id.x * 2u + 1u] = uint(gatheredLevel(levels, p, 0.0));
}
