using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A wall under a lamp, read on the wall in six parts as a pixel's depth points lie on it, and the
/// floor of a slot that no probe of the finest level about it sees.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // The room from -4 to 4 along x and z and from 0 to 4 high, closed on every side, and a lamp in
    // its ceiling 2 square a unit off the wall at -x, sending 5 under a black sky. Every other face
    // sends nothing. The lamp's light is given to every cell over 3.75 within its square, the
    // ceiling's inside among them, since a hit of a coarser cascade of an engine's field lies up to
    // a cell past the surface it meets, and a lamp one cell thick read a probe's light toward it at
    // half the light traced from where the probe stands.
    private static readonly (Vector3 Low, Vector3 High)[] LampWallBoxes = LowRoom(
        (new(4, 0, -4.5f), new(4.5f, 4, 4.5f)), (new(-4.5f, 4, -4.5f), new(4.5f, 4.5f, 4.5f)));

    private static float LampWallLight(Vector3 p) => p.Y > 3.75f && p.X is > -3 and < -1 && MathF.Abs(p.Z) < 1 ? 5 : 0;

    // A part of the wall at -x, 6 rows from y up and 8 points across from z, a quarter of a unit
    // apart, a thousandth of a unit off the wall as a pixel's depth point lies on it, since a
    // point on a box's face counts that face as met where it starts.
    private static (Vector3, Vector3)[] LampWallPart(float y, float z) =>
        Grid(new(-3.999f, y, z), Vector3.UnitY * 0.25f, 6, Vector3.UnitZ * 0.25f, 8, Vector3.UnitX);

    // The wall at -x read in six parts, the top from 2.375 and the bottom from 0.375, each part 2
    // across, the lamp over the top middle.
    private static LadderScene WallUnderALamp() => BoxScene("a wall under a lamp", LampWallBoxes, LampWallLight, 0,
        new(new(-3.5f, 0.5f, -3.5f), 1, 8, 4, 8, NearTexels, 2),
        [
            ("top left", LampWallPart(2.375f, 1.125f)), ("top middle", LampWallPart(2.375f, -0.875f)),
            ("top right", LampWallPart(2.375f, -2.875f)), ("bottom left", LampWallPart(0.375f, 1.125f)),
            ("bottom middle", LampWallPart(0.375f, -0.875f)), ("bottom right", LampWallPart(0.375f, -2.875f)),
        ]);

    [CpuKernelFact]
    public void A_Wall_Under_A_Lamp_Reads_Over_Its_Trace_From_Where_Its_Probes_Stand_And_The_Loop_Gains_As_The_Trace_Does()
    {
        // At an engine's configuration by reach sight, the wall's top middle under the lamp reads
        // over its trace after one gather and at the steady state alike. The finest level's probe
        // half a unit in front of the wall at the lamp's height keeps near the light traced along
        // its texels' ways from where it stands, and from there those ways take far more than they
        // take from the wall's point behind it, which sees the lamp lower. The surplus is the
        // probes' place off the wall, not the merge, the kept texels or the levels, and the loop
        // gains a round as the trace does, so the surplus does not grow to the steady state.
        var each = WallUnderALamp();
        var (points, lit) = LadderPoints(each);
        var transport = TransportOf(each.Bounce!, points);
        var own = points.Select(p => each.Traced(p.Item1, p.Item2)).ToArray();
        var (steady, _, tracedGain, _) = TracedLoop(transport, own, lit);
        var scene = each.AtEngine!();
        var (reading, light, rounds, settles, gain, _) = MergedLoop(scene, transport, scene.Setting with { Merge = Merge.ByReachSight },
            points, lit);
        var first = ByPart(each, reading.Taken[..points.Length])[1] / ByPart(each, own)[1];
        var settled = ByPart(each, light)[1] / ByPart(each, steady)[1];
        var (merged, fromProbe, fromPoint) = TexelsAgainstTrace(reading, scene.Levels, 0, (0, 5, 3), new(-3.999f, 3.5f, -0.5f), Vector3.UnitX,
            LampWallBoxes, CellLightBy(LampWallLight));
        var said = $"the top middle reads {first:0.000} of its trace after one gather and {settled:0.000} at the steady state in {rounds} "
            + $"rounds{(settles ? "" : ", unsettled")}, the loop gaining {gain:0.000} a round where the trace gains {tracedGain:0.000}, and the "
            + $"probe before it keeps {merged / fromProbe:0.000} of the light traced from its place, which takes {fromProbe / fromPoint:0.000} of "
            + "the wall's point's";
        output.WriteLine(said);
        Assert.True(settles && first > 1.3f && settled > 1.3f && settled < first + 0.02f && MathF.Abs(gain - tracedGain) < 0.01f
            && merged / fromProbe < 1.25f && fromProbe / fromPoint > 1.6f, said);
    }

    [CpuKernelFact]
    public void A_Point_No_Probe_Of_Its_Level_Sees_Takes_The_Light_Of_The_Level_Above_And_No_Further()
    {
        // A slot 2 deep between two blocks on the ground under a sky of 1, every face sending a
        // tenth, its floor read at an engine's configuration with its probes where they stand. The
        // finest level's probes about the floor stand inside the blocks or the ground, so none of
        // them sees it. In a slot 0.6 wide the level above's probes in the slot a unit over the
        // floor see it, and the floor takes their light, which sees more of the sky than the floor
        // does. In a slot 0.2 wide, narrower than the level above's cells, the floor where that
        // level's probes do not see it stays dark, where a level further up would lend it the open
        // sky. A point on the open ground beside the blocks, which the finest level's probes see,
        // reads near its trace.
        var light = CellLightBy(_ => 0.1f);
        string said = "";
        var ratios = new[] { 0.6f, 0.2f }.Select(width =>
        {
            (Vector3, Vector3)[] boxes =
            [
                (new(-4.5f, -0.75f, -4.5f), new(4.5f, 0, 4.5f)), (new(-4.5f, 0, -4.5f), new(-3 - width / 2, 2, 4.5f)),
                (new(-3 + width / 2, 0, -4.5f), new(-1.5f, 2, 4.5f)),
            ];
            (Vector3, Vector3)[] points =
                [.. Grid(new(-3, 0.001f, -1.75f), Vector3.UnitZ, 4, Vector3.UnitX, 1, Vector3.UnitY), (new(0.25f, 0.001f, 0.25f), Vector3.UnitY)];
            var scene = BoxScene("a slot", boxes, _ => 0.1f, 1, new(new(-3.5f, 0.5f, -3.5f), 1, 8, 4, 8, NearTexels, 2), [("slot", points)])
                .AtEngine!();
            var reading = Passes(scene.Cells, scene.Levels, scene.Place,
                scene.Setting with { Merge = Merge.ByReachSight, Points = points, Moved = false });
            var each = points.Select((p, k) => reading.Taken[k] / TracedAmongBoxes(boxes, p.Item1, p.Item2, light, 1)).ToArray();
            said += $"a slot {width} wide reads {string.Join(", ", each[..4].Select(r => r.ToString("0.000")))} of its trace on its floor and "
                + $"{each[4]:0.000} on the open ground; ";
            return each;
        }).ToArray();
        output.WriteLine(said);
        Assert.True(ratios[0][..4].All(r => r > 1 && r < 2) && ratios[1][..4].All(r => r < 1.5f) && ratios[1][..4].Any(r => r > 1)
            && ratios.All(each => MathF.Abs(MathF.Log(each[4])) < 0.05f), said);
    }
}
