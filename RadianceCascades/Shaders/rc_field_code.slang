// What a cell of the scene's distance field keeps of the triangles near it and what it resolves to.
// The splat gives each cell within the band of a triangle a code, a uint ordered so the least any
// triangle gives is the one the cell keeps by an atomic minimum, and the resolve turns the code
// kept, with the code of the face the cell lies straight behind, into the distance a reader
// marches, below zero inside a mesh.
module rc_field_code;

// The code's fields, from its highest bits down. The distance from the cell's middle to the
// triangle's nearest point, in 1024ths of a cell, lies above this bit.
public static const uint DistanceShift = 19;

// Set where a double-sided face's nearest point to the cell lies off its plane's inside, on its
// rim or a corner of it, where its plane would stand past the face.
public static const uint Rim = 0x40000u;

// Set where the face is double-sided, a sheet with no inside, held at its distance with its plane.
public static const uint DoubleSided = 0x20000u;

// Set where the cell lies in front of a face, or on the side of a double-sided face's plane its
// face looks toward. Clear sorts first, so of two faces as near, one the cell lies behind wins,
// the cell being inside some mesh.
public static const uint Front = 0x10000u;

// The face's way in eight bits (wayCode) lies above this bit, and the instance's seven bits below
// it, so of two meshes' faces alike in all but the instance, as two boxes' sides in one plane, the
// same one keeps the cell in every run, whichever thread writes first.
public static const uint WayShift = 8;
public static const uint InstanceMask = 127u;

// The code a cell no triangle comes within the band of keeps, which resolves to the band's distance
// and keeps no plane.
public static const uint EmptyCode = 0xFFFFFFFFu;

// How far beyond its offset a double-sided face's plane is kept in a cell, in cells, so the vector
// of its way and its offset is never of no length (sheetOf).
public static const float SheetLength = 2.0;

// The point of the triangle abc nearest p, as Ericson's Real-Time Collision Detection finds it by
// the region of the triangle's plane p falls in, and in open whether that point lies on an edge the
// triangle shares with no other, as openEdges says of ab, bc and ca, or on a corner of one.
public float3 nearestOnTriangle(float3 p, float3 a, float3 b, float3 c, bool3 openEdges, out bool open)
{
    float3 ab = b - a, ac = c - a, ap = p - a;
    float d1 = dot(ab, ap), d2 = dot(ac, ap);
    open = openEdges.x || openEdges.z;
    if (d1 <= 0.0 && d2 <= 0.0)
        return a;
    float3 bp = p - b;
    float d3 = dot(ab, bp), d4 = dot(ac, bp);
    open = openEdges.x || openEdges.y;
    if (d3 >= 0.0 && d4 <= d3)
        return b;
    float vc = d1 * d4 - d3 * d2;
    open = openEdges.x;
    if (vc <= 0.0 && d1 >= 0.0 && d3 <= 0.0)
        return a + ab * (d1 / (d1 - d3));
    float3 cp = p - c;
    float d5 = dot(ab, cp), d6 = dot(ac, cp);
    open = openEdges.y || openEdges.z;
    if (d6 >= 0.0 && d5 <= d6)
        return c;
    float vb = d5 * d2 - d1 * d6;
    open = openEdges.z;
    if (vb <= 0.0 && d2 >= 0.0 && d6 <= 0.0)
        return a + ac * (d2 / (d2 - d6));
    float va = d3 * d6 - d5 * d4;
    open = openEdges.y;
    if (va <= 0.0 && (d4 - d3) >= 0.0 && (d5 - d6) >= 0.0)
        return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
    open = false;
    float denominator = 1.0 / (va + vb + vc);
    return a + ab * (vb * denominator) + ac * (vc * denominator);
}

// A face's way in eight bits, four along each side of the octahedron the sphere is folded onto,
// enough to tell faces that look apart from faces that meet square.
public uint wayCode(float3 n)
{
    n /= abs(n.x) + abs(n.y) + abs(n.z);
    float2 o = n.z >= 0.0 ? n.xy : (1.0 - abs(n.yx)) * float2(n.x >= 0.0 ? 1.0 : -1.0, n.y >= 0.0 ? 1.0 : -1.0);
    uint2 q = min(uint2((o * 0.5 + 0.5) * 16.0), uint2(15u));
    return q.x | (q.y << 4);
}

// The way the eight bits of wayCode stand for, the middle of their square of the octahedron.
public float3 wayOf(uint bits)
{
    float2 o = (float2(float(bits & 15u), float((bits >> 4) & 15u)) + 0.5) / 16.0 * 2.0 - 1.0;
    float3 n = float3(o, 1.0 - abs(o.x) - abs(o.y));
    if (n.z < 0.0)
        n.xy = (1.0 - abs(n.yx)) * float2(n.x >= 0.0 ? 1.0 : -1.0, n.y >= 0.0 ? 1.0 : -1.0);
    return normalize(n);
}

// Whether the triangle abc holds a surface. One of next to no area beside its longest side, as a
// sphere's last row has at a pole whose corners the rounding of sin(pi) leaves apart, has a face
// turned whichever way that rounding took it, which at a distance the triangles about it share
// would say cells outside lie behind it, and is passed over.
public bool holdsSurface(float3 a, float3 b, float3 c)
{
    float3 face = cross(b - a, c - a);
    float longest = max(dot(b - a, b - a), max(dot(c - b, c - b), dot(a - c, a - c)));
    return dot(face, face) > 1e-8 * longest * longest;
}

// The code the cell whose middle is p, cell wide, takes of the triangle abc of the instance given,
// its corners in the world, with in distance how far the cell lies from the triangle, which a cell
// past the band takes nothing at, and in behind the code of the face the cell lies straight behind,
// its distance in 1024ths of a cell above its way's eight bits, or EmptyCode where it does not.
public uint cellCode(float3 p, float3 a, float3 b, float3 c, bool3 openEdges, bool doubleSided, uint instance, float cell,
    out float distance, out uint behind)
{
    float3 face = cross(b - a, c - a);
    float faceLength = length(face);
    bool sided = !doubleSided && dot(face, face) > 1e-20;
    uint way = dot(face, face) > 1e-20 ? wayCode(face) : 0u;
    bool open;
    float3 q = nearestOnTriangle(p, a, b, c, openEdges, open);
    float d = length(p - q);
    distance = d;
    // A cell lies behind a face only where the way from the face's nearest point to it lies within
    // 60 degrees of straight back, so where an edge or a corner is nearest a face the way runs
    // along does not put the cell inside, and one whose nearest point lies on an open edge, as a
    // ground plane's rim, is in front. A double-sided face's front bit is the side of its plane the
    // cell lies on, and its rim bit is set where the way from its nearest point leaves the plane's
    // normal.
    uint front = sided ? (!open && dot(p - q, face) < -0.5 * d * faceLength ? 0u : 1u) : (dot(p - a, face) >= 0.0 ? 1u : 0u);
    uint rim = !sided && abs(dot(p - q, face)) < 0.999 * d * faceLength ? 1u : 0u;
    uint tenths = uint(d / cell * 1024.0);
    // Straight behind, its nearest point inside the face and not on an edge, as a wall's far face
    // lies behind a cell by its near one, which the resolve reads for a wall thinner than a cell.
    behind = sided && front == 0u && dot(p - q, face) < -0.999 * d * faceLength ? (tenths << WayShift) | way : EmptyCode;
    return (tenths << DistanceShift) | (rim * Rim) | (sided ? 0u : DoubleSided) | (front * Front) | (way << WayShift) | (instance & InstanceMask);
}

// The distance a cell's code resolves to, cell wide, held to band cells, from the least code the
// cell kept and the least code of a face it lies straight behind. A double-sided face is held at
// its distance, its plane kept beside it (sheetOf). A cell in front of one face and straight behind
// another that looks the other way, less than a cell beyond it, lies against a wall thinner than a
// cell, held as a sheet is at thinHold either side of the wall's middle, so a ray between the cells
// on either side meets it, where the faces of a box's edge, which meet square, do not make one. A
// cell behind its nearest face lies inside and is below zero.
public float resolvedDistance(uint code, uint behind, float cell, float band, float thinHold)
{
    float d = min(float(code >> DistanceShift) / 1024.0, band) * cell;
    float beyond = float(behind >> WayShift) / 1024.0 * cell;
    bool thin = (code & (DoubleSided | Front)) == Front && behind != EmptyCode && beyond - d < cell
        && dot(wayOf(code >> WayShift), wayOf(behind)) < -0.7;
    if ((code & DoubleSided) != 0u)
        return d;
    if (thin)
        return (d + beyond) * 0.5 - thinHold;
    return (code & Front) == 0u ? -d : d;
}

// The distances of a cascade's cells as the resolve made them, which the pinch reads about a cell,
// an engine's over its own images or the codes it kept.
public interface IResolvedCells
{
    // The cells along each side of the cascade.
    int size();

    // A cell's distance, below zero inside.
    float resolvedAt(int3 at);
}

// The axis of an index, made from it and kept in no array, since an array a function keeps lies in
// a thread's own memory, which one device reads wrong (README, Binding it in your engine).
int3 axisOf(int a)
{
    return int3(a == 0 ? 1 : 0, a == 1 ? 1 : 0, a == 2 ? 1 : 0);
}

// Whether a cell outside every surface lies between two cells beside it along two axes that lie
// inside one while the cell across the diagonal from it lies outside, the pattern two blocks that
// meet along an edge leave in the cells about it, which no single face makes. Such a cell is held
// inside at its distance, so a march through the edge between the two blocks meets the field there,
// where between the four cells about the edge the distance never falls below zero, and a march
// from the floor on one side of a wall whose halves meet along one edge would see part of a lamp on
// the other side and light the floor about the edge, which no light reaches.
public bool pinched<R : IResolvedCells>(R cells, int3 c)
{
    int size = cells.size();
    [loop]
    for (int a = 0; a < 3; a++)
        [loop]
        for (int b = a + 1; b < 3; b++)
            [loop]
            for (int signs = 0; signs < 4; signs++)
            {
                int3 one = c + axisOf(a) * ((signs & 1) != 0 ? 1 : -1);
                int3 other = c + axisOf(b) * ((signs & 2) != 0 ? 1 : -1);
                int3 across = one + other - c;
                if (any(min(min(one, other), across) < 0) || any(max(max(one, other), across) >= size))
                    continue;
                if (cells.resolvedAt(one) < 0.0 && cells.resolvedAt(other) < 0.0 && cells.resolvedAt(across) > 0.0)
                    return true;
            }
    return false;
}

// The plane of the double-sided face a code is of, where its nearest point lies on the face's
// inside, its way times two cells more than the plane's offset from the cell's middle along it, in
// cells, or nothing where the face is not double-sided, is nearest at its rim, past which the plane
// would stand where no face does, or lies a cell or more off, where a march within the cell comes
// no nearer it than half a cell. A march through the field meets the plane where it passes the
// cell, from either side, wherever the sheet lies between the cells' middles.
public float3 sheetOf(uint code)
{
    float offset = float(code >> DistanceShift) / 1024.0;
    if ((code & (Rim | DoubleSided)) != DoubleSided || offset >= 1.0)
        return float3(0.0);
    return wayOf(code >> WayShift) * (((code & Front) != 0u ? -offset : offset) + SheetLength);
}
