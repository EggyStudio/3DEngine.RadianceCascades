// The distances field_code_cpu.slang resolved, each cell held inside where it is pinched between
// two blocks that meet along an edge, a thread a cell.
import rc_field_code;

RWStructuredBuffer<float> distances;

// For each cell, its distance after the pinch.
RWStructuredBuffer<float> pinchedDistances;

static const int Size = 32;

// The resolved cells as the pinch reads them.
struct Resolved : IResolvedCells
{
    int size() { return Size; }
    float resolvedAt(int3 at) { return distances[(at.z * Size + at.y) * Size + at.x]; }
};

[shader("compute")]
[numthreads(4, 4, 4)]
void test(uint3 id : SV_DispatchThreadID)
{
    Resolved cells;
    float d = cells.resolvedAt(int3(id));
    pinchedDistances[(id.z * Size + id.y) * Size + id.x] = d > 0.0 && pinched(cells, int3(id)) ? -d : d;
}
