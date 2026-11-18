// A probe's directions laid out on an octahedron folded flat into a square, the rays a texel of
// that square sends, and the mark a texel's rays leave for the passes that read them. Both engines
// lay their probes' texels out by these functions, so a texel means one direction in either.
module rc_octahedron;

// A unit direction as a point of the square from 0 to 1, the octahedron folded flat, which lays
// every direction on a square with texels of nearly even solid angle.
public float2 octahedron(float3 d)
{
    d /= abs(d.x) + abs(d.y) + abs(d.z);
    float2 p = d.xy;
    if (d.z < 0.0)
        p = (1.0 - abs(p.yx)) * float2(p.x >= 0.0 ? 1.0 : -1.0, p.y >= 0.0 ? 1.0 : -1.0);
    return p * 0.5 + 0.5;
}

// The point of the octahedron a point of the square stands for, not yet a direction.
public float3 octahedronPoint(float2 uv)
{
    float2 p = uv * 2.0 - 1.0;
    float3 d = float3(p, 1.0 - abs(p.x) - abs(p.y));
    if (d.z < 0.0)
        d.xy = (1.0 - abs(d.yx)) * float2(d.x >= 0.0 ? 1.0 : -1.0, d.y >= 0.0 ? 1.0 : -1.0);
    return d;
}

// The direction a point of the square stands for.
public float3 fromOctahedron(float2 uv)
{
    return normalize(octahedronPoint(uv));
}

// How much of the sphere a texel of the square spans at `uv`, against the others, one over the cube
// of its octahedron point's distance from the middle, as a patch of the octahedron there covers its
// area over that cube. The texels near the octahedron's corners span the least, those at the
// middles of its faces the most, near three times as much.
public float octahedronShare(float2 uv)
{
    float l = length(octahedronPoint(uv));
    return 1.0 / (l * l * l);
}

// The rays a texel of a cascade's octahedron of `texels` along each side sends, four where it has
// 8, one from the middle of each quarter of the texel, and one from its middle where it has 16, so
// every probe sends 256. The count follows the texels and not the cascade, since a texel of 8 a
// side spans a sixty-fourth of the sphere, which one ray takes a bright spot or misses it for,
// whichever cascade it is.
public uint RaysAt(uint texels)
{
    return texels <= 8u ? 4u : 1u;
}

// A texel's mark as the trace writes it for `rays` rays, `nothing` of which met nothing, a part of
// one for a ray that crossed surfaces letting light through and met nothing past them. The mark
// says what most of the rays met, 0 a surface, a half a surface before their interval began, a
// quarter the sky and 1 nothing, moved a sixteenth on for each that met nothing, up from 0 or a
// half and down from 1 for each that met something, so it stays within the band each reader tells
// what a texel's rays met by.
public float texelMark(float most, float nothing, uint rays)
{
    return most >= 0.75 ? 1.0 - 0.0625 * (float(rays) - nothing) : most + 0.0625 * nothing;
}

// How many of a texel's `rays` rays met nothing, read back from the mark `texelMark` wrote.
public float texelNothing(float mark, uint rays)
{
    if (mark >= 0.75)
        return float(rays) - (1.0 - mark) / 0.0625;
    if (mark >= 0.375)
        return (mark - 0.5) / 0.0625;
    return mark >= 0.1875 ? 0.0 : mark / 0.0625;
}
