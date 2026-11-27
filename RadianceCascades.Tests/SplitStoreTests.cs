using System.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace RadianceCascades.Tests;

/// <summary> The split cascades' store run on the CPU over the frames of a walk, read as which
/// probes it keeps, where they stand, whether a probe keeps its entry while the eye moves, which go
/// first when a pool fills, and how a point takes the same light from wherever the eye stands.
/// </summary>
public sealed class SplitStoreTests(ITestOutputHelper output)
{
    // A store the tests can run in a moment, the finest cascade's probes half a unit apart.
    private static readonly SplitStoreShape Shape = new() { Slots = 4096, Spacing = 0.5f, Cascades = 4 };

    // A store's buffers kept from frame to frame and the passes of a frame run over them, its rays
    // traced among boxes, each its low corner and its light then its high corner, in a world's cube
    // 128 a side about its middle under a sky of 1, one box out past the world where none is given.
    private sealed class Store(SplitStoreShape shape, float[]? boxes = null)
    {
        public readonly uint[] Uints = new uint[shape.Uints];
        public readonly float[] Light = new float[4 * shape.Lights];
        private readonly float[] setup = new float[16];
        private readonly float[] boxes = boxes is { Length: > 0 } ? boxes : [1000, 1000, 1000, 0, 1001, 1001, 1001, 0];

        public float[] Run(int pass, Vector3 eye, uint frame, float[] points, int threads, int cascade = 0)
        {
            var count = points.Length / 8;
            (setup[0], setup[1], setup[2], setup[3], setup[4]) = (pass, eye.X, eye.Y, eye.Z, frame);
            (setup[5], setup[6], setup[7]) = (shape.Slots, shape.Spacing, shape.Cascades);
            (setup[11], setup[12], setup[13], setup[14], setup[15]) = (cascade, count, boxes.Length / 8, 64, 1);
            var taken = new float[4 * Math.Max(Math.Max((1 + shape.Cascades) * count, shape.Entries), Math.Max(160, threads))];
            CpuKernel.Run("split_store_cpu.slang", ((uint)(threads + 63) / 64, 1, 1), setup, points.Length == 0 ? new float[8] : points, Uints, Light, taken, boxes);
            return taken;
        }

        // A frame's passes, the table emptied, the probes kept refilled in it, the seen points'
        // probes asked for and each cascade's asked for from the one below, and where rays is set
        // the seen points' rays traced into them, their sums kept and merged where keep is set.
        public void Frame(Vector3 eye, uint frame, float[] points, bool rays = false, bool keep = true)
        {
            Run(0, eye, frame, points, 4 * shape.Slots);
            Run(1, eye, frame, points, shape.Entries);
            Run(2, eye, frame, points, points.Length / 8);
            for (int c = 0; c + 1 < shape.Cascades; c++)
                Run(3, eye, frame, points, shape.PoolProbes(c), c);
            if (rays)
                Run(9, eye, frame, points, points.Length / 8);
            if (rays && keep)
            {
                Keep(eye, frame);
                Merge(eye, frame);
            }
        }

        // Merges each cascade's ways from the coarsest down.
        public void Merge(Vector3 eye, uint frame)
        {
            for (int c = shape.Cascades - 1; c >= 0; c--)
                Run(14, eye, frame, [], shape.CascadeLights(c), c);
        }

        // Keeps each way's light from the frame's sums.
        public void Keep(Vector3 eye, uint frame) => Run(10, eye, frame, [], shape.OwnLights);

        // The first uint of the sums.
        public int SumsAt => (int)Run(5, Vector3.Zero, 0, [], 1)[7];

        // Each entry's probe standing, its place and its cascade.
        public List<(int Entry, Vector3 Place, int Cascade)> Probes(Vector3 eye, uint frame)
        {
            var taken = Run(7, eye, frame, [], shape.Entries);
            return Enumerable.Range(0, shape.Entries).Where(e => taken[4 * e + 3] >= 0)
                .Select(e => (e, new Vector3(taken[4 * e], taken[4 * e + 1], taken[4 * e + 2]), (int)taken[4 * e + 3])).ToList();
        }

        // Each entry's key and the frame it was taken, by key.
        public Dictionary<uint, (int Entry, uint Taken)> Keys()
        {
            var pool = (int)Run(5, Vector3.Zero, 0, [], 1)[5];
            const int stride = SplitStoreShape.EntryUints;
            return Enumerable.Range(0, shape.Entries).Where(e => Uints[pool + stride * e] != 0)
                .ToDictionary(e => Uints[pool + stride * e], e => (e, Uints[pool + stride * e + 2]));
        }
    }

    // The inside faces of the corridor of the merge's tests seen from an eye in it, which sees them
    // all, from -12 to 12 along it an eighth of a unit apart, the gap in its roof left out.
    private static float[] CorridorFaces() => ProbeMergeTests.CorridorSeenFaces(-12, 12);

    // The corridor's boxes, each sending a twentieth, with the lamp in its roof sending 5 and the
    // floor under its gap 0.6, as the merge's tests light them, each a box a hundredth of a unit
    // into the corridor off the face it lights.
    private static readonly float[] CorridorLitBoxes =
    [
        .. new (Vector3 Low, Vector3 High, float Light)[]
        {
            (new(-61, -1, -2.25f), new(61, 0, 2.25f), 0.05f), (new(-61, 0, -2.25f), new(61, 3.5f, -1.25f), 0.05f),
            (new(-61, 0, 1.25f), new(61, 3.5f, 2.25f), 0.05f), (new(-61, 3.5f, -2.25f), new(-1, 4.5f, 2.25f), 0.05f),
            (new(1, 3.5f, -2.25f), new(61, 4.5f, 2.25f), 0.05f), (new(-61, 0, -1.25f), new(-60, 3.5f, 1.25f), 0.05f),
            (new(60, 0, -1.25f), new(61, 3.5f, 1.25f), 0.05f), (new(3, 3.49f, -0.75f), new(4.5f, 3.5f, 0.75f), 5),
            (new(-1, 0, -1.25f), new(1, 0.01f, 1.25f), 0.6f),
        }.SelectMany(b => new[] { b.Low.X, b.Low.Y, b.Low.Z, b.Light, b.High.X, b.High.Y, b.High.Z, 0 }),
    ];

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
            Assert.Equal(32 + SplitStoreShape.AgeBins * shape.Cascades, (int)layout[9]);
        }
        var defaults = new SplitStoreShape();
        output.WriteLine($"the default shape, {defaults.Slots} slots to its finest pool, its spacing {defaults.Spacing} and {defaults.Cascades} cascades, "
            + $"holds {defaults.Entries} probes in {defaults.Uints} uints and {defaults.Lights} float4 of light, {defaults.Bytes / 1048576.0:0.0} MB");
        Assert.Throws<ArgumentOutOfRangeException>(() => (Shape with { Slots = 4000 }).Uints);
        Assert.Throws<ArgumentOutOfRangeException>(() => (Shape with { Slots = 32 }).Uints);
        Assert.Throws<ArgumentOutOfRangeException>(() => (Shape with { Cascades = 9, Slots = 1 << 16 }).Lights);
        Assert.Throws<ArgumentOutOfRangeException>(() => (Shape with { Spacing = 0 }).Lights);
    }

    [CpuKernelFact]
    public void A_Split_Store_Holds_Probes_Only_Beside_The_Points_The_Eye_Sees()
    {
        // Each seen point asks for its nearest probe of the finest cascade, so every probe of it
        // stands within half a cell's diagonal of a point, and each above within half its own of a
        // probe below. A dense grid over the faces' bounds stands every probe of the finest
        // spacing within them, where the store stands those beside the corridor's faces.
        var points = CorridorFaces();
        var eye = new Vector3(0.3f, 1.5f, 0);
        var store = new Store(Shape);
        store.Frame(eye, 1, points);
        var probes = store.Probes(eye, 1);
        var near = store.Run(6, eye, 1, points, points.Length / 8);
        for (int k = 0; k < points.Length / 8; k++)
            Assert.True(near[4 * k + 3] >= 0, $"point {k} at {PlaceOf(points, k)} finds no probe of the finest cascade");
        var seen = Enumerable.Range(0, points.Length / 8).Select(k => PlaceOf(points, k)).ToArray();
        var far = 0f;
        foreach (var (_, place, cascade) in probes)
        {
            var spacing = Shape.Spacing * MathF.Pow(2, cascade);
            var nearest = cascade == 0
                ? seen.Min(p => Vector3.Distance(p, place))
                : probes.Where(p => p.Cascade == cascade - 1).Min(p => Vector3.Distance(p.Place, place));
            far = MathF.Max(far, nearest / spacing);
        }
        var counts = Enumerable.Range(0, Shape.Cascades).Select(c => probes.Count(p => p.Cascade == c)).ToArray();
        var standing = Enumerable.Range(0, Shape.Cascades).Select(c => (int)store.Uints[4 * c + 2]).ToArray();
        var (low, high) = (seen.Aggregate(Vector3.Min), seen.Aggregate(Vector3.Max));
        var cells = (high - low) / Shape.Spacing + Vector3.One * 2;
        var dense = (int)cells.X * (int)cells.Y * (int)cells.Z;
        var said = $"the store holds {string.Join(", ", counts)} probes by cascade against a dense grid's {dense} of the finest over "
            + $"the faces' bounds, each at most {far:0.000} of its spacing from what asked for it";
        output.WriteLine(said);
        Assert.Equal(counts, standing);
        Assert.True(far <= MathF.Sqrt(3) / 2 + 1e-4f && counts[0] < dense / 2, said);
    }

    [CpuKernelFact]
    public void A_Split_Stores_Probe_Keeps_Its_Entry_While_The_Eye_Moves()
    {
        // The eye walks the corridor in quarter steps. A probe asked for at one step and the next
        // keeps its key and its entry, and the frame it was taken, so the light it keeps stays with
        // it, and since each probe stands on a grid fixed in the world, every point asks for the
        // same probe from every step and no probe is taken after the first.
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
        Assert.True(moved == 0 && fresh == 0 && kept > 0, said);
    }

    [CpuKernelFact]
    public void A_Split_Stores_Probe_Is_Kept_Until_Its_Pool_Fills_And_Those_Asked_For_Longest_Ago_Go_First()
    {
        // A pool of 1,024 probes of one cascade. A group of 200 points asks for its probes once,
        // and they stand through 200 frames no point asks for any. Then eleven groups of 200 ask in
        // turn, a group a frame, and the pool, keeping room for twice the probes the frame before
        // took, sheds those asked for longest ago first, so the groups standing are the latest
        // ones, each whole, and once the pool presses at least three stand. The frame after the
        // last group sheds what leaves room for twice its probes, and through 300 frames more with
        // no point asking every probe that stood then still does.
        var shape = new SplitStoreShape { Slots = 1024, Spacing = 0.5f, Cascades = 1 };
        const int group = 200, groups = 12;
        var cells = Enumerable.Range(0, group * groups).Select(k => new Vector3(k % 50 * 0.5f + 0.1f, 0, k / 50 * 0.5f + 0.1f)).ToArray();
        float[] Group(int g) => Floats(cells[(g * group)..((g + 1) * group)].Select(c => (c, Vector3.UnitY)));
        static int GroupOf(Vector3 place) => ((int)MathF.Round(place.Z / 0.5f) * 50 + (int)MathF.Round(place.X / 0.5f)) / group;
        var eye = new Vector3(12, 1.5f, 12);
        var store = new Store(shape);
        // The groups standing, each by its number where all of its probes stand and by its number
        // less one, negated, where some do.
        int[] Standing(uint frame) => [.. store.Probes(eye, frame).GroupBy(p => GroupOf(p.Place)).OrderBy(g => g.Key)
            .Select(g => g.Count() == group ? g.Key : -1 - g.Key)];
        uint frame = 1;
        store.Frame(eye, frame, Group(0));
        while (frame < 201)
            store.Frame(eye, ++frame, Floats([]));
        var idle = Standing(frame);
        var said = new List<string> { $"after 200 frames unasked {string.Join(", ", idle)} stand" };
        var (ordered, least) = (true, groups);
        for (int g = 1; g < groups; g++)
        {
            store.Frame(eye, ++frame, Group(g));
            var standing = Standing(frame);
            said.Add(string.Join(" ", standing));
            ordered &= standing.SequenceEqual(Enumerable.Range(g - standing.Length + 1, standing.Length));
            if (g >= 6)
                least = Math.Min(least, standing.Length);
        }
        store.Frame(eye, ++frame, Floats([]));
        var settled = Standing(frame);
        for (int k = 0; k < 300; k++)
            store.Frame(eye, ++frame, Floats([]));
        var after = Standing(frame);
        var free = (int)store.Uints[0];
        var message = $"{string.Join("; ", said)}, the frame after {string.Join(" ", settled)}, after 300 frames more "
            + $"{string.Join(" ", after)}, {free} entries free";
        output.WriteLine(message);
        Assert.True(idle.SequenceEqual([0]) && ordered && least >= 3 && settled.Length >= 3 && settled[^1] == groups - 1
            && settled.SequenceEqual(Enumerable.Range(groups - settled.Length, settled.Length)) && after.SequenceEqual(settled)
            && free == shape.Slots - group * settled.Length, message);
    }

    [CpuKernelFact]
    public void A_Split_Gather_Takes_The_Same_Light_At_A_Point_From_Wherever_The_Eye_Stands()
    {
        // Points along a line out to 16 from the eye, each finest probe holding along every way a
        // light that rises with its place, so a point takes pi times the probes' light about it by
        // their trilinear weights. The eye at the line's start, at its middle and past its end
        // reads each point the same, the probes standing on a grid fixed in the world, and the
        // light steps nowhere along the line.
        var line = Enumerable.Range(0, 1601).Select(i => (new Vector3(i * 0.01f, 1.5f, 0.2f), Vector3.UnitY)).ToArray();
        var points = Floats(line);
        var eyes = new Vector3[] { new(0, 1.5f, 0), new(8, 1.5f, 0), new(20, 1.5f, 0) };
        var store = new Store(Shape);
        store.Frame(eyes[0], 1, points);
        foreach (var (entry, place, _) in store.Probes(eyes[0], 1).Where(p => p.Cascade == 0))
            for (int way = 0; way < SplitStoreShape.BaseWays; way++)
            {
                var at = 4 * (Shape.OwnLights + entry * SplitStoreShape.BaseWays + way);
                var light = 1 + place.X / 4;
                (store.Light[at], store.Light[at + 1], store.Light[at + 2]) = (light, light, light);
            }
        var taken = eyes.Select(eye => store.Run(4, eye, 1, points, line.Length)).ToArray();
        float worst = 0, apart = 0, steepest = 0;
        for (int k = 0; k < line.Length; k++)
        {
            var expected = MathF.PI * (1 + line[k].Item1.X / 4);
            worst = MathF.Max(worst, MathF.Abs(taken[0][4 * k] - expected) / expected);
            apart = MathF.Max(apart, taken.Max(t => MathF.Abs(t[4 * k] - taken[0][4 * k])) / expected);
            if (k > 0)
                steepest = MathF.Max(steepest, MathF.Abs(taken[0][4 * k] - taken[0][4 * k - 4]) / taken[0][4 * k - 4]);
        }
        var said = $"the gathered light lies {worst:0.000000} of the probes' blend off it at worst, the three eyes read it "
            + $"{apart:0.000000} apart and it steps {steepest:0.0000} of itself between points a hundredth of a unit apart at most";
        output.WriteLine(said);
        Assert.True(worst < 1e-4f && apart < 1e-6f && steepest < 0.01f, said);
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

    // The index of the way of entry's probe of cascade among the probes' own light, the cascades
    // below first, then its pool's entries in order, each its ways.
    private static int LightAt(int cascade, int entry, int way) =>
        Enumerable.Range(0, cascade).Sum(Shape.CascadeLights)
        + (entry - Enumerable.Range(0, cascade).Sum(Shape.PoolProbes)) * (SplitStoreShape.BaseWays << 2 * cascade) + way;

    [CpuKernelFact]
    public void A_Split_Points_Rays_Fall_Evenly_Over_The_Half_Their_Normal_Faces()
    {
        // A point's rays over 4,096 frames each fall in the half its normal faces, the share of
        // them within each eighth of the half's height is an eighth, as rays spread evenly over a
        // half of the sphere fall, their mean is half the normal, and facing up each of the
        // finest cascade's 16 ways of the upper half takes a sixteenth of them. Each way of every
        // cascade is read back as itself.
        const int frames = 4096;
        var store = new Store(Shape);
        var tilted = Vector3.Normalize(new(0.3f, 0.8f, -0.52f));
        var (worstBand, worstWay, meanOff, below) = (0f, 0f, 0f, 0f);
        foreach (var n in new[] { Vector3.UnitZ, tilted })
        {
            var taken = store.Run(11, Vector3.Zero, 0, Floats(Enumerable.Repeat((Vector3.Zero, n), frames)), frames);
            var rays = Enumerable.Range(0, frames).Select(i => new Vector3(taken[4 * i], taken[4 * i + 1], taken[4 * i + 2])).ToArray();
            Assert.All(rays, r => Assert.InRange(r.Length(), 0.9999f, 1.0001f));
            below = MathF.Min(below, rays.Min(r => Vector3.Dot(r, n)));
            var bands = new int[8];
            foreach (var r in rays)
                bands[Math.Min((int)(Vector3.Dot(r, n) * 8), 7)]++;
            worstBand = MathF.Max(worstBand, bands.Max(b => MathF.Abs(b - frames / 8f)) / (frames / 8f));
            meanOff = MathF.Max(meanOff, (rays.Aggregate(Vector3.Zero, (a, r) => a + r) / frames - n / 2).Length());
            if (n == Vector3.UnitZ)
            {
                var ways = new int[32];
                for (int i = 0; i < frames; i++)
                    ways[(int)taken[4 * i + 3]]++;
                Assert.All(ways[16..], w => Assert.Equal(0, w));
                worstWay = ways[..16].Max(w => MathF.Abs(w - frames / 16f)) / (frames / 16f);
            }
        }
        for (int c = 0; c < Shape.Cascades; c++)
        {
            var count = SplitStoreShape.BaseWays << 2 * c;
            var back = store.Run(12, Vector3.Zero, 0, [], count, c);
            Assert.All(Enumerable.Range(0, count), i => Assert.Equal(i, (int)back[4 * i]));
        }
        var said = $"over {frames} frames a point's rays fall {below:0.000000} of their normal at the lowest, each eighth of the half's height "
            + $"takes its share within {worstBand:0.000} of it, their mean lies {meanOff:0.0000} from half the normal and each way of the "
            + $"finest cascade takes its share within {worstWay:0.000} of it";
        output.WriteLine(said);
        Assert.True(below >= -1e-5f && worstBand < 0.02f && meanOff < 0.005f && worstWay < 0.04f, said);
    }

    [CpuKernelFact]
    public void A_Split_Points_Rays_Add_Each_Part_To_The_Probe_Of_The_Interval_It_Lies_In()
    {
        // Each seen point's ray in the lit corridor adds to the probe of each cascade its finest
        // probe leads up to the part of it within that cascade's interval, the light it met there
        // or the sky's past where it left the world with no share let through, or none and all of
        // it let through where it passed, and nothing past where it ended, as the same ray traced
        // whole and split by where it ended gives, and every part finds its probe.
        var points = CorridorFaces();
        var count = points.Length / 8;
        var eye = new Vector3(0, 1.5f, 0);
        var store = new Store(Shape, CorridorLitBoxes);
        store.Frame(eye, 1, points, rays: true, keep: false);
        var whole = store.Run(13, eye, 1, points, count);
        var own = Shape.OwnLights;
        var expected = new uint[5 * own];
        var (rays, missing) = (0, 0);
        var reached = new int[Shape.Cascades];
        static uint Parts(float light) => (uint)(Math.Clamp(light, 0, 16384) * 1024 + 0.5f);
        for (int k = 0; k < count; k++)
        {
            var at = 4 * (1 + Shape.Cascades) * k;
            rays++;
            var (ended, t) = ((uint)whole[at + 3], whole[at + 7]);
            for (int c = 0; c < Shape.Cascades; c++)
            {
                var (entry, way, end) = ((int)whole[at + 4 + 4 * c], (int)whole[at + 5 + 4 * c], whole[at + 6 + 4 * c]);
                var passed = ended == 1 || t >= end;
                reached[c]++;
                if (entry < 0)
                    missing++;
                else
                {
                    var sums = 5 * LightAt(c, entry, way);
                    for (int i = 0; i < 3; i++)
                        expected[sums + i] += passed ? 0 : Parts(whole[at + i]);
                    expected[sums + 3] += passed ? 1024u : 0;
                    expected[sums + 4]++;
                }
                if (!passed)
                    break;
            }
        }
        var sumsAt = store.SumsAt;
        var off = Enumerable.Range(0, expected.Length).Count(i => store.Uints[sumsAt + i] != expected[i]);
        var touched = Enumerable.Range(0, own).Count(i => expected[5 * i + 4] > 0);
        var said = $"{rays} rays of {count} seen points add {reached.Sum()} parts to {touched} ways, {string.Join(", ", reached)} reaching "
            + $"each cascade from the finest, {missing} parts finding no probe and {off} of the sums' uints off the rays traced whole";
        output.WriteLine(said);
        Assert.True(missing == 0 && off == 0 && reached[Shape.Cascades - 1] > 0, said);
    }

    [CpuKernelFact]
    public void A_Split_Ways_Keep_The_Mean_Of_Their_Rays_And_Follow_It_By_Their_Count()
    {
        // Over two frames in the lit corridor each way its rays reach takes the mean of light and
        // of the share let through of the rays its kept light is the mean of and the frame's, a
        // way no ray reached keeps what it held, and the sums are emptied with the count of rays
        // the kept light is the mean of left above the frame's. Over 32 frames the rays reach most
        // of the finest cascade's ways, and a way whose count would pass the cap moves by a share
        // of one over the cap a ray.
        var points = CorridorFaces();
        var eye = new Vector3(0, 1.5f, 0);
        var store = new Store(Shape, CorridorLitBoxes);
        var own = Shape.OwnLights;
        var sumsAt = store.SumsAt;
        float worst = 0;
        var (fresh, followed, held) = (0, 0, 0);
        for (uint frame = 1; frame <= 2; frame++)
        {
            store.Frame(eye, frame, points, rays: true, keep: false);
            var sums = store.Uints[sumsAt..(sumsAt + 5 * own)];
            var before = store.Light[..(4 * own)];
            var probes = store.Probes(eye, frame);
            var keys = store.Keys();
            var takenNow = keys.Values.Where(v => v.Taken == frame).Select(v => v.Entry).ToHashSet();
            store.Keep(eye, frame);
            foreach (var (entry, _, cascade) in probes)
                for (int way = 0; way < SplitStoreShape.BaseWays << 2 * cascade; way++)
                {
                    var i = LightAt(cascade, entry, way);
                    var (n, rays) = (sums[5 * i + 4] & 0xFFFFF, takenNow.Contains(entry) ? 0 : sums[5 * i + 4] >> 20);
                    var after = n == 0 ? rays : Math.Min(rays + n, KeptRays);
                    worst = MathF.Max(worst, Kept(store, sumsAt, i, sums, before, takenNow.Contains(entry), after));
                    if (n == 0)
                        held++;
                    else if (rays == 0)
                        fresh++;
                    else
                        followed++;
                }
        }
        for (uint frame = 3; frame <= 32; frame++)
            store.Frame(eye, frame, points, rays: true);
        var finest = store.Probes(eye, 32).Where(p => p.Cascade == 0).ToList();
        var known = finest.Sum(p => Enumerable.Range(0, SplitStoreShape.BaseWays).Count(w => store.Light[4 * LightAt(0, p.Entry, w) + 3] >= 0));
        var share = known / (float)(finest.Count * SplitStoreShape.BaseWays);

        // Each known finest way handed sums of 10 rays of light 3 as though it were the mean of
        // KeptRays less 4, so its count passes the cap.
        store.Frame(eye, 33, points, keep: false);
        var capped = finest.SelectMany(p => Enumerable.Range(0, SplitStoreShape.BaseWays).Select(w => LightAt(0, p.Entry, w)))
            .Where(i => store.Light[4 * i + 3] >= 0).ToList();
        foreach (var i in capped)
        {
            for (int k = 0; k < 4; k++)
                store.Uints[sumsAt + 5 * i + k] = k < 3 ? 10 * 3 * 1024u : 10 * 512u;
            store.Uints[sumsAt + 5 * i + 4] = (uint)(KeptRays - 4) << 20 | 10;
        }
        var (capSums, capBefore) = (store.Uints[sumsAt..(sumsAt + 5 * own)], store.Light[..(4 * own)]);
        store.Keep(eye, 33);
        var worstCapped = capped.Max(i => Kept(store, sumsAt, i, capSums, capBefore, false, KeptRays));
        var said = $"over two frames {fresh} ways took their rays' mean, {followed} moved toward it and {held} kept their light, each "
            + $"within {worst:0.000000} of it, over 32 frames the rays reached {share:0.000} of the finest cascade's ways, and "
            + $"{capped.Count} ways past the cap moved by a share of one over it a ray within {worstCapped:0.000000}";
        output.WriteLine(said);
        Assert.True(worst < 1e-5f && worstCapped < 1e-5f && fresh > 0 && followed > 0 && held > 0 && capped.Count > 0, said);
    }

    // The most rays a way's kept light is the mean of, as the Slang SplitKeptRays.
    private const uint KeptRays = 256;

    // How far the light kept at way i lies from the mean of what it held, of no rays where fresh,
    // over the rays its sums' count kept above the frame's and of the frame's rays in its sums,
    // those counted up to after, as a share of that mean, or infinity unless the store's sums from
    // sumsAt are emptied with after kept above the frame's count.
    private static float Kept(Store store, int sumsAt, int i, uint[] sums, float[] before, bool fresh, uint after)
    {
        var (n, rays) = (sums[5 * i + 4] & 0xFFFFF, fresh ? 0 : sums[5 * i + 4] >> 20);
        var kept = fresh ? new Vector4(0, 0, 0, -1) : new Vector4(before[4 * i], before[4 * i + 1], before[4 * i + 2], before[4 * i + 3]);
        var mean = new Vector4(sums[5 * i], sums[5 * i + 1], sums[5 * i + 2], sums[5 * i + 3]) / (1024f * Math.Max(n, 1));
        var want = n == 0 ? kept : Vector4.Lerp(kept, mean, Math.Min(n / (float)Math.Max(after, 1), 1));
        var got = new Vector4(store.Light[4 * i], store.Light[4 * i + 1], store.Light[4 * i + 2], store.Light[4 * i + 3]);
        var at = sumsAt + 5 * i;
        var emptied = store.Uints[at] == 0 && store.Uints[at + 1] == 0 && store.Uints[at + 2] == 0 && store.Uints[at + 3] == 0
            && store.Uints[at + 4] == after << 20;
        return emptied ? (got - want).Length() / MathF.Max(want.Length(), 1e-3f) : float.PositiveInfinity;
    }

    [CpuKernelFact]
    public void A_Split_Store_Holds_No_Probe_Past_What_Its_Keys_Tell_Apart()
    {
        // A point within 254 of the finest spacings of the eye asks for its probe, and one past
        // them asks for none and gathers no light, so no probe's key stands for a cell its bits
        // would read back elsewhere, each probe within 255 of its own spacings. A probe kept as the
        // eye walks away from it is freed once its key would read back as another cell.
        var points = Floats([(new(250 * Shape.Spacing, 0, 0), Vector3.UnitY), (new(258 * Shape.Spacing, 0, 0), Vector3.UnitY)]);
        var store = new Store(Shape);
        store.Frame(Vector3.Zero, 1, points);
        var asked = store.Run(6, Vector3.Zero, 1, points, 2);
        var gathered = store.Run(4, Vector3.Zero, 1, points, 2);
        var probes = store.Probes(Vector3.Zero, 1);
        var far = probes.Max(p => MathF.Abs(p.Place.X) / (Shape.Spacing * (1 << p.Cascade)));
        var back = new Vector3(-10 * Shape.Spacing, 0, 0);
        store.Frame(back, 2, Floats([]));
        var left = store.Probes(back, 2).Count(p => p.Cascade == 0);
        var said = $"the near point's probe is entry {asked[3]}, the far point's {asked[7]}, the far point gathers {gathered[4]}, "
            + $"the farthest of {probes.Count} probes stands {far:0.0} of its spacings from the eye, and {left} of the finest stand "
            + "once the eye steps ten spacings back";
        output.WriteLine(said);
        Assert.True(asked[3] >= 0 && asked[7] < 0 && gathered[4] == 0 && far <= 255 && left == 0, said);
    }

    // A probe's cell, its place over its spacing.
    private static (int, int, int) CellOf((int Entry, Vector3 Place, int Cascade) p)
    {
        var q = p.Place / (Shape.Spacing * (1 << p.Cascade));
        return ((int)MathF.Round(q.X), (int)MathF.Round(q.Y), (int)MathF.Round(q.Z));
    }

    // The merged light of each way of every standing probe as the merge's rule gives it from the
    // store's own light, each probe above weighed by the share the probe's own rays let through
    // along the way toward it and by none where no ray reached that way, the one at half its cell
    // rounded up by all of it, the sky of 1 above the coarsest cascade, and where through is set a
    // way no ray reached letting all through in place of merging to none, with the count of the
    // probes above at half a cell rounded up that do not stand, and of the others about a probe
    // standing toward which its rays let some through and let none through.
    private static (Dictionary<(int, int), Vector4[]> Merged, int Missing, int Seen, int Unseen) MergedBy(Store store,
        List<(int Entry, Vector3 Place, int Cascade)> probes, bool through = false)
    {
        var standing = probes.ToDictionary(p => (p.Cascade, CellOf(p)), p => p.Entry);
        var merged = new Dictionary<(int, int), Vector4[]>();
        var (missing, seen, unseen) = (0, 0, 0);
        // The way of each cascade that each step of the lattice of cells falls in, by the kernel's
        // own reading of a way, a probe above standing a step from the probe below along each axis
        // where the probe below lies between two.
        (Vector3, Vector3)[] steps = [.. from x in new[] { -1, 0, 1 } from y in new[] { -1, 0, 1 } from z in new[] { -1, 0, 1 }
                                         select (Vector3.Zero, x == 0 && y == 0 && z == 0 ? Vector3.UnitY : Vector3.Normalize(new(x, y, z)))];
        var wayOf = Enumerable.Range(0, Shape.Cascades).Select(c => store.Run(15, Vector3.Zero, 0, Floats(steps), steps.Length, c)).ToArray();
        for (int c = Shape.Cascades - 1; c >= 0; c--)
            foreach (var p in probes.Where(p => p.Cascade == c))
            {
                var (side, ways) = (4 << c, SplitStoreShape.BaseWays << 2 * c);
                var (x, y, z) = CellOf(p);
                var corners = new List<(float Weight, int Entry)>();
                var (low, high) = ((x >> 1, y >> 1, z >> 1), ((x + 1) >> 1, (y + 1) >> 1, (z + 1) >> 1));
                for (int corner = 0; c + 1 < Shape.Cascades && corner < 8; corner++)
                {
                    var (ox, oy, oz) = (corner & 1, corner >> 1 & 1, corner >> 2);
                    if (ox > high.Item1 - low.Item1 || oy > high.Item2 - low.Item2 || oz > high.Item3 - low.Item3)
                        continue;
                    var place = (low.Item1 + ox, low.Item2 + oy, low.Item3 + oz);
                    if (!standing.TryGetValue((c + 1, place), out var above))
                    {
                        missing += place == high ? 1 : 0;
                        continue;
                    }
                    var step = (2 * ox - (x & 1) + 1) * 9 + (2 * oy - (y & 1) + 1) * 3 + 2 * oz - (z & 1) + 1;
                    var share = store.Light[4 * LightAt(c, p.Entry, (int)wayOf[c][4 * step]) + 3];
                    var weight = place == high ? 1f : share < 0 ? 0f : share;
                    (seen, unseen) = place == high ? (seen, unseen) : weight > 0 ? (seen + 1, unseen) : (seen, unseen + 1);
                    if (weight > 0)
                        corners.Add((weight, above));
                }
                var result = new Vector4[ways];
                for (int w = 0; w < ways; w++)
                {
                    var i = 4 * LightAt(c, p.Entry, w);
                    var own = new Vector4(store.Light[i], store.Light[i + 1], store.Light[i + 2], store.Light[i + 3]);
                    if (own.W < 0 && through)
                        own = new(0, 0, 0, 1);
                    var up = Vector4.One;
                    if (c + 1 < Shape.Cascades)
                    {
                        var first = w / (side * side) * 4 * side * side + 2 * (w / side % side) * 2 * side + 2 * (w % side);
                        var (sum, weights) = (Vector4.Zero, 0f);
                        foreach (var (weight, e) in corners)
                            for (int k = 0; k < 4; k++)
                            {
                                var m = merged[(c + 1, e)][first + (k & 1) + (k >> 1) * 2 * side];
                                if (m.W >= 0)
                                    (sum, weights) = (sum + m * weight, weights + weight);
                            }
                        up = weights > 0 ? sum / weights : new(0, 0, 0, -1);
                    }
                    result[w] = own.W < 0 ? new(0, 0, 0, -1) : up.W < 0 ? new(own.X, own.Y, own.Z, 0)
                        : new(own.X + own.W * up.X, own.Y + own.W * up.Y, own.Z + own.W * up.Z, own.W * up.W);
                }
                merged[(c, p.Entry)] = result;
            }
        return (merged, missing, seen, unseen);
    }

    // The inside faces of a closed room 8 by 4 by 6 about the eye at (0, 2, 0), a quarter of a
    // unit apart an eighth in from each edge, and its six walls as boxes a unit thick, each sending
    // light.
    private static (float[] Points, float[] Boxes) Room(float light)
    {
        var points = new List<(Vector3 Place, Vector3 Normal)>();
        for (float a = -3.875f; a < 4; a += 0.25f)
            for (float b = -2.875f; b < 3; b += 0.25f)
            {
                points.Add((new(a, 0, b), Vector3.UnitY));
                points.Add((new(a, 4, b), -Vector3.UnitY));
            }
        for (float h = 0.125f; h < 4; h += 0.25f)
        {
            for (float b = -2.875f; b < 3; b += 0.25f)
            {
                points.Add((new(-4, h, b), Vector3.UnitX));
                points.Add((new(4, h, b), -Vector3.UnitX));
            }
            for (float a = -3.875f; a < 4; a += 0.25f)
            {
                points.Add((new(a, h, -3), Vector3.UnitZ));
                points.Add((new(a, h, 3), -Vector3.UnitZ));
            }
        }
        var walls = new (Vector3 Low, Vector3 High)[]
        {
            (new(-5, -1, -4), new(5, 0, 4)), (new(-5, 4, -4), new(5, 5, 4)), (new(-5, 0, -4), new(-4, 4, 4)),
            (new(4, 0, -4), new(5, 4, 4)), (new(-4, 0, -4), new(4, 4, -3)), (new(-4, 0, 3), new(4, 4, 4)),
        };
        return (Floats(points), walls.SelectMany(w => new[] { w.Low.X, w.Low.Y, w.Low.Z, light, w.High.X, w.High.Y, w.High.Z, 0 }).ToArray());
    }

    [CpuKernelFact]
    public void A_Split_Merge_Takes_Each_Way_From_The_Probes_Above_That_Stand()
    {
        // Each probe's own light set along every way, a fifth of them reached by no ray, the merge
        // gives each way its own light and, by the share it lets through, the mean of the four ways
        // above it that hold light at the probes of the cascade above about it, each weighed by the
        // share the probe's own rays let through along the way toward it, the one at half its cell
        // rounded up, which its rays' next parts are added to, by all of it, as the rule read on
        // the CPU gives it.
        var points = CorridorFaces();
        var eye = new Vector3(0, 1.5f, 0);
        var store = new Store(Shape, CorridorLitBoxes);
        store.Frame(eye, 1, points);
        var probes = store.Probes(eye, 1);
        var unknown = 0;
        foreach (var (entry, _, cascade) in probes)
            for (int w = 0; w < SplitStoreShape.BaseWays << 2 * cascade; w++)
            {
                var i = 4 * LightAt(cascade, entry, w);
                var h = (uint)HashCode.Combine(entry, w);
                var none = h % 5 == 0;
                unknown += none ? 1 : 0;
                (store.Light[i], store.Light[i + 1], store.Light[i + 2]) = none ? (0, 0, 0) : ((h >> 4) % 97 / 96f, (h >> 11) % 89 / 88f, (h >> 18) % 83 / 82f);
                store.Light[i + 3] = none ? -1 : (h >> 25) % 65 / 64f;
            }
        store.Merge(eye, 1);
        var (merged, missing, seen, unseen) = MergedBy(store, probes);
        float worst = 0;
        var ways = 0;
        foreach (var ((cascade, entry), want) in merged)
            for (int w = 0; w < want.Length; w++, ways++)
            {
                var i = 4 * (Shape.OwnLights + LightAt(cascade, entry, w));
                var got = new Vector4(store.Light[i], store.Light[i + 1], store.Light[i + 2], store.Light[i + 3]);
                worst = MathF.Max(worst, (got - want[w]).Length());
            }
        var said = $"over {ways} ways of {probes.Count} probes, {unknown} reached by no ray, {missing} probes above them "
            + $"not standing and {seen} others about them toward which their own rays let some through and {unseen} none, the merge lies within {worst:0.000000} of its rule";
        output.WriteLine(said);
        Assert.True(worst < 1e-5f && unknown > 0 && seen > 0 && unseen > 0, said);
    }

    [CpuKernelFact]
    public void A_Split_Store_Under_An_Even_Sky_With_Nothing_To_Meet_Gathers_The_Sky()
    {
        // The corridor's points with no corridor about them, under a sky of 1 with nothing for
        // their rays to meet, gather its irradiance of pi after four frames, each way's rays
        // leaving the world past their cascades' intervals.
        var points = CorridorFaces();
        var count = points.Length / 8;
        var eye = new Vector3(0, 1.5f, 0);
        var store = new Store(Shape);
        for (uint frame = 1; frame <= 4; frame++)
            store.Frame(eye, frame, points, rays: true);
        var taken = store.Run(4, eye, 4, points, count);
        var worst = Enumerable.Range(0, count).Max(k => MathF.Abs(taken[4 * k] / MathF.PI - 1));
        var said = $"{count} points under a sky of 1 gather pi times it within {worst:0.000000} of it";
        output.WriteLine(said);
        Assert.True(worst < 1e-4f, said);
    }

    [CpuKernelFact]
    public void A_Split_Store_In_A_Closed_Room_Whose_Walls_Send_One_Light_Gathers_That_Light()
    {
        // In a closed room whose walls all send 0.3, which a ray's part holds as 307 of 1024, every
        // seen point gathers pi times that light after four frames, the ways no ray reached left
        // out of the merge, where letting all their light through would let the sky in at the
        // coarsest cascade.
        var (points, walls) = Room(0.3f);
        var count = points.Length / 8;
        var eye = new Vector3(0, 2, 0);
        var store = new Store(Shape, walls);
        for (uint frame = 1; frame <= 4; frame++)
            store.Frame(eye, frame, points, rays: true);
        var held = 307 / 1024f;
        var taken = store.Run(4, eye, 4, points, count);
        var worst = Enumerable.Range(0, count).Max(k => MathF.Abs(taken[4 * k] / MathF.PI - held) / held);
        var probes = store.Probes(eye, 4);
        var finest = probes.Where(p => p.Cascade == 0).ToList();
        var (through, _, _, _) = MergedBy(store, probes, through: true);
        var (left, _, _, _) = MergedBy(store, probes);
        int Off(Dictionary<(int, int), Vector4[]> merged) =>
            finest.Sum(p => merged[(0, p.Entry)].Count(m => m.W >= 0 && MathF.Abs(m.X - held) > 0.01f * held));
        var known = finest.Sum(p => left[(0, p.Entry)].Count(m => m.W >= 0));
        var brightest = finest.Max(p => through[(0, p.Entry)].Max(m => m.X));
        var said = $"{count} points gather pi times the walls' light within {worst:0.000000} of it, {Off(left)} of the finest cascade's "
            + $"{known} merged ways lying a hundredth off it, and had the ways no ray reached let all through, "
            + $"{Off(through)} of {finest.Count * SplitStoreShape.BaseWays} would, the brightest at {brightest:0.000}";
        output.WriteLine(said);
        Assert.True(worst < 1e-4f && Off(left) == 0, said);
    }
}
