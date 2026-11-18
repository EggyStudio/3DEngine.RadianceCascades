// Every way of a square of 256 by 256 points over the octahedron written in eight bits and read
// back, each thread writing the angle in degrees between the way and the one its bits stand for.
import rc_field_code;
import rc_octahedron;

RWStructuredBuffer<float> degrees;

[shader("compute")]
[numthreads(16, 16, 1)]
void test(uint3 id : SV_DispatchThreadID)
{
    float3 way = fromOctahedron((float2(id.xy) + 0.5) / 256.0);
    float3 back = wayOf(wayCode(way));
    degrees[id.y * 256 + id.x] = acos(clamp(dot(way, back), -1.0, 1.0)) * 57.29578;
}
