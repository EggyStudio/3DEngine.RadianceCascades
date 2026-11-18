// The splat of a triangle into one cascade of the scene's distance field, the codes it gives the
// cells its band covers and the paint it gives the cells that kept its code, each the work of one
// thread over a share of those cells, through an interface the engine implements over its own
// buffers and atomics. A GPU runs a workgroup a triangle, each thread a lane taking every lanes-th
// cell, and the engine's kernel calls these with its lane and its count of lanes.
module rc_field_splat;

__exported import rc_field_code;
__exported import rc_field_paint;

// One cascade's cells as the splat keeps their codes, which an engine implements over its buffers,
// each keep an atomic minimum where threads of other triangles write the same cell.
public interface ISplatCells
{
    // Keeps the least of a cell's code and the one given.
    void keepLeast(uint index, uint code);

    // Keeps the least of the code of the face a cell lies straight behind and the one given, a face
    // the resolve reads for a wall thinner than a cell (resolvedDistance).
    void keepBehind(uint index, uint code);
}

// One cascade's cells as the paint reads their codes and writes their paint, which an engine
// implements over its buffers once every triangle has been splatted.
public interface IPaintCells
{
    // The code a cell kept once every triangle was splatted.
    uint kept(uint index);

    // Paints a cell its nearest surface's color times the paint's alpha, with the alpha, the
    // surface's emission and the gloss it keeps above its face's way (glossBits).
    void paint(uint index, float4 paint, float3 emission, uint gloss);
}

// A triangle of a mesh in the world, its corners, which of its edges no other triangle shares,
// ab, bc and ca, whether its mesh has no inside, and its instance, whose seven bits order a tie.
public struct SplatTriangle
{
    public float3 a;
    public float3 b;
    public float3 c;
    public bool3 openEdges;
    public bool doubleSided;
    public uint instance;
}

// The cascade a triangle is splatted into, the least corner of its least cell, a cell's width,
// its cells along each side, the band in cells, and the first and the last cell along each axis
// a build writes, every cell of the cascade or the cells of a region.
public struct SplatCascade
{
    public float3 corner;
    public float cell;
    public int size;
    public float band;
    public int3 first;
    public int3 last;
}

// What a triangle paints, its mesh's color in linear light, the colors of its corners a, b and c
// in linear light, which the paint blends at a cell's nearest point and multiplies the color by,
// the share of light that passes a unit of its depth, its emission and its gloss (glossBits).
public struct SplatSurface
{
    public float3 color;
    public float3x3 corners;
    public float lightThrough;
    public float3 emission;
    public uint gloss;
}

// The alpha a cell is painted with by a surface that lets through of the light pass a unit of its
// depth, its color painted times it, 1 for a solid surface down to a half for one that lets all of
// it through, so a reader that divides a blend of the colors by their alphas takes the colors back
// and weighs such a cell less.
public float lightThroughPaint(float through)
{
    return 1.0 - 0.5 * saturate(through);
}

// A color of three sRGB bytes, red lowest, as a triangle's corner keeps it, in linear light.
public float3 cornerColor(uint bytes)
{
    float3 c = float3(float(bytes & 255u), float((bytes >> 8) & 255u), float((bytes >> 16) & 255u)) / 255.0;
    return select(c <= 0.04045, c / 12.92, pow((c + 0.055) / 1.055, 2.4));
}

// How much of each of a triangle's corners a point on it is made of.
public float3 barycentric(float3 p, float3 a, float3 b, float3 c)
{
    float3 ab = b - a, ac = c - a, ap = p - a;
    float d00 = dot(ab, ab), d01 = dot(ab, ac), d11 = dot(ac, ac), d20 = dot(ap, ab), d21 = dot(ap, ac);
    float denominator = d00 * d11 - d01 * d01;
    if (denominator <= 1e-20)
        return float3(1.0, 0.0, 0.0);
    float v = saturate((d11 * d20 - d01 * d21) / denominator);
    float w = saturate((d00 * d21 - d01 * d20) / denominator);
    float3 weights = float3(max(1.0 - v - w, 0.0), v, w);
    return weights / max(weights.x + weights.y + weights.z, 1e-6);
}

// The cells a triangle comes within the band of, its bounds grown by the band, from first along
// each axis and span of them, held to the cells the build writes, or false where none is.
public bool bandCells(SplatTriangle t, SplatCascade cascade, out int3 first, out int3 span)
{
    float3 low = (min(t.a, min(t.b, t.c)) - cascade.corner) / cascade.cell - cascade.band;
    float3 high = (max(t.a, max(t.b, t.c)) - cascade.corner) / cascade.cell + cascade.band;
    first = max(int3(floor(low)), cascade.first);
    int3 last = min(int3(ceil(high)), cascade.last);
    span = last - first + 1;
    return all(last >= first);
}

// Splats a triangle's codes into the cells its band covers, the cells from lane on in steps of
// lanes, keeping each cell's least code and the least code of a face it lies straight behind. A
// triangle that holds no surface (holdsSurface) gives none.
public void splatCodes<C : ISplatCells>(C cells, SplatTriangle t, SplatCascade cascade, uint lane, uint lanes)
{
    if (!holdsSurface(t.a, t.b, t.c))
        return;
    int3 first, span;
    if (!bandCells(t, cascade, first, span))
        return;
    float reach = cascade.band * cascade.cell;
    uint count = uint(span.x * span.y * span.z);
    for (uint k = lane; k < count; k += lanes)
    {
        int3 at = first + int3(int(k % uint(span.x)), int((k / uint(span.x)) % uint(span.y)), int(k / uint(span.x * span.y)));
        float3 p = cascade.corner + (float3(at) + 0.5) * cascade.cell;
        float d;
        uint behind;
        uint code = cellCode(p, t.a, t.b, t.c, t.openEdges, t.doubleSided, t.instance, cascade.cell, d, behind);
        if (d >= reach)
            continue;
        uint index = uint((at.z * cascade.size + at.y) * cascade.size + at.x);
        cells.keepLeast(index, code);
        if (behind != EmptyCode)
            cells.keepBehind(index, behind);
    }
}

// Paints the cells whose code is the one this triangle gives them, its nearest surface, the cells
// from lane on in steps of lanes, each its surface's color times its corners' colors blended at
// the cell's nearest point, times the paint's alpha, with the alpha, its emission and its gloss.
// Two triangles give a cell one code only where they are alike in every bit, the instance's among
// them, so one alone paints it.
public void splatPaint<C : IPaintCells>(C cells, SplatTriangle t, SplatCascade cascade, SplatSurface surface, uint lane, uint lanes)
{
    if (!holdsSurface(t.a, t.b, t.c))
        return;
    int3 first, span;
    if (!bandCells(t, cascade, first, span))
        return;
    float reach = cascade.band * cascade.cell;
    float coverage = lightThroughPaint(surface.lightThrough);
    uint count = uint(span.x * span.y * span.z);
    for (uint k = lane; k < count; k += lanes)
    {
        int3 at = first + int3(int(k % uint(span.x)), int((k / uint(span.x)) % uint(span.y)), int(k / uint(span.x * span.y)));
        float3 p = cascade.corner + (float3(at) + 0.5) * cascade.cell;
        float d;
        uint behind;
        uint code = cellCode(p, t.a, t.b, t.c, t.openEdges, t.doubleSided, t.instance, cascade.cell, d, behind);
        uint index = uint((at.z * cascade.size + at.y) * cascade.size + at.x);
        if (d >= reach || cells.kept(index) != code)
            continue;
        bool open;
        float3 q = nearestOnTriangle(p, t.a, t.b, t.c, t.openEdges, open);
        float3 weights = barycentric(q, t.a, t.b, t.c);
        float3 painted = surface.color * mul(weights, surface.corners);
        cells.paint(index, float4(saturate(painted) * coverage, coverage), surface.emission, surface.gloss);
    }
}
