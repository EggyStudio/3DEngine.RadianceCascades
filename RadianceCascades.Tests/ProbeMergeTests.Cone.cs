using System.Globalization;
using System.Numerics;
using System.Text;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The cone a rough surface reflects, read from the cascades on the CPU by the first read behind
/// <c>IConeRead</c>, <c>ConeAtProbes</c>, against the same cone traced with many rays, by
/// roughness.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // The roughnesses a cone is read at.
    private static readonly float[] Roughnesses = [0.1f, 0.2f, 0.3f, 0.5f, 0.7f, 1.0f];

    // The traced cone's half-ways along each side, ConeSide squared rays a cone.
    private const int ConeSide = 64;

    // The rays a traced cone takes, as the ladder's file and the sheet write them.
    private static readonly string ConeRays = (ConeSide * ConeSide).ToString("N0", CultureInfo.InvariantCulture);

    // The difference past which a reading parts from its trace.
    private const float ConeParts = 0.10f;

    // The heading of the cones' section of the ladder's file, which their reading writes and the
    // merges' reading keeps.
    private const string ConeHeading = "## The cones by roughness";

    // The least alpha the read takes a cone at from texels of `texels` along a side, as
    // coneLeastAlpha gives it.
    private static float ConeLeastAlpha(int texels) => MathF.Sqrt(MathF.PI) / texels;

    // The alpha the read takes a cone of alpha at over the levels, its own where a level keeps
    // texels as narrow and otherwise the least the level with the most allows, as coneLevel
    // chooses. The levels a point gathers from up to the coarsest hold the most texels at every
    // configuration here, so the level a point gathers from leaves it as it is.
    private static float ConeReadAlpha(Level[] levels, float alpha)
    {
        var kept = levels.Select(level => level.Kept > 0 && level.Kept < level.Texels ? level.Kept : level.Texels).ToArray();
        return kept.Any(texels => ConeLeastAlpha(texels) <= alpha) ? alpha : ConeLeastAlpha(kept.Max());
    }

    // The way toward the eye a point facing normal reflects to, 45 degrees off its normal along the
    // first way of its basis.
    private static Vector3 ConeView(Vector3 normal) => Vector3.Normalize(normal + Basis(normal).U);

    // The cone of alpha a point at place facing normal reflects toward view, traced in each
    // channel's scene, ConeSide by ConeSide half-ways drawn by the GGX surface's density of them,
    // each mirrored about its half-way and left out where it falls behind the surface, as
    // coneWeight weighs the ways. Each ray takes the light of the surface it meets or the sky's, as
    // Rays does. The noise is half the difference of the two halves of the rays, those of a
    // checkerboard's black squares and its white ones, about how far the whole trace lies from the
    // lobe's own light.
    private static (float[] Light, float[] Noise) ConeTraced(Bounce[] channels, Vector3 place, Vector3 normal, Vector3 view, float alpha)
    {
        var (u, w) = Basis(normal);
        var sums = new double[2, channels.Length];
        var counts = new int[2];
        var a2 = (double)alpha * alpha;
        for (int i = 0; i < ConeSide; i++)
            for (int j = 0; j < ConeSide; j++)
            {
                var (a, b) = ((i + 0.5) / ConeSide, (j + 0.5) / ConeSide);
                var cos = 1 / Math.Sqrt(1 + a2 * a / (1 - a));
                var sin = Math.Sqrt(Math.Max(1 - cos * cos, 0));
                var phi = 2 * Math.PI * b;
                var half = u * (float)(sin * Math.Cos(phi)) + w * (float)(sin * Math.Sin(phi)) + normal * (float)cos;
                var way = 2 * Vector3.Dot(view, half) * half - view;
                if (Vector3.Dot(normal, way) <= 0)
                    continue;
                way = Vector3.Normalize(way);
                var t = channels[0].Meets(place, way);
                var at = place + way * (t - 0.01f);
                var square = (i + j) & 1;
                for (int c = 0; c < channels.Length; c++)
                    sums[square, c] += t == float.MaxValue ? channels[c].Sky : channels[c].OwnAt(at);
                counts[square]++;
            }
        var light = new float[channels.Length];
        var noise = new float[channels.Length];
        for (int c = 0; c < channels.Length; c++)
        {
            var all = counts[0] + counts[1];
            light[c] = all == 0 ? 0 : (float)((sums[0, c] + sums[1, c]) / all);
            noise[c] = counts[0] == 0 || counts[1] == 0 ? 0 : (float)Math.Abs(sums[0, c] / counts[0] - sums[1, c] / counts[1]) / 2;
        }
        return (light, noise);
    }

    [CpuKernelFact]
    public void A_Closed_Room_Whose_Every_Face_Sends_The_Same_Light_Reflects_That_Light_In_Every_Cone_At_Every_Roughness()
    {
        // The closed room of the closed room's test, every face sending a light of 1 under a black
        // sky, its finest level keeping 8 texels a side of its 16, so a cone narrower than that
        // level's texels is read from the level above. Its points face up from the floor, +x at
        // the middle height and down under the lid, each reflecting toward an eye 45 degrees off
        // its normal at every roughness, and every cone takes 1.
        const float o = 4 + Wall;
        var cells = Field(OpenBox().Concat(Cuboid(new(-o, 8, -o), new(o, 8 + Wall, o), 6)));
        Level[] chain =
        [
            new(new(-3.5f, 0.25f, -3.5f), 1, 8, 8, 8, NearTexels, 2, Kept: 8),
            new(FarCorner, FarSpacing, FarCount, FarCount, FarCount, FarTexels, 4),
            new(new(-14, -10, -14), 4, 8, 8, 8, 16, 8),
            new(new(-28, -28, -28), 8, 8, 8, 8, 16, FarEnd),
        ];
        var points = Enumerable.Range(0, 7).Select(i => (new Vector3(-3 + i, 0.25f, 0.5f), Vector3.UnitY))
            .Concat(Enumerable.Range(0, 7).Select(i => (new Vector3(-3 + i, 4f, 0.5f), Vector3.UnitX)))
            .Concat(Enumerable.Range(0, 7).Select(i => (new Vector3(-3 + i, 7.75f, 0.5f), -Vector3.UnitY))).ToArray();
        var cones = Roughnesses.SelectMany(r => points.Select((p, i) => (i, ConeView(p.Item2), r))).ToArray();
        var light = Enumerable.Repeat(1f, Size * Size * Size).ToArray();
        var reading = Passes(cells, chain, Floor, new Setting { Points = points, CellLight = light, SkyLight = 0, Cones = cones });
        var byRoughness = Roughnesses.Select((r, k) =>
        {
            var each = reading.ConeLight[(k * points.Length)..((k + 1) * points.Length)];
            var reads = reading.ConeReads[(k * points.Length)..((k + 1) * points.Length)].Average();
            return (Roughness: r, Least: each.Min(), Most: each.Max(), Reads: reads);
        }).ToArray();
        var said = "the closed room's cones read " + string.Join(", ", byRoughness.Select(b =>
            $"{b.Least:0.0000} to {b.Most:0.0000} at roughness {b.Roughness:0.0} in {b.Reads:0} reads"));
        output.WriteLine(said);
        // Every cone 1.0000, the lobe's weights over the texels it reads normalized whatever its
        // width.
        Assert.True(reading.ConeLight.All(q => Math.Abs(q - 1) <= 0.005), said);
    }

    // The scenes the cones are read over, the ladder's and two of their own, each as a scene a
    // channel of its light, one where the light is gray and three where it has a color.
    private static (string Name, LadderScene[] Channels)[] ConeScenes()
    {
        var lowFinest = new Level(new(-3.5f, 0.5f, -3.5f), 1, 8, 4, 8, NearTexels, 2);
        (string, (Vector3, Vector3)[])[] lowParts =
        [
            ("floor", Grid(new(-3, 0.25f, -3), Vector3.UnitX, 7, Vector3.UnitZ, 7, Vector3.UnitY)),
            ("walls", RoomWalls(0.5f, 1, 4)),
            ("ceiling", Grid(new(-3, 3.75f, -3), Vector3.UnitX, 7, Vector3.UnitZ, 7, -Vector3.UnitY)),
        ];
        // The low room closed on every side, the middle of its ceiling half a unit a side sending a
        // light of 32 and every other face a fiftieth.
        var small = BoxScene("a low room lit by a small bright light", LowRoom(
                (new(4, 0, -4.5f), new(4.5f, 4, 4.5f)),
                (new(-4.5f, 4, -4.5f), new(4.5f, 4.5f, 4.5f))),
            p => MathF.Abs(p.X) < 0.25f && MathF.Abs(p.Z) < 0.25f && p.Y > 3.75f ? 32 : 0.02f, 0, lowFinest, lowParts);
        return [.. LadderScenes().Select(scene => (scene.Name, new[] { scene })), (small.Name, [small]), (RedWallName, RedWallScenes(lowFinest, lowParts))];
    }

    private const string RedWallName = "a red wall beside a gray floor";

    // The low room open above to a sky of a half, its wall at -x sending a red light, its floor a
    // gray one of a quarter and its other walls a tenth, one scene a channel.
    private static LadderScene[] RedWallScenes(Level finest, (string, (Vector3, Vector3)[])[] parts)
    {
        float[] red = [0.9f, 0.2f, 0.1f];
        return [.. red.Select(wall => BoxScene(RedWallName, LowRoom((new(4, 0, -4.5f), new(4.5f, 4, 4.5f))),
            p => p.X < -3.75f ? wall : p.Y < 0.25f ? 0.25f : 0.1f, 0.5f, finest, parts))];
    }

    // The text of the cones' section of the ladder's file at path, from its heading to the file's
    // end with a blank line before it, or none where the file or the section is missing.
    private static string KeptConeSection(string path)
    {
        if (!File.Exists(path))
            return "";
        var text = File.ReadAllText(path);
        var at = text.IndexOf(ConeHeading, StringComparison.Ordinal);
        return at < 0 ? "" : "\n" + text[at..];
    }

    // A difference of a reading from its trace over a part's points, the sum of each point's
    // channels' differences taken whole over the points' count times mean, the scene's traced light
    // on the mean over its points with its channels summed. A part whose cones see little reads by
    // the scene's light and not its own, so a dark part does not stand for the scene.
    private static float ConeDifference(int[] points, float[][] read, float[][] traced, float mean)
    {
        double apart = 0;
        foreach (var i in points)
            for (int c = 0; c < traced[i].Length; c++)
                apart += Math.Abs(read[i][c] - traced[i][c]);
        return mean > 0 ? (float)(apart / (points.Length * mean)) : 0;
    }

    [CpuKernelFact]
    public void The_Ladder_Reads_Every_Cone_By_Roughness_Against_Its_Trace_And_Writes_It_Where_RC_LADDER_Names_A_File()
    {
        // dotnet test RadianceCascades.Tests with RC_LADDER set to a file writes there, in place of
        // its cones' section, the cones read by roughness at each configuration against their
        // trace, and their table into the README under its heading of rough reflections.
        var path = Environment.GetEnvironmentVariable("RC_LADDER");
        if (string.IsNullOrEmpty(path))
            return;
        path = Path.GetFullPath(path, Repository.Root);
        var configurations = Enum.GetValues<Configuration>();
        var scenes = ConeScenes();
        var thinName = scenes.First(s => s.Channels[0].Dark is not null).Name;
        // Each scene's channels built once at each configuration.
        var jobs = configurations.SelectMany(configuration => scenes.SelectMany(s => s.Channels.Select((channel, c) => (configuration, s.Name, c, channel))))
            .ToArray().AsParallel().AsOrdered()
            .Select(b => (b.configuration, b.Name, b.c, Scene: b.configuration == Configuration.Engine ? b.channel.AtEngine!() : b.channel)).ToArray();
        Level[] LevelsOf(Configuration configuration, string name) => jobs.First(b => b.configuration == configuration && b.Name == name).Scene.Levels;

        // Each scene's cones, every point at every roughness, and their trace at each roughness's
        // own alpha and at each alpha the read takes it at.
        var cones = new Dictionary<string, (int Point, Vector3 View, float Roughness)[]>();
        var traced = new Dictionary<(string Scene, float Alpha), float[][]>();
        var noises = new Dictionary<(string Scene, float Alpha), float[][]>();
        foreach (var (name, channels) in scenes)
        {
            var (points, _) = LadderPoints(channels[0]);
            cones[name] = [.. Roughnesses.SelectMany(r => points.Select((p, i) => (i, ConeView(p.Item2), r)))];
            var bounces = channels.Select(c => c.Bounce!).ToArray();
            var alphas = Roughnesses.Select(r => r * r)
                .Concat(configurations.SelectMany(c => Roughnesses.Select(r => ConeReadAlpha(LevelsOf(c, name), r * r))))
                .Distinct().ToArray();
            foreach (var alpha in alphas)
            {
                var each = new (float[] Light, float[] Noise)[points.Length];
                Parallel.For(0, points.Length, i => each[i] = ConeTraced(bounces, bounces[0].From(points[i].Item1), points[i].Item2, ConeView(points[i].Item2), alpha));
                traced[(name, alpha)] = [.. each.Select(e => e.Light)];
                noises[(name, alpha)] = [.. each.Select(e => e.Noise)];
            }
        }

        // Every scene's channel read at each configuration, side by side over half the machine's
        // threads.
        var readings = new (float[] Light, float[] Reads)[jobs.Length];
        Parallel.For(0, jobs.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) }, j =>
        {
            var (_, name, _, scene) = jobs[j];
            var (points, _) = LadderPoints(scene);
            var reading = Passes(scene.Cells, scene.Levels, scene.Place, scene.Setting with { Points = points, Cones = cones[name] });
            readings[j] = (reading.ConeLight, reading.ConeReads);
        });

        // Each configuration's, scene's and roughness's cones as each point's channels, with each
        // part's difference, the widening's, the trace's noise, the reading less the trace, and the
        // reads. Each is taken over the scene's light, its traced light on the mean over its points
        // at the widest lobe, a scale that does not change with the roughness, so a scene whose
        // narrow cones mostly see nothing is read against the light it holds and not the little
        // they see.
        var rows = new List<(Configuration Configuration, string Scene, float Roughness, float[] Parts, float[] Widened, float[] Noise, float[] Less, float Whole,
            float Reads, float Leak)>();
        foreach (var configuration in configurations)
            foreach (var (name, channels) in scenes)
            {
                var scene = channels[0];
                var levels = LevelsOf(configuration, name);
                var (points, lit) = LadderPoints(scene);
                var mine = Enumerable.Range(0, jobs.Length).Where(j => jobs[j].configuration == configuration && jobs[j].Name == name).ToArray();
                var widest = Roughnesses.Max();
                var light = Enumerable.Range(0, lit).Average(i => traced[(name, widest * widest)][i].Sum());
                for (int k = 0; k < Roughnesses.Length; k++)
                {
                    var r = Roughnesses[k];
                    var read = Enumerable.Range(0, points.Length).Select(i => mine.Select(j => readings[j].Light[k * points.Length + i]).ToArray()).ToArray();
                    var trace = traced[(name, r * r)];
                    var widened = traced[(name, ConeReadAlpha(levels, r * r))];
                    var noise = noises[(name, r * r)];
                    var none = noise.Select(n => new float[n.Length]).ToArray();
                    var (parts, widenings, noisy, less) = (new float[scene.Parts.Length], new float[scene.Parts.Length], new float[scene.Parts.Length],
                        new float[scene.Parts.Length]);
                    var first = 0;
                    for (int p = 0; p < scene.Parts.Length; p++)
                    {
                        var part = Enumerable.Range(first, scene.Parts[p].Points.Length).ToArray();
                        parts[p] = ConeDifference(part, read, trace, light);
                        widenings[p] = ConeDifference(part, widened, trace, light);
                        noisy[p] = ConeDifference(part, noise, none, light);
                        less[p] = (part.Sum(i => read[i].Sum()) - part.Sum(i => trace[i].Sum())) / (part.Length * light);
                        first += part.Length;
                    }
                    var whole = Enumerable.Range(0, lit).Sum(i => read[i].Sum() - trace[i].Sum()) / (lit * light);
                    var reads = mine.Average(j => readings[j].Reads[(k * points.Length)..(k * points.Length + lit)].Average());
                    var leak = points.Length > lit
                        ? Enumerable.Range(lit, points.Length - lit).Average(i => read[i].Sum()) / light
                        : float.NaN;
                    rows.Add((configuration, name, r, parts, widenings, noisy, less, whole, reads, leak));
                }
            }

        string Number(float value, string format = "0.000") => Invariant(value, format);
        var table = new StringBuilder();
        table.AppendLine("| Configuration | Roughness | Mean difference | Median scene | Worst part | Of it the cone widened | The trace's own noise "
            + "| Reading less the trace | Leak | Reads a point |");
        table.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        // The least roughness from which on each of a run of differences, one a roughness, is
        // within a tenth, said as the line of marks says it.
        string MarkOf(IEnumerable<float> differences)
        {
            var each = differences.ToArray();
            var from = Enumerable.Range(0, each.Length).FirstOrDefault(k => each.Skip(k).All(m => m <= ConeParts), -1);
            return from < 0 ? "none" : Number(Roughnesses[from], "0.0");
        }
        float Median(IEnumerable<float> values)
        {
            var sorted = values.Order().ToArray();
            return sorted.Length % 2 == 1 ? sorted[sorted.Length / 2] : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2;
        }
        var marks = new List<(string Median, string Mean)>();
        foreach (var configuration in configurations)
        {
            var (means, medians) = (new List<float>(), new List<float>());
            foreach (var r in Roughnesses)
            {
                var those = rows.Where(row => row.Configuration == configuration && row.Roughness == r).ToArray();
                var mean = those.SelectMany(row => row.Parts).Average();
                var median = Median(those.Select(row => row.Parts.Average()));
                (means, medians) = ([.. means, mean], [.. medians, median]);
                table.AppendLine($"| {ConfigurationName(configuration)} | {Number(r, "0.0")} | {Number(mean)} | {Number(median)} | "
                    + $"{Number(those.SelectMany(row => row.Parts).Max())} | {Number(those.SelectMany(row => row.Widened).Average())} | "
                    + $"{Number(those.SelectMany(row => row.Noise).Average())} | {Number(those.Average(row => row.Whole), "+0.000;-0.000;0.000")} | "
                    + $"{Number(those.Single(row => row.Scene == thinName).Leak, "0.00000")} | {Number(those.Average(row => row.Reads), "0")} |");
            }
            marks.Add((MarkOf(medians), MarkOf(means)));
        }
        string Said(string from) => from == "none" ? "at no roughness of the six" : $"from roughness {from} up";
        var mark = $"The cones read within a tenth of their trace on the median scene {Said(marks[0].Median)} at "
            + $"{ConfigurationName(configurations[0])} configuration and {Said(marks[1].Median)} at {ConfigurationName(configurations[1])}, and on "
            + $"the mean over every part {Said(marks[0].Mean)} and {Said(marks[1].Mean)}.";
        var scenesMarks = new StringBuilder();
        scenesMarks.AppendLine($"| Scene | {string.Join(" | ", configurations.Select(c => $"From {ConfigurationName(c)} configuration"))} |");
        scenesMarks.AppendLine($"|---|{string.Concat(configurations.Select(_ => "---|"))}");
        foreach (var (name, _) in scenes)
            scenesMarks.AppendLine($"| {name} | {string.Join(" | ", configurations.Select(c => MarkOf(Roughnesses.Select(r => rows.Single(row => row.Configuration == c
                && row.Scene == name && row.Roughness == r).Parts.Average()))))} |");

        var file = new StringBuilder();
        file.AppendLine(ConeHeading);
        file.AppendLine();
        foreach (var line in Wrapped(
            $"Measured on {DateTime.UtcNow:yyyy-MM-dd} on the tree of {TreeCommit()}, the cone a rough surface reflects read from the cascades by "
            + $"`ConeAtProbes` (`IConeRead`) over {scenes.Length} scenes, the ladder's and two of their own, on the CPU at {configurations.Length} "
            + "configurations after one gather, as `dotnet test RadianceCascades.Tests` writes it with `RC_LADDER` naming this file. Each point of "
            + "a scene reflects toward an eye 45 degrees off its normal, at each roughness of "
            + $"{string.Join(", ", Roughnesses.Select(r => Number(r, "0.0")))}, the lobe of a GGX surface whose alpha is the roughness squared, and "
            + $"the trace draws the same lobe's ways with {ConeRays} rays a cone, each taking the light of the surface it meets or the "
            + "sky's."))
            file.AppendLine(line);
        file.AppendLine();
        foreach (var line in Wrapped(
            "A part's difference is the sum over its points of the reading less the trace, taken whole, over the count of its points times "
            + "the scene's light, its traced light on the mean over its points at roughness 1.0, the channels of the red wall's scene summed "
            + "alike, so a scene whose narrow cones mostly see nothing is read against the light it holds. The median scene is the median over "
            + "the scenes of each one's mean over its parts. The reading less the trace is the same sum signed, over a scene's points. "
            + "The trace's own noise is the same difference of half the two halves of its rays, those of a checkerboard's black squares "
            + "against its white ones, about how far the trace lies from the lobe's light. The cone widened is the difference the "
            + "trace makes at the alpha the read takes, never under half the width of a texel of the level it reads, against the trace at its "
            + "own alpha, the part of the difference the cascades' count of directions makes alone. The leak is the light the dark room's "
            + "cones take through the wall of one cell, as a share of the lit room's traced cones. The reads are a cone's reads of a texel or "
            + "a reach texel. A mark is the least roughness from which on every roughness reads within a tenth, and the leak is the dark "
            + "room's cones' light over the lit room's light."))
            file.AppendLine(line);
        file.AppendLine();
        file.Append(table);
        file.AppendLine();
        file.AppendLine(mark);
        file.AppendLine();
        file.AppendLine("### Each scene's mark");
        file.AppendLine();
        file.Append(scenesMarks);
        file.AppendLine();
        file.AppendLine("### Each scene's cones");
        file.AppendLine();
        file.AppendLine("| Configuration | Scene | Roughness | Each part's difference | Each part's reading less its trace | The scene's reading less its trace |");
        file.AppendLine("|---|---|---|---|---|---|");
        foreach (var row in rows)
            file.AppendLine($"| {ConfigurationName(row.Configuration)} | {row.Scene} | {Number(row.Roughness, "0.0")} | "
                + $"{string.Join(", ", row.Parts.Select(p => Number(p)))} | {string.Join(", ", row.Less.Select(p => Number(p, "+0.000;-0.000;0.000")))} | "
                + $"{Number(row.Whole, "+0.000;-0.000;0.000")} |");

        var text = File.Exists(path) ? File.ReadAllText(path) : "";
        var begins = text.IndexOf(ConeHeading, StringComparison.Ordinal);
        var kept = begins < 0 ? (text.Length == 0 ? "" : text.TrimEnd('\n') + "\n\n") : text[..begins];
        File.WriteAllText(path, kept + file);

        // The README's table under its heading of rough reflections takes the cones' in place of
        // the last, and the line after it the mark.
        var readme = Path.Combine(Repository.Root, "README.md");
        var lines = File.ReadAllLines(readme).ToList();
        var heading = lines.IndexOf("## Rough reflections");
        var start = heading < 0 ? -1 : lines.FindIndex(heading, l => l.StartsWith("| Configuration | Roughness |", StringComparison.Ordinal));
        if (start >= 0)
        {
            var end = lines.FindIndex(start, l => !l.StartsWith('|'));
            lines.RemoveRange(start, (end < 0 ? lines.Count : end) - start);
            var tableLines = table.ToString().TrimEnd('\n').Split('\n');
            lines.InsertRange(start, tableLines);
            var said = start + tableLines.Length + 1;
            if (said < lines.Count && lines[said].StartsWith("The cones read within", StringComparison.Ordinal))
                lines[said] = mark;
            File.WriteAllText(readme, string.Join('\n', lines) + "\n");
        }
        output.WriteLine(file.ToString());
        Assert.True(rows.Count == configurations.Length * scenes.Length * Roughnesses.Length, "every cone is read at every configuration");
    }

    // The spheres' pixels along a side and their radius.
    private const int SpherePixels = 96;
    private const float SphereRadius = 0.5f;

    [CpuKernelFact]
    public void Six_Spheres_Before_A_Red_Wall_Are_Written_By_Roughness_Beside_Their_Trace_Where_RC_PICTURES_Names_A_Folder()
    {
        // dotnet test RadianceCascades.Tests with RC_PICTURES set to a folder writes there the six
        // spheres of the red wall's room as their cones are read at each configuration and as
        // traced, each a PFM of linear light, and spheres.json, the layout from which the Tools
        // module's sheet.py sets them beside each other with the reading less the trace, as
        // python3 -I modules/Tools/sheets/sheet.py <folder>/spheres.json <folder>/spheres.png. The
        // spheres stand in a row along x a unit and a half up, 1.2 apart, rougher toward +x, each
        // seen along one way from a little above, every pixel a point of its surface reflecting
        // toward that eye. The spheres are no part of the field, so the probes see past them.
        var folder = Environment.GetEnvironmentVariable("RC_PICTURES");
        if (string.IsNullOrEmpty(folder))
            return;
        Directory.CreateDirectory(folder);
        var lowFinest = new Level(new(-3.5f, 0.5f, -3.5f), 1, 8, 4, 8, NearTexels, 2);
        var channels = RedWallScenes(lowFinest, []);
        var eye = Vector3.Normalize(new Vector3(0, 0.35f, 1));
        var right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, eye));
        var up = Vector3.Cross(eye, right);
        var (wide, high) = (Roughnesses.Length * SpherePixels, SpherePixels);
        var pixels = new List<(int Pixel, Vector3 Place, Vector3 Normal, float Roughness)>();
        for (int s = 0; s < Roughnesses.Length; s++)
            for (int j = 0; j < SpherePixels; j++)
                for (int i = 0; i < SpherePixels; i++)
                {
                    var (x, y) = ((i + 0.5f) / SpherePixels * 2 - 1, 1 - (j + 0.5f) / SpherePixels * 2);
                    if (x * x + y * y >= 0.9f * 0.9f)
                        continue;
                    var normal = right * x + up * y + eye * MathF.Sqrt(1 - x * x - y * y);
                    pixels.Add((j * wide + s * SpherePixels + i, new Vector3(-3 + 1.2f * s, 1.5f, 0) + normal * SphereRadius, normal, Roughnesses[s]));
                }
        var points = pixels.Select(p => (p.Place, p.Normal)).ToArray();
        var cones = pixels.Select((p, k) => (k, eye, p.Roughness)).ToArray();
        var bounces = channels.Select(c => c.Bounce!).ToArray();
        var trace = new float[pixels.Count][];
        Parallel.For(0, pixels.Count, k => trace[k] = ConeTraced(bounces, pixels[k].Place, pixels[k].Normal, eye, pixels[k].Roughness * pixels[k].Roughness).Light);
        var reads = new Dictionary<Configuration, float[][]>();
        foreach (var configuration in Enum.GetValues<Configuration>())
        {
            var light = new float[channels.Length][];
            Parallel.For(0, channels.Length, c =>
            {
                var scene = configuration == Configuration.Engine ? channels[c].AtEngine!() : channels[c];
                light[c] = Passes(scene.Cells, scene.Levels, scene.Place, scene.Setting with { Points = points, Cones = cones }).ConeLight;
            });
            reads[configuration] = [.. Enumerable.Range(0, pixels.Count).Select(k => light.Select(l => l[k]).ToArray())];
        }

        void Write(string name, float[][] light)
        {
            var image = new float[wide * high * 3];
            for (int k = 0; k < pixels.Count; k++)
                for (int c = 0; c < 3; c++)
                    image[pixels[k].Pixel * 3 + c] = light[k][c];
            using var file = File.Create(Path.Combine(folder, name));
            file.Write(Encoding.ASCII.GetBytes($"PF\n{wide} {high}\n-1.0\n"));
            // A PFM's rows run from the bottom up, little-endian where its scale is negative.
            for (int j = high - 1; j >= 0; j--)
                for (int i = 0; i < wide * 3; i++)
                {
                    var bytes = BitConverter.GetBytes(image[j * wide * 3 + i]);
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(bytes);
                    file.Write(bytes);
                }
        }
        Write("traced.pfm", trace);
        Write("module.pfm", reads[Configuration.Module]);
        Write("engine.pfm", reads[Configuration.Engine]);
        var each = Enum.GetValues<Configuration>().ToDictionary(c => c, c => Roughnesses.Select(r =>
        {
            var sphere = Enumerable.Range(0, pixels.Count).Where(k => pixels[k].Roughness == r).ToArray();
            return ConeDifference(sphere, reads[c], trace, sphere.Average(k => trace[k].Sum()));
        }).ToArray());
        var subtitle = "Six spheres before a red wall in a room open to the sky, roughness "
            + $"{string.Join(", ", Roughnesses.Select(r => Invariant(r, "0.0")))} from the left, each pixel the cone its surface reflects toward the eye";
        File.WriteAllText(Path.Combine(folder, "spheres.json"), "{\"title\": \"Rough reflections read from the cascades\", "
            + $"\"subtitle\": \"{subtitle}\", \"font\": 16, "
            + $"\"columns\": [\"read from the cascades\", \"traced with {ConeRays} rays a cone\", \"the reading less the trace\"], "
            + "\"gain\": {\"mean\": \"traced.pfm\", \"to\": 0.18}, \"share\": 0.25, \"rows\": ["
            + "{\"title\": \"the module's configuration\", \"panels\": [{\"image\": \"module.pfm\"}, {\"image\": \"traced.pfm\"}, {\"signed\": [\"module.pfm\", \"traced.pfm\"]}]}, "
            + "{\"title\": \"an engine's configuration\", \"panels\": [{\"image\": \"engine.pfm\"}, {\"image\": \"traced.pfm\"}, {\"signed\": [\"engine.pfm\", \"traced.pfm\"]}]}]}\n");
        var said = string.Join("; ", each.Select(e => $"{ConfigurationName(e.Key)} configuration's spheres differ from their trace by "
            + string.Join(", ", e.Value.Select(d => Invariant(d, "0.000")))));
        output.WriteLine(said);
        Assert.True(each.Values.All(d => d.All(float.IsFinite)), said);
    }
}
