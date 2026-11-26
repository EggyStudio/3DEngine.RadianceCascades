using System.Collections.Concurrent;
using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The split cascades walked along the corridor beside the dense levels, their store kept over the
/// walk as an engine keeps it and their rays traced through the same field, read as how far each
/// fixed point's gathered light moves between steps of the eye.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // The store of the corridor's walk, the finest pool large enough for every probe the walk
    // stands at once with room to spare in every cascade's.
    private static readonly SplitStoreShape CorridorSplitShape = new() { Slots = 4096 };

    // The frames the store runs at the walk's first eye before its first reading, so its ways have
    // taken a few tens of rays each, and the frames at each eye after.
    private const int CorridorSplitWarm = 256;
    private const int CorridorSplitFrames = 4;

    // The readings taken at the first eye after the warm frames, each CorridorSplitFrames apart, by
    // which the swing the rays' noise alone gives is read apart from the walk's.
    private const int CorridorSplitStill = 8;

    // The corridor's inside faces from low to high along it an eighth of a unit apart, each on the
    // world's grid of an eighth so a face keeps its points as the eye moves, as an eye in it sees
    // them, the gap in its roof left out.
    internal static float[] CorridorSeenFaces(float low, float high)
    {
        var points = new List<(Vector3 Place, Vector3 Normal)>();
        for (var i = (int)MathF.Ceiling(low * 8); i <= (int)MathF.Floor(high * 8); i++)
        {
            var x = i / 8f;
            for (int j = 0; j <= 20; j++)
            {
                var z = -1.25f + j * 0.125f;
                points.Add((new(x, 0, z), Vector3.UnitY));
                if (MathF.Abs(x) >= 1)
                    points.Add((new(x, 3.5f, z), -Vector3.UnitY));
            }
            for (int j = 0; j <= 28; j++)
            {
                var y = j * 0.125f;
                points.Add((new(x, y, -1.25f), Vector3.UnitZ));
                points.Add((new(x, y, 1.25f), -Vector3.UnitZ));
            }
        }
        return points.SelectMany(p => new[] { p.Place.X, p.Place.Y, p.Place.Z, 1f, p.Normal.X, p.Normal.Y, p.Normal.Z, 0f }).ToArray();
    }

    // Each corridor point's gathered light at each eye of a walk by the split store, kept over the
    // walk as an engine keeps it, its rays traced through the field at a configuration, an
    // engine's snapped about the eye or the module's one cascade, which holds still. The eye sees
    // the corridor's faces within 16 of it along the corridor and inside the field, the store runs
    // CorridorSplitWarm frames at the first eye and CorridorSplitFrames at each, and each point is
    // gathered after an eye's last frame, with the CorridorSplitStill readings at the first eye
    // and, for each point whose level and the next both stand about it, the next's light over its
    // own.
    private static (float[][] Walk, float[][] Still, List<float> Levels) CorridorSplitWalk(Configuration configuration, float[] eyes)
    {
        var shape = CorridorSplitShape;
        var triangles = CorridorBoxes.SelectMany((box, i) => Cuboid(box.Low, box.High, i + 1)).ToList();
        var cellLight = CellLightBy(CorridorLight);
        var cascades = new ConcurrentDictionary<Vector4, float[]>();
        var moduleCells = configuration == Configuration.Module ? Field(triangles) : Array.Empty<float>();
        var (uints, light) = (new uint[shape.Uints], new float[4 * shape.Lights]);
        var gathered = CorridorPoints.SelectMany(p => new[] { p.Item1.X, p.Item1.Y, p.Item1.Z, 1f, p.Item2.X, p.Item2.Y, p.Item2.Z, 0f }).ToArray();
        var taken = new float[4 * CorridorPoints.Length];
        var setup = new float[64];
        (setup[5], setup[6], setup[7], setup[8], setup[9], setup[10]) = (shape.Slots, shape.Spacing, shape.Levels, shape.Cascades, shape.Reach, shape.Age);
        (setup[13], setup[14], setup[48]) = (CorridorPoints.Length, Size, 1);
        (setup[49], setup[50], setup[51], setup[52], setup[53]) = (Corner.X, Corner.Y, Corner.Z, Cell, Size);
        var (walk, still, levels) = (new float[eyes.Length][], new List<float[]>(), new List<float>());
        uint frame = 0;
        for (int e = 0; e < eyes.Length; e++)
        {
            var eye = new Vector3(eyes[e], 1.5f, 0);
            Vector4[] field = configuration == Configuration.Module ? [new(Corner, Cell)] : SnappedEngineField(eye);
            var cells = configuration == Configuration.Module ? moduleCells
                : field.SelectMany(f => cascades.GetOrAdd(f, k => FieldCascade(triangles, new(k.X, k.Y, k.Z), k.W))).ToArray();
            setup[15] = field.Length;
            for (int c = 0; c < field.Length; c++)
                (setup[16 + 4 * c], setup[17 + 4 * c], setup[18 + 4 * c], setup[19 + 4 * c]) = (field[c].X, field[c].Y, field[c].Z, field[c].W);
            var (low, high) = configuration == Configuration.Module ? (Corner.X + Cell / 2, Corner.X + Size * Cell - Cell / 2) : (-60f, 60f);
            var seen = CorridorSeenFaces(MathF.Max(eye.X - 16, low), MathF.Min(eye.X + 16, high));
            setup[12] = seen.Length / 8;
            void Run(int pass, int threads, int cascade = 0)
            {
                (setup[0], setup[1], setup[2], setup[3], setup[4], setup[11]) = (pass, eye.X, eye.Y, eye.Z, frame, cascade);
                CpuKernel.Run("split_field_cpu.slang", ((uint)(threads + 63) / 64, 1, 1), cells, setup, cellLight, seen, gathered, uints, light, taken);
            }
            float[] Gather()
            {
                Run(7, CorridorPoints.Length);
                return Enumerable.Range(0, CorridorPoints.Length).Select(k => taken[4 * k]).ToArray();
            }
            void Frames(int count)
            {
                for (int f = 0; f < count; f++)
                {
                    frame++;
                    Run(0, 4 * shape.Slots);
                    Run(1, shape.Entries);
                    Run(2, seen.Length / 8);
                    for (int c = 0; c + 1 < shape.Cascades; c++)
                        Run(3, shape.PoolProbes(c), c);
                    Run(4, seen.Length / 8);
                    Run(5, shape.Cascades * SplitStoreShape.BaseWays * shape.Slots);
                    for (int c = shape.Cascades - 1; c >= 0; c--)
                        Run(6, SplitStoreShape.BaseWays * shape.Slots, c);
                }
            }
            if (e == 0)
            {
                Frames(CorridorSplitWarm);
                for (int s = 0; s < CorridorSplitStill; s++)
                {
                    Frames(CorridorSplitFrames);
                    still.Add(Gather());
                }
            }
            Frames(CorridorSplitFrames);
            Run(8, CorridorPoints.Length);
            for (int k = 0; k < CorridorPoints.Length; k++)
                if (taken[4 * k + 2] > 0 && taken[4 * k + 3] > 0)
                    levels.Add(taken[4 * k + 1] / taken[4 * k]);
            walk[e] = Gather();
        }
        levels.Sort();
        return (walk, [.. still], levels);
    }

    [CpuKernelFact]
    public void A_Corridors_Fixed_Point_Moves_Between_Steps_Of_The_Eye_On_The_Split_Probes_No_More_Than_They_Read()
    {
        // The split probes walked as the dense levels are, at both configurations. With the eye
        // held at the walk's first place the rays' noise alone moves a point between readings,
        // each way's light the mean of the few tens of rays it took over the warm frames, short of
        // the cap of 256. As the eye walks a point moves most where its level steps to the next,
        // since the two levels read it apart, a coarser level's probes standing for the rays of a
        // wider patch of the corridor about the lamp and the gap, and the overlap over which the
        // gather blends them is the last tenth of a level's reach, narrower than a quarter step
        // below the fourth level. The test holds each at its reading, the step under the dense
        // levels' 3.558 and 1.299, until the levels read nearer.
        foreach (var (configuration, bound, partBound, stillBound, stillPartBound) in new[] { (Configuration.Engine, 0.45f, 0.21f, 0.09f, 0.075f), (Configuration.Module, 0.33f, 0.17f, 0.07f, 0.06f) })
        {
            var (walk, still, levels) = CorridorSplitWalk(configuration, CorridorEyes(configuration));
            var (worst, part, worstStep, stepPart) = CorridorSwings(walk);
            var (_, _, stillStep, stillPart) = CorridorSwings(still);
            var said = $"at {configuration}'s configuration a fixed point's light on the split probes moves over the walk {worst:0.000} of its mean "
                + $"at worst and {part:0.000} at the 99th part, between steps {worstStep:0.000} at worst and {stepPart:0.000} at the 99th part, "
                + $"with the eye still between readings {stillStep:0.000} at worst and {stillPart:0.000} at the 99th part, and where two levels "
                + $"stand about a point the next reads {levels[levels.Count / 2]:0.000} of its own at the median, {levels[levels.Count / 20]:0.000} "
                + $"at the 5th part and {levels[levels.Count * 19 / 20]:0.000} at the 95th";
            output.WriteLine(said);
            Assert.True(worstStep < bound && stepPart < partBound && stillStep < stillBound && stillPart < stillPartBound, said);
        }
    }
}
