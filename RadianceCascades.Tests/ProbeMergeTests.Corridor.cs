using System.Collections.Concurrent;
using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// A corridor of fixed walls under a sun and a lamp, the field and its levels walked along it with
/// the eye, read as how far each fixed point's gathered light moves over the walk.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // The corridor along x from -60 to 60, 2.5 wide and 3.5 high inside, its floor, walls and roof
    // a unit thick and its ends closed, and a gap 2 wide across its roof over its middle.
    private static readonly (Vector3 Low, Vector3 High)[] CorridorBoxes =
    [
        (new(-61, -1, -2.25f), new(61, 0, 2.25f)), (new(-61, 0, -2.25f), new(61, 3.5f, -1.25f)),
        (new(-61, 0, 1.25f), new(61, 3.5f, 2.25f)), (new(-61, 3.5f, -2.25f), new(-1, 4.5f, 2.25f)),
        (new(1, 3.5f, -2.25f), new(61, 4.5f, 2.25f)), (new(-61, 0, -1.25f), new(-60, 3.5f, 1.25f)),
        (new(60, 0, -1.25f), new(61, 3.5f, 1.25f)),
    ];

    // The floor under the gap sends 0.6, as the sun through the gap lights it, a lamp in the roof
    // from 3 to 4.5 along x and 1.5 across sends 5, and every other face a twentieth, under a sky
    // of 1. Each light is given to the cells a quarter of a unit off its face within its square,
    // since a hit of a coarser cascade of an engine's field lies up to a cell off the surface it
    // meets.
    private static float CorridorLight(Vector3 p) =>
        p.Y < 0.25f && MathF.Abs(p.X) < 1 ? 0.6f : p.Y > 3.25f && p.X is > 3 and < 4.5f && MathF.Abs(p.Z) < 0.75f ? 5 : 0.05f;

    // The floor's points from -6 to 6 along the corridor half a unit apart, two across its middle,
    // and the wall's at -z in rows 0.5, 1.5 and 2.5 high, each a thousandth of a unit off its face.
    private static readonly (Vector3, Vector3)[] CorridorPoints =
    [
        .. Grid(new(-6, 0.001f, -0.5f), Vector3.UnitX * 0.5f, 25, Vector3.UnitZ, 2, Vector3.UnitY),
        .. Grid(new(-6, 0.5f, -1.249f), Vector3.UnitX * 0.5f, 25, Vector3.UnitY, 3, Vector3.UnitZ),
    ];

    // What holds still about the walk's middle as the eye walks, nothing, the field or the levels.
    private enum Still
    {
        None,
        Field,
        Levels,
    }

    // The eyes along the corridor a walk takes in quarter steps over several cells of every
    // level, from -8 to 8 at an engine's configuration, whose coarsest level is spaced 16, and
    // from -4 to 4 at the module's, whose levels stand in the test's one cascade of the field 16
    // across.
    private static float[] CorridorEyes(Configuration configuration)
    {
        var reach = configuration == Configuration.Engine ? 8 : 4;
        return Enumerable.Range(0, reach * 8 + 1).Select(k => k * 0.25f - reach).ToArray();
    }

    // An engine's field about the eye, each cascade's corner snapped to its levels' spacing of 8
    // cells, so its cells and its levels' probes keep their places in the world as the eye moves.
    private static Vector4[] SnappedEngineField(Vector3 eye) => EngineField(Vector3.Zero).Select(f =>
    {
        var step = 8 * f.W;
        return f + new Vector4(new Vector3(MathF.Floor(eye.X / step + 0.5f), MathF.Floor(eye.Y / step + 0.5f), MathF.Floor(eye.Z / step + 0.5f)) * step, 0);
    }).ToArray();

    // The module's levels about the eye along x, each snapped to its spacing, the finest 8 probes
    // along the corridor, 3 up and 2 across in its air, and the ladder's three above it.
    private static Level[] CorridorModuleLevels(float eye) =>
        LadderLevels(new(new(-3.5f, 0.5f, -0.5f), 1, 8, 3, 2, NearTexels, 2))
            .Select(level => level with { Corner = level.Corner + Vector3.UnitX * MathF.Floor(eye / level.Spacing + 0.5f) * level.Spacing })
            .ToArray();

    // Each corridor point's gathered light at each eye of a walk at a configuration by a merge, at
    // an engine's its field and levels laid about the eye but what holds still, and at the module's
    // its levels in the field's one cascade, which holds still. Each point is gathered from the
    // finest level about it within half its spacing past its edge probes, as a binding gathers a
    // pixel, since the walk takes points out of the finest level. A layout the walk meets again is
    // run once.
    private static float[][] CorridorWalk(Configuration configuration, Merge merge, float[] eyes, Still still = Still.None)
    {
        var triangles = CorridorBoxes.SelectMany((box, i) => Cuboid(box.Low, box.High, i + 1)).ToList();
        var setting = new Setting { CellLight = CellLightBy(CorridorLight), SkyLight = 1, LightByPlace = true, GatherRim = true, Merge = merge, Points = CorridorPoints };
        var middle = new Vector3(0, 1.5f, 0);
        var layouts = eyes.Select(e =>
        {
            var eye = new Vector3(e, 1.5f, 0);
            if (configuration == Configuration.Module)
                return (Field: (Vector4[]?)null, Levels: CorridorModuleLevels(e));
            return (Field: SnappedEngineField(still == Still.Field ? middle : eye), Levels: EngineLevels(SnappedEngineField(still == Still.Levels ? middle : eye)));
        }).ToArray();
        string Key((Vector4[]? Field, Level[] Levels) layout) =>
            string.Join(",", layout.Field ?? Array.Empty<Vector4>()) + "|" + string.Join(",", layout.Levels.Select(level => level.Corner));
        var cascades = new ConcurrentDictionary<Vector4, float[]>();
        var moduleCells = configuration == Configuration.Module ? Field(triangles) : Array.Empty<float>();
        var light = new ConcurrentDictionary<string, float[]>();
        var distinct = layouts.DistinctBy(Key).ToArray();
        Parallel.ForEach(distinct, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) }, layout =>
        {
            var cells = layout.Field is { } field
                ? field.SelectMany(f => cascades.GetOrAdd(f, k => FieldCascade(triangles, new(k.X, k.Y, k.Z), k.W))).ToArray()
                : moduleCells;
            var at = layout.Field is null ? setting : EngineSetting(setting, middle) with { FieldCascades = layout.Field };
            light[Key(layout)] = Passes(cells, layout.Levels, CorridorPoints[0].Item1, at).Taken[..CorridorPoints.Length];
        });
        return layouts.Select(layout => light[Key(layout)]).ToArray();
    }

    // How far a walk moves each point's light, every eye's light over the point's mean over the
    // walk taken whole, and each step's change over that mean, each the worst and the 99th part, a
    // point dark at every eye left out.
    private static (float Worst, float Part, float WorstStep, float StepPart) CorridorSwings(float[][] light)
    {
        var (swings, steps) = (new List<float>(), new List<float>());
        for (int k = 0; k < light[0].Length; k++)
        {
            var mean = light.Average(eye => eye[k]);
            if (mean <= 0)
                continue;
            swings.AddRange(light.Select(eye => MathF.Abs(eye[k] / mean - 1)));
            steps.AddRange(Enumerable.Range(1, light.Length - 1).Select(s => MathF.Abs(light[s][k] - light[s - 1][k]) / mean));
        }
        swings.Sort();
        steps.Sort();
        return (swings[^1], swings[(int)(0.99f * (swings.Count - 1))], steps[^1], steps[(int)(0.99f * (steps.Count - 1))]);
    }

    [CpuKernelFact]
    public void A_Corridors_Fixed_Point_Moves_Between_Steps_Of_The_Eye_No_More_Than_The_Dense_Levels_Read()
    {
        // An engine's configuration by reach sight, the eye walked from -8 to 8 along the
        // corridor in quarter steps. Inside a level's region a point's light holds near still, its
        // probes keeping their places, and it moves most where the region steps past it. Half a
        // spacing past a level's edge probes a point takes those probes alone, and past that the
        // level above, and the levels read a point far apart. No probe of the level spaced 4
        // stands in the corridor's air, 2.5 wide, so it reads a point by probes over the roof or
        // past the walls. Where neither a point's level nor the one above has a probe seeing it,
        // the point goes dark. The bound holds what the dense levels read, a change that moves a
        // point more failing it.
        var (worst, part, worstStep, stepPart) = CorridorSwings(CorridorWalk(Configuration.Engine, Merge.ByReachSight, CorridorEyes(Configuration.Engine)));
        var said = $"a fixed point's light moves over the walk {worst:0.000} of its mean at worst and {part:0.000} at the 99th part, and "
            + $"between steps {worstStep:0.000} at worst and {stepPart:0.000} at the 99th part";
        output.WriteLine(said);
        Assert.True(worstStep < 3.6f && stepPart < 1.31f, said);
    }
}
