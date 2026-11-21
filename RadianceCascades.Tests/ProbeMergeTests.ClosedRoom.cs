using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A closed room every face of which sends the same light, read by a chain of four cascades on
/// the CPU, where every point inside takes that light along every way, so the passes neither add
/// light nor lose it.
/// </summary>
public sealed partial class ProbeMergeTests
{
    [CpuKernelFact]
    public void A_Closed_Room_Whose_Every_Face_Sends_The_Same_Light_Reads_That_Light_At_Every_Point_Through_Four_Cascades()
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
        var reading = Passes(cells, chain, Floor, new Setting { Points = points, CellLight = light, SkyLight = 0 });
        var read = reading.Taken.Select(t => t / MathF.PI).ToArray();
        var said = $"the closed room's {points.Length} points read from {read.Min():0.0000} to {read.Max():0.0000} of the light every face sends, {reading}";
        output.WriteLine(said);
        // 1.0000 at every point. Under broad light, a wall, half the floor or the lid alone
        // sending it, the points read 0.99 to 1.12 of the light traced against the room's faces
        // on the mean and texels each marched on to the coarsest level's end 0.97 to 1.15, single
        // points up to a quarter either way in both and more where they take little, the sampling
        // of 16 by 16 texels.
        Assert.True(read.All(q => Math.Abs(q - 1) <= 0.005), said);
    }
}
