using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The ladder the merges climb, every merge the passes take by name run over a set of scenes on the
/// CPU and read against the light traced there, the standing written where RC_LADDER names a file.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // A scene of the ladder, its cells, its setting but for its points and its merge, its levels
    // and the place the field is about, its parts each a name and its points, the light traced to a
    // point facing a way, whether it is held out of the standing, and the points of a dark room
    // beside a lit one whose light is the leak through the wall between them.
    private sealed record LadderScene(string Name, float[] Cells, Setting Setting, Level[] Levels, Vector3 Place,
        (string Name, (Vector3, Vector3)[] Points)[] Parts, Func<Vector3, Vector3, float> Traced, bool HeldOut = false,
        (Vector3, Vector3)[]? Dark = null);

    // A merge's reading of a scene, each part's light over the light traced there, the leak, and a
    // texel's steps past its ray's end and its reads of the probes above on the mean.
    private sealed record LadderRow(LadderScene Scene, Merge Merge, float[] Ratios, float Leak, float Steps, float Reads)
    {
        public float[] Distances => Ratios.Select(r => MathF.Abs(MathF.Log(r))).ToArray();
        public float Mean => Distances.Average();
        public float Worst => Distances.Max();
        public float Cost => Steps + Reads;
    }

    // The points of a grid from origin, count along a at a apart and along b at b apart, facing
    // normal.
    private static (Vector3, Vector3)[] Grid(Vector3 origin, Vector3 a, int along, Vector3 b, int across, Vector3 normal) =>
        (from i in Enumerable.Range(0, along) from j in Enumerable.Range(0, across) select (origin + a * i + b * j, normal)).ToArray();

    // The points a quarter of a unit off each of the four walls of a room from -4 to 4 along x and
    // z, facing in, seven along each wall from -3 to 3 and rows from low up, a apart.
    private static (Vector3, Vector3)[] RoomWalls(float low, float apart, int rows) =>
    [
        .. Grid(new(-3.75f, low, -3), Vector3.UnitY * apart, rows, Vector3.UnitZ, 7, Vector3.UnitX),
        .. Grid(new(3.75f, low, -3), Vector3.UnitY * apart, rows, Vector3.UnitZ, 7, -Vector3.UnitX),
        .. Grid(new(-3, low, -3.75f), Vector3.UnitX, 7, Vector3.UnitY * apart, rows, Vector3.UnitZ),
        .. Grid(new(-3, low, 3.75f), Vector3.UnitX, 7, Vector3.UnitY * apart, rows, -Vector3.UnitZ),
    ];

    // The closed room's coarser levels, below a finest level of the scene's own.
    private static Level[] LadderLevels(Level finest) =>
    [
        finest,
        new(FarCorner, FarSpacing, FarCount, FarCount, FarCount, FarTexels, 4),
        new(new(-14, -10, -14), 4, 8, 8, 8, 16, 8),
        new(new(-28, -28, -28), 8, 8, 8, 8, 16, FarEnd),
    ];

    // A scene in the test's one cascade of the field, its boxes, the light each cell takes by its
    // place and the sky's light, traced among the boxes.
    private static LadderScene BoxScene(string name, (Vector3 Low, Vector3 High)[] boxes, Func<Vector3, float> lightAt, float sky, Level finest,
        (string, (Vector3, Vector3)[])[] parts, bool heldOut = false, (Vector3, Vector3)[]? dark = null)
    {
        var light = CellLightBy(lightAt);
        return new(name, Field(boxes.SelectMany((box, i) => Cuboid(box.Low, box.High, i + 1))), new Setting { CellLight = light, SkyLight = sky },
            LadderLevels(finest), Floor, parts, (p, n) => TracedAmongBoxes(boxes, p, n, light, sky), heldOut, dark);
    }

    // The room from -4 to 4 along x and z and from 0 to 4 high, its walls, floor and ceiling half a
    // unit thick, and the boxes given beside it.
    private static (Vector3, Vector3)[] LowRoom(params (Vector3, Vector3)[] beside) =>
    [
        (new(-4.5f, -0.5f, -4.5f), new(4.5f, 0, 4.5f)),
        (new(-4.5f, 0, -4.5f), new(-4, 4, 4.5f)),
        (new(-4, 0, -4.5f), new(4, 4, -4)),
        (new(-4, 0, 4), new(4, 4, 4.5f)),
        .. beside,
    ];

    // The ladder's scenes, six the standing is read over and one held out.
    private static LadderScene[] LadderScenes()
    {
        var lowFinest = new Level(new(-3.5f, 0.5f, -3.5f), 1, 8, 4, 8, NearTexels, 2);
        var lowParts = new[]
        {
            ("floor", Grid(new(-3, 0.25f, -3), Vector3.UnitX, 7, Vector3.UnitZ, 7, Vector3.UnitY)),
            ("walls", RoomWalls(0.5f, 1, 4)),
            ("ceiling", Grid(new(-3, 3.75f, -3), Vector3.UnitX, 7, Vector3.UnitZ, 7, -Vector3.UnitY)),
        };

        // A room with a door 1.5 wide and 2.5 high in its wall at +x, the ground outside it lit to
        // half the sky's light and every face inside sending a twentieth of it.
        var door = BoxScene("a room with a door", LowRoom(
                (new(4.5f, -0.5f, -4.5f), new(7.75f, 0, 4.5f)),
                (new(-4.5f, 4, -4.5f), new(4.5f, 4.5f, 4.5f)),
                (new(4, 0, -4.5f), new(4.5f, 4, -0.75f)),
                (new(4, 0, 0.75f), new(4.5f, 4, 4.5f)),
                (new(4, 2.5f, -0.75f), new(4.5f, 4, 0.75f))),
            p => p.X > 4.5f && p.Y < 0.25f ? 0.5f : 0.05f, 1, lowFinest, lowParts);

        // The closed room 8 high, its lid's middle square of 2 a side sending a light of 5 and
        // every other face none.
        const float o = 4 + Wall;
        var closedTriangles = OpenBox().Concat(Cuboid(new(-o, 8, -o), new(o, 8 + Wall, o), 6)).ToList();
        var lampLight = CellLightBy(p => MathF.Abs(p.X) < 1 && MathF.Abs(p.Z) < 1 && p.Y is > 7.75f and < 8 ? 5 : 0);
        (Vector3, Vector3)[] closedBoxes =
        [
            (new(-o, -1.5f, -o), new(o, 0, o)), (new(4, 0, -o), new(o, 8, o)), (new(-o, 0, -o), new(-4, 8, o)),
            (new(-4, 0, 4), new(4, 8, o)), (new(-4, 0, -o), new(4, 8, -4)), (new(-o, 8, -o), new(o, 8 + Wall, o)),
        ];
        var lamp = new LadderScene("a closed box with one lamp", Field(closedTriangles), new Setting { CellLight = lampLight, SkyLight = 0 },
            LadderLevels(new(new(-3.5f, 0.5f, -3.5f), 1, 8, 8, 8, NearTexels, 2)), Floor,
            [("floor", Grid(new(-3, 0.25f, -3), Vector3.UnitX, 7, Vector3.UnitZ, 7, Vector3.UnitY)), ("walls", RoomWalls(1, 2, 4))],
            (p, n) => TracedAmongBoxes(closedBoxes, p, n, lampLight, 0));

        // The thin wall's rooms, read in the lit one, with the dark one's light as the leak.
        var thin = BoxScene("a lit room beside a dark one through a wall of one cell", ThinWallRooms, ThinWallLight, 0, ThinWallLevels[0],
            [
                ("lit floor", Grid(new(-5.75f, 0.25f, -3.5f), Vector3.UnitX, 6, Vector3.UnitZ, 8, Vector3.UnitY)),
                ("lit walls", Grid(new(-6.25f, 0.5f, -3.5f), Vector3.UnitY, 4, Vector3.UnitZ, 8, Vector3.UnitX)),
            ], dark: DarkRoomPoints());

        // The lintel's room, its faces lit by the sky, read at the finest level's probes facing the
        // opening, the columns nearest it, those between and the two deepest.
        var (lintelCells, lintelLight) = LintelScene();
        (Vector3, Vector3)[] Columns(int column, int count) =>
            (from z in Enumerable.Range(0, 8) from x in Enumerable.Range(column, count) select (new Vector3(-5.5f + x, 1.5f, -3.5f + z), -Vector3.UnitX)).ToArray();
        var lintel = new LadderScene("a room under a lintel", lintelCells,
            new Setting { FieldCascades = LintelField, CellLight = lintelLight, SkyLight = LintelSky, Moved = true }, LintelLevels, UnderLintel,
            [("by the opening", Columns(0, 3)), ("between", Columns(3, 3)), ("deepest", Columns(6, 2))],
            (p, n) => TracedAgainstBoxes(MovedOff(p), n));

        // The open box under a sky of 1, its floor sending 0.3 and its walls 0.15.
        (Vector3, Vector3)[] openBoxes =
        [
            (new(-o, -1.5f, -o), new(o, 0, o)), (new(4, 0, -o), new(o, 8, o)), (new(-o, 0, -o), new(-4, 8, o)),
            (new(-4, 0, 4), new(4, 8, o)), (new(-4, 0, -o), new(4, 8, -4)),
        ];
        var open = BoxScene("an open box under a sky", openBoxes, p => p.Y < 0.25f ? 0.3f : 0.15f, 1,
            new(new(-3.5f, 0.5f, -3.5f), 1, 8, 8, 8, NearTexels, 2),
            [("floor", Grid(new(-3, 0.25f, -3), Vector3.UnitX, 7, Vector3.UnitZ, 7, Vector3.UnitY)), ("walls", RoomWalls(1, 2, 4))]);

        // A hall 14.5 long, 2 wide from -2.5 to -0.5 along z and 3.5 high, so that the coarser
        // levels' probes at -1 and -2 along z and 1, 2 and 3 high stand within it, open to a sky of
        // 1 at its end at -x, its floor sending 0.2 and its other faces 0.1, read on its floor and
        // along a wall in thirds.
        (Vector3, Vector3)[] Third(int column, int count) =>
        [
            .. Grid(new(-6.5f + column, 0.25f, -2), Vector3.UnitX, count, Vector3.UnitZ, 2, Vector3.UnitY),
            .. Grid(new(-6.5f + column, 1.5f, -2.25f), Vector3.UnitX, count, Vector3.UnitY, 1, Vector3.UnitZ),
        ];
        var hall = BoxScene("a long hall", [
                (new(-7.5f, -0.5f, -3), new(7.5f, 0, 0)), (new(-7.5f, 3.5f, -3), new(7.5f, 4, 0)),
                (new(-7.5f, 0, -3), new(7.5f, 3.5f, -2.5f)), (new(-7.5f, 0, -0.5f), new(7.5f, 3.5f, 0)), (new(7, 0, -2.5f), new(7.5f, 3.5f, -0.5f)),
            ], p => p.Y < 0.25f ? 0.2f : 0.1f, 1, new(new(-6.5f, 0.5f, -2), 1, 14, 3, 2, NearTexels, 2),
            [("by the opening", Third(0, 5)), ("middle", Third(5, 4)), ("far end", Third(9, 5))]);

        // Held out, a room with a window of 2 by 2 in its roof over the floor's middle and a pillar
        // a unit square from 1.5 to 2.5 along x and z, the floor under the window sending 0.8 and
        // every other face a twentieth of the sky's light of 1.
        bool InPillar(Vector3 p) => p.X is > 1.25f and < 2.75f && p.Z is > 1.25f and < 2.75f;
        var window = BoxScene("held out, a room with a roof window and a pillar", LowRoom(
                (new(4, 0, -4.5f), new(4.5f, 4, 4.5f)),
                (new(-4.5f, 4, -4.5f), new(4.5f, 4.5f, -1)), (new(-4.5f, 4, 1), new(4.5f, 4.5f, 4.5f)),
                (new(-4.5f, 4, -1), new(-1, 4.5f, 1)), (new(1, 4, -1), new(4.5f, 4.5f, 1)),
                (new(1.5f, 0, 1.5f), new(2.5f, 4, 2.5f))),
            p => MathF.Abs(p.X) < 1.25f && MathF.Abs(p.Z) < 1.25f && p.Y < 0.25f ? 0.8f : 0.05f, 1, lowFinest,
            [
                ("floor", lowParts[0].Item2.Where(p => !InPillar(p.Item1)).ToArray()),
                ("walls", lowParts[1].Item2),
                ("ceiling", lowParts[2].Item2.Where(p => !InPillar(p.Item1)).ToArray()),
                ("pillar", Grid(new(1.25f, 0.5f, 1.75f), Vector3.UnitY, 4, Vector3.UnitZ * 0.5f, 2, -Vector3.UnitX)),
            ], heldOut: true);
        return [door, lamp, thin, lintel, open, hall, window];
    }

    private static string Invariant(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

    // How the ladder's tables name each merge, by what it does and its struct's name.
    private static string MergeName(Merge merge) => merge switch
    {
        Merge.AtRayEnd => "at the ray's end, `MergeAtRayEnd`",
        Merge.ByReachSight => "by reach sight, `MergeByReachSight`",
        _ => merge.ToString(),
    };

    // The commit the tree stands on, with a mark where it holds changes not committed.
    private static string TreeCommit()
    {
        string Git(string arguments)
        {
            using var git = Process.Start(new ProcessStartInfo("git", $"-C \"{Repository.Root}\" {arguments}") { RedirectStandardOutput = true })!;
            var output = git.StandardOutput.ReadToEnd().Trim();
            git.WaitForExit();
            return output;
        }
        var hash = Git("rev-parse --short HEAD");
        return Git("status --porcelain --untracked-files=no").Length > 0 ? $"{hash} with the changes committed beside it" : hash;
    }

    [CpuKernelFact]
    public void The_Ladder_Runs_Every_Merge_Over_Its_Scenes_And_Writes_Their_Standing_Where_RC_LADDER_Names_A_File()
    {
        // dotnet test RadianceCascades.Tests with RC_LADDER set to a file, relative to the
        // repository's root or whole, writes there every merge's reading of each scene and their
        // standing, and the standing into the README's table under its ladder's heading.
        var path = Environment.GetEnvironmentVariable("RC_LADDER");
        if (string.IsNullOrEmpty(path))
            return;
        path = Path.GetFullPath(path, Repository.Root);
        var rows = new List<LadderRow>();
        foreach (var scene in LadderScenes())
        {
            var traced = scene.Parts.Select(part => part.Points.Sum(p => scene.Traced(p.Item1, p.Item2))).ToArray();
            var litMean = traced.Sum() / scene.Parts.Sum(part => part.Points.Length);
            var points = scene.Parts.SelectMany(part => part.Points).Concat(scene.Dark ?? []).Select(p => (p.Item1, p.Item2)).ToArray();
            foreach (var merge in Enum.GetValues<Merge>())
            {
                var reading = Passes(scene.Cells, scene.Levels, scene.Place, scene.Setting with { Points = points, Merge = merge });
                var (ratios, first) = (new float[scene.Parts.Length], 0);
                for (int p = 0; p < ratios.Length; p++)
                {
                    var count = scene.Parts[p].Points.Length;
                    ratios[p] = reading.Taken[first..(first + count)].Sum() / traced[p];
                    first += count;
                }
                var leak = scene.Dark is null ? 0 : reading.Taken[first..].Average() / litMean;
                rows.Add(new(scene, merge, ratios, leak, reading.MergeSteps, reading.MergeReads));
                output.WriteLine($"{scene.Name}, {merge}, {string.Join(", ", ratios.Select(r => Invariant(r, "0.000")))}, leak {Invariant(leak, "0.00000")}");
            }
        }

        // Each merge over the six scenes the standing is read over, every part's distance pooled,
        // the worst part, the worst leak and the cost on the mean over the scenes.
        var standing = Enum.GetValues<Merge>().Select(merge =>
        {
            var own = rows.Where(r => r.Merge == merge && !r.Scene.HeldOut).ToArray();
            var held = rows.Single(r => r.Merge == merge && r.Scene.HeldOut);
            var distances = own.SelectMany(r => r.Distances).ToArray();
            return (Merge: merge, Mean: distances.Average(), Worst: distances.Max(), Leak: own.Max(r => r.Leak), Steps: own.Average(r => r.Steps),
                Reads: own.Average(r => r.Reads), Held: held);
        }).OrderBy(s => s.Mean).ToArray();
        bool Above((Merge Merge, float Mean, float Worst, float Leak, float Steps, float Reads, LadderRow Held) a,
            (Merge Merge, float Mean, float Worst, float Leak, float Steps, float Reads, LadderRow Held) b) =>
            a.Mean < b.Mean && a.Worst <= b.Worst && a.Leak <= b.Leak && a.Steps + a.Reads <= b.Steps + b.Reads;
        var top = standing.Where(a => standing.All(b => b.Merge == a.Merge || Above(a, b))).Select(a => a.Merge).ToArray();

        var table = new StringBuilder();
        table.AppendLine("| Merge | Mean distance | Worst part | Leak | Steps a texel | Reads a texel | Stands above | Held out, mean and worst |");
        table.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var s in standing)
        {
            var above = standing.Where(b => b.Merge != s.Merge && Above(s, b)).Select(b => MergeName(b.Merge).Split(',')[0]).ToArray();
            table.AppendLine($"| {MergeName(s.Merge)} | {Invariant(s.Mean, "0.000")} | {Invariant(s.Worst, "0.000")} | {Invariant(s.Leak, "0.00000")} | "
                + $"{Invariant(s.Steps, "0.0")} | {Invariant(s.Reads, "0.0")} | {(above.Length == 0 ? "none" : string.Join(", ", above))} | "
                + $"{Invariant(s.Held.Mean, "0.000")} and {Invariant(s.Held.Worst, "0.000")} |");
        }
        var verdict = top.Length == 1
            ? $"The merge {MergeName(top[0])}, stands above every other and is the default."
            : "No merge stands above every other, so the default stays the merge at the ray's end, `MergeAtRayEnd`.";

        var file = new StringBuilder();
        file.AppendLine("# The ladder of merges");
        file.AppendLine();
        file.AppendLine($"Measured on {DateTime.UtcNow:yyyy-MM-dd} on the tree of {TreeCommit()}, every merge the passes take by name");
        file.AppendLine("(`IMerge`) run over seven scenes on the CPU, as `dotnet test RadianceCascades.Tests` writes it with");
        file.AppendLine("`RC_LADDER` naming this file. A part of a scene is read as the light its points take over the light");
        file.AppendLine("traced to them, and its distance is that ratio's logarithm taken whole, 0.105 a tenth over or under.");
        file.AppendLine("The leak is the light the dark room takes through a wall of one cell beside the lit one, as a share");
        file.AppendLine("of the lit room's traced light. The cost is a texel's steps past its ray's end and its reads of the");
        file.AppendLine("probes above, a texel's light, its share of the sky or a reach texel, on the mean over every level's");
        file.AppendLine("texels. A merge stands above another where it is nearer on the mean and no worse in its worst part,");
        file.AppendLine("its leak and its steps and reads together, over the six scenes, and the one standing above every");
        file.AppendLine("other is the default. The seventh is held out of the standing and read apart, since no merge is");
        file.AppendLine("tuned on it.");
        file.AppendLine();
        file.AppendLine("## Standing");
        file.AppendLine();
        file.Append(table);
        file.AppendLine();
        file.AppendLine(verdict);
        file.AppendLine();
        file.AppendLine("## Each scene");
        file.AppendLine();
        file.AppendLine("| Scene | Merge | Each part's ratio | Mean distance | Worst part | Leak | Steps a texel | Reads a texel |");
        file.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var r in rows)
            file.AppendLine($"| {r.Scene.Name} | {MergeName(r.Merge)} | "
                + string.Join(", ", r.Scene.Parts.Select((part, p) => $"{part.Name} {Invariant(r.Ratios[p], "0.000")}"))
                + $" | {Invariant(r.Mean, "0.000")} | {Invariant(r.Worst, "0.000")} | {(r.Scene.Dark is null ? "" : Invariant(r.Leak, "0.00000"))} | "
                + $"{Invariant(r.Steps, "0.0")} | {Invariant(r.Reads, "0.0")} |");
        File.WriteAllText(path, file.ToString());

        // The README's table under its ladder's heading takes the standing in place of the last.
        var readme = Path.Combine(Repository.Root, "README.md");
        var lines = File.ReadAllLines(readme).ToList();
        var heading = lines.IndexOf("## The ladder of merges");
        var start = heading < 0 ? -1 : lines.FindIndex(heading, l => l.StartsWith("| Merge |", StringComparison.Ordinal));
        if (start >= 0)
        {
            var end = lines.FindIndex(start, l => !l.StartsWith('|'));
            lines.RemoveRange(start, (end < 0 ? lines.Count : end) - start);
            lines.InsertRange(start, table.ToString().TrimEnd('\n').Split('\n'));
            // The line after the table and a blank one says which merge is the default.
            var said = start + table.ToString().TrimEnd('\n').Split('\n').Length + 1;
            if (said < lines.Count && (lines[said].StartsWith("The merge ", StringComparison.Ordinal) || lines[said].StartsWith("No merge stands", StringComparison.Ordinal)))
                lines[said] = verdict;
            File.WriteAllText(readme, string.Join('\n', lines) + "\n");
        }
        output.WriteLine(file.ToString());
        Assert.True(rows.Count == 2 * 7, "every merge reads every scene");
    }
}
