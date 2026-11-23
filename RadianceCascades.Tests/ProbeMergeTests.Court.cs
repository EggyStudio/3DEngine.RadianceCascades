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
    private static readonly (Vector3 Low, Vector3 High)[] CourtBoxes =
    [
        (new(-7.75f, -0.5f, -4.5f), new(7.75f, 0, 4.5f)), (new(-7.75f, 0, -4.5f), new(7.75f, 8, -4)), (new(-7.75f, 0, 4), new(7.75f, 8, 4.5f)),
        (new(-7.75f, 0, -4), new(-7.25f, 8, 4)), (new(7.25f, 0, -4), new(7.75f, 8, 4)),
        (new(-7.25f, 2.5f, -2.75f), new(7.25f, 3, -2.5f)), (new(-7.25f, 2.5f, 2.5f), new(7.25f, 3, 2.75f)),
        (new(-7.25f, 3, -4), new(7.25f, 3.5f, -2.5f)), (new(-7.25f, 3, 2.5f), new(7.25f, 3.5f, 4)),
    ];

    // The light the court's surfaces send by their place, the sunlit strip's 1 and none elsewhere.
    private static float CourtLight(Vector3 p) => p.Y < 0.25f && MathF.Abs(p.Z) < 1.5f && MathF.Abs(p.X) < 7.25f ? 1 : 0;

    private static LadderScene ArcadeCourt()
    {
        (Vector3, Vector3)[] Walls(float from) =>
        [
            .. Grid(new(-3, from, -3.75f), Vector3.UnitX, 7, Vector3.UnitY, 3, Vector3.UnitZ),
            .. Grid(new(-3, from, 3.75f), Vector3.UnitX, 7, Vector3.UnitY, 3, -Vector3.UnitZ),
        ];
        return BoxScene("a court lit in a strip between arcades", CourtBoxes, CourtLight, 0, new(new(-3.5f, 0.5f, -3.5f), 1, 8, 8, 8, NearTexels, 2),
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
        // than the low wall does, 1.240 and 1.177 of it against the low wall's 1.175 and 1.137.
        // Each point gathered within half a spacing past the finest level's probes, the merges that
        // blend the far probes about the near probe's own place read the high wall 1.67 to 1.78 of
        // the trace after one gather there, so the court parts the merges with this one nearest.
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

    // The probe of an engine's level spaced 2 the court's high wall reads most where the gather
    // takes it to that level, at (-1, 5, -3) of its grid, three quarters of a unit off the wall and
    // a unit and a half over the roof, and the wall's point behind it.
    private static readonly (int X, int Y, int Z) CourtProbe = (3, 4, 2);
    private static readonly Vector3 CourtPoint = new(-1, 5, -3.75f);

    // A probe's light toward n summed over its texels at their middle ways, each texel's merged
    // light by its facing, beside the light traced along the same ways from the probe's grid place
    // and from point, the light of the surface first met among boxes, at the same weights for the
    // three.
    private static (float Merged, float FromProbe, float FromPoint) TexelsAgainstTrace(Chained reading, Level[] levels, int l,
        (int X, int Y, int Z) probe, Vector3 point, Vector3 n, (Vector3 Low, Vector3 High)[] boxes, float[] light)
    {
        var level = levels[l];
        var place = level.Corner + new Vector3(probe.X, probe.Y, probe.Z) * level.Spacing;
        var at = ((probe.Z * level.Y + probe.Y) * level.X + probe.X) * level.Texels * level.Texels;
        float Along(Vector3 from, Vector3 way)
        {
            var t = FirstBox(boxes, from, way);
            return t < LeavesField(from, way) ? light[ClosedCellOf(from + way * (t - 0.01f))] : 0;
        }
        var (merged, fromProbe, fromPoint) = (0f, 0f, 0f);
        for (int k = 0; k < level.Texels * level.Texels; k++)
        {
            var way = Vector3.Normalize(OctahedronPoint((k % level.Texels + 0.5f) / level.Texels, (k / level.Texels + 0.5f) / level.Texels));
            var facing = MathF.Max(Vector3.Dot(n, way), 0);
            merged += reading.LevelLight[l][at + k] * facing;
            fromProbe += Along(place, way) * facing;
            fromPoint += Along(point, way) * facing;
        }
        return (merged, fromProbe, fromPoint);
    }

    [CpuKernelFact]
    public void A_Court_Wall_Past_The_Finest_Probes_Takes_The_Light_The_Probes_A_Level_Above_See_Three_Quarters_Of_A_Unit_Off_It()
    {
        // The court's walls lie a quarter of a unit past an engine's finest level's last probes.
        // A gather that holds a point by the finest level whose probes stand about it on every
        // side takes them to the level spaced 2, and of its probes about the high wall those that
        // count stand three quarters of a unit off the wall over the roof. Such a probe keeps the
        // light traced from where it stands, and along its ways down past the roof's edge onto
        // the sunlit strip the wall's point behind it meets the roof's top, so the wall reads what
        // the probe sees. Gathered from the finest level within half its spacing, as an engine's
        // configuration gathers, its edge probes a quarter of a unit off, the high wall reads near
        // its trace.
        var court = ArcadeCourt();
        var scene = court.AtEngine!();
        var (points, _) = LadderPoints(court);
        var traced = ByPart(scene, points.Select(p => court.Traced(p.Item1, p.Item2)).ToArray());
        var setting = scene.Setting with { Merge = Merge.AtRayEnd, Points = points };
        float High(Chained r) => ByPart(scene, r.Taken[..points.Length])[1] / traced[1];
        var about = Passes(scene.Cells, scene.Levels, scene.Place, setting with { GatherRim = false });
        var rim = Passes(scene.Cells, scene.Levels, scene.Place, setting);
        var (merged, fromProbe, fromPoint) = TexelsAgainstTrace(about, scene.Levels, 1, CourtProbe, CourtPoint, Vector3.UnitZ, CourtBoxes,
            CellLightBy(CourtLight));
        var said = $"the probe's texels give {merged:0.000} toward the court, the light traced along their ways {fromProbe:0.000} from its place "
            + $"and {fromPoint:0.000} from the wall's point, and the high wall reads {High(about):0.000} of its trace, {High(rim):0.000} "
            + "gathered from the finest level within half its spacing";
        output.WriteLine(said);
        Assert.True(MathF.Abs(merged / fromProbe - 1) < 0.01f && fromPoint < fromProbe / 3 && High(about) > 2 && High(rim) < 1.3f, said);
    }

    [CpuKernelFact]
    public void The_Court_And_The_Lamps_Box_Read_Taking_One_Thing_Out_At_A_Time_Where_RC_COURT_Is_Set()
    {
        // dotnet test RadianceCascades.Tests with RC_COURT set reads the court's walls after one
        // gather at an engine's configuration, each point held by the finest level whose probes
        // stand about it on every side, with one thing changed at a time, and follows probes of
        // the lamp's box's two finest levels toward its walls and its lid, the merge at the ray's
        // end throughout, some three minutes in all.
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RC_COURT")))
            return;
        var court = ArcadeCourt();
        var scene = court.AtEngine!();
        var (points, lit) = LadderPoints(court);
        var own = points.Select(p => court.Traced(p.Item1, p.Item2)).ToArray();
        var traced = ByPart(scene, own);
        string Ratios(LadderScene at, float[] light, float[] by) =>
            string.Join(", ", ByPart(at, light).Select((v, i) => $"{at.Parts[i].Name} {v / by[i]:0.000}"));
        var center = new Vector3(0, 4, 0);
        Level[] halved = [.. EngineField(center).Select((f, c) =>
        {
            var spacing = 4 * f.W;
            return new Level(new Vector3(f.X, f.Y, f.Z) + new Vector3(spacing / 2), spacing, 16, 16, 16, 16, c < 3 ? 4 * spacing : EngineEnd, c < 2 ? 8 : 0);
        })];
        var setting = scene.Setting with { Merge = Merge.AtRayEnd, Points = points, GatherRim = false };
        (string, LadderScene, Level[], Setting)[] readings =
        [
            ("held by the probes about it", scene, scene.Levels, setting),
            ("gathered from the finest level", scene, scene.Levels, setting with { GatherHolding = false }),
            ("gathered from the finest level within half its spacing", scene, scene.Levels, setting with { GatherRim = true }),
            ("gathered along each point's own rays", scene, scene.Levels, setting with { Traced = true }),
            ("the level spaced 2 the last, its light past its end none", scene, scene.Levels[..2], setting),
            ("four times the rays a probe, 32 texels a side", scene, [.. scene.Levels.Select(v => v with { Texels = v.Texels * 2, Kept = v.Kept * 2 })],
                setting),
            ("the spacing halved", scene, halved, setting),
            ("the probes at their grid places", scene, scene.Levels, setting with { Moved = false }),
            ("the module's configuration", court, court.Levels, court.Setting with { Merge = Merge.AtRayEnd, Points = points }),
            ("the module's configuration along each point's own rays", court, court.Levels,
                court.Setting with { Merge = Merge.AtRayEnd, Points = points, Traced = true }),
        ];
        foreach (var (name, at, levels, s) in readings)
        {
            var r = Passes(at.Cells, levels, at.Place, s);
            output.WriteLine($"{name}: {Ratios(at, r.Taken[..points.Length], traced)}, the first point's share of the sky {r.SkyShare:0.000}");
        }

        // The share of the trilinear weight of the level spaced 2 over the high wall's points that
        // each row of its probes holds, both walls' together, and whether the gather counts it, a
        // probe inside a box or whose straight line to the point meets one short of the sight's
        // slack left out.
        var level = scene.Levels[1];
        var count = new Vector3(level.X, level.Y, level.Z);
        var rows = new SortedDictionary<string, double>(StringComparer.Ordinal);
        bool Inside(Vector3 q) => CourtBoxes.Any(b => Vector3.Min(q - b.Low, b.High - q) is { X: > 0, Y: > 0, Z: > 0 });
        foreach (var (p, _) in points[scene.Parts[0].Points.Length..lit])
        {
            var g = (p - level.Corner) / level.Spacing;
            var b = Vector3.Clamp(new(MathF.Floor(g.X), MathF.Floor(g.Y), MathF.Floor(g.Z)), Vector3.Zero, count - new Vector3(2));
            var f = Vector3.Clamp(g - b, Vector3.Zero, Vector3.One);
            for (int c = 0; c < 8; c++)
            {
                var offset = new Vector3(c & 1, (c >> 1) & 1, c >> 2);
                var q = level.Corner + (b + offset) * level.Spacing;
                var (d, slack) = (p - q, SeeingSlackOf(level));
                var lerps = Vector3.Lerp(Vector3.One - f, f, offset);
                var counts = !Inside(q) && FirstBox(CourtBoxes, q, Vector3.Normalize(d)) >= d.Length() - slack;
                var key = $"y {q.Y} {MathF.Abs(q.Z)} off the middle{(counts ? "" : " left out")}";
                rows[key] = rows.GetValueOrDefault(key) + lerps.X * lerps.Y * lerps.Z;
            }
        }
        output.WriteLine("the high wall's weight by row of the level spaced 2: "
            + string.Join(", ", rows.Select(r => $"{r.Key} {r.Value / rows.Values.Sum():0.000}")));

        // The lamp's box at an engine's configuration, its walls' points a quarter of a unit past
        // the finest level's last probes as the court's are, read held by the probes about them
        // and within half a spacing, and a probe of each of its two finest levels by a wall
        // toward the room, low and high, and of the finest over the floor toward the lid, against
        // the light traced along their ways and at their places beside the points by them.
        var lamp = LadderScenes().Single(x => x.Name == "a closed box with one lamp");
        var box = lamp.AtEngine!();
        var (lampPoints, _) = LadderPoints(lamp);
        var lampTraced = ByPart(box, lampPoints.Select(p => lamp.Traced(p.Item1, p.Item2)).ToArray());
        var lampLight = CellLightBy(LampAt);
        var lampSetting = box.Setting with { Merge = Merge.AtRayEnd, Points = lampPoints };
        foreach (var (name, s) in new[] { ("held by the probes about them", lampSetting with { GatherRim = false }), ("within half a spacing", lampSetting) })
        {
            var r = Passes(box.Cells, box.Levels, box.Place, s);
            output.WriteLine($"the lamp's box {name}: {Ratios(box, r.Taken[..lampPoints.Length], lampTraced)}");
            if (s.GatherRim)
                foreach (var (l, probe, n) in new[] { (0, (0, 4, 3), Vector3.UnitX), (1, (2, 4, 3), Vector3.UnitX), (0, (0, 6, 3), Vector3.UnitX),
                    (1, (2, 5, 3), Vector3.UnitX), (0, (3, 0, 3), Vector3.UnitY) })
                {
                    var place = box.Levels[l].Corner + new Vector3(probe.Item1, probe.Item2, probe.Item3) * box.Levels[l].Spacing;
                    var by = n == Vector3.UnitX ? place with { X = -3.75f } : place with { Y = 0.25f };
                    var (merged, fromProbe, _) = TexelsAgainstTrace(r, box.Levels, l, probe, by, n, LampBoxes, lampLight);
                    output.WriteLine($"level {l}'s probe at {place} toward {n} keeps {merged:0.000} of the {fromProbe:0.000} its ways trace, "
                        + $"its place traced {TracedAmongBoxes(LampBoxes, place, n, lampLight, 0):0.000} and the point by it "
                        + $"{TracedAmongBoxes(LampBoxes, by, n, lampLight, 0):0.000}");
                }
        }
    }

    // The sight's slack the gather allows a probe of level, as farProbeSees takes it.
    private static float SeeingSlackOf(Level level) => 0.125f * level.Spacing;
}
