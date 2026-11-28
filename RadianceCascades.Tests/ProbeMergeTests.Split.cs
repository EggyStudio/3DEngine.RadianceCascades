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
    // The store of the corridor's walk and of the ladder's split rows, half the default's slots,
    // whose finest pool holds every probe the walk asks for at an eye and keeps those of the eyes
    // before until it presses.
    private static readonly SplitStoreShape CorridorSplitShape = new() { Slots = 8192 };

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
    // Bounce holds, read a cell back along the ray. The points gathered are given, and the points
    // seen are given by Seen.
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
            (Uints, Light, Taken) = (new uint[Shape.Uints], new float[4 * Shape.Lights], new float[4 * points.Length]);
            (setup[5], setup[6], setup[7]) = (Shape.Slots, Shape.Spacing, Shape.Cascades);
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

        // Runs a pass over threads, for a cascade where it takes one.
        public void Run(int pass, int threads, int cascade = 0)
        {
            (setup[0], setup[1], setup[2], setup[3], setup[4], setup[11]) = (pass, eye.X, eye.Y, eye.Z, frame, cascade);
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
                // The frame's rays in the low 20 bits of each finest way's count (SplitCountBits),
                // the sums last in the store's uints.
                var sums = Uints.Length - 5 * Shape.OwnLights;
                Rays = Enumerable.Range(0, Shape.CascadeLights(0)).Sum(i => (int)(Uints[sums + 5 * i + 4] & 0xFFFFF));
                Run(5, Shape.OwnLights);
                for (int c = Shape.Cascades - 1; c >= 0; c--)
                    Run(6, Shape.CascadeLights(c), c);
            }
        }

        // Each point's gathered light.
        public float[] Gather()
        {
            Run(7, (int)setup[13]);
            return Enumerable.Range(0, (int)setup[13]).Select(k => Taken[4 * k]).ToArray();
        }

        // Each point's light from the probes about it, each by the share it lets through toward the
        // point, and from those standing by their trilinear weights alone, and the weights of each,
        // four floats a point.
        public float[] Lights()
        {
            Run(8, (int)setup[13]);
            return Taken[..(4 * (int)setup[13])];
        }

        // The probes standing in each cascade, the store's count of them.
        public int[] Standing() => [.. Enumerable.Range(0, Shape.Cascades).Select(c => (int)Uints[4 * c + 2])];

        // The probes of the finest cascade the seen points asked for at the last frame, read from
        // the pool's entries, which begin past the counts, the ages' counts of both parities and
        // the table, as splitPoolAt lays them.
        public int Asked()
        {
            var pool = 32 + 2 * SplitStoreShape.AgeBins * Shape.Cascades + 2 * 4 * Shape.Slots;
            return Enumerable.Range(0, Shape.PoolProbes(0)).Count(e => Uints[pool + SplitStoreShape.EntryUints * e] != 0
                && Uints[pool + SplitStoreShape.EntryUints * e + 1] == frame);
        }

        // The probes of each cascade asked for at the last frame that found no entry left in its
        // pool, read from the table, each of whose slots holds a key and its entry, none for such
        // a probe, the key's cascade one up in its top five bits as splitKey lays it.
        public int[] Short()
        {
            var table = 32 + 2 * SplitStoreShape.AgeBins * Shape.Cascades;
            var shortOf = new int[Shape.Cascades];
            for (int s = 0; s < 4 * Shape.Slots; s++)
                if (Uints[table + 2 * s] != 0 && Uints[table + 2 * s + 1] == uint.MaxValue)
                    shortOf[(int)(Uints[table + 2 * s] >> 27) - 1]++;
            return shortOf;
        }

        // The points seen at a frame.
        public int SeenCount => seen.Length / 8;

        // The rays the finest cascade's probes took at the last frame, each ray of a seen point
        // whose probe stands adding one part to its probe of the finest.
        public int Rays { get; private set; }

        // The frames within which a probe is fresh, the store's own unless given, 0 for one ray a
        // point always.
        public int FreshFrames { init => setup[55] = value + 1; }
    }

    // The split store over the corridor's field at a configuration. At places the eye, the field
    // about it at an engine's configuration or the module's one cascade, which holds still, and the
    // faces the eye sees, the corridor's within 16 of it along the corridor and inside the field
    // unless others are given. Its hits are lit by light by place, and its store takes the walk's
    // shape unless another is given.
    private sealed class SplitCorridor(Configuration configuration, (Vector3 Place, Vector3 Normal)[] points,
        ConcurrentDictionary<Vector4, float[]>? cascades = null, Func<Vector3, float>? light = null, float sky = 1,
        SplitStoreShape? shape = null)
        : SplitRun(shape ?? CorridorSplitShape, points, CellLightBy(light ?? CorridorLight), sky)
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
    // readings at the first eye, and at the last eye the probes standing in each cascade, the
    // finest asked for at its last frame and the rays a frame each takes at the mean.
    private static (float[][] Walk, float[][] Still, int[] Standing, int[] Short, int Asked, float Rays) CorridorSplitWalk(Configuration configuration,
        float[] eyes, int warm = CorridorSplitWarm, int stillReadings = CorridorSplitStill)
    {
        var store = new SplitCorridor(configuration, CorridorPoints);
        var (walk, still) = (new float[eyes.Length][], new List<float[]>());
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
            walk[e] = store.Gather();
        }
        var asked = store.Asked();
        return (walk, [.. still], store.Standing(), store.Short(), asked, store.Rays / (float)Math.Max(asked, 1));
    }

    [CpuKernelFact]
    public void A_Corridors_Fixed_Point_Moves_Between_Steps_Of_The_Eye_On_The_Split_Probes_No_More_Than_They_Read()
    {
        // The split probes walked as the dense levels are, at both configurations. With the eye
        // held at the walk's first place the rays' noise alone moves a point between readings, each
        // way's light the mean of the rays it took over the warm frames. A point reads the finest
        // probes about it alone, on a grid fixed in the world and kept while points ask for them,
        // and through them the chain of the cascades above, so the probes it reads and the light
        // they hold stay as the eye walks, and at the module's configuration, whose field holds
        // still, a point moves between steps of the eye by about the rays' noise. At an engine's
        // configuration the field's cascades snap about the eye and the rays' hits move with them,
        // but a probe's sight of a point is read from the rays it holds rather than marched through
        // the field, so a point there moves between steps by about the rays' noise too. The test
        // holds each at its reading, the step under the dense levels' 3.558 and 1.299, and the
        // merge's eight probes cost the ladder's split rows the light a seen probe above lends from
        // rays of points past the lintel's edge (RC_LADDER=.github/LADDER.md dotnet test
        // RadianceCascades.Tests --filter FullyQualifiedName~The_Ladder). The whole walk runs where
        // RC_LADDER is set, as in the ladder's run, in 5 minutes on a desktop
        // (RC_LADDER=.github/LADDER.md dotnet test RadianceCascades.Tests --filter
        // FullyQualifiedName~On_The_Split_Probes). Elsewhere, as in the build's run, the walk warms
        // 64 frames, reads 4 times with the eye still and walks the first 2 units at an engine's
        // configuration, in 108 seconds (the same without RC_LADDER), since the build's slowest
        // system takes several times what a desktop takes and an engine's field is built anew at
        // each eye.
        var whole = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RC_LADDER"));
        var bounds = whole
            ? new[] { (Configuration.Engine, 0.012f, 0.008f, 0.016f, 0.009f), (Configuration.Module, 0.011f, 0.006f, 0.01f, 0.007f) }
            : new[] { (Configuration.Engine, 0.045f, 0.026f, 0.048f, 0.038f), (Configuration.Module, 0.038f, 0.019f, 0.046f, 0.027f) };
        var failed = new List<string>();
        foreach (var (configuration, bound, partBound, stillBound, stillPartBound) in bounds)
        {
            var eyes = CorridorEyes(configuration);
            var (walk, still, standing, shortOf, asked, rays) = whole ? CorridorSplitWalk(configuration, eyes)
                : CorridorSplitWalk(configuration, configuration == Configuration.Engine ? eyes[..CorridorShortEngineEyes] : eyes, CorridorShortWarm, CorridorShortStill);
            var (worst, part, worstStep, stepPart) = CorridorSwings(walk);
            var (_, _, stillStep, stillPart) = CorridorSwings(still);
            var said = $"{(whole ? "over the whole walk" : "over the short walk")} at {configuration}'s configuration a fixed point's light on the split probes moves over the walk {worst:0.000} of its mean "
                + $"at worst and {part:0.000} at the 99th part, between steps {worstStep:0.000} at worst and {stepPart:0.000} at the 99th part, "
                + $"with the eye still between readings {stillStep:0.000} at worst and {stillPart:0.000} at the 99th part, the store standing "
                + $"{string.Join(", ", standing)} probes by cascade at the last eye, {string.Join(", ", shortOf)} asked for at its last frame with no "
                + $"entry left in their pool, {asked} of the finest asked for and taking {rays:0.0} rays a frame each at the mean, in {CorridorSplitShape.Bytes / 1048576.0:0.0} MB";
            output.WriteLine(said);
            if (!(worstStep < bound && stepPart < partBound && stillStep < stillBound && stillPart < stillPartBound))
                failed.Add(said);
        }
        Assert.True(failed.Count == 0, string.Join("; ", failed));
    }

    [CpuKernelFact]
    public void A_Surface_Turned_Into_View_On_The_Split_Probes_Settles_Within_Its_Probes_Fresh_Frames()
    {
        // The eye at the corridor's middle sees its faces toward -x for 32 frames and then turns to
        // see those toward +x, each face newly seen read at every frame after the turn against its
        // light 160 frames on, at both configurations. Each point whose finest probe is fresh sends
        // 4 rays a frame for its first 16 frames (SplitFreshFrames), so the faces come within a
        // tenth of that light at the 95th part within those frames, where at one ray a point always
        // they are still past it 24 frames on, and fewer of them read dark at the first frame,
        // under a fifth of it. The rays a frame are 4 a seen point while its probe is fresh and one
        // after.
        var eye = new Vector3(0, 1.5f, 0);
        var faces = CorridorSeenFaces(0.5f, 8);
        var newly = Enumerable.Range(0, faces.Length / 8).Select(i =>
        {
            var n = new Vector3(faces[8 * i + 4], faces[8 * i + 5], faces[8 * i + 6]);
            return (new Vector3(faces[8 * i], faces[8 * i + 1], faces[8 * i + 2]) + n * 0.001f, n);
        }).ToArray();
        var jobs = Enum.GetValues<Configuration>().SelectMany(c => new[] { (Configuration: c, Fresh: true), (Configuration: c, Fresh: false) }).ToArray();
        var read = new (int? Settle, int Dark, double[] Means, int Far, int Seen, int FreshRays, int Rays)[jobs.Length];
        Parallel.For(0, jobs.Length, j =>
        {
            var (configuration, fresh) = jobs[j];
            var store = fresh ? new SplitCorridor(configuration, newly) : new SplitCorridor(configuration, newly) { FreshFrames = 0 };
            store.At(eye, CorridorSeenFaces(-8, 0));
            store.Frames(32);
            store.At(eye, CorridorSeenFaces(0, 8));
            var (reads, rays) = (new List<float[]>(), new List<int>());
            for (int f = 0; f < 24; f++)
            {
                store.Frames(1);
                rays.Add(store.Rays);
                reads.Add(store.Gather());
            }
            store.Frames(160);
            var steady = store.Gather();
            double[] Off(float[] light) => [.. Enumerable.Range(0, newly.Length)
                .Select(i => Math.Abs(Math.Log(Math.Max(light[i], 1e-4) / Math.Max(steady[i], 1e-4)))).Order()];
            var offs = reads.Select(Off).ToArray();
            var settle = Enumerable.Range(0, offs.Length).Where(f => offs[f][(int)(0.95 * (newly.Length - 1))] < 0.105).Select(f => (int?)f + 1).FirstOrDefault();
            var dark = Enumerable.Range(0, newly.Length).Count(i => reads[0][i] < 0.2f * steady[i]);
            read[j] = (settle, dark, [offs[0].Average(), offs[3].Average(), offs[15].Average()], offs[15].Count(d => d >= 0.105), store.SeenCount, rays[0], rays[^1]);
        });
        var said = string.Join("; ", jobs.Select((job, j) => $"at {job.Configuration}'s configuration {(job.Fresh ? "with fresh probes' points sending 4 rays" : "at one ray a point always")} "
            + $"{newly.Length} points newly seen come within a tenth of their light at the 95th part {(read[j].Settle is { } f ? $"at frame {f}" : "past frame 24")}, "
            + $"{read[j].Dark} under a fifth of it at the first frame, their mean distance {string.Join(", ", read[j].Means.Select(m => $"{m:0.000}"))} at frames 1, 4 and 16, "
            + $"{read[j].Far} past a tenth at frame 16, {read[j].FreshRays} rays at the first frame and {read[j].Rays} at the 24th for {read[j].Seen} points seen"));
        output.WriteLine(said);
        var held = Enumerable.Range(0, jobs.Length / 2).All(c =>
        {
            var (fresh, one) = (read[2 * c], read[2 * c + 1]);
            return fresh.Settle <= 16 && (one.Settle is null || one.Settle > fresh.Settle) && fresh.Dark < one.Dark
                && fresh.FreshRays > 3.5f * fresh.Seen && fresh.Rays == fresh.Seen && one.FreshRays == one.Seen;
        });
        Assert.True(held, said);
    }
}
