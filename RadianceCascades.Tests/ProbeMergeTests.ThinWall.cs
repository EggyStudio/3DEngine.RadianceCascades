using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A lit room beside a dark one through a wall of one cell of the field, read in the dark room.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // Two rooms 4 high and 8 across along z, the lit one from -6.5 to 0 along x and the dark one
    // from 0.25 to 6.5, the wall between them a quarter of a unit thick, one cell of the field.
    private static readonly (Vector3 Low, Vector3 High)[] ThinWallRooms =
    [
        (new(-7, -0.5f, -4.5f), new(7, 0, 4.5f)),
        (new(-7, 4, -4.5f), new(7, 4.5f, 4.5f)),
        (new(-7, 0, -4.5f), new(-6.5f, 4, 4.5f)),
        (new(6.5f, 0, -4.5f), new(7, 4, 4.5f)),
        (new(-6.5f, 0, -4.5f), new(6.5f, 4, -4)),
        (new(-6.5f, 0, 4), new(6.5f, 4, 4.5f)),
        (new(0, 0, -4), new(0.25f, 4, 4)),
    ];

    // Each face of the lit room sends a light of 1, the wall's face toward it among them, and each
    // of the dark room's none, under a black sky.
    private static float ThinWallLight(Vector3 p) => p.X < 0 ? 1 : 0;

    // The finest level's probes a unit apart from -6.25 to 5.75 along x, the nearest the wall a
    // quarter of a unit before its lit face and half a unit behind its dark one, and the closed
    // room's coarser levels.
    private static readonly Level[] ThinWallLevels =
    [
        new(new(-6.25f, 0.5f, -3.5f), 1, 13, 4, 8, NearTexels, 2),
        new(FarCorner, FarSpacing, FarCount, FarCount, FarCount, FarTexels, 4),
        new(new(-14, -10, -14), 4, 8, 8, 8, 16, 8),
        new(new(-28, -28, -28), 8, 8, 8, 8, 16, FarEnd),
    ];

    // The dark room's points, a grid on its floor facing up and under its ceiling facing down, and
    // a quarter of a unit off the wall facing it.
    private static (Vector3, Vector3)[] DarkRoomPoints() =>
        (from a in Enumerable.Range(0, 6)
         from b in Enumerable.Range(0, 8)
         from n in new[] { Vector3.UnitY, -Vector3.UnitY }
         select (new Vector3(0.75f + a, n.Y > 0 ? 0.25f : 3.75f, -3.5f + b), n))
        .Concat(from a in Enumerable.Range(0, 4)
                from b in Enumerable.Range(0, 8)
                select (new Vector3(0.5f, 0.5f + a, -3.5f + b), -Vector3.UnitX)).ToArray();

    [CpuKernelTheory]
    [InlineData(Merge.AtRayEnd)]
    [InlineData(Merge.ByReachSight)]
    [InlineData(Merge.ByMarchedSight)]
    [InlineData(Merge.AboutProbe)]
    public void No_Light_Passes_A_Wall_Of_One_Cell_Into_The_Dark_Room_Beside_A_Lit_One(Merge merge)
    {
        var cells = Field(ThinWallRooms.SelectMany((box, i) => Cuboid(box.Low, box.High, i + 1)));
        var light = CellLightBy(ThinWallLight);
        var points = DarkRoomPoints();
        var setting = new Setting { Points = points, CellLight = light, SkyLight = 0, Merge = merge };
        var (merged, alone) = (Passes(cells, ThinWallLevels, Floor, setting), Passes(cells, ThinWallLevels, Floor, setting with { Reference = true }));
        var traced = points.Sum(p => TracedAmongBoxes(ThinWallRooms, p.Item1, p.Item2, light, 0));
        // The share of the light a point facing a face that sends 1 takes, pi, the dark room's
        // points take on the mean, and the most one takes.
        var (leak, most) = (merged.Taken.Average() / MathF.PI, merged.Taken.Max() / MathF.PI);
        var said = $"{merge}, the dark room's {points.Length} points take {leak:0.00000} of the lit room's light on the mean and {most:0.00000} at most, "
            + $"traced {traced / points.Length / MathF.PI:0.00000}, each texel marched on alone {alone.Taken.Average() / MathF.PI:0.00000}";
        output.WriteLine(said);
        Assert.True(most <= 0.001f, said);
    }

    // The split store over the same rooms as the ladder lays them, the lit room's points, the dark
    // room's and the places of the bounce's cells seen for the frames of a ladder's round from the
    // eye the ladder reads it from, at the module's configuration. The probes of the levels the
    // dark room's points read stand a unit and two apart, four and eight times the wall, so a
    // probe in the lit face's plane holds the rays of the places off that face, and a probe of the
    // cascade above holds the far parts of the rays of points past the wall as well. A point adds
    // its rays to the nearest probe it sees and gathers from the probes it sees, and the merge
    // takes the light above a way from the one probe its rays go on into, so the dark room's
    // points take none of the lit room's light and the lit room's points read their trace.
    [CpuKernelFact]
    public void No_Light_Passes_A_Wall_Of_One_Cell_Into_The_Dark_Room_On_The_Split_Probes()
    {
        var scene = LadderScenes().Single(s => s.Dark is not null);
        var (points, lit) = LadderPoints(scene);
        var transport = TransportOf(scene.Bounce!, points);
        (Vector3, Vector3)[] all = [.. points.Select(p => (scene.Bounce!.From(p.Item1), p.Item2)), .. transport.Places];
        var store = new SplitRun(CorridorSplitShape, all, scene.Setting.CellLight!, scene.Setting.SkyLight);
        store.Field(scene.Eye, [new(Corner, Cell)], scene.Cells);
        store.Seen(SplitRun.Floats(all));
        store.Frames(LadderSplitFrames);
        var light = store.Gather();
        var traced = points[..lit].Average(p => scene.Traced(p.Item1, p.Item2));
        var (litLight, darkLight) = (light[..lit].Average(), light[lit..points.Length].Max());
        var said = $"the dark room's points take {darkLight / litLight:0.00000} of the lit room's light at most, "
            + $"the lit room's {litLight / traced:0.000} of its trace";
        output.WriteLine(said);
        Assert.True(darkLight <= 0.001f * litLight && MathF.Abs(litLight / traced - 1) < 0.1f, said);
    }
}
