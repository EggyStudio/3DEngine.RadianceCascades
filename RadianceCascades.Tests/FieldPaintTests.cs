using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The color a point on a surface reads from the cells of the field about it, as a ray's hit takes
/// it, the cells built and read on the CPU from the library's Slang over a cascade of 32 cells of a
/// quarter unit a side about the origin.
/// </summary>
public sealed class FieldPaintTests
{
    private const int Size = 32;
    private static readonly Vector3 White = new(1), Green = new(0.1f, 0.6f, 0.1f), Red = new(0.8f, 0.1f, 0.1f);

    // A square of half side h facing up at height y, or along the way given, of the color given.
    private static IEnumerable<(Vector3, Vector3, Vector3, Vector3)> Square(Vector3 middle, Vector3 u, Vector3 v, Vector3 color)
    {
        var (a, b, c, d) = (middle - u - v, middle + u - v, middle + u + v, middle - u + v);
        yield return (a, b, c, color);
        yield return (a, c, d, color);
    }

    private static Vector3[] Read(IEnumerable<(Vector3 A, Vector3 B, Vector3 C, Vector3 Color)> triangles, params (Vector3 At, Vector3 Normal)[] hits)
    {
        var all = triangles.ToList();
        var floats = new List<float> { all.Count };
        foreach (var (a, b, c, color) in all)
            floats.AddRange([a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z, 1, 0, color.X, color.Y, color.Z]);
        var (paints, faces, distances) = (new float[4 * Size * Size * Size], new uint[Size * Size * Size], new float[Size * Size * Size]);
        CpuKernel.Run("field_paint_build_cpu.slang", (Size / 4, Size / 4, Size / 4), floats.ToArray(), paints, faces, distances);
        var at = hits.SelectMany(hit => new[] { hit.At.X, hit.At.Y, hit.At.Z, hit.Normal.X, hit.Normal.Y, hit.Normal.Z }).ToArray();
        var colors = new float[3 * hits.Length];
        CpuKernel.Run("field_paint_read_cpu.slang", ((uint)hits.Length, 1, 1), paints, faces, distances, at, colors);
        return [.. Enumerable.Range(0, hits.Length).Select(h => new Vector3(colors[h * 3], colors[h * 3 + 1], colors[h * 3 + 2]))];
    }

    private static void Near(Vector3 expected, Vector3 read, string what)
    {
        var off = Vector3.Abs(read - expected);
        Assert.True(MathF.Max(off.X, MathF.Max(off.Y, off.Z)) <= 0.1f, $"{what} read {read} where its surface is {expected}");
    }

    [CpuKernelFact]
    public void A_Floor_Over_Another_Of_Unlike_Color_A_Tenth_Below_Reads_Its_Own_Color()
    {
        // A white floor at 0.06 over a green floor a tenth of a unit below, both facing up, so the
        // cells between them lie nearer the green, which faces the hit's way as the white does.
        var floors = Square(new(0, 0.06f, 0), new(2, 0, 0), new(0, 0, -2), White)
            .Concat(Square(new(0, -0.04f, 0), new(2, 0, 0), new(0, 0, -2), Green));
        var read = Read(floors, (new(0.3f, 0.06f, 0.1f), Vector3.UnitY), (new(-0.7f, 0.06f, 0.45f), Vector3.UnitY));
        Near(White, read[0], "the upper floor's hit");
        Near(White, read[1], "the upper floor's second hit");
    }

    [CpuKernelFact]
    public void A_Red_Wall_On_A_White_Floor_Reads_Red_On_The_Wall_And_White_On_The_Floor_Within_A_Cell_Of_The_Corner()
    {
        var corner = Square(new(1, 0, 0), new(1, 0, 0), new(0, 0, -2), White)
            .Concat(Square(new(0, 1, 0), new(0, 0, -2), new(0, 1, 0), Red));
        var read = Read(corner, (new(0, 0.1f, 0.3f), Vector3.UnitX), (new(0.1f, 0, 0.3f), Vector3.UnitY));
        Near(Red, read[0], "the wall's hit a tenth above the floor");
        Near(White, read[1], "the floor's hit a tenth from the wall");
    }

    [CpuKernelFact]
    public void A_Slab_Half_A_Cell_Thick_Red_Above_And_White_Below_Reads_Each_Side_Its_Own_Color()
    {
        // A closed slab from -0.0625 to 0.0625, its top red, its bottom white and its sides white,
        // read from above on its top and from below on its bottom.
        const float h = 0.0625f;
        var slab = Square(new(0, h, 0), new(2, 0, 0), new(0, 0, -2), Red)
            .Concat(Square(new(0, -h, 0), new(2, 0, 0), new(0, 0, 2), White));
        var read = Read(slab, (new(0.3f, h, 0.1f), Vector3.UnitY), (new(0.3f, -h, 0.1f), -Vector3.UnitY));
        Near(Red, read[0], "the slab's top");
        Near(White, read[1], "the slab's bottom");
    }
}
