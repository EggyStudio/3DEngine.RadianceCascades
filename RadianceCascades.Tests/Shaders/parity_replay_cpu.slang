// A texel's trace or merge run again from a log a binding recorded on its device, every call the
// passes make through the shapes they take answered by the logged call in its place, so the passes
// on the CPU and the device part only where the calls part. Each call's kind and arguments are held
// against the logged one's, the first that differs is kept, and the light the pass keeps is set
// beside the device's by the test. A thread replays one texel.
import rc_probe_passes;

// Setup, as floats. The cascade the pass runs over at 0, the cascade above at 11, each as its
// corner, spacing, count, texels, start, end and cap of steps, the march's end at 22, the field's
// cells a side at 23, each field cascade's corner and cell at 24, four floats each, and at 44 the
// difference a float argument may lie from the logged one's, over the larger of 1 and its size.
RWStructuredBuffer<float> setup;

// The calls logged for the texel's pass, 16 uints each. The first holds the kind in its lowest
// byte, the next 11 the arguments, ints as they are and floats as their bits, a vector's components
// one a uint, and the last 4 the result.
RWStructuredBuffer<uint> calls;

// The replay's state. The next call's place in the log at 0, at 1 the first call that differs
// plus one, the place in the call of what differs at 2 (0 its kind), the replay's value there at 3
// and the log's at 4, the calls past the log's end at 5, at 6 the pass (0 the trace, 1 the merge),
// at 7 the texel's index and at 8 the count of calls logged.
RWStructuredBuffer<uint> state;

// The ray rayAt gives at 0 (ended, t, carried and light), the ray keepRay takes at 6, the merged
// light at 12, its sky share at 15, the merge's marched at 16, and at 17 the largest difference of
// a float argument from the logged one's.
RWStructuredBuffer<float> texel;

uint logged() { return state[8]; }

// Keeps the first difference from the log, the call, the part and the two values.
void differsAt(uint at, uint part, uint mine, uint theirs)
{
    if (state[1] != 0u)
        return;
    state[1] = at + 1u;
    state[2] = part;
    state[3] = mine;
    state[4] = theirs;
}

// The place in the log of the call the replay makes, of kind, counted past the log's end where
// the replay calls more than the device did.
uint nextCall(uint kind)
{
    uint at = state[0];
    state[0] = at + 1u;
    if (at >= logged())
    {
        state[5] = state[5] + 1u;
        return at;
    }
    uint theirs = calls[at * 16u] & 255u;
    if (theirs != kind)
        differsAt(at, 0u, kind, theirs);
    return at;
}

void sameInt(uint at, uint part, int value)
{
    if (at < logged() && calls[at * 16u + part] != uint(value))
        differsAt(at, part, uint(value), calls[at * 16u + part]);
}

void sameInt3(uint at, uint part, int3 value)
{
    sameInt(at, part, value.x);
    sameInt(at, part + 1u, value.y);
    sameInt(at, part + 2u, value.z);
}

void sameFloat(uint at, uint part, float value)
{
    if (at >= logged())
        return;
    float theirs = asfloat(calls[at * 16u + part]);
    float apart = abs(theirs - value);
    texel[17] = max(texel[17], apart);
    if (!(apart <= setup[44] * max(1.0, abs(theirs))))
        differsAt(at, part, asuint(value), calls[at * 16u + part]);
}

void sameFloat3(uint at, uint part, float3 value)
{
    sameFloat(at, part, value.x);
    sameFloat(at, part + 1u, value.y);
    sameFloat(at, part + 2u, value.z);
}

float answer(uint at, uint part) { return at < logged() ? asfloat(calls[at * 16u + 12u + part]) : 0.0; }
float3 answer3(uint at) { return float3(answer(at, 0u), answer(at, 1u), answer(at, 2u)); }
float4 answer4(uint at) { return float4(answer(at, 0u), answer(at, 1u), answer(at, 2u), answer(at, 3u)); }

// The field the passes trace and merge through, every call but the field's layout answered from
// the log, its layout from the setup as the binding's header gives it.
struct LoggedField : IProbeField
{
    int cascadeOf(float3 p, float margin)
    {
        uint at = nextCall(6u);
        sameFloat3(at, 1u, p);
        sameFloat(at, 4u, margin);
        return at < logged() ? int(calls[at * 16u + 12u]) : -1;
    }
    float4 cornerAndCell(int cascade)
    {
        int i = 24 + cascade * 4;
        return float4(setup[i], setup[i + 1], setup[i + 2], setup[i + 3]);
    }
    int size() { return int(setup[23]); }
    float4 between(int cascade, float3 cells)
    {
        uint at = nextCall(5u);
        sameInt(at, 1u, cascade);
        sameFloat3(at, 2u, cells);
        return answer4(at);
    }
    float4 cellAt(int cascade, int3 cell)
    {
        uint at = nextCall(3u);
        sameInt(at, 1u, cascade);
        sameInt3(at, 2u, cell);
        return answer4(at);
    }
    float3 secondAt(int cascade, int3 cell)
    {
        uint at = nextCall(4u);
        sameInt(at, 1u, cascade);
        sameInt3(at, 2u, cell);
        return answer3(at);
    }
    float3 hitLight(float3 place, float3 way, int cascade, int3 cell)
    {
        uint at = nextCall(1u);
        sameFloat3(at, 1u, place);
        sameFloat3(at, 4u, way);
        sameInt(at, 7u, cascade);
        sameInt3(at, 8u, cell);
        return answer3(at);
    }
    float throughAt(int cascade, int3 cell)
    {
        uint at = nextCall(2u);
        sameInt(at, 1u, cascade);
        sameInt3(at, 2u, cell);
        return answer(at, 0u);
    }
}

// A cascade's probes' places, its own logged as one kind and the cascade above's as another.
struct LoggedPlaces : IProbePlaces
{
    uint kind;

    float3 placeOf(int3 probe)
    {
        uint at = nextCall(kind);
        sameInt3(at, 1u, probe);
        return answer3(at);
    }
}

// The cascade above's merged light, sky shares and reach.
struct LoggedAbove : IFarSkyProbes
{
    uint read(uint kind, int3 probe, uint2 at)
    {
        uint call = nextCall(kind);
        sameInt3(call, 1u, probe);
        sameInt(call, 4u, int(at.x));
        sameInt(call, 5u, int(at.y));
        return call;
    }
    float3 lightAt(int3 probe, uint2 at) { return answer3(read(7u, probe, at)); }
    float skyShareAt(int3 probe, uint2 at) { return answer(read(8u, probe, at), 0u); }
    float reachAt(int3 probe, uint2 at) { return answer(read(9u, probe, at), 0u); }
}

// The sky past the coarsest cascade's end.
struct LoggedSky : ISkyShare
{
    float3 skyAlong(float3 way)
    {
        uint at = nextCall(11u);
        sameFloat3(at, 1u, way);
        return answer3(at);
    }
    float shareAlong(float3 way)
    {
        uint at = nextCall(12u);
        sameFloat3(at, 1u, way);
        return answer(at, 0u);
    }
}

// The texel's ray as the device kept it and the ray the replay keeps.
struct LoggedRays : IProbeRays
{
    ProbeRay rayAt(uint index)
    {
        ProbeRay ray;
        ray.ended = uint(texel[0]);
        ray.t = texel[1];
        ray.carried = texel[2];
        ray.light = float3(texel[3], texel[4], texel[5]);
        return ray;
    }
    void keepRay(uint index, ProbeRay ray)
    {
        texel[6] = float(ray.ended);
        texel[7] = ray.t;
        texel[8] = ray.carried;
        texel[9] = ray.light.x;
        texel[10] = ray.light.y;
        texel[11] = ray.light.z;
    }
}

// The light the replay's merge keeps.
struct LoggedMerged : IMergedSkyLight
{
    void keepMerged(uint index, float3 light)
    {
        texel[12] = light.x;
        texel[13] = light.y;
        texel[14] = light.z;
    }
    void keepSkyShare(uint index, float share) { texel[15] = share; }
}

ProbeCascade cascadeAt(int i)
{
    ProbeCascade cascade;
    cascade.corner = float3(setup[i], setup[i + 1], setup[i + 2]);
    cascade.spacing = setup[i + 3];
    cascade.count = int3(int(setup[i + 4]), int(setup[i + 5]), int(setup[i + 6]));
    cascade.texels = uint(setup[i + 7]);
    cascade.start = setup[i + 8];
    cascade.end = setup[i + 9];
    cascade.stepCap = uint(setup[i + 10]);
    return cascade;
}

[shader("compute")]
[numthreads(1, 1, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    LoggedField field;
    LoggedRays rays;
    LoggedPlaces places = { 10u };
    uint index = state[7];
    if (state[6] == 0u)
    {
        traceTexel(field, rays, places, cascadeAt(0), index);
        return;
    }
    LoggedAbove above;
    LoggedPlaces abovePlaces = { 13u };
    LoggedMerged merged;
    LoggedSky sky;
    NoSteps steps;
    bool marched = mergeTexel(field, rays, places, above, abovePlaces, merged, cascadeAt(0), cascadeAt(11), setup[22], sky, steps, index);
    texel[16] = marched ? 1.0 : 0.0;
}
