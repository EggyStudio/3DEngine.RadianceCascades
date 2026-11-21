using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A double-sided sheet resting on a box's walls' tops, read by rays marched up through the cells
/// by the walls' inner faces, which keep the walls' codes and not the sheet's plane, built, mended
/// and read on the CPU from the library's Slang over a cascade of 32 cells of a quarter unit.
/// </summary>
public sealed class FieldSeamTests
{
    private const int Size = 32;

    // A box 4 units inside, its walls three cells thick from y -2 to 2 on a floor, and a sheet at
    // y 2 over the walls' tops, each triangle's corners, instance and whether it is double-sided.
    private static List<float> Triangles()
    {
        var floats = new List<float> { 0 };
        void Box(Vector3 low, Vector3 high, int instance)
        {
            var middle = (low + high) * 0.5f;
            var half = (high - low) * 0.5f;
            for (int axis = 0; axis < 3; axis++)
                foreach (var sign in new[] { -1f, 1f })
                {
                    Vector3 n = default, u = default, v = default;
                    n[axis] = sign; u[(axis + 1) % 3] = 1; v[(axis + 2) % 3] = 1;
                    if (sign < 0) (u, v) = (v, u);
                    var (a, b, c, d) = (middle + (n - u - v) * half, middle + (n + u - v) * half, middle + (n + u + v) * half, middle + (n - u + v) * half);
                    floats.AddRange([a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z, instance, 0]);
                    floats.AddRange([a.X, a.Y, a.Z, c.X, c.Y, c.Z, d.X, d.Y, d.Z, instance, 0]);
                    floats[0] += 2;
                }
        }
        const float o = 2.75f;
        Box(new(-o, -2.75f, -o), new(o, -2, o), 1);
        Box(new(2, -2, -o), new(o, 2, o), 2);
        Box(new(-o, -2, -o), new(-2, 2, o), 3);
        Box(new(-2, -2, 2), new(2, 2, o), 4);
        Box(new(-2, -2, -o), new(2, 2, -2), 5);
        floats.AddRange([-o, 2, -o, o, 2, -o, o, 2, o, 6, 1, -o, 2, -o, o, 2, o, -o, 2, o, 6, 1]);
        floats[0] += 2;
        return floats;
    }

    // Where each ray first meets the field reading the sheets' planes, or below zero where it
    // meets nothing.
    private static float[] March((Vector3 Origin, Vector3 Way)[] rays)
    {
        var (distances, codes, sheets) = (new float[Size * Size * Size], new float[2 * Size * Size * Size], new float[3 * Size * Size * Size]);
        CpuKernel.Run("field_code_cpu.slang", (Size / 4, Size / 4, Size / 4), Triangles().ToArray(), distances, codes, sheets);
        var cells = new float[4 * Size * Size * Size];
        for (int i = 0; i < distances.Length; i++)
            (cells[i * 4], cells[i * 4 + 1], cells[i * 4 + 2], cells[i * 4 + 3]) = (distances[i], sheets[i * 3], sheets[i * 3 + 1], sheets[i * 3 + 2]);
        var seams = new float[3 * Size * Size * Size];
        CpuKernel.Run("field_seam_cpu.slang", (Size / 4, Size / 4, Size / 4), cells, codes, seams);
        for (int i = 0; i < distances.Length; i++)
            if (seams[i * 3] != 0 || seams[i * 3 + 1] != 0 || seams[i * 3 + 2] != 0)
                (cells[i * 4 + 1], cells[i * 4 + 2], cells[i * 4 + 3]) = (seams[i * 3], seams[i * 3 + 1], seams[i * 3 + 2]);
        var r = new List<float> { rays.Length };
        foreach (var (origin, way) in rays)
            r.AddRange([origin.X, origin.Y, origin.Z, way.X, way.Y, way.Z, 0, 2.5f]);
        var (hits, steps) = (new float[2 * rays.Length], new float[2 * 64 * rays.Length]);
        CpuKernel.Run("field_read_cpu.slang", ((uint)rays.Length, 1, 1), cells, new float[] { 0 }, new float[2], r.ToArray(), hits, steps);
        return Enumerable.Range(0, rays.Length).Select(k => hits[k * 2 + 1]).ToArray();
    }

    [CpuKernelFact]
    public void A_Ray_Up_Through_The_Cells_By_A_Walls_Face_Meets_A_Sheet_Resting_On_The_Walls_Top()
    {
        // Rays from y 1 straight up and leaning 20 degrees toward the wall, spread a fiftieth of a
        // unit apart over the last half unit before the wall's inner face at x 2, the cells by the
        // face keeping the wall's code where the sheet and the face lie as near their middles.
        var rays = new List<(Vector3, Vector3)>();
        for (int k = 0; k < 25; k++)
        {
            var x = 1.5f + 0.02f * k;
            rays.Add((new(x, 1, 0.1f), Vector3.UnitY));
            rays.Add((new(x - 0.364f, 1, 0.1f), Vector3.Normalize(new(0.364f, 1, 0))));
        }
        var hits = March([.. rays]);
        var passed = Enumerable.Range(0, rays.Count)
            .Where(k => hits[k] < 0 || rays[k].Item1.Y + rays[k].Item2.Y * hits[k] > 2.1f)
            .Select(k => $"({rays[k].Item1.X + rays[k].Item2.X * (1 / rays[k].Item2.Y):0.00} at the lid)")
            .ToList();
        Assert.True(passed.Count == 0, $"{passed.Count} of {rays.Count} rays passed the sheet, crossing its height at " + string.Join(", ", passed));
    }
}
