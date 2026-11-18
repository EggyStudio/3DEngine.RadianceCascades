// Every function of rc_octahedron called once, so a compile of this kernel generates each of them
// for the target it is compiled to. Its entry is test, as every kernel's of the tests is, since the
// compile names it and Metal reserves main.
import rc_octahedron;

RWStructuredBuffer<float4> written;

[shader("compute")]
[numthreads(64, 1, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    float2 uv = (float2(id.xy) + 0.5) / 16.0;
    float2 back = octahedron(fromOctahedron(uv));
    float3 onOctahedron = octahedronPoint(uv);
    uint rays = RaysAt(id.x);
    float mark = texelMark(0.25, 1.0, rays);
    written[id.x] = float4(back, octahedronShare(uv) + onOctahedron.z, texelNothing(mark, rays));
}
