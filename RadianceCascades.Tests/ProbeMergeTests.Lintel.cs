using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A room open to one side under a lintel, read by a chain of four cascades on the CPU, where a
/// probe's rays toward the opening end short of the lintel and the far probes about their ends
/// stand below it, against the light its face takes traced against the room's boxes.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // The room's boxes in meters, a floor slab running far out past the opening, the roof, the
    // lintel over the opening at -x, the back wall and the side walls, every surface opaque. The
    // room is open to -x from the floor up to the lintel's foot at 2.
    private static readonly (Vector3 Low, Vector3 High)[] LintelRoom =
    [
        (new(-24, -0.5f, -12), new(10, 0, 12)),
        (new(-6.5f, 4, -3.5f), new(10.5f, 4.5f, 3.5f)),
        (new(-6.5f, 2, -3.5f), new(-6, 4, 3.5f)),
        (new(10, 0, -3.5f), new(10.5f, 4, 3.5f)),
        (new(-6.5f, 0, -4), new(10.5f, 4, -3.5f)),
        (new(-6.5f, 0, 3.5f), new(10.5f, 4, 4)),
    ];

    private const float LintelAlbedo = 0.352f;

    // The luminance of sRGB 200, 220, 255, the sky's light along every way.
    private const float LintelSky = 0.707f;

    // The field's four cascades of 64 cells about (-2, 1.5, 0), each its least corner and cell.
    private static readonly Vector4[] LintelField = new[] { 0.125f, 0.25f, 0.5f, 1f }
        .Select(cell => new Vector4(new Vector3(-2, 1.5f, 0) - new Vector3(32 * cell), cell)).ToArray();

    // The probes, 8 along each axis a level, each standing in the middle of its cell of the
    // level's grid, the finest two traced at 16 texels a side and read at 8, their 2 by 2 means.
    private static readonly Level[] LintelLevels =
    [
        new(new(-5.5f, -1.5f, -3.5f), 1, 8, 8, 8, 16, 2, 8),
        new(new(-9, -5, -7), 2, 8, 8, 8, 16, 4, 8),
        new(new(-14, -14, -14), 4, 8, 8, 8, 16, 8),
        new(new(-28, -28, -28), 8, 8, 8, 8, 16, 110.85f),
    ];

    private static readonly Vector3 UnderLintel = new(0.5f, 1.5f, 0.5f);

    // The cells of the field's cascades one after another, and the light a hit takes in each, the
    // lit light of the surface nearest the cell, its albedo over pi times the sky's irradiance
    // there, read against the boxes, for each cell within a cell of a surface.
    private static (float[] Cells, float[] Light) LintelScene()
    {
        var triangles = LintelRoom.SelectMany((box, i) => Cuboid(box.Low, box.High, i + 1)).ToList();
        const int each = Size * Size * Size;
        var cells = new float[5 * each * LintelField.Length];
        var light = new float[each * LintelField.Length];
        for (int c = 0; c < LintelField.Length; c++)
        {
            var one = FieldCascade(triangles, new(LintelField[c].X, LintelField[c].Y, LintelField[c].Z), LintelField[c].W);
            one.CopyTo(cells, 5 * each * c);
            var corner = new Vector3(LintelField[c].X, LintelField[c].Y, LintelField[c].Z);
            var cell = LintelField[c].W;
            Parallel.For(0, each, i =>
            {
                if (MathF.Abs(one[i * 5]) > cell)
                    return;
                var p = corner + (new Vector3(i % Size, i / Size % Size, i / (Size * Size)) + new Vector3(0.5f)) * cell;
                var (q, n) = NearestSurface(p);
                light[each * c + i] = LintelAlbedo / MathF.PI * Irradiance(q + n * 1e-3f, n, 16);
            });
        }
        return (cells, light);
    }

    // The point of the boxes' surfaces nearest p and the way the surface faces there, the face of
    // the box p lies in that it is nearest where it lies in one.
    private static (Vector3 Point, Vector3 Normal) NearestSurface(Vector3 p)
    {
        var (outside, inside) = (float.MaxValue, float.MaxValue);
        var (point, normal, innerPoint, innerNormal) = (p, Vector3.UnitY, p, Vector3.UnitY);
        foreach (var (low, high) in LintelRoom)
        {
            if (p.X > low.X && p.X < high.X && p.Y > low.Y && p.Y < high.Y && p.Z > low.Z && p.Z < high.Z)
            {
                for (int axis = 0; axis < 3; axis++)
                    foreach (var (face, sign) in new[] { (low[axis], -1f), (high[axis], 1f) })
                        if (MathF.Abs(p[axis] - face) < inside)
                        {
                            (inside, innerPoint, innerNormal) = (MathF.Abs(p[axis] - face), p, Vector3.Zero);
                            innerPoint[axis] = face;
                            innerNormal[axis] = sign;
                        }
                continue;
            }
            var nearest = Vector3.Clamp(p, low, high);
            var off = p - nearest;
            if (off.Length() < outside)
            {
                (outside, point, normal) = (off.Length(), nearest, Vector3.Zero);
                if (off.Length() > 1e-6f)
                {
                    var axisOf = MathF.Abs(off.X) >= MathF.Abs(off.Y) && MathF.Abs(off.X) >= MathF.Abs(off.Z) ? 0 : MathF.Abs(off.Y) >= MathF.Abs(off.Z) ? 1 : 2;
                    normal[axisOf] = MathF.Sign(off[axisOf]);
                }
                else
                {
                    // A point on a face, which no offset says the way of, as a cell's middle on
                    // a slab's top, takes the face it lies on.
                    for (int axis = 2; axis >= 0; axis--)
                        if (p[axis] == high[axis] || p[axis] == low[axis])
                            (normal, normal[axis]) = (Vector3.Zero, p[axis] == high[axis] ? 1 : -1);
                }
            }
        }
        return inside < float.MaxValue ? (innerPoint, innerNormal) : (point, normal);
    }

    // Where a ray from origin along way first meets a box, and the way the face it meets looks, or
    // no meeting, a ray from inside a box meeting it at once.
    private static bool MeetsBox(Vector3 origin, Vector3 way, out float at, out Vector3 normal)
    {
        (at, normal) = (float.MaxValue, Vector3.Zero);
        foreach (var (low, high) in LintelRoom)
        {
            var (near, far, axisNear, signNear) = (float.MinValue, float.MaxValue, 0, 0f);
            for (int axis = 0; axis < 3; axis++)
            {
                if (MathF.Abs(way[axis]) < 1e-9f)
                {
                    if (origin[axis] < low[axis] || origin[axis] > high[axis])
                        far = float.MinValue;
                    continue;
                }
                var (a, b) = ((low[axis] - origin[axis]) / way[axis], (high[axis] - origin[axis]) / way[axis]);
                var sign = way[axis] > 0 ? -1f : 1f;
                if (a > b)
                    (a, b) = (b, a);
                if (a > near)
                    (near, axisNear, signNear) = (a, axis, sign);
                far = MathF.Min(far, b);
            }
            if (far < MathF.Max(near, 0) || near >= at)
                continue;
            at = MathF.Max(near, 0);
            normal = Vector3.Zero;
            normal[axisNear] = signNear;
        }
        return at < float.MaxValue;
    }

    // The sky's irradiance at a point facing normal, its light times the cosine over the ways the
    // boxes leave open, summed over a side by side grid of ways spread by their cosine.
    private static float Irradiance(Vector3 point, Vector3 normal, int side)
    {
        var (u, v) = Basis(normal);
        var open = 0;
        for (int i = 0; i < side; i++)
            for (int j = 0; j < side; j++)
            {
                var way = CosineWay(normal, u, v, (i + 0.5f) / side, (j + 0.5f) / side);
                if (!MeetsBox(point, way, out _, out _))
                    open++;
            }
        return LintelSky * MathF.PI * open / (side * side);
    }

    private static (Vector3 U, Vector3 V) Basis(Vector3 normal)
    {
        var u = Vector3.Normalize(Vector3.Cross(MathF.Abs(normal.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX, normal));
        return (u, Vector3.Cross(normal, u));
    }

    private static Vector3 CosineWay(Vector3 normal, Vector3 u, Vector3 v, float a, float b)
    {
        var (r, phi) = (MathF.Sqrt(a), 2 * MathF.PI * b);
        return Vector3.Normalize(u * (r * MathF.Cos(phi)) + v * (r * MathF.Sin(phi)) + normal * MathF.Sqrt(1 - a));
    }

    // Where the test's rule moves a probe that stands within a quarter of a meter of a surface, a
    // quarter of a meter off it along the surface's way, as the passes' kernel moves it by the
    // field's normal.
    private static Vector3 MovedOff(Vector3 p)
    {
        var (q, n) = NearestSurface(p);
        var off = Vector3.Dot(p - q, n);
        return off < Margin ? p + n * (Margin - off) : p;
    }

    // The light a point facing normal takes, traced against the boxes, the sky's along each way the
    // boxes leave open and the lit light of the surface each other way meets, over 64 by 64 ways
    // spread by their cosine and 16 by 16 at each surface met.
    private static float TracedAgainstBoxes(Vector3 point, Vector3 normal)
    {
        var (u, v) = Basis(normal);
        const int side = 64;
        var rows = new double[side];
        Parallel.For(0, side, i =>
        {
            for (int j = 0; j < side; j++)
            {
                var way = CosineWay(normal, u, v, (i + 0.5f) / side, (j + 0.5f) / side);
                rows[i] += MeetsBox(point, way, out var at, out var face)
                    ? LintelAlbedo / MathF.PI * Irradiance(point + way * at + face * 1e-3f, face, 16)
                    : LintelSky;
            }
        });
        return (float)(MathF.PI * rows.Sum() / (side * side));
    }

    [CpuKernelFact]
    public void A_Room_Open_Under_A_Lintel_Reads_Its_Deep_End_Within_Half_Again_The_Light_Traced_There()
    {
        var (cells, light) = LintelScene();
        // The finest level's probes at 1.5 m in the room's two deepest columns, x 0.5 and 1.5, at
        // every z, their faces toward the opening at -x, the first eight at x 0.5.
        var points = (from x in new[] { 0.5f, 1.5f }
                      from z in Enumerable.Range(0, 8)
                      select (new Vector3(x, 1.5f, -3.5f + z), -Vector3.UnitX)).ToArray();
        var setting = new Setting { FieldCascades = LintelField, CellLight = light, SkyLight = LintelSky, Moved = true, Points = points };
        var merged = Passes(cells, LintelLevels, UnderLintel, setting);
        var marched = Passes(cells, LintelLevels, UnderLintel, setting with { Reference = true });
        var traced = points.Select(p => TracedAgainstBoxes(MovedOff(p.Item1), -Vector3.UnitX)).ToArray();
        var over = merged.Taken.Select((taken, i) => taken / traced[i]).ToArray();
        var (nearer, deepest, probe) = (over[..8].Average(), over[8..].Average(), over[4]);
        var said = $"under the lintel the probe at (0.5, 1.5, 0.5) takes {merged.Taken[4]:0.0000} merged against {traced[4]:0.0000} traced, {probe:0.000} of it, "
            + $"the columns at x 0.5 and 1.5 {nearer:0.000} and {deepest:0.000} of it on the mean, and with each probe's own texels marched on "
            + $"{marched.Taken[4]:0.0000} at the probe, {marched.Taken[..8].Select((m, i) => m / traced[i]).Average():0.000} and "
            + $"{marched.Taken[8..].Select((m, i) => m / traced[i + 8]).Average():0.000} on the mean";
        output.WriteLine(said);
        // The merge hands a ray ending short of the lintel to far probes standing a spacing off it,
        // whose own rays pass under the lintel and see the sky where the ray would meet the
        // lintel's inner face, and the finest two cascades' 2 by 2 means mix ways across the
        // opening's narrow strip, so the deep end takes more than its light, 1.402 of it at the
        // probe and 1.472 and 1.405 on the means at x 0.5 and 1.5, against 0.1202 traced at the
        // probe. Each probe's own texels marched on take 0.609 and 0.475 of it on the means, one
        // ray a texel missing the strip. The bounds sit a little over the merge's figures, so a
        // rule that lessens the smear passes and one that adds to it fails, and no reading may
        // fall a quarter under the trace.
        Assert.True(probe is >= 0.75f and <= 1.45f && nearer is >= 0.75f and <= 1.52f && deepest is >= 0.75f and <= 1.46f, said);
    }
}
