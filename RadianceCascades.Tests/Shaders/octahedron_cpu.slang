// The octahedral map and the texels' marks run over a square of 16 by 16 texels, each thread
// writing how far its texel's middle comes back from the map and the way it stands for, and the
// first thread writing each mark's count read back, so a run on the CPU says what they compute.
import rc_octahedron;

// For each texel, the distance its middle lands from itself after the way it stands for is
// mapped back onto the square.
RWStructuredBuffer<float> missed;

// For each case of what most of a texel's rays met, its rays and how many met nothing, that
// count as texelNothing reads it back from texelMark's mark.
RWStructuredBuffer<float> nothing;

[shader("compute")]
[numthreads(16, 16, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    float2 uv = (float2(id.xy) + 0.5) / 16.0;
    missed[id.y * 16 + id.x] = length(octahedron(fromOctahedron(uv)) - uv);
    if (id.x != 0 || id.y != 0)
        return;
    // A surface and a surface before the interval with up to half of 4 rays meeting nothing, and
    // nothing with half of 4 rays or more, each mark staying within its band.
    int at = 0;
    for (int met = 0; met < 3; met++)
    {
        float most = met == 0 ? 0.0 : met == 1 ? 0.5 : 1.0;
        for (int none = 0; none <= 4; none++)
            nothing[at++] = texelNothing(texelMark(most, float(none), 4u), 4u);
    }
}
