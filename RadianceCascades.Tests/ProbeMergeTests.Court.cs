using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The ladder's court lit in a strip between arcades, a floor the sun lights through the court's
/// open top with the shadow lines of its walls' tops along it, and walls in shade lit by that floor
/// alone, read low within the arcades and high above them.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // A court 14.5 long and 8 wide, its walls 8 high and open above to a black sky, the sun
    // lighting its floor in a strip 3 wide down its middle, which sends 1, the shadow lines of the
    // walls' tops along either side. Along each long wall runs an arcade 1.5 deep under a roof at
    // 3, a beam half a unit high and a cell deep across its front under the roof's edge, and every
    // face but the strip sends nothing. Its walls take their light from the strip alone, and from
    // one another, read on the long walls in the court's middle 7, low within the arcades at 0.5,
    // 1.5 and 2.5 and high above them at 4.5, 5.5 and 6.5, its finest level at the module's
    // configuration 8 by 8 by 8 probes a unit apart.
    private static LadderScene ArcadeCourt()
    {
        (Vector3, Vector3)[] boxes =
        [
            (new(-7.75f, -0.5f, -4.5f), new(7.75f, 0, 4.5f)), (new(-7.75f, 0, -4.5f), new(7.75f, 8, -4)), (new(-7.75f, 0, 4), new(7.75f, 8, 4.5f)),
            (new(-7.75f, 0, -4), new(-7.25f, 8, 4)), (new(7.25f, 0, -4), new(7.75f, 8, 4)),
            (new(-7.25f, 2.5f, -2.75f), new(7.25f, 3, -2.5f)), (new(-7.25f, 2.5f, 2.5f), new(7.25f, 3, 2.75f)),
            (new(-7.25f, 3, -4), new(7.25f, 3.5f, -2.5f)), (new(-7.25f, 3, 2.5f), new(7.25f, 3.5f, 4)),
        ];
        (Vector3, Vector3)[] Walls(float from) =>
        [
            .. Grid(new(-3, from, -3.75f), Vector3.UnitX, 7, Vector3.UnitY, 3, Vector3.UnitZ),
            .. Grid(new(-3, from, 3.75f), Vector3.UnitX, 7, Vector3.UnitY, 3, -Vector3.UnitZ),
        ];
        return BoxScene("a court lit in a strip between arcades", boxes,
            p => p.Y < 0.25f && MathF.Abs(p.Z) < 1.5f && MathF.Abs(p.X) < 7.25f ? 1 : 0, 0, new(new(-3.5f, 0.5f, -3.5f), 1, 8, 8, 8, NearTexels, 2),
            [("low wall", Walls(0.5f)), ("high wall", Walls(4.5f))], center: new Vector3(0, 4, 0));
    }

    [CpuKernelFact]
    public void A_Court_Lit_In_A_Strip_Reads_Its_High_Wall_Over_The_Trace_By_The_Merge_At_The_Rays_End_At_An_Engines_Configuration()
    {
        // On one engine's device, a wall in shade beside a sunlit floor, its parts high on the wall
        // carrying the most, read over its path-traced reference by the merge at the ray's end,
        // with the bounce at zero and settled, where the ladder's scenes read that merge within a
        // tenth of the trace. The court holds that case at an engine's configuration. Its high
        // wall reads over the trace after one gather and at the steady state, and more over it
        // than the low wall does, 2.367 and 1.922 of it against the low wall's 1.163 and 1.082.
        // Every merge reads the high wall from 2.37 to 2.73 of the trace after one gather there,
        // so the surplus lies outside how a merge hands a ray off, and the merges do not part on
        // it as the device's did.
        var court = ArcadeCourt();
        var (points, lit) = LadderPoints(court);
        var transport = TransportOf(court.Bounce!, points);
        var own = points.Select(p => court.Traced(p.Item1, p.Item2)).ToArray();
        var (steady, _, _, _) = TracedLoop(transport, own, lit);
        var scene = court.AtEngine!();
        var (reading, light, rounds, settles, _, _) = MergedLoop(scene, transport, scene.Setting with { Merge = Merge.AtRayEnd }, points, lit);
        var (tracedFirst, tracedSteady) = (ByPart(scene, own), ByPart(scene, steady));
        var first = ByPart(scene, reading.Taken[..points.Length]).Select((sum, p) => sum / tracedFirst[p]).ToArray();
        var ratios = ByPart(scene, light).Select((sum, p) => sum / tracedSteady[p]).ToArray();
        var said = $"the low wall reads {first[0]:0.000} and the high wall {first[1]:0.000} of the trace after one gather, {ratios[0]:0.000} and "
            + $"{ratios[1]:0.000} at the steady state in {rounds} rounds{(settles ? "" : ", unsettled")}";
        output.WriteLine(said);
        Assert.True(settles && first[1] > 1 && ratios[1] > 1 && first[1] > first[0] && ratios[1] > ratios[0], said);
    }
}
