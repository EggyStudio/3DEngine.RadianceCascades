using System.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace RadianceCascades.Tests;

/// <summary>
/// The split cascades' store run on the CPU over the frames of a walk, read as which probes it
/// keeps, where they stand, whether a probe keeps its entry while the eye moves, when it is freed,
/// and how a point takes the light of two levels over their overlap.
/// </summary>
public sealed class SplitStoreTests(ITestOutputHelper output)
{
    // A store the tests can run in a moment, the finest level's probes half a unit apart reaching 4
    // spacings, as a dense level of 8 a side reaches.
    private static readonly SplitStoreShape Shape = new() { Slots = 4096, Spacing = 0.5f, Levels = 4, Cascades = 4, Reach = 2, Age = 8 };

    // A store's buffers kept from frame to frame and the passes of a frame run over them.
    private sealed class Store(SplitStoreShape shape)
    {
        public readonly uint[] Uints = new uint[shape.Uints];
        public readonly float[] Light = new float[4 * shape.Lights];
        private readonly float[] setup = new float[16];

        public float[] Run(int pass, Vector3 eye, uint frame, float[] points, int threads, int cascade = 0)
        {
            var count = points.Length / 8;
            (setup[0], setup[1], setup[2], setup[3], setup[4]) = (pass, eye.X, eye.Y, eye.Z, frame);
            (setup[5], setup[6], setup[7], setup[8], setup[9], setup[10]) = (shape.Slots, shape.Spacing, shape.Levels, shape.Cascades, shape.Reach, shape.Age);
            (setup[11], setup[12]) = (cascade, count);
            var taken = new float[4 * Math.Max(Math.Max(count, shape.Entries), 160)];
            CpuKernel.Run("split_store_cpu.slang", ((uint)(threads + 63) / 64, 1, 1), setup, points.Length == 0 ? new float[8] : points, Uints, Light, taken);
            return taken;
        }

        // A frame's passes, the table emptied, the probes kept refilled in it, the seen points'
        // probes asked for and each cascade's asked for from the one below.
        public void Frame(Vector3 eye, uint frame, float[] points)
        {
            Run(0, eye, frame, points, 4 * shape.Slots);
            Run(1, eye, frame, points, shape.Entries);
            Run(2, eye, frame, points, points.Length / 8);
            for (int c = 0; c + 1 < shape.Cascades; c++)
                Run(3, eye, frame, points, shape.PoolProbes(c), c);
        }

        // Each entry's probe standing, its place, its level and its cascade.
        public List<(int Entry, Vector3 Place, int Level, int Cascade)> Probes(Vector3 eye, uint frame)
        {
            var taken = Run(7, eye, frame, [], shape.Entries);
            return Enumerable.Range(0, shape.Entries).Where(e => taken[4 * e + 3] >= 0)
                .Select(e => (e, new Vector3(taken[4 * e], taken[4 * e + 1], taken[4 * e + 2]), (int)taken[4 * e + 3] / 4, (int)taken[4 * e + 3] % 4)).ToList();
        }

        // Each entry's key and the frame it was taken, by key.
        public Dictionary<uint, (int Entry, uint Taken)> Keys()
        {
            var pool = (int)Run(5, Vector3.Zero, 0, [], 1)[5];
            return Enumerable.Range(0, shape.Entries).Where(e => Uints[pool + 3 * e] != 0)
                .ToDictionary(e => Uints[pool + 3 * e], e => (e, Uints[pool + 3 * e + 2]));
        }
    }

    // The inside faces of the corridor of the merge's tests seen from an eye in it, which sees them
    // all, from -12 to 12 along it an eighth of a unit apart, the gap in its roof left out.
    private static float[] CorridorFaces()
    {
        var points = new List<(Vector3 Place, Vector3 Normal)>();
        for (int i = 0; i <= 192; i++)
        {
            var x = -12 + i * 0.125f;
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
        return Floats(points);
    }

    private static float[] Floats(IEnumerable<(Vector3 Place, Vector3 Normal)> points) =>
        points.SelectMany(p => new[] { p.Place.X, p.Place.Y, p.Place.Z, 1f, p.Normal.X, p.Normal.Y, p.Normal.Z, 0f }).ToArray();

    private static Vector3 PlaceOf(float[] points, int k) => new(points[8 * k], points[8 * k + 1], points[8 * k + 2]);

    [CpuKernelFact]
    public void A_Split_Stores_Layout_Is_The_Size_The_Library_Gives()
    {
        foreach (var shape in new[] { Shape, new SplitStoreShape(), Shape with { Cascades = 1, Slots = 64 } })
        {
            var layout = new Store(shape).Run(5, Vector3.Zero, 0, [], 1);
            Assert.Equal(shape.Uints, (int)layout[0]);
            Assert.Equal(shape.Lights, (int)layout[1]);
            Assert.Equal(shape.Entries, (int)layout[2]);
        }
        Assert.Throws<ArgumentOutOfRangeException>(() => (Shape with { Slots = 4000 }).Uints);
        Assert.Throws<ArgumentOutOfRangeException>(() => (Shape with { Slots = 32 }).Uints);
        Assert.Throws<ArgumentOutOfRangeException>(() => (Shape with { Reach = 65 }).Lights);
    }

    [CpuKernelFact]
    public void A_Split_Store_Holds_Probes_Only_Beside_The_Points_The_Eye_Sees()
    {
        // Each seen point asks for its nearest probe of its level, so every probe of the finest
        // cascade stands within half a cell's diagonal of a point, and each above within half its
        // own of a probe below. A dense grid stands every probe of its levels, 8 a side each,
        // where the store stands those beside the corridor's faces.
        var points = CorridorFaces();
        var eye = new Vector3(0.3f, 1.5f, 0);
        var store = new Store(Shape);
        store.Frame(eye, 1, points);
        var probes = store.Probes(eye, 1);
        var near = store.Run(6, eye, 1, points, points.Length / 8);
        for (int k = 0; k < points.Length / 8; k++)
            Assert.True(near[4 * k + 2] >= 0 && (near[4 * k + 1] <= 0 || near[4 * k + 3] >= 0), $"point {k} at {PlaceOf(points, k)} finds no probe of its level");
        var seen = Enumerable.Range(0, points.Length / 8).Select(k => PlaceOf(points, k)).ToArray();
        var far = 0f;
        foreach (var (_, place, level, cascade) in probes)
        {
            var spacing = Shape.Spacing * MathF.Pow(2, level + cascade);
            var nearest = cascade == 0
                ? seen.Min(p => Vector3.Distance(p, place))
                : probes.Where(p => p.Level == level && p.Cascade == cascade - 1).Min(p => Vector3.Distance(p.Place, place));
            far = MathF.Max(far, nearest / spacing);
        }
        var counts = Enumerable.Range(0, Shape.Cascades).Select(c => probes.Count(p => p.Cascade == c)).ToArray();
        var standing = Enumerable.Range(0, Shape.Cascades).Select(c => (int)store.Uints[4 * c + 2]).ToArray();
        var dense = Shape.Levels * 8 * 8 * 8;
        var said = $"the store holds {string.Join(", ", counts)} probes by cascade against a dense grid's {dense} of the finest, "
            + $"each at most {far:0.000} of its spacing from what asked for it";
        output.WriteLine(said);
        Assert.Equal(counts, standing);
        Assert.True(far <= MathF.Sqrt(3) / 2 + 1e-4f && counts[0] < dense / 2, said);
    }

    [CpuKernelFact]
    public void A_Split_Stores_Probe_Keeps_Its_Entry_While_The_Eye_Moves()
    {
        // The eye walks the corridor in quarter steps. A probe asked for at one step and the next
        // keeps its key and its entry, and the frame it was taken, so the light it keeps stays with
        // it. A probe the eye leaves outside its level's reach is freed, and the point that asked
        // for it asks for one of the level above.
        var points = CorridorFaces();
        var store = new Store(Shape);
        Dictionary<uint, (int Entry, uint Taken)>? before = null;
        int kept = 0, fresh = 0, moved = 0;
        for (uint frame = 1; frame <= 17; frame++)
        {
            store.Frame(new(-2 + (frame - 1) * 0.25f, 1.5f, 0), frame, points);
            var keys = store.Keys();
            if (before is not null)
                foreach (var (key, (entry, taken)) in keys)
                {
                    if (!before.TryGetValue(key, out var was))
                        fresh++;
                    else if (was == (entry, taken))
                        kept++;
                    else
                        moved++;
                }
            before = keys;
        }
        var said = $"over 16 steps of the eye {kept} probes kept their entries, {fresh} were taken and {moved} moved";
        output.WriteLine(said);
        Assert.True(moved == 0 && kept > 10 * fresh, said);
    }

    [CpuKernelFact]
    public void A_Split_Stores_Probe_No_Point_Asks_For_Is_Freed_Past_Its_Age()
    {
        // The points ask for their probes once, then see nothing. Every probe stands for the
        // store's age of frames and is freed at the frame after, every pool's entries free again.
        var points = CorridorFaces();
        var eye = new Vector3(0, 1.5f, 0);
        var store = new Store(Shape);
        store.Frame(eye, 1, points);
        var first = store.Probes(eye, 1).Count;
        var none = Floats([]);
        for (uint frame = 2; frame <= 1 + (uint)Shape.Age; frame++)
            store.Frame(eye, frame, none);
        var aged = store.Probes(eye, 1 + (uint)Shape.Age).Count;
        store.Frame(eye, 2 + (uint)Shape.Age, none);
        var freed = store.Probes(eye, 2 + (uint)Shape.Age).Count;
        var free = Enumerable.Range(0, Shape.Cascades).Select(c => (int)store.Uints[4 * c]).ToArray();
        Assert.Equal(first, aged);
        Assert.Equal(0, freed);
        Assert.Equal(Enumerable.Range(0, Shape.Cascades).Select(Shape.PoolProbes), free);
    }

    [CpuKernelFact]
    public void A_Split_Gather_Blends_Two_Levels_Over_Their_Overlap_And_Steps_Nowhere()
    {
        // Points along a line from the eye out past every level's reach, each level's probes
        // holding a light of 2 to its level along every way, so a point takes its level's light
        // where it stands in one and the two blended by its share over the last tenth of a reach,
        // and its light climbs smoothly where the eye's distance takes it from a level to the next.
        var line = Enumerable.Range(0, 1601).Select(i => (new Vector3(i * 0.01f, 1.5f, 0.2f), Vector3.UnitY)).ToArray();
        var points = Floats(line);
        var eye = new Vector3(0, 1.5f, 0);
        var store = new Store(Shape);
        store.Frame(eye, 1, points);
        foreach (var (entry, _, level, cascade) in store.Probes(eye, 1).Where(p => p.Cascade == 0))
            for (int way = 0; way < SplitStoreShape.BaseWays; way++)
            {
                var at = 4 * (Shape.Cascades * SplitStoreShape.BaseWays * Shape.Slots + entry * SplitStoreShape.BaseWays + way);
                (store.Light[at], store.Light[at + 1], store.Light[at + 2]) = (MathF.Pow(2, level), MathF.Pow(2, level), MathF.Pow(2, level));
            }
        var taken = store.Run(4, eye, 1, points, line.Length);
        float worst = 0, steepest = 0;
        for (int k = 0; k < line.Length; k++)
        {
            var d = MathF.Max(line[k].Item1.X, 0.2f);
            var (level, reach) = (0, Shape.Reach);
            while (d >= reach && level + 1 < Shape.Levels)
                (level, reach) = (level + 1, reach * 2);
            var next = level + 1 < Shape.Levels ? Math.Clamp((d / reach - 0.9f) / 0.1f, 0, 1) : 0;
            var expected = MathF.Pow(2, level) * (1 + next);
            worst = MathF.Max(worst, MathF.Abs(taken[4 * k] - expected) / expected);
            if (k > 0)
                steepest = MathF.Max(steepest, MathF.Abs(taken[4 * k] - taken[4 * k - 4]) / taken[4 * k - 4]);
        }
        var said = $"the gathered light lies {worst:0.000000} of the levels' blend off it at worst and steps {steepest:0.000} of itself "
            + "between points a hundredth of a unit apart at most";
        output.WriteLine(said);
        Assert.True(worst < 1e-4f && steepest < 0.06f, said);
    }

    [CpuKernelFact]
    public void A_Split_Probes_Ways_Each_Stand_For_An_Equal_Share_Of_The_Sphere()
    {
        // The ways of the two finest cascades are unit ways whose sums cancel, and each way of the
        // finest holds the same count of the second's ways nearest it, four, as a way standing
        // for an equal share of the sphere does.
        var store = new Store(Shape);
        var ways = store.Run(8, Vector3.Zero, 0, [], 160);
        var finest = Enumerable.Range(0, 32).Select(i => new Vector3(ways[4 * i], ways[4 * i + 1], ways[4 * i + 2])).ToArray();
        var second = Enumerable.Range(32, 128).Select(i => new Vector3(ways[4 * i], ways[4 * i + 1], ways[4 * i + 2])).ToArray();
        Assert.All(finest.Concat(second), w => Assert.InRange(w.Length(), 0.9999f, 1.0001f));
        Assert.True(finest.Aggregate(Vector3.Zero, (a, w) => a + w).Length() < 1e-4f && second.Aggregate(Vector3.Zero, (a, w) => a + w).Length() < 1e-4f);
        var held = new int[32];
        foreach (var w in second)
            held[Enumerable.Range(0, 32).MaxBy(i => Vector3.Dot(finest[i], w))]++;
        Assert.All(held, h => Assert.Equal(4, h));
    }
}
