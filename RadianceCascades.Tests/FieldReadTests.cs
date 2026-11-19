using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The field's distance at a point and the march of a ray through it, read from the cells a box of
/// two and a double-sided sheet resolve to, built and read on the CPU from the library's Slang over
/// a cascade of 32 cells of a quarter unit a side about the origin.
/// </summary>
public sealed class FieldReadTests
{
    private const int Size = 32;
    private const float Cell = 0.25f;

    // The box of two's twelve triangles, and a double-sided sheet at 0.08 four units square.
    private static IEnumerable<(Vector3 A, Vector3 B, Vector3 C, bool DoubleSided)> Box(float h)
    {
        for (int axis = 0; axis < 3; axis++)
            foreach (var sign in new[] { -1f, 1f })
            {
                Vector3 n = default, u = default, v = default;
                n[axis] = sign; u[(axis + 1) % 3] = 1; v[(axis + 2) % 3] = 1;
                if (sign < 0) (u, v) = (v, u);
                var (a, b, c, d) = ((n - u - v) * h, (n + u - v) * h, (n + u + v) * h, (n - u + v) * h);
                yield return (a, b, c, false);
                yield return (a, c, d, false);
            }
    }

    private static IEnumerable<(Vector3 A, Vector3 B, Vector3 C, bool DoubleSided)> Sheet(float at)
    {
        yield return (new(-2, at, -2), new(2, at, -2), new(2, at, 2), true);
        yield return (new(-2, at, -2), new(2, at, 2), new(-2, at, 2), true);
    }

    // The cells the triangles resolve to, each its distance and its plane.
    private static float[] Cells(IEnumerable<(Vector3 A, Vector3 B, Vector3 C, bool DoubleSided)> triangles)
    {
        var all = triangles.ToList();
        var floats = new List<float> { all.Count };
        foreach (var (a, b, c, sided) in all)
            floats.AddRange([a.X, a.Y, a.Z, b.X, b.Y, b.Z, c.X, c.Y, c.Z, 1, sided ? 1 : 0]);
        var (distances, codes, sheets) = (new float[Size * Size * Size], new float[2 * Size * Size * Size], new float[3 * Size * Size * Size]);
        CpuKernel.Run("field_code_cpu.slang", (Size / 4, Size / 4, Size / 4), floats.ToArray(), distances, codes, sheets);
        var cells = new float[4 * Size * Size * Size];
        for (int i = 0; i < distances.Length; i++)
            (cells[i * 4], cells[i * 4 + 1], cells[i * 4 + 2], cells[i * 4 + 3]) = (distances[i], sheets[i * 3], sheets[i * 3 + 1], sheets[i * 3 + 2]);
        return cells;
    }

    private static (float[] Distances, float[] Hits, float[] Steps) Read(float[] cells, Vector3[] points, (Vector3 Origin, Vector3 Way, float From, float To)[] rays)
    {
        var p = new List<float> { points.Length };
        foreach (var point in points) p.AddRange([point.X, point.Y, point.Z]);
        var r = new List<float> { rays.Length };
        foreach (var (origin, way, from, to) in rays) r.AddRange([origin.X, origin.Y, origin.Z, way.X, way.Y, way.Z, from, to]);
        var (distances, hits, steps) = (new float[2 * Math.Max(1, points.Length)], new float[2 * Math.Max(1, rays.Length)], new float[2 * 64 * Math.Max(1, rays.Length)]);
        CpuKernel.Run("field_read_cpu.slang", ((uint)Math.Max(points.Length, rays.Length), 1, 1), cells, p.ToArray(), distances, r.ToArray(), hits, steps);
        return (distances, hits, steps);
    }

    private static float BoxDistance(Vector3 p)
    {
        var q = Vector3.Abs(p) - Vector3.One;
        return Vector3.Max(q, Vector3.Zero).Length() + MathF.Min(MathF.Max(q.X, MathF.Max(q.Y, q.Z)), 0);
    }

    [CpuKernelFact]
    public void The_Distance_At_A_Point_About_A_Box_Of_Two_Lies_Near_The_Boxs_Own()
    {
        var random = new Random(3);
        var points = new List<Vector3>();
        while (points.Count < 400)
        {
            var p = new Vector3(random.NextSingle() * 4 - 2, random.NextSingle() * 4 - 2, random.NextSingle() * 4 - 2);
            if (MathF.Abs(BoxDistance(p)) < 0.6f)
                points.Add(p);
        }
        var (distances, _, _) = Read(Cells(Box(1)), [.. points], []);
        var worst = points.Select((p, i) => MathF.Abs(distances[i * 2] - BoxDistance(p))).Max();
        // The blend of the cells' distances rounds the box's edges and corners, 0.286 of a cell off
        // at worst over these points.
        Assert.True(worst <= 0.3f * Cell, $"the distance read about a box of two lies {worst / Cell:0.000} of a cell from the box's at worst");
    }

    [CpuKernelFact]
    public void A_Ray_Toward_The_Box_Meets_Its_Face_And_One_Over_It_Meets_Nothing()
    {
        var rays = new List<(Vector3, Vector3, float, float)>();
        for (int k = 0; k < 9; k++)
            rays.Add((new Vector3(-0.8f + 0.2f * k, 0.3f, -3.5f), Vector3.UnitZ, 0, 6));
        rays.Add((new Vector3(0, 1.5f, -3.5f), Vector3.UnitZ, 0, 6));
        // Rays leaning toward the face, so their steps do not land on it, each a fifth to a half
        // off straight on.
        for (int k = 0; k < 4; k++)
            rays.Add((new Vector3(-0.6f, 0.2f, -3.5f), Vector3.Normalize(new Vector3(0.2f + 0.1f * k, 0.05f * k, 1)), 0, 6));
        var (_, hits, steps) = Read(Cells(Box(1)), [], [.. rays]);
        var (shortest, past) = (0f, 0f);
        for (int k = 0; k < rays.Count; k++)
        {
            if (k == 9)
                continue;
            // The face at z -1 lies where the ray's z has gone 2.5.
            var face = 2.5f / rays[k].Item2.Z;
            Assert.True(hits[k * 2] > 0, $"ray {k} met nothing, its face at {face}");
            (shortest, past) = (MathF.Max(shortest, face - hits[k * 2]), MathF.Max(past, hits[k * 2] - face));
            Assert.True(hits[k * 2 + 1] == hits[k * 2], $"ray {k} met the box at {hits[k * 2 + 1]} reading the planes, at {hits[k * 2]} not");
        }
        // A march stops within a tenth of a cell of a surface and steps a quarter of a cell at
        // least, so a ray that comes at a face aslant may stop short of it by the tenth or past it
        // by the step. These rays stop 0.043 of a cell short and 0.144 past at worst.
        Assert.True(shortest <= 0.1f * Cell && past <= 0.25f * Cell, $"a ray met the box {shortest / Cell:0.000} of a cell short of its face and {past / Cell:0.000} past it at worst");
        Assert.True(hits[9 * 2] < 0 && hits[9 * 2 + 1] < 0, $"the ray half a unit over the box met something at {hits[9 * 2]}");
        // Each step goes on by the distance read, a quarter of a cell at least, never back.
        for (int s = 1; s < 64 && steps[(0 * 64 + s) * 2] > 0; s++)
            Assert.True(steps[(0 * 64 + s) * 2] - steps[(0 * 64 + s - 1) * 2] >= Cell * 0.25f - 1e-6f, $"step {s} went on by less than a quarter of a cell");
    }

    [CpuKernelFact]
    public void A_Ray_From_Either_Side_Of_A_Double_Sided_Sheet_Meets_It_On_Its_Own_Side_Within_The_Sheets_Hold_And_The_Marchs_Hit()
    {
        const float at = 0.08f;
        var rays = new (Vector3, Vector3, float, float)[] { (new(0.3f, 1, 0.1f), -Vector3.UnitY, 0, 3), (new(0.3f, -1, 0.1f), Vector3.UnitY, 0, 3) };
        var (_, hits, _) = Read(Cells(Sheet(at)), [], rays);
        for (int k = 0; k < 2; k++)
        {
            // The sheet is held a quarter of a cell either side of its plane and a march meets a
            // surface within a tenth of a cell of it, so the ray stops within 0.35 of a cell of the
            // plane and on the side it came from.
            var met = rays[k].Item1.Y + rays[k].Item2.Y * hits[k * 2 + 1];
            var off = (met - at) / Cell * MathF.Sign(rays[k].Item1.Y - at);
            Assert.True(hits[k * 2 + 1] > 0 && off >= 0 && off <= 0.35f, $"the ray from {rays[k].Item1.Y} met the sheet {off:0.000} of a cell off its plane on its own side");
        }
    }

    [Fact]
    public void The_Band_The_Slang_Reads_Is_The_Band_The_Plan_Resolves_To()
    {
        // A march stands in open space no farther from a surface than the band (openAt), past which
        // the field reads the band whatever lies farther, so a band of the Slang's other than the
        // plan's would hand a ray off beside a surface or march it on through open space.
        var source = File.ReadAllText(Path.Combine(Repository.Shaders, "rc_field_read.slang"));
        var match = System.Text.RegularExpressions.Regex.Match(source, @"public static const float FieldBand = ([0-9.]+);");
        Assert.True(match.Success, "rc_field_read.slang declares no FieldBand");
        var band = float.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture);
        Assert.True(band == SceneFieldPlan<string, int>.Band, $"the Slang reads a band of {band} cells where the plan resolves {SceneFieldPlan<string, int>.Band}");
    }
}
