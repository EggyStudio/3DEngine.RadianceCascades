using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// What a cell of the field keeps and resolves to, the code the splat gives it from the triangles
/// near it and the distance the resolve makes of that code, run on the CPU from the library's Slang
/// over a cascade of 32 cells of a quarter unit a side about the origin.
/// </summary>
public sealed class FieldCodeTests
{
    private const int Size = 32;
    private const float Cell = 0.25f, Band = 4;

    private static Vector3 Middle(int x, int y, int z) => new Vector3(-Size * Cell / 2) + (new Vector3(x, y, z) + new Vector3(0.5f)) * Cell;

    // The twelve triangles of a box of half side h about the origin, its faces looking out.
    private static List<(Vector3 A, Vector3 B, Vector3 C)> Box(float h)
    {
        var faces = new List<(Vector3, Vector3, Vector3)>();
        for (int axis = 0; axis < 3; axis++)
            foreach (var sign in new[] { -1f, 1f })
            {
                Vector3 n = default, u = default, v = default;
                n[axis] = sign; u[(axis + 1) % 3] = 1; v[(axis + 2) % 3] = 1;
                if (sign < 0) (u, v) = (v, u);
                var (a, b, c, d) = ((n - u - v) * h, (n + u - v) * h, (n + u + v) * h, (n - u + v) * h);
                faces.Add((a, b, c));
                faces.Add((a, c, d));
            }
        return faces;
    }

    private static float[] Triangles(IEnumerable<(Vector3 A, Vector3 B, Vector3 C, int Instance, bool DoubleSided)> triangles)
    {
        var all = triangles.ToList();
        var floats = new List<float> { all.Count };
        foreach (var (a, b, c, instance, sided) in all)
            floats.AddRange([a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z, instance, sided ? 1 : 0]);
        return [.. floats];
    }

    private static (float[] Distances, float[] Codes, float[] Sheets) Build(float[] triangles)
    {
        var (distances, codes, sheets) = (new float[Size * Size * Size], new float[2 * Size * Size * Size], new float[3 * Size * Size * Size]);
        Array.Fill(distances, float.NaN);
        CpuKernel.Run("field_code_cpu.slang", (Size / 4, Size / 4, Size / 4), triangles, distances, codes, sheets);
        return (distances, codes, sheets);
    }

    [CpuKernelFact]
    public void A_Box_Of_Two_Resolves_To_Its_Distances_Within_A_1024th_Of_A_Cell_At_Every_Cell_Of_Its_Band()
    {
        var (distances, _, _) = Build(Triangles(Box(1).Select(t => (t.A, t.B, t.C, 1, false))));
        int within = 0, faults = 0;
        string first = "";
        for (int z = 0; z < Size; z++)
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    var p = Middle(x, y, z);
                    var q = Vector3.Abs(p) - Vector3.One;
                    var exact = Vector3.Max(q, Vector3.Zero).Length() + MathF.Min(MathF.Max(q.X, MathF.Max(q.Y, q.Z)), 0);
                    if (MathF.Abs(exact) >= Band * Cell - Cell)
                        continue;
                    within++;
                    var d = distances[(z * Size + y) * Size + x];
                    if (MathF.Abs(d - exact) <= Cell / 1024 + 1e-6f)
                        continue;
                    if (faults++ == 0)
                        first = $"cell ({x}, {y}, {z}) at {p} resolved to {d} where the box is {exact} away";
                }
        // A box of two holds some 2,500 cells within three quarters of a unit of its faces.
        Assert.True(within > 2_000, $"only {within} cells lay within the band");
        Assert.True(faults == 0, $"{faults} of {within} cells off by more than a 1024th of a cell, the first {first}");
    }

    [CpuKernelFact]
    public void A_Faces_Way_Read_Back_From_Its_Eight_Bits_Lies_Within_Fourteen_And_A_Half_Degrees_Of_It()
    {
        // The largest over 65,536 ways is 14.15 degrees, near the octahedron's folded corners,
        // where a square of its sixteen by sixteen spans the most of the sphere.
        var degrees = Enumerable.Repeat(180f, 256 * 256).ToArray();
        CpuKernel.Run("way_cpu.slang", (16, 16, 1), degrees);
        Assert.True(degrees.Max() <= 14.5f, $"a way read back {degrees.Max()} degrees from itself");
    }

    [CpuKernelFact]
    public void Two_Meshes_Coplanar_Faces_Give_A_Cell_Two_Codes_Of_Which_The_Lesser_Instance_Keeps_It()
    {
        // Two squares in one plane, of instances 9 and 4, overlapping over the cells about the
        // origin, so the splat's minimum keeps one code whichever triangle a thread takes first and
        // the paint takes the triangle whose code that is, one alone.
        var square = new List<(Vector3, Vector3, Vector3)>
        {
            (new(-2, 0.05f, -2), new(2, 0.05f, -2), new(2, 0.05f, 2)),
            (new(-2, 0.05f, -2), new(2, 0.05f, 2), new(-2, 0.05f, 2)),
        };
        var both = square.Select(t => (t.Item1, t.Item2, t.Item3, 9, true)).Concat(square.Select(t => (t.Item1, t.Item2, t.Item3, 4, true)));
        var (_, codes, _) = Build(Triangles(both));
        var index = (16 * Size + 16) * Size + 16;
        uint Code(float[] halves) => (uint)halves[index * 2] | (uint)halves[index * 2 + 1] << 16;
        Assert.True((Code(codes) & 127) == 4, $"the cell kept instance {Code(codes) & 127}, where the lesser of 4 and 9 keeps it");
        var nine = Code(Build(Triangles(square.Select(t => (t.Item1, t.Item2, t.Item3, 9, true)))).Codes);
        Assert.True(nine != Code(codes), "the two instances' faces give the cell two codes, so the paint takes one of them alone");
        Assert.True(nine >> 7 == Code(codes) >> 7, "the two codes are alike but for the instance's bits");
    }

    [CpuKernelFact]
    public void A_Double_Sided_Sheet_Between_Two_Cells_Keeps_Its_Plane_In_Both_Within_A_1024th_Of_A_Cell()
    {
        // A sheet at 0.08 above the plane between two layers of cells' middles, a third of a cell
        // from the cell below and the cell above it alike, each keeping the plane it lies past.
        const float at = 0.08f;
        var sheet = new List<(Vector3, Vector3, Vector3, int, bool)>
        {
            (new(-2, at, -2), new(2, at, -2), new(2, at, 2), 1, true),
            (new(-2, at, -2), new(2, at, 2), new(-2, at, 2), 1, true),
        };
        var (distances, _, sheets) = Build(Triangles(sheet));
        foreach (var y in new[] { 15, 16 })
        {
            var index = (16 * Size + y) * Size + 16;
            var plane = new Vector3(sheets[index * 3], sheets[index * 3 + 1], sheets[index * 3 + 2]);
            var middle = Middle(16, y, 16).Y;
            // The plane is its way times two cells more than its offset from the cell's middle, in
            // cells, so the offset along the way read back is where the sheet lies.
            var way = Vector3.Normalize(plane);
            var offset = (plane.Length() - 2) * Cell * MathF.Sign(Vector3.Dot(way, Vector3.UnitY));
            Assert.True(plane != Vector3.Zero, $"the cell at y {middle} keeps no plane");
            Assert.True(MathF.Abs(MathF.Abs(middle - at) - MathF.Abs(offset)) <= Cell / 1024 + 1e-5f,
                $"the cell at y {middle} keeps a plane {offset} off where the sheet lies {at - middle} off");
            Assert.True(distances[index] > 0, $"a double-sided sheet holds the cell at y {middle} at its distance, {distances[index]}");
        }
    }
}
