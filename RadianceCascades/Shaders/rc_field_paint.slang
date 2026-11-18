// The color and the gloss a point on a surface reads from the cells of the scene's distance field
// about it, as a ray's hit takes them, from the paint each cell keeps of its nearest surface.
module rc_field_paint;

import rc_field_code;

// The cells of one cascade of the field as a reader reads them, which an engine implements over
// its own images.
public interface IFieldCells
{
    // The cells along each side of the cascade.
    int size();

    // The least corner of the cascade's least cell, in the world.
    float3 corner();

    // The width of a cell.
    float cell();

    // A cell's paint, its nearest surface's color times the share of the cell it painted, with the
    // share in alpha, 0 where no surface painted it.
    float4 paint(int3 at);

    // A cell's face, its nearest surface's way in eight bits (wayCode) with its gloss above them
    // (glossBits).
    uint face(int3 at);

    // A cell's distance as the resolve made it, below zero inside.
    float distance(int3 at);
}

// A face's gloss in the bits above its way, its perceptual roughness in bits 8 to 15, its metallic
// share in bits 16 to 23 and its reflectance in bits 24 to 31, each in 255ths, the roughness held
// to a 255th at least so a byte of 0 says no surface's gloss painted the cell.
public uint glossBits(float roughness, float metallic, float reflectance)
{
    uint r = max(uint(saturate(roughness) * 255.0 + 0.5), 1u);
    uint m = uint(saturate(metallic) * 255.0 + 0.5);
    uint f = uint(saturate(reflectance) * 255.0 + 0.5);
    return (r << 8) | (m << 16) | (f << 24);
}

// The gloss glossBits wrote, its roughness, metallic share and reflectance, or a material's default
// of a roughness of a half, no metal and a reflectance of a half where no gloss painted the cell.
public float3 glossOf(uint bits)
{
    uint r = (bits >> 8) & 255u;
    return r != 0u ? float3(float(r), float((bits >> 16) & 255u), float(bits >> 24)) / 255.0 : float3(0.5, 0.0, 0.5);
}

// The color of the field at p, the eight cells about it blended and divided by how much of them a
// surface painted, so a cell no surface painted lends nothing, or gray where none about p is.
public float3 paintAt<F : IFieldCells>(F field, float3 p)
{
    int size = field.size();
    float3 q = clamp((p - field.corner()) / field.cell(), 0.5, float(size) - 0.5) - 0.5;
    int3 base = int3(floor(q));
    float3 f = q - float3(base);
    float4 sum = float4(0.0);
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        sum += field.paint(min(base + offset, int3(size - 1))) * (lerps.x * lerps.y * lerps.z);
    }
    return sum.a > 0.05 ? sum.rgb / sum.a : float3(0.5);
}

// How far a cell's own surface may pass from a point, in cells, for the cell's paint to count at
// it, short of a second surface of the same way four tenths of a cell off. A face's way read back
// from its eight bits leans up to 14 degrees from it, so across the cells about a point a surface
// held through it may pass up to a third of a cell off at the coarsest ways, and such a cell is
// left out, the cells that face the point's way lending the color where none holds a surface
// through it.
public static const float PaintReach = 0.3;

// The color of the field at p on a surface facing n, with in gloss the gloss of the same cells
// blended the same way, the eight cells about p each kept where the face whose color it holds looks
// the hit's way, within 60 degrees of n, and the surface it holds passes within PaintReach of p,
// that surface lying the cell's distance back from its middle along its face's way. Where none
// holds a surface through p the cells that face its way are blended, and where none faces it
// paintAt's blend, as at a thin edge, so the hit is not left dark. A cell on a corner's diagonal
// lies as far from either wall and holds whichever painted it, so a red wall's hit within a cell of
// the white wall it meets would blend the white in but for the facing, and a floor a tenth of a
// unit over another of another color, the cells between them nearer the lower and facing the same
// way, would blend the lower's color in but for the surface each cell holds.
public float3 paintFacing<F : IFieldCells>(F field, float3 p, float3 n, out float3 gloss)
{
    gloss = float3(0.5, 0.0, 0.5);
    int size = field.size();
    float3 q = clamp((p - field.corner()) / field.cell(), 0.5, float(size) - 0.5) - 0.5;
    int3 base = int3(floor(q));
    float3 f = q - float3(base);
    float4 held = float4(0.0), faced = float4(0.0);
    float3 heldGloss = float3(0.0), facedGloss = float3(0.0);
    for (int corner = 0; corner < 8; corner++)
    {
        int3 offset = int3(corner & 1, (corner >> 1) & 1, corner >> 2);
        int3 at = min(base + offset, int3(size - 1));
        float3 lerps = lerp(1.0 - f, f, float3(offset));
        uint bits = field.face(at);
        float3 way = wayOf(bits & 255u);
        if (dot(way, n) < 0.5)
            continue;
        float4 paint = field.paint(at) * (lerps.x * lerps.y * lerps.z);
        faced += paint;
        facedGloss += glossOf(bits) * paint.a;
        float3 middle = field.corner() + (float3(at) + 0.5) * field.cell();
        if (abs(dot(p - middle, way) + field.distance(at)) > PaintReach * field.cell())
            continue;
        held += paint;
        heldGloss += glossOf(bits) * paint.a;
    }
    if (held.a > 0.01)
    {
        gloss = heldGloss / held.a;
        return held.rgb / held.a;
    }
    if (faced.a > 0.01)
    {
        gloss = facedGloss / faced.a;
        return faced.rgb / faced.a;
    }
    return paintAt(field, p);
}
