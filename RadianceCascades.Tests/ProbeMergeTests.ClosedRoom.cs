using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A closed room read by a chain of four cascades on the CPU, every face of it sending the same
/// light, where every point inside takes that light along every way, so the passes neither add
/// light nor lose it, or one wall alone sending it, against the light traced against its faces.
/// </summary>
public sealed partial class ProbeMergeTests
{
    [CpuKernelTheory]
    [InlineData(Merge.AtRayEnd)]
    [InlineData(Merge.ByReachSight)]
    public void A_Closed_Room_Whose_Every_Face_Sends_The_Same_Light_Reads_That_Light_At_Every_Point_Through_Four_Cascades(Merge merge)
    {
        // The open box with a lid, 8 units a side inside, each surface a ray meets sending a light
        // of 1 under a black sky, read at points on the floor facing up, at the room's middle
        // height facing +x, up the middle facing -z, before the -x wall facing it and under the lid
        // facing down, each of which takes pi.
        const float o = 4 + Wall;
        var cells = Field(OpenBox().Concat(Cuboid(new(-o, 8, -o), new(o, 8 + Wall, o), 6)));
        Level[] chain =
        [
            new(new(-3.5f, 0.25f, -3.5f), 1, 8, 8, 8, NearTexels, 2),
            new(FarCorner, FarSpacing, FarCount, FarCount, FarCount, FarTexels, 4),
            new(new(-14, -10, -14), 4, 8, 8, 8, 16, 8),
            new(new(-28, -28, -28), 8, 8, 8, 8, 16, FarEnd),
        ];
        var points = Enumerable.Range(0, 8).Select(i => (new Vector3(-3.5f + i, 0.25f, 0.5f), Vector3.UnitY))
            .Concat(Enumerable.Range(0, 7).Select(i => (new Vector3(-3.5f + i, 4f, 0.5f), Vector3.UnitX)))
            .Concat(Enumerable.Range(0, 7).Select(i => (new Vector3(0.5f, 0.75f + i, 0.5f), -Vector3.UnitZ)))
            .Append((new Vector3(-3.75f, 4f, 0.5f), Vector3.UnitX)).Append((new Vector3(0.5f, 7.75f, 0.5f), -Vector3.UnitY)).ToArray();
        var light = Enumerable.Repeat(1f, Size * Size * Size).ToArray();
        var reading = Passes(cells, chain, Floor, new Setting { Points = points, CellLight = light, SkyLight = 0, Merge = merge });
        var read = reading.Taken.Select(t => t / MathF.PI).ToArray();
        // Each level's texels at its probes that stand inside the room, more than a cell off its
        // faces, the coarsest level's none.
        var inside = chain.Select((level, l) => Enumerable.Range(0, level.TexelCount).Where(k =>
        {
            var probe = k / (level.Texels * level.Texels);
            var place = level.Corner + new Vector3(probe % level.X, probe / level.X % level.Y, probe / (level.X * level.Y)) * level.Spacing;
            return MathF.Abs(place.X) < 4 - Cell && MathF.Abs(place.Z) < 4 - Cell && place.Y is > Cell and < 8 - Cell;
        }).Select(k => reading.LevelLight[l][k]).ToArray()).ToArray();
        var levels = string.Join(", ", inside.Select((texels, l) => texels.Length == 0 ? $"level {l} none"
            : $"level {l} {texels.Min():0.0000} to {texels.Max():0.0000} over {texels.Length} texels"));
        var said = $"{merge}, the closed room's {points.Length} points read from {read.Min():0.0000} to {read.Max():0.0000} of the light every face sends, "
            + $"the levels' texels inside it {levels}, {reading}";
        output.WriteLine(said);
        // 1.0000 at every point. Under broad light, a wall, half the floor or the lid alone
        // sending it, the points read 0.99 to 1.12 of the light traced against the room's faces
        // on the mean and texels each marched on to the coarsest level's end 0.97 to 1.15, single
        // points up to a quarter either way in both and more where they take little, the sampling
        // of 16 by 16 texels.
        Assert.True(read.All(q => Math.Abs(q - 1) <= 0.005) && inside.All(texels => texels.All(q => Math.Abs(q - 1) <= 0.005)), said);
    }

    // The cell of the field a point lies in, x fastest.
    private static int ClosedCellOf(Vector3 p)
    {
        var q = (p - Corner) / Cell;
        return ((int)MathF.Floor(q.Z) * Size + (int)MathF.Floor(q.Y)) * Size + (int)MathF.Floor(q.X);
    }

    // What a point facing normal inside the closed box takes, over 96 by 96 ways spread by their
    // cosine, each meeting the box's inner faces and taking the light of the cell it meets them in.
    private static float TracedInClosedBox(Vector3 point, Vector3 normal, float[] light)
    {
        var (u, v) = Basis(normal);
        const int side = 96;
        var rows = new double[side];
        Parallel.For(0, side, i =>
        {
            for (int j = 0; j < side; j++)
            {
                var way = CosineWay(normal, u, v, (i + 0.5f) / side, (j + 0.5f) / side);
                var t = float.MaxValue;
                for (int axis = 0; axis < 3; axis++)
                {
                    var (low, high) = axis == 1 ? (0f, 8f) : (-4f, 4f);
                    if (way[axis] > 1e-9f)
                        t = MathF.Min(t, (high - point[axis]) / way[axis]);
                    else if (way[axis] < -1e-9f)
                        t = MathF.Min(t, (low - point[axis]) / way[axis]);
                }
                rows[i] += light[ClosedCellOf(point + way * (t - 0.01f))];
            }
        });
        return (float)(MathF.PI * rows.Sum() / (side * side));
    }

    [CpuKernelTheory]
    [InlineData(Merge.AtRayEnd)]
    [InlineData(Merge.ByReachSight)]
    public void A_Closed_Room_Lit_By_One_Wall_Reads_The_Light_Traced_There_Over_Each_Facings_Points(Merge merge)
    {
        // The closed box, its -x wall alone sending a light of 1 under a black sky, read at 49
        // points for each of five facings, a 7 by 7 grid on the floor facing up, under the lid
        // facing down, before the +z and -z walls facing them and up the middle facing the lit
        // wall, against the light traced against the room's faces, summed over each facing's
        // points.
        const float o = 4 + Wall;
        var cells = Field(OpenBox().Concat(Cuboid(new(-o, 8, -o), new(o, 8 + Wall, o), 6)));
        Level[] chain =
        [
            new(new(-3.5f, 0.25f, -3.5f), 1, 8, 8, 8, NearTexels, 2),
            new(FarCorner, FarSpacing, FarCount, FarCount, FarCount, FarTexels, 4),
            new(new(-14, -10, -14), 4, 8, 8, 8, 16, 8),
            new(new(-28, -28, -28), 8, 8, 8, 8, 16, FarEnd),
        ];
        var light = new float[Size * Size * Size];
        for (int i = 0; i < light.Length; i++)
            light[i] = (Corner + (new Vector3(i % Size, i / Size % Size, i / (Size * Size)) + new Vector3(0.5f)) * Cell).X < -3.75f ? 1 : 0;
        var read = new List<string>();
        var faults = 0;
        var within = merge == Merge.AtRayEnd ? 0.03 : 0.12;
        foreach (var normal in new[] { Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ, -Vector3.UnitX })
        {
            var points = (from a in Enumerable.Range(0, 7)
                          from b in Enumerable.Range(0, 7)
                          select (normal.Y != 0 ? new Vector3(-3 + a, normal.Y > 0 ? 0.25f : 7.75f, -3 + b)
                              : new Vector3(-3 + a, 1 + b, normal.Z != 0 ? -normal.Z * 3.5f : 0.5f), normal)).ToArray();
            var reading = Passes(cells, chain, Floor, new Setting { Points = points, CellLight = light, SkyLight = 0, Merge = merge });
            var share = reading.Taken.Sum() / points.Sum(p => TracedInClosedBox(p.Item1, p.Item2, light));
            read.Add($"{share:0.000} facing {normal}");
            faults += Math.Abs(share - 1) <= within ? 0 : 1;
        }
        var said = $"{merge}, the closed room lit by its -x wall reads {string.Join(", ", read)} of the light traced there";
        output.WriteLine(said);
        // By the merge at the ray's end 1.010 on the floor, 0.978 under the lid, 1.004 before
        // either side wall and 0.997 facing the lit wall, within 3 percent, where each texel
        // marched on to the coarsest level's end alone reads 1.011, 0.975, 0.998 and 0.983, the
        // merge adding 1.4 percent at most to its texels' own rays. By reach sight 0.914, 0.900,
        // 0.970 and 1.006, within 12 percent, the far probes about the near probe's own place read
        // toward where the ray meets a face, each from its own place, losing a tenth on the floor
        // and under the lid, whose texels toward the lit wall graze.
        Assert.True(faults == 0, said);
    }
}
