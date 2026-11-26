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

    // The short walk's warm frames, its still readings and its eyes at an engine's configuration,
    // the first 2 units of the walk, whose field is built anew at each eye.
    private const int CorridorShortWarm = 64;
    private const int CorridorShortStill = 4;
    private const int CorridorShortEngineEyes = 9;

    // The module's one cascade of the corridor's field, built once for every store over it.
    private static readonly Lazy<float[]> CorridorModuleCells =
        new(() => Field(CorridorBoxes.SelectMany((box, i) => Cuboid(box.Low, box.High, i + 1)).ToList()));

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
        return SplitRun.Floats([.. points]);
    }

    // The split store over a field, its buffers and setup kept from frame to frame as an engine
    // keeps them. Its rays are traced through the field Field gives about the eye and lit by the
    // light given, by the hit's place in the test's one cascade or, where FieldCellLight is set, by
    // the field's own cell, under an even sky, and, where a bounce's grid is given, by the bounce
    // Bounce holds, read a cell back along the ray. The points gathered are given, with room in
    // Taken for the probes' ways pass 9 reads, and the points seen are given by Seen.
    private class SplitRun
    {
        private readonly float[] cellLight, gathered;
        private readonly float[] setup = new float[64];
        private float[] cells = [], seen = [];
        private Vector3 eye;
        private uint frame;

        public SplitRun(SplitStoreShape shape, (Vector3 Place, Vector3 Normal)[] points, float[] light, float sky, Vector4? bounceGrid = null)
        {
            Shape = shape;
            cellLight = bounceGrid is null ? light : [.. light, .. new float[Size * Size * Size]];
            gathered = Floats(points);
            (Uints, Light, Taken) = (new uint[Shape.Uints], new float[4 * Shape.Lights], new float[4 * Math.Max(points.Length, 4 * 8 * SplitStoreShape.BaseWays)]);
            (setup[5], setup[6], setup[7], setup[8], setup[9], setup[10]) = (Shape.Slots, Shape.Spacing, Shape.Levels, Shape.Cascades, Shape.Reach, Shape.Age);
            (setup[13], setup[14], setup[48]) = (points.Length, Size, sky);
            (setup[49], setup[50], setup[51], setup[52], setup[53]) = (Corner.X, Corner.Y, Corner.Z, Cell, Size);
            if (bounceGrid is { } grid)
                (setup[58], setup[59], setup[60], setup[61], setup[62], setup[63]) = (light.Length, grid.X, grid.Y, grid.Z, grid.W, Size);
        }

        public SplitStoreShape Shape { get; }
        public uint[] Uints { get; }
        public float[] Light { get; }
        public float[] Taken { get; }
        public bool FieldCellLight { init => setup[57] = value ? 1 : 0; }

        // Places and normals as the kernel takes them, two float4 a point, 1 in the first's w.
        public static float[] Floats((Vector3 Place, Vector3 Normal)[] points) =>
            points.SelectMany(p => new[] { p.Place.X, p.Place.Y, p.Place.Z, 1f, p.Normal.X, p.Normal.Y, p.Normal.Z, 0f }).ToArray();

        // The eye, the field about it, its cascades' least corners and cells and their cells one
        // cascade after another, finest first.
        public void Field(Vector3 place, Vector4[] field, float[] fieldCells)
        {
            (eye, cells, setup[15]) = (place, fieldCells, field.Length);
            for (int c = 0; c < field.Length; c++)
                (setup[16 + 4 * c], setup[17 + 4 * c], setup[18 + 4 * c], setup[19 + 4 * c]) = (field[c].X, field[c].Y, field[c].Z, field[c].W);
        }

        // The points seen, as Floats gives them.
        public void Seen(float[] points) => (seen, setup[12]) = (points, points.Length / 8);

        // The light each cell of the bounce's grid lends a hit, x fastest.
        public void Bounce(float[] grid) => grid.CopyTo(cellLight, (int)setup[58]);

        // Runs a pass over threads, for a cascade, a level and a point where it takes them.
        public void Run(int pass, int threads, int cascade = 0, int level = 0, int point = 0)
        {
            (setup[0], setup[1], setup[2], setup[3], setup[4], setup[11]) = (pass, eye.X, eye.Y, eye.Z, frame, cascade);
            (setup[55], setup[56]) = (level, point);
            CpuKernel.Run("split_field_cpu.slang", ((uint)(threads + 63) / 64, 1, 1), cells, setup, cellLight, seen, gathered, Uints, Light, Taken);
        }

        // Runs count frames of the store's passes at the eye.
        public void Frames(int count)
        {
            for (int f = 0; f < count; f++)
            {
                frame++;
                Run(0, 4 * Shape.Slots);
                Run(1, Shape.Entries);
                Run(2, seen.Length / 8);
                for (int c = 0; c + 1 < Shape.Cascades; c++)
                    Run(3, Shape.PoolProbes(c), c);
                Run(4, seen.Length / 8);
                Run(5, Shape.Cascades * SplitStoreShape.BaseWays * Shape.Slots);
                for (int c = Shape.Cascades - 1; c >= 0; c--)
                    Run(6, SplitStoreShape.BaseWays * Shape.Slots, c);
            }
        }

        // Each point's gathered light.
        public float[] Gather()
        {
            Run(7, (int)setup[13]);
            return Enumerable.Range(0, (int)setup[13]).Select(k => Taken[4 * k]).ToArray();
        }

        // Each point's light from its level and from the next, and the weights of the probes of
        // each standing about it, four floats a point.
        public float[] Levels()
        {
            Run(8, (int)setup[13]);
            return Taken[..(4 * (int)setup[13])];
        }
    }

    // The split store over the corridor's field at a configuration. At places the eye, the field
    // about it at an engine's configuration or the module's one cascade, which holds still, and the
    // faces the eye sees, the corridor's within 16 of it along the corridor and inside the field
    // unless others are given. Its hits are lit by light by place.
    private sealed class SplitCorridor(Configuration configuration, (Vector3 Place, Vector3 Normal)[] points,
        ConcurrentDictionary<Vector4, float[]>? cascades = null, Func<Vector3, float>? light = null, float sky = 1)
        : SplitRun(CorridorSplitShape, points, CellLightBy(light ?? CorridorLight), sky)
    {
        private readonly List<Triangle> triangles = CorridorBoxes.SelectMany((box, i) => Cuboid(box.Low, box.High, i + 1)).ToList();
        private readonly ConcurrentDictionary<Vector4, float[]> cascades = cascades ?? new();

        public void At(Vector3 place, float[]? seenFaces = null)
        {
            Vector4[] field = configuration == Configuration.Module ? [new(Corner, Cell)] : SnappedEngineField(place);
            Field(place, field, configuration == Configuration.Module ? CorridorModuleCells.Value
                : field.SelectMany(f => cascades.GetOrAdd(f, k => FieldCascade(triangles, new(k.X, k.Y, k.Z), k.W))).ToArray());
            var (low, high) = configuration == Configuration.Module ? (Corner.X + Cell / 2, Corner.X + Size * Cell - Cell / 2) : (-60f, 60f);
            Seen(seenFaces ?? CorridorSeenFaces(MathF.Max(place.X - 16, low), MathF.Min(place.X + 16, high)));
        }
    }

    // Each corridor point's gathered light at each eye of a walk by the split store, kept over the
    // walk as an engine keeps it (SplitCorridor). The store runs warm frames at the first eye and
    // CorridorSplitFrames at each, and each point is gathered after an eye's last frame, with still
    // readings at the first eye and, for each point whose level and the next both stand about it,
    // the next's light over its own.
    private static (float[][] Walk, float[][] Still, List<float> Levels) CorridorSplitWalk(Configuration configuration, float[] eyes,
        int warm = CorridorSplitWarm, int stillReadings = CorridorSplitStill)
    {
        var store = new SplitCorridor(configuration, CorridorPoints);
        var (walk, still, levels) = (new float[eyes.Length][], new List<float[]>(), new List<float>());
        for (int e = 0; e < eyes.Length; e++)
        {
            store.At(new(eyes[e], 1.5f, 0));
            if (e == 0)
            {
                store.Frames(warm);
                for (int s = 0; s < stillReadings; s++)
                {
                    store.Frames(CorridorSplitFrames);
                    still.Add(store.Gather());
                }
            }
            store.Frames(CorridorSplitFrames);
            var taken = store.Levels();
            for (int k = 0; k < CorridorPoints.Length; k++)
                if (taken[4 * k + 2] > 0 && taken[4 * k + 3] > 0)
                    levels.Add(taken[4 * k + 1] / taken[4 * k]);
            walk[e] = store.Gather();
        }
        levels.Sort();
        return (walk, [.. still], levels);
    }

    [CpuKernelFact]
    public void A_Corridors_Fixed_Point_Moves_Between_Steps_Of_The_Eye_On_The_Split_Probes_No_More_Than_They_Read()
    {
        // The split probes walked as the dense levels are, at both configurations. With the eye
        // held at the walk's first place the rays' noise alone moves a point between readings, each
        // way's light the mean of the few tens of rays it took over the warm frames, short of the
        // cap of 256. As the eye walks a point moves most where its level steps to the next, since
        // the two levels read it apart, a coarser level's probes standing for the rays of a wider
        // patch of the corridor about the lamp and the gap, and the overlap over which the gather
        // blends them is the last tenth of a level's reach, narrower than a quarter step below the
        // fourth level. The merge weighs the probes of the cascade above about a probe by the share
        // of each it sees from its place, the one at half its cell rounded up, which its rays go on
        // into, by all of it, so a probe at an odd cell along an axis takes the light of the rays
        // on both sides of it where it sees them. Read through that one probe alone, it took the
        // light of a patch a cell of its own past it, and on the wall under the lamp the finer of
        // two levels read a point up to half again its trace, so the eye's step from one level to
        // the other stepped it most. A far point at a coarse level still steps where its level
        // changes, and a level's probes taken anew hold few rays at the frame they are first
        // gathered. The test holds each at its reading, the step under the dense levels' 3.558 and
        // 1.299, and the merge's eight probes cost the ladder's split rows the light a seen probe
        // above lends from rays of points past the lintel's edge (RC_LADDER=.github/LADDER.md
        // dotnet test RadianceCascades.Tests --filter FullyQualifiedName~The_Ladder). The whole
        // walk runs where RC_LADDER is set, as in the ladder's run, in 6 minutes on a desktop
        // (RC_LADDER=.github/LADDER.md dotnet test RadianceCascades.Tests --filter
        // FullyQualifiedName~On_The_Split_Probes). Elsewhere, as in the build's run, the walk warms
        // 64 frames, reads 4 times with the eye still and walks the first 2 units at an engine's
        // configuration, in 81 seconds (the same without RC_LADDER), since the build's slowest
        // system takes several times what a desktop takes and an engine's field is built anew at
        // each eye.
        var whole = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RC_LADDER"));
        var bounds = whole
            ? new[] { (Configuration.Engine, 0.65f, 0.27f, 0.095f, 0.085f), (Configuration.Module, 0.39f, 0.24f, 0.085f, 0.065f) }
            : new[] { (Configuration.Engine, 0.38f, 0.22f, 0.115f, 0.07f), (Configuration.Module, 0.58f, 0.24f, 0.055f, 0.04f) };
        var failed = new List<string>();
        foreach (var (configuration, bound, partBound, stillBound, stillPartBound) in bounds)
        {
            var eyes = CorridorEyes(configuration);
            var (walk, still, levels) = whole ? CorridorSplitWalk(configuration, eyes)
                : CorridorSplitWalk(configuration, configuration == Configuration.Engine ? eyes[..CorridorShortEngineEyes] : eyes, CorridorShortWarm, CorridorShortStill);
            var (worst, part, worstStep, stepPart) = CorridorSwings(walk);
            var (_, _, stillStep, stillPart) = CorridorSwings(still);
            var said = $"{(whole ? "over the whole walk" : "over the short walk")} at {configuration}'s configuration a fixed point's light on the split probes moves over the walk {worst:0.000} of its mean "
                + $"at worst and {part:0.000} at the 99th part, between steps {worstStep:0.000} at worst and {stepPart:0.000} at the 99th part, "
                + $"with the eye still between readings {stillStep:0.000} at worst and {stillPart:0.000} at the 99th part, and where two levels "
                + $"stand about a point the next reads {levels[levels.Count / 2]:0.000} of its own at the median, {levels[levels.Count / 20]:0.000} "
                + $"at the 5th part and {levels[levels.Count * 19 / 20]:0.000} at the 95th";
            output.WriteLine(said);
            if (!(worstStep < bound && stepPart < partBound && stillStep < stillBound && stillPart < stillPartBound))
                failed.Add(said);
        }
        Assert.True(failed.Count == 0, string.Join("; ", failed));
    }
}
