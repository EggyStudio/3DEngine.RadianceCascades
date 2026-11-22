using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The bounce the ladder follows, the light each surface reflects of the light it takes, round
/// after round. A merge's chain is lit at each hit from its own probes of the round before, and
/// the traced light at each hit from the light traced the round before, each until its points'
/// light changes by under a hundredth.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // A scene's bounce. Meets gives where along a way from a point a ray first meets a surface,
    // float.MaxValue where it meets none in the field, OwnAt the light a surface sends of its own
    // where a ray meets it, read a hundredth short of the surface, Sky the light along a way that
    // meets none, AlbedoAt the albedo of the surface at a point of it, Nearest the surface nearest
    // a place and the way it faces, From where a point of the scene is traced from, and Grid the
    // grid the bounce is held on, its least corner and its cell, Size cells a side.
    private sealed record Bounce(Func<Vector3, Vector3, float> Meets, Func<Vector3, float> OwnAt, float Sky, Func<Vector3, float> AlbedoAt,
        Func<Vector3, (Vector3 Point, Vector3 Normal)> Nearest, Vector4 Grid)
    {
        public Func<Vector3, Vector3> From { get; init; } = p => p;
    }

    // The bounce's cells and how light passes between them as traced. Cells are the cells of the
    // grid the scene's points' rays meet a surface in, and those the cells' own rays meet one in
    // in turn, each gathered at the place in Places, a quarter unit off its nearest surface and
    // facing out of it, as the scene's points stand. Albedo is each cell's surface's, Own the
    // light each takes of the surfaces' own light and the sky's, and Seen the cells each one's rays
    // meet, by their order, with the rays meeting each, of CellSide by CellSide. PointSeen holds
    // the same for each point of the scene, of PointSide by PointSide. Fill gives for each cell of
    // the grid the cell whose bounce it holds, the cell itself or the nearest within two cells
    // through cells in the open, or -1. A merge's hit reads the cell a cell back along its ray,
    // after a march that stops short of a surface by up to a tenth of the field's cell there, which
    // may lie a cell or two before the one a traced ray meets the surface in. A cell inside a box
    // holds none, since it lies between a thin wall's two sides.
    private sealed record Transport(int[] Cells, (Vector3, Vector3)[] Places, float[] Albedo, float[] Own, (int Cell, int Rays)[][] Seen,
        (int Cell, int Rays)[][] PointSeen, int[] Fill);

    private const int CellSide = 24, PointSide = 64;

    // The most rounds a loop runs, and the share its points' light changes by in a round under
    // which the loop has settled.
    private const int MostRounds = 40;
    private const float Settled = 0.01f;

    // The cell of grid p lies in, x fastest, or -1 outside it.
    private static int GridCell(Vector4 grid, Vector3 p)
    {
        var q = (p - new Vector3(grid.X, grid.Y, grid.Z)) / grid.W;
        var (x, y, z) = ((int)MathF.Floor(q.X), (int)MathF.Floor(q.Y), (int)MathF.Floor(q.Z));
        return x < 0 || y < 0 || z < 0 || x >= Size || y >= Size || z >= Size ? -1 : (z * Size + y) * Size + x;
    }

    // The rays of a place facing normal, side by side ways spread by their cosine, the cells of the
    // grid they meet a surface in with the rays meeting each, in the order first met, and the light
    // they take of the surfaces' own light and the sky's, pi times their mean.
    private static (List<(int Cell, int Rays)> Seen, float Own) Rays(Bounce bounce, Vector3 place, Vector3 normal, int side)
    {
        var (u, v) = Basis(normal);
        var seen = new Dictionary<int, int>();
        var order = new List<int>();
        double own = 0;
        for (int i = 0; i < side; i++)
            for (int j = 0; j < side; j++)
            {
                var way = CosineWay(normal, u, v, (i + 0.5f) / side, (j + 0.5f) / side);
                var t = bounce.Meets(place, way);
                if (t == float.MaxValue)
                {
                    own += bounce.Sky;
                    continue;
                }
                var at = place + way * (t - 0.01f);
                own += bounce.OwnAt(at);
                var cell = GridCell(bounce.Grid, at);
                if (cell < 0)
                    continue;
                if (!seen.TryAdd(cell, 1))
                    seen[cell]++;
                else
                    order.Add(cell);
            }
        return (order.Select(c => (c, seen[c])).ToList(), (float)(Math.PI * own / (side * side)));
    }

    // The scene's bounce's cells, found outward from its points' rays a wave of cells at a time.
    private static Transport TransportOf(Bounce bounce, (Vector3 Place, Vector3 Normal)[] points)
    {
        var pointRays = new (List<(int Cell, int Rays)> Seen, float Own)[points.Length];
        Parallel.For(0, points.Length, k => pointRays[k] = Rays(bounce, bounce.From(points[k].Place), points[k].Normal, PointSide));
        var order = new Dictionary<int, int>();
        var cells = new List<int>();
        void Add(IEnumerable<(int Cell, int Rays)> met)
        {
            foreach (var (cell, _) in met)
                if (order.TryAdd(cell, cells.Count))
                    cells.Add(cell);
        }
        foreach (var rays in pointRays)
            Add(rays.Seen);
        var places = new List<(Vector3, Vector3)>();
        var albedo = new List<float>();
        var cellRays = new List<(List<(int Cell, int Rays)> Seen, float Own)>();
        while (places.Count < cells.Count)
        {
            var wave = cells.Skip(places.Count).ToArray();
            var at = new (Vector3, Vector3)[wave.Length];
            var reflects = new float[wave.Length];
            var rays = new (List<(int Cell, int Rays)> Seen, float Own)[wave.Length];
            Parallel.For(0, wave.Length, k =>
            {
                var middle = new Vector3(bounce.Grid.X, bounce.Grid.Y, bounce.Grid.Z)
                    + (new Vector3(wave[k] % Size, wave[k] / Size % Size, wave[k] / (Size * Size)) + new Vector3(0.5f)) * bounce.Grid.W;
                var (surface, normal) = bounce.Nearest(middle);
                at[k] = (surface + normal * 0.25f, normal);
                reflects[k] = bounce.AlbedoAt(surface);
                rays[k] = Rays(bounce, at[k].Item1, normal, CellSide);
            });
            places.AddRange(at);
            albedo.AddRange(reflects);
            cellRays.AddRange(rays);
            foreach (var each in rays)
                Add(each.Seen);
        }
        var fill = new int[Size * Size * Size];
        Array.Fill(fill, -1);
        var frontier = new List<int>();
        for (int k = 0; k < cells.Count; k++)
            fill[cells[k]] = k;
        frontier.AddRange(cells);
        bool Open(int g)
        {
            var middle = new Vector3(bounce.Grid.X, bounce.Grid.Y, bounce.Grid.Z)
                + (new Vector3(g % Size, g / Size % Size, g / (Size * Size)) + new Vector3(0.5f)) * bounce.Grid.W;
            var (surface, normal) = bounce.Nearest(middle);
            return Vector3.Dot(middle - surface, normal) > 0;
        }
        for (int step = 0; step < 2; step++)
        {
            var next = new List<int>();
            foreach (var g in frontier)
            {
                var (x, y, z) = (g % Size, g / Size % Size, g / (Size * Size));
                foreach (var (dx, dy, dz) in new[] { (1, 0, 0), (-1, 0, 0), (0, 1, 0), (0, -1, 0), (0, 0, 1), (0, 0, -1) })
                {
                    var (a, b, c) = (x + dx, y + dy, z + dz);
                    if (a < 0 || b < 0 || c < 0 || a >= Size || b >= Size || c >= Size)
                        continue;
                    var h = (c * Size + b) * Size + a;
                    if (fill[h] >= 0 || !Open(h))
                        continue;
                    fill[h] = fill[g];
                    next.Add(h);
                }
            }
            frontier = next;
        }
        (int, int)[] Ordered(List<(int Cell, int Rays)> seen) => seen.Select(s => (order[s.Cell], s.Rays)).ToArray();
        return new([.. cells], [.. places], [.. albedo], cellRays.Select(r => r.Own).ToArray(), cellRays.Select(r => Ordered(r.Seen)).ToArray(),
            pointRays.Select(r => Ordered(r.Seen)).ToArray(), fill);
    }

    // The light each cell of the grid lends a hit the next round, its bounce cell's albedo over pi
    // times the light that cell took.
    private static float[] Spread(Transport transport, float[] taken)
    {
        var grid = new float[Size * Size * Size];
        for (int g = 0; g < grid.Length; g++)
            if (transport.Fill[g] is var f and >= 0)
                grid[g] = transport.Albedo[f] / MathF.PI * taken[f];
        return grid;
    }

    // The light a place takes of the bounce, its rays' cells' albedo over pi times what each took
    // the round before, pi times their mean over side by side rays.
    private static float BounceTaken((int Cell, int Rays)[] seen, Transport transport, float[] before, int side)
    {
        double sum = 0;
        foreach (var (cell, rays) in seen)
            sum += rays * transport.Albedo[cell] / Math.PI * before[cell];
        return (float)(Math.PI * sum / (side * side));
    }

    // Whether a loop's last round changed its points' light by under a hundredth.
    private static bool HasSettled(List<double> sums) => sums.Count > 1 && Math.Abs(sums[^1] - sums[^2]) < Settled * Math.Abs(sums[^1]);

    // The gain a round of a loop, the change its last round made over the change of the round
    // before, the first round's light standing for the change it made where two rounds ran.
    private static float Gain(List<double> sums)
    {
        if (sums.Count < 2)
            return 0;
        var before = sums.Count < 3 ? sums[^2] : sums[^2] - sums[^3];
        return before == 0 ? 0 : (float)((sums[^1] - sums[^2]) / before);
    }

    // The traced loop. Each round a point takes the light traced to it of the surfaces' own and the
    // sky's (own), and the light its rays' cells reflect of what they took the round before, and a
    // cell alike, from none the first round, until the first lit points' light changes by under a
    // hundredth, with the rounds it took, the gain a round and the points' light each round.
    private static (float[] Light, int Rounds, float Gain, List<float[]> EachRound) TracedLoop(Transport transport, float[] own, int lit)
    {
        var (cells, points) = (transport.Own, own);
        var sums = new List<double> { points[..lit].Sum() };
        var each = new List<float[]> { points };
        while (sums.Count < MostRounds && !HasSettled(sums))
        {
            var before = cells;
            cells = transport.Own.Select((o, k) => o + BounceTaken(transport.Seen[k], transport, before, CellSide)).ToArray();
            points = own.Select((o, k) => o + BounceTaken(transport.PointSeen[k], transport, before, PointSide)).ToArray();
            sums.Add(points[..lit].Sum());
            each.Add(points);
        }
        return (points, sums.Count, Gain(sums), each);
    }

    // A merge's loop over a scene at a setting, each round's chain lit at every hit by the
    // surface's own light and the light its bounce cell took of the round before's probes,
    // reflected, until the first lit points' light changes by under a hundredth, with the first
    // round's reading, each point's light at the last, the rounds it took, whether it settled
    // within MostRounds, the gain a round and the points' light each round.
    private static (Chained First, float[] Light, int Rounds, bool Settles, float Gain, List<float[]> EachRound) MergedLoop(LadderScene scene, Transport transport, Setting setting,
        (Vector3, Vector3)[] points, int lit)
    {
        (Vector3, Vector3)[] all = [.. points, .. transport.Places];
        var bounce = new float[Size * Size * Size];
        var sums = new List<double>();
        var each = new List<float[]>();
        Chained first = default;
        while (true)
        {
            var reading = Passes(scene.Cells, scene.Levels, scene.Place, setting with { Points = all, Bounce = bounce, BounceGrid = scene.Bounce!.Grid });
            if (sums.Count == 0)
                first = reading;
            var light = reading.Taken[..points.Length];
            sums.Add(light[..lit].Sum());
            each.Add(light);
            if (sums.Count == MostRounds || HasSettled(sums))
                return (first, light, sums.Count, HasSettled(sums), Gain(sums), each);
            bounce = Spread(transport, reading.Taken[points.Length..]);
        }
    }

    // The bounce among boxes in the test's one cascade of the field, a ray meeting the first box it
    // enters short of leaving the field and taking the light of the cell it meets it in, as the
    // light traced among the boxes takes it, every surface of albedo Albedo but where albedo gives
    // another.
    private static Bounce BoxBounce((Vector3 Low, Vector3 High)[] boxes, float[] light, float sky, Func<Vector3, float>? albedo = null) => new(
        (p, way) => FirstBox(boxes, p, way) is var t && t < LeavesField(p, way) ? t : float.MaxValue, p => light[ClosedCellOf(p)], sky,
        albedo ?? (_ => Albedo), p => NearestSurface(p, boxes), new(Corner, Cell));

    // The lintel's room's bounce, a ray meeting its boxes or the sky, each surface sending the
    // light the finest cascade of LintelField that holds the place gives it, its albedo over pi
    // times the sky's irradiance there, held on a grid of half a unit over the room and the ground
    // before its opening, each point traced from where the test's rule moves it.
    private static Bounce LintelBounce(float[] light) => new(
        (p, way) => MeetsBox(p, way, out var at, out _) ? at : float.MaxValue, p => LintelLightAt(light, p), LintelSky, _ => LintelAlbedo,
        p => NearestSurface(p), new(-20, -1, -16, 0.5f)) { From = MovedOff };

    // The light the finest cascade of LintelField that holds p gives the cell p lies in.
    private static float LintelLightAt(float[] light, Vector3 p)
    {
        for (int c = 0; c < LintelField.Length; c++)
        {
            var q = (p - new Vector3(LintelField[c].X, LintelField[c].Y, LintelField[c].Z)) / LintelField[c].W;
            var (x, y, z) = ((int)MathF.Floor(q.X), (int)MathF.Floor(q.Y), (int)MathF.Floor(q.Z));
            if (x >= 0 && y >= 0 && z >= 0 && x < Size && y < Size && z < Size)
                return light[Size * Size * Size * c + (z * Size + y) * Size + x];
        }
        return 0;
    }

    [CpuKernelFact]
    public void A_Closed_Room_Whose_Faces_Send_The_Same_Light_And_Reflect_Half_Settles_Near_Twice_It_Traced_And_Merged_At_The_Ray_End()
    {
        // The closed room 8 a side, every face sending 1 of its own under a black sky and
        // reflecting Albedo, a half, of the light it takes, read on the floor facing up, on a
        // wall facing in and under the lid facing down. Every ray a point or a cell casts meets a
        // face that took what the point took the round before, so the traced loop takes pi times
        // 1, 1.5, 1.75 and on, its gain a round a half, and settles in the seventh round, whose
        // change of pi over 64 is the first under a hundredth of its light, at pi times 2 less a
        // sixty-fourth. The merge at the ray's end reads the room's light whole at one gather, so
        // its loop follows the traced one round for round.
        const float o = 4 + Wall;
        (Vector3, Vector3)[] boxes =
        [
            (new(-o, -1.5f, -o), new(o, 0, o)), (new(4, 0, -o), new(o, 8, o)), (new(-o, 0, -o), new(-4, 8, o)),
            (new(-4, 0, 4), new(4, 8, o)), (new(-4, 0, -o), new(4, 8, -4)), (new(-o, 8, -o), new(o, 8 + Wall, o)),
        ];
        var light = Enumerable.Repeat(1f, Size * Size * Size).ToArray();
        (Vector3, Vector3)[] points =
        [
            .. Grid(new(-3, 0.25f, -3), Vector3.UnitX, 4, Vector3.UnitZ * 2, 4, Vector3.UnitY),
            .. Grid(new(-3.75f, 1, -3), Vector3.UnitY * 2, 4, Vector3.UnitZ * 2, 4, Vector3.UnitX),
            .. Grid(new(-3, 7.75f, -3), Vector3.UnitX * 2, 4, Vector3.UnitZ * 2, 4, -Vector3.UnitY),
        ];
        var bounce = BoxBounce(boxes, light, 0);
        var transport = TransportOf(bounce, points);
        var own = points.Select(p => TracedAmongBoxes(boxes, p.Item1, p.Item2, light, 0)).ToArray();
        var (traced, rounds, gain, _) = TracedLoop(transport, own, points.Length);
        var scene = new LadderScene("the closed room", Field(OpenBox().Concat(Cuboid(new(-o, 8, -o), new(o, 8 + Wall, o), 6))),
            new Setting { CellLight = light, SkyLight = 0 }, LadderLevels(new(new(-3.5f, 0.5f, -3.5f), 1, 8, 8, 8, NearTexels, 2)), Floor,
            [("every point", points)], (_, _) => 0) { Bounce = bounce };
        var (_, merged, mergedRounds, settles, mergedGain, _) = MergedLoop(scene, transport, scene.Setting, points, points.Length);
        var (tracedPi, over) = (traced.Select(t => t / MathF.PI).ToArray(), merged.Select((m, k) => m / traced[k]).ToArray());
        output.WriteLine($"over {transport.Cells.Length} cells, traced {tracedPi.Min():0.0000} to {tracedPi.Max():0.0000} of pi in {rounds} rounds, "
            + $"gain {gain:0.0000}, merged {over.Min():0.0000} to {over.Max():0.0000} of it in {mergedRounds} rounds, gain {mergedGain:0.0000}");
        Assert.True(rounds == 7 && Math.Abs(gain - 0.5f) < 1e-3f && tracedPi.All(t => Math.Abs(t - (2 - 1f / 64)) < 1e-3f), "the traced loop");
        Assert.True(settles && mergedRounds == rounds && Math.Abs(mergedGain - 0.5f) < 0.01f && over.All(m => Math.Abs(m - 1) < 0.01f), "the merged loop");
    }
}
