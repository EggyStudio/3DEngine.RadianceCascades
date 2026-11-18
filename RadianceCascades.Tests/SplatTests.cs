using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The splat of a mesh's triangles into a cascade of the field, a triangle at a time over the cells
/// its band covers, against the cells built one at a time from every triangle, and the paint of
/// each cell by the triangle whose code it kept, run on the CPU from the library's Slang.
/// </summary>
public sealed class SplatTests
{
    private const int Size = 32;
    private const float Cell = 0.25f;

    // A box of two about the origin, each face a color of its own, its faces looking out.
    private static List<(Vector3 A, Vector3 B, Vector3 C, Vector3 Color)> Box()
    {
        var faces = new List<(Vector3, Vector3, Vector3, Vector3)>();
        for (int axis = 0; axis < 3; axis++)
            foreach (var sign in new[] { -1f, 1f })
            {
                Vector3 n = default, u = default, v = default;
                n[axis] = sign; u[(axis + 1) % 3] = 1; v[(axis + 2) % 3] = 1;
                if (sign < 0) (u, v) = (v, u);
                var color = new Vector3(axis == 0 ? 0.9f : 0.1f, axis == 1 ? 0.9f : 0.1f, axis == 2 ? 0.9f : 0.1f) * (sign > 0 ? 1 : 0.5f);
                var (a, b, c, d) = (n - u - v, n + u - v, n + u + v, n - u + v);
                faces.Add((a, b, c, color));
                faces.Add((a, c, d, color));
            }
        return faces;
    }

    private static float[] Splatted(IEnumerable<(Vector3 A, Vector3 B, Vector3 C, Vector3 Color)> triangles)
    {
        var all = triangles.ToList();
        var floats = new List<float> { all.Count };
        foreach (var (a, b, c, color) in all)
            floats.AddRange([a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z, 1, 0, color.X, color.Y, color.Z]);
        return [.. floats];
    }

    private static float[] Built(IEnumerable<(Vector3 A, Vector3 B, Vector3 C, Vector3 Color)> triangles)
    {
        var all = triangles.ToList();
        var floats = new List<float> { all.Count };
        foreach (var (a, b, c, _) in all)
            floats.AddRange([a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z, 1, 0]);
        return [.. floats];
    }

    private static uint[] Empty() => Enumerable.Repeat(uint.MaxValue, Size * Size * Size).ToArray();

    [CpuKernelFact]
    public void A_Box_Splatted_A_Triangle_At_A_Time_Keeps_At_Every_Cell_The_Code_The_Cells_Built_One_At_A_Time_Keep()
    {
        var box = Box();
        var (codes, behind) = (Empty(), Empty());
        CpuKernel.Run("splat_cpu.slang", ((uint)box.Count, 1, 1), Splatted(box), codes, behind);

        var (distances, halves, sheets) = (new float[Size * Size * Size], new float[2 * Size * Size * Size], new float[3 * Size * Size * Size]);
        CpuKernel.Run("field_code_cpu.slang", (Size / 4, Size / 4, Size / 4), Built(box), distances, halves, sheets);
        int kept = 0, faults = 0;
        string first = "";
        for (int i = 0; i < codes.Length; i++)
        {
            var built = (uint)halves[i * 2] | (uint)halves[i * 2 + 1] << 16;
            if (built != uint.MaxValue)
                kept++;
            if (codes[i] == built)
                continue;
            if (faults++ == 0)
                first = $"cell {i} kept {codes[i]:X8} where the cells built one at a time keep {built:X8}";
        }
        Assert.True(kept > 2_000, $"only {kept} cells kept a code");
        Assert.True(faults == 0, $"{faults} cells differ, the first {first}");
    }

    [CpuKernelFact]
    public void Each_Cell_Is_Painted_By_The_Triangle_Whose_Code_It_Kept_Its_Color_And_Gloss()
    {
        var box = Box();
        var (codes, behind) = (Empty(), Empty());
        var triangles = Splatted(box);
        CpuKernel.Run("splat_cpu.slang", ((uint)box.Count, 1, 1), triangles, codes, behind);
        var (paints, glosses) = (new float[4 * Size * Size * Size], new uint[Size * Size * Size]);
        CpuKernel.Run("splat_paint_cpu.slang", ((uint)box.Count, 1, 1), triangles, codes, paints, glosses);

        // A cell a cell outside the middle of each face takes that face's color, painted whole.
        for (int axis = 0; axis < 3; axis++)
            foreach (var sign in new[] { -1, 1 })
            {
                var at = new int[] { 16, 16, 16 };
                at[axis] = sign > 0 ? 20 : 11;
                var index = (at[2] * Size + at[1]) * Size + at[0];
                var paint = new Vector4(paints[index * 4], paints[index * 4 + 1], paints[index * 4 + 2], paints[index * 4 + 3]);
                var color = new Vector3(axis == 0 ? 0.9f : 0.1f, axis == 1 ? 0.9f : 0.1f, axis == 2 ? 0.9f : 0.1f) * (sign > 0 ? 1 : 0.5f);
                var off = Vector3.Abs(new Vector3(paint.X, paint.Y, paint.Z) - color);
                Assert.True(MathF.Max(off.X, MathF.Max(off.Y, off.Z)) <= 0.01f && paint.W == 1,
                    $"the cell beside the face along axis {axis} at {sign} is painted {paint} where the face is {color}");
                Assert.True((glosses[index] >> 8 & 255) == 102, $"the cell beside the face along axis {axis} at {sign} holds a roughness of {glosses[index] >> 8 & 255} 255ths");
            }
    }
}
