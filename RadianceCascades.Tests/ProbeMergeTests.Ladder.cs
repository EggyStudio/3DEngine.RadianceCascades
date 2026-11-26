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
    // beside a lit one whose light is the leak through the wall between them. AtEngine builds the
    // scene again at an engine's configuration, its field, setting and levels, and Bounce is how
    // its surfaces pass light between them round after round, whatever the configuration.
    private sealed record LadderScene(string Name, float[] Cells, Setting Setting, Level[] Levels, Vector3 Place,
        (string Name, (Vector3, Vector3)[] Points)[] Parts, Func<Vector3, Vector3, float> Traced, bool HeldOut = false,
        (Vector3, Vector3)[]? Dark = null)
    {
        public Func<LadderScene>? AtEngine { get; init; }
        public Bounce? Bounce { get; init; }
    }

    // The configurations the ladder reads every merge at, the module's chain of the closed room's
    // levels in the test's one cascade of the field, and a configuration an engine binds the
    // passes at, its field's cascades and its levels (EngineField, EngineLevels).
    private enum Configuration
    {
        Module,
        Engine,
    }

    // A merge's reading of a scene at a configuration, each part's light over the light traced
    // there after one gather (First) and at the steady state of the bounce (Ratios), the rounds its
    // loop took and whether it settled within MostRounds, the gain a round, the leak at the steady
    // state, and a texel's steps past its ray's end, the per-probe stage's among them, and its
    // reads of the probes above on the mean.
    private sealed record LadderRow(Configuration Configuration, LadderScene Scene, Merge Merge, float[] First, float[] Ratios, int Rounds, bool Settles,
        float Gain, float Leak, float Steps, float Reads)
    {
        // Each part's light each round, summed over its points.
        public List<float[]> EachRound { get; init; } = [];
        public float[] Distances => Ratios.Select(r => MathF.Abs(MathF.Log(r))).ToArray();
        public float Mean => Distances.Average();
        public float Worst => Distances.Max();
        public float Cost => Steps + Reads;
    }

    // A merge's standing at a configuration over the scenes the standing is read over, every
    // part's distance at the steady state pooled, the worst part, the worst leak, the cost on the
    // mean over the scenes, the largest gain a round, and its reading of the scene held out. It
    // stands above another where it is nearer on the mean and no worse in its worst part, its leak
    // and its steps and reads together.
    private sealed record Standing(Configuration Configuration, Merge Merge, float Mean, float Worst, float Leak, float Steps, float Reads, float Gain,
        LadderRow Held)
    {
        public bool Above(Standing b) => Mean < b.Mean && Worst <= b.Worst && Leak <= b.Leak && Steps + Reads <= b.Steps + b.Reads;
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

    // An engine's field about center, five cascades of 64 cells, the finest a cell of 0.125 and
    // each one past it twice the last.
    private static Vector4[] EngineField(Vector3 center) =>
        Enumerable.Range(0, 5).Select(c => 0.125f * (1 << c)).Select(cell => new Vector4(center - new Vector3(32 * cell), cell)).ToArray();

    // An engine's levels about center, one for each cascade of its field, 8 probes along each axis
    // every 8 of the cascade's cells, each in the middle of its cell of the level's grid. The
    // finest two are traced at 16 texels a side and read at 8, their 2 by 2 means, as four rays a
    // texel at 8 give. Every ray begins at its probe and ends at four times the spacing, but the
    // last two's at the field's diagonal (EngineEnd), and each is capped at 64 steps but the
    // last's.
    private static Level[] EngineLevels(Vector3 center) => EngineLevels(EngineField(center));

    // An engine's levels in its field's cascades, as EngineLevels lays them about a center.
    private static Level[] EngineLevels(Vector4[] field) => field.Select((f, c) =>
    {
        var spacing = 8 * f.W;
        return new Level(new Vector3(f.X, f.Y, f.Z) + new Vector3(spacing / 2), spacing, 8, 8, 8, 16, c < 3 ? 4 * spacing : EngineEnd, c < 2 ? 8 : 0);
    }).ToArray();

    // The engine's field's diagonal, 64 of its coarsest cells of 2 times the square root of 3.
    private const float EngineEnd = 221.7025f;

    // The engine's setting beside a scene's own, its field about center, its caps of steps, its
    // probes moved off the field's surfaces to stand a tenth of a unit clear by at most 0.45 of
    // their spacing, and each point gathered from the finest level whose probes stand about it or
    // within half its spacing past its edge probes, the level's own cells. A wall's points a
    // quarter of a unit past the finest level's last probes would go to the level above, whose
    // probes stand a spacing off the wall and see what the wall does not.
    private static Setting EngineSetting(Setting setting, Vector3 center) => setting with
    {
        FieldCascades = EngineField(center),
        Caps = [64, 64, 64, 64, 0],
        Moved = true,
        MovedTo = 0.1f,
        MovedAtMost = 0.45f,
        GatherHolding = true,
        GatherRim = true,
    };

    // The cells of the engine's field about center that the triangles resolve to, its cascades one
    // after another.
    private static float[] EngineCells(IEnumerable<Triangle> triangles, Vector3 center)
    {
        var all = triangles.ToList();
        return EngineField(center).SelectMany(f => FieldCascade(all, new(f.X, f.Y, f.Z), f.W)).ToArray();
    }

    // A scene in the test's one cascade of the field, its boxes, the light each cell takes by its
    // place and the sky's light, traced among the boxes, and at the engine's configuration about
    // center, each hit's light read by its place in the one cascade, as the traced light reads it,
    // every surface of albedo Albedo but where albedo gives another.
    private static LadderScene BoxScene(string name, (Vector3 Low, Vector3 High)[] boxes, Func<Vector3, float> lightAt, float sky, Level finest,
        (string, (Vector3, Vector3)[])[] parts, bool heldOut = false, (Vector3, Vector3)[]? dark = null, Vector3? center = null,
        Func<Vector3, float>? albedo = null)
    {
        var light = CellLightBy(lightAt);
        var triangles = boxes.SelectMany((box, i) => Cuboid(box.Low, box.High, i + 1)).ToList();
        var about = center ?? new Vector3(0, 2, 0);
        var setting = new Setting { CellLight = light, SkyLight = sky };
        var bounce = BoxBounce(boxes, light, sky, albedo);
        return new(name, Field(triangles), setting, LadderLevels(finest), Floor, parts, (p, n) => TracedAmongBoxes(boxes, p, n, light, sky), heldOut,
            dark)
        {
            AtEngine = () => new(name, EngineCells(triangles, about), EngineSetting(setting with { LightByPlace = true }, about), EngineLevels(about), Floor,
                parts, (p, n) => TracedAmongBoxes(boxes, p, n, light, sky), heldOut, dark) { Bounce = bounce },
            Bounce = bounce,
        };
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

    // The closed room 8 high of the lamp's box, its walls, floor and lid, and the light each place
    // of its surfaces sends, the lid's middle square of 2 a side 5 and every other face none.
    private static readonly (Vector3 Low, Vector3 High)[] LampBoxes =
    [
        (new(-4 - Wall, -1.5f, -4 - Wall), new(4 + Wall, 0, 4 + Wall)), (new(4, 0, -4 - Wall), new(4 + Wall, 8, 4 + Wall)),
        (new(-4 - Wall, 0, -4 - Wall), new(-4, 8, 4 + Wall)), (new(-4, 0, 4), new(4, 8, 4 + Wall)), (new(-4, 0, -4 - Wall), new(4, 8, -4)),
        (new(-4 - Wall, 8, -4 - Wall), new(4 + Wall, 8 + Wall, 4 + Wall)),
    ];

    private static float LampAt(Vector3 p) => MathF.Abs(p.X) < 1 && MathF.Abs(p.Z) < 1 && p.Y is > 7.75f and < 8 ? 5 : 0;

    // The ladder's scenes, nine the standing is read over and one held out.
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
        var lampLight = CellLightBy(LampAt);
        var closedBoxes = LampBoxes;
        var lamp = new LadderScene("a closed box with one lamp", Field(closedTriangles), new Setting { CellLight = lampLight, SkyLight = 0 },
            LadderLevels(new(new(-3.5f, 0.5f, -3.5f), 1, 8, 8, 8, NearTexels, 2)), Floor,
            [("floor", Grid(new(-3, 0.25f, -3), Vector3.UnitX, 7, Vector3.UnitZ, 7, Vector3.UnitY)), ("walls", RoomWalls(1, 2, 4))],
            (p, n) => TracedAmongBoxes(closedBoxes, p, n, lampLight, 0)) { Bounce = BoxBounce(closedBoxes, lampLight, 0) };
        var lampCenter = new Vector3(0, 4, 0);
        lamp = lamp with
        {
            AtEngine = () => lamp with
            {
                Cells = EngineCells(closedTriangles, lampCenter),
                Setting = EngineSetting(lamp.Setting with { LightByPlace = true }, lampCenter),
                Levels = EngineLevels(lampCenter),
            },
        };

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
            (p, n) => TracedAgainstBoxes(MovedOff(p), n)) { Bounce = LintelBounce(lintelLight) };
        // At the engine's configuration the lintel's own four cascades of the field are the
        // engine's finest four, about the same place, its light built again over the five.
        var lintelCenter = new Vector3(-2, 1.5f, 0);
        lintel = lintel with
        {
            AtEngine = () =>
            {
                var (cells, light) = LintelScene(true, EngineField(lintelCenter));
                return lintel with
                {
                    Cells = cells,
                    Setting = EngineSetting(lintel.Setting with { CellLight = light }, lintelCenter),
                    Levels = EngineLevels(lintelCenter),
                };
            },
        };

        // The open box under a sky of 1, its floor sending 0.3 and its walls 0.15.
        (Vector3, Vector3)[] openBoxes =
        [
            (new(-o, -1.5f, -o), new(o, 0, o)), (new(4, 0, -o), new(o, 8, o)), (new(-o, 0, -o), new(-4, 8, o)),
            (new(-4, 0, 4), new(4, 8, o)), (new(-4, 0, -o), new(4, 8, -4)),
        ];
        var open = BoxScene("an open box under a sky", openBoxes, p => p.Y < 0.25f ? 0.3f : 0.15f, 1,
            new(new(-3.5f, 0.5f, -3.5f), 1, 8, 8, 8, NearTexels, 2),
            [("floor", Grid(new(-3, 0.25f, -3), Vector3.UnitX, 7, Vector3.UnitZ, 7, Vector3.UnitY)), ("walls", RoomWalls(1, 2, 4))],
            center: new Vector3(0, 4, 0));

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
            [("by the opening", Third(0, 5)), ("middle", Third(5, 4)), ("far end", Third(9, 5))], center: new Vector3(0, 2, -1.5f));

        // A vestibule 8 square and 4 high, its side at -x open 3 high onto a court 3.25 deep and
        // 15.5 wide whose ground sends 0.6, as ground the sun lights does, its door 2 wide and 3
        // high in the +x wall closed by a black panel, and every other face sending a twentieth of
        // the sky's light of 1. Its walls take their light from the court and the sky through the
        // opening and from one another, read on the walls at -z and +z, the wall beside the door,
        // the floor and the ceiling.
        var vestibule = BoxScene("a vestibule off a sunlit court", [
                (new(-4.5f, -0.5f, -4.5f), new(4.5f, 0, 4.5f)), (new(-7.75f, -0.5f, -7.75f), new(-4.5f, 0, 7.75f)),
                (new(-4.5f, 0, -4.5f), new(4.5f, 4, -4)), (new(-4.5f, 0, 4), new(4.5f, 4, 4.5f)), (new(4, 0, -4), new(4.5f, 4, 4)),
                (new(-4.5f, 4, -4.5f), new(4.5f, 4.5f, 4.5f)), (new(-4.5f, 3, -4), new(-4, 4, 4)),
            ], p => p.X < -4.5f && p.Y < 0.25f ? 0.6f : p.X > 3.75f && MathF.Abs(p.Z) < 1 && p.Y < 3 ? 0 : 0.05f, 1, lowFinest,
            [
                ("wall at -z", Grid(new(-3, 0.5f, -3.75f), Vector3.UnitX, 7, Vector3.UnitY, 4, Vector3.UnitZ)),
                ("wall at +z", Grid(new(-3, 0.5f, 3.75f), Vector3.UnitX, 7, Vector3.UnitY, 4, -Vector3.UnitZ)),
                ("beside the door", [.. Grid(new(3.75f, 0.5f, -3), Vector3.UnitY, 4, Vector3.UnitZ, 2, -Vector3.UnitX),
                    .. Grid(new(3.75f, 0.5f, 2), Vector3.UnitY, 4, Vector3.UnitZ, 2, -Vector3.UnitX)]),
                ("floor", Grid(new(-3, 0.25f, -3), Vector3.UnitX, 7, Vector3.UnitZ, 7, Vector3.UnitY)),
                ("ceiling", Grid(new(-3, 3.75f, -3), Vector3.UnitX, 7, Vector3.UnitZ, 7, -Vector3.UnitY)),
            ], albedo: p => p.X > 3.75f && MathF.Abs(p.Z) < 1 && p.Y < 3 ? 0 : Albedo);

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
        return [door, lamp, thin, lintel, open, hall, vestibule, ArcadeCourt(), WallUnderALamp(), window];
    }

    // A paragraph's words in lines of at most width characters.
    private static IEnumerable<string> Wrapped(string paragraph, int width = 100)
    {
        var line = new StringBuilder();
        foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + 1 + word.Length > width)
            {
                yield return line.ToString();
                line.Clear();
            }
            line.Append(line.Length > 0 ? " " : "").Append(word);
        }
        if (line.Length > 0)
            yield return line.ToString();
    }

    private static string Invariant(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);

    // How the ladder's tables name each merge, by what it does and its struct's name.
    private static string MergeName(Merge merge) => merge switch
    {
        Merge.AtRayEnd => "at the ray's end, `MergeAtRayEnd`",
        Merge.ByReachSight => "by reach sight, `MergeByReachSight`",
        Merge.ByMarchedSight => "by marched sight, `MergeByMarchedSight`",
        Merge.AboutProbe => "about its own probe, `MergeAboutProbe`",
        _ => merge.ToString(),
    };

    // How the ladder's tables name each configuration.
    private static string ConfigurationName(Configuration configuration) => configuration switch
    {
        Configuration.Module => "the module's",
        Configuration.Engine => "an engine's",
        _ => configuration.ToString(),
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

    // A scene's points, its parts' and then its dark room's, and the count of its parts' alone.
    private static ((Vector3, Vector3)[] Points, int Lit) LadderPoints(LadderScene scene) =>
        (scene.Parts.SelectMany(part => part.Points).Concat(scene.Dark ?? []).ToArray(), scene.Parts.Sum(part => part.Points.Length));

    // The light of each part's points summed.
    private static float[] ByPart(LadderScene scene, float[] light)
    {
        var (sums, first) = (new float[scene.Parts.Length], 0);
        for (int p = 0; p < sums.Length; p++)
        {
            sums[p] = light[first..(first + scene.Parts[p].Points.Length)].Sum();
            first += scene.Parts[p].Points.Length;
        }
        return sums;
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
        var configurations = Enum.GetValues<Configuration>();
        var merges = Enum.GetValues<Merge>();
        var scenes = LadderScenes();
        // Each scene's traced loop, the same at every configuration, its transport, each point's
        // light traced after one gather and at the steady state, and the loop's rounds and gain.
        var traced = new Dictionary<string, (Transport Transport, float[] First, float[] Steady, int Rounds, float Gain, List<float[]> EachRound)>();
        foreach (var each in scenes)
        {
            var (points, lit) = LadderPoints(each);
            var transport = TransportOf(each.Bounce!, points);
            var own = points.Select(p => each.Traced(p.Item1, p.Item2)).ToArray();
            var (steady, rounds, gain, eachRound) = TracedLoop(transport, own, lit);
            traced[each.Name] = (transport, own, steady, rounds, gain, eachRound);
            output.WriteLine($"{each.Name}, traced over {transport.Cells.Length} cells, {rounds} rounds, gain {Invariant(gain, "0.000")}");
        }

        // Every merge's loop over each scene at each configuration, the loops run side by side
        // over half the machine's threads, each scene built once at each configuration.
        var built = configurations.SelectMany(configuration => scenes.Select(each => (Configuration: configuration, Each: each))).ToArray()
            .AsParallel().AsOrdered().Select(c => (c.Configuration, c.Each, Scene: c.Configuration == Configuration.Engine ? c.Each.AtEngine!() : c.Each))
            .ToArray();
        var jobs = built.SelectMany(b => merges.Select(merge => (b.Configuration, b.Each, b.Scene, Merge: merge))).ToArray();
        var rows = new LadderRow[jobs.Length];
        Parallel.For(0, jobs.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) }, j =>
        {
            var (configuration, each, scene, merge) = jobs[j];
            var (points, lit) = LadderPoints(scene);
            var reference = traced[each.Name];
            var (tracedFirst, tracedSteady) = (ByPart(scene, reference.First), ByPart(scene, reference.Steady));
            var (reading, light, rounds, settles, gain, eachRound) = MergedLoop(scene, reference.Transport, scene.Setting with { Merge = merge }, points, lit);
            var first = ByPart(scene, reading.Taken[..points.Length]).Select((sum, p) => sum / tracedFirst[p]).ToArray();
            var ratios = ByPart(scene, light).Select((sum, p) => sum / tracedSteady[p]).ToArray();
            var leak = scene.Dark is null ? 0 : light[lit..].Average() / reference.Steady[..lit].Average();
            rows[j] = new(configuration, each, merge, first, ratios, rounds, settles, gain, leak, reading.MergeSteps + reading.StageSteps, reading.MergeReads)
            {
                EachRound = eachRound.Select(round => ByPart(scene, round)).ToList(),
            };
        });
        foreach (var r in rows)
            output.WriteLine($"{r.Configuration}, {r.Scene.Name}, {r.Merge}, {string.Join(", ", r.First.Select(q => Invariant(q, "0.000")))} after one gather, "
                + $"{string.Join(", ", r.Ratios.Select(q => Invariant(q, "0.000")))} at the steady state in {r.Rounds} rounds, gain {Invariant(r.Gain, "0.000")}, "
                + $"leak {Invariant(r.Leak, "0.00000")}");

        // Each merge at each configuration over the scenes the standing is read over, at the
        // steady state.
        var standing = configurations.SelectMany(configuration => merges.Select(merge =>
        {
            var own = rows.Where(r => r.Configuration == configuration && r.Merge == merge && !r.Scene.HeldOut).ToArray();
            var held = rows.Single(r => r.Configuration == configuration && r.Merge == merge && r.Scene.HeldOut);
            var distances = own.SelectMany(r => r.Distances).ToArray();
            return new Standing(configuration, merge, distances.Average(), distances.Max(), own.Max(r => r.Leak), own.Average(r => r.Steps),
                own.Average(r => r.Reads), own.Max(r => r.Gain), held);
        }).OrderBy(s => s.Mean)).ToArray();
        // A merge stands on top where it stands above every other at every configuration.
        var top = merges.Where(m => configurations.All(c =>
        {
            var at = standing.Where(s => s.Configuration == c).ToArray();
            var own = at.Single(s => s.Merge == m);
            return at.All(b => b.Merge == m || own.Above(b));
        })).ToArray();

        var table = new StringBuilder();
        table.AppendLine("| Configuration | Merge | Mean distance | Worst part | Leak | Steps a texel | Reads a texel | Most gain a round | Stands above | Held out, mean and worst |");
        table.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
        foreach (var s in standing)
        {
            var above = standing.Where(b => b.Configuration == s.Configuration && b.Merge != s.Merge && s.Above(b))
                .Select(b => MergeName(b.Merge).Split(',')[0]).ToArray();
            table.AppendLine($"| {ConfigurationName(s.Configuration)} | {MergeName(s.Merge)} | {Invariant(s.Mean, "0.000")} | {Invariant(s.Worst, "0.000")} | "
                + $"{Invariant(s.Leak, "0.00000")} | {Invariant(s.Steps, "0.0")} | {Invariant(s.Reads, "0.0")} | {Invariant(s.Gain, "0.000")} | "
                + $"{(above.Length == 0 ? "none" : string.Join(", ", above))} | {Invariant(s.Held.Mean, "0.000")} and {Invariant(s.Held.Worst, "0.000")} |");
        }
        var verdict = top.Length == 1
            ? $"The merge {MergeName(top[0])}, stands above every other at every configuration and is the default."
            : "No merge stands above every other at every configuration, so the default stays the merge at the ray's end, `MergeAtRayEnd`.";

        var (counted, heldOut) = (scenes.Count(x => !x.HeldOut), scenes.Count(x => x.HeldOut));
        var file = new StringBuilder();
        file.AppendLine("# The ladder of merges");
        file.AppendLine();
        foreach (var line in Wrapped(
            $"Measured on {DateTime.UtcNow:yyyy-MM-dd} on the tree of {TreeCommit()}, every merge the passes take by name (`IMerge`) run over "
            + $"{scenes.Length} scenes on the CPU at {configurations.Length} configurations, as `dotnet test RadianceCascades.Tests` writes it with "
            + "`RC_LADDER` naming this file. The module's configuration is its tests' chain of four levels in one cascade of the field of 64 cells "
            + "of 0.25. An engine's is one an engine binds the passes at, five levels of 8 probes a side every 8 cells of the field's five cascades "
            + "of 64 cells from 0.125, the finest two traced at 16 texels a side and read at 8, every ray from its probe to four times its spacing "
            + "but the last two's to the field's diagonal, each point gathered from the finest level about it."))
            file.AppendLine(line);
        file.AppendLine();
        foreach (var line in Wrapped(
            "Each merge runs to a steady state of the bounce. A round runs the whole chain with every hit taking its surface's own light and what "
            + "the surface reflects of the light the probes gave it the round before, gathered at a cell of a grid over the scene's surfaces, "
            + $"every surface reflecting {Invariant(Albedo, "0.0")} of it but a black one and the lintel's room's {Invariant(LintelAlbedo, "0.000")}, "
            + "and nothing kept past that one round. The light traced to the points follows the same rounds over the same cells by tracing "
            + $"alone. Each loop stops at the first round that changes its points' light by under a hundredth, or at the {MostRounds}th."))
            file.AppendLine(line);
        file.AppendLine();
        foreach (var line in Wrapped(
            "A part of a scene is read as the light its points take over the light traced to them, after one gather and at the steady state, "
            + "and its distance is the steady state's ratio's logarithm taken whole, 0.105 a tenth over or under. The gain a round is the change "
            + "a loop's last round makes over the change of the round before, the factor a bounce multiplies an error by. The leak is the light "
            + "the dark room takes through a wall of one cell beside the lit one at the steady state, as a share of the lit room's traced light. "
            + "The cost is a texel's steps past its ray's end, the per-probe stage's steps among them, and its reads of the probes above, a "
            + "texel's light, its share of the sky or a reach texel, on the mean over every level's texels. A merge stands above another at a "
            + "configuration where it is nearer on the mean and no worse in its worst part, its leak and its steps and reads together, over the "
            + $"{counted} scenes, and the one standing above every other at every configuration is the default. The last "
            + $"{(heldOut == 1 ? "scene is" : $"{heldOut} scenes are")} held out of the standing and read apart, since no merge is tuned on it."))
            file.AppendLine(line);
        file.AppendLine();
        file.AppendLine("## Standing");
        file.AppendLine();
        file.Append(table);
        file.AppendLine();
        file.AppendLine(verdict);
        file.AppendLine();
        file.AppendLine("## Each scene");
        file.AppendLine();
        file.AppendLine("| Configuration | Scene | Merge | Each part's ratio after one gather | Each part's ratio at the steady state | Rounds | Gain a round "
            + "| Mean distance | Worst part | Leak | Steps a texel | Reads a texel |");
        file.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
        string Each(LadderRow r, float[] ratios) => string.Join(", ", r.Scene.Parts.Select((part, p) => $"{part.Name} {Invariant(ratios[p], "0.000")}"));
        foreach (var r in rows)
            file.AppendLine($"| {ConfigurationName(r.Configuration)} | {r.Scene.Name} | {MergeName(r.Merge)} | {Each(r, r.First)} | {Each(r, r.Ratios)} | "
                + $"{(r.Settles ? r.Rounds.ToString(CultureInfo.InvariantCulture) : $"{r.Rounds}, unsettled")} | {Invariant(r.Gain, "0.000")} | "
                + $"{Invariant(r.Mean, "0.000")} | {Invariant(r.Worst, "0.000")} | {(r.Scene.Dark is null ? "" : Invariant(r.Leak, "0.00000"))} | "
                + $"{Invariant(r.Steps, "0.0")} | {Invariant(r.Reads, "0.0")} |");
        file.AppendLine();
        file.AppendLine("## The traced loop");
        file.AppendLine();
        file.AppendLine("| Scene | Cells | Rounds | Gain a round |");
        file.AppendLine("|---|---|---|---|");
        foreach (var each in scenes)
            file.AppendLine($"| {each.Name} | {traced[each.Name].Transport.Cells.Length} | {traced[each.Name].Rounds} | {Invariant(traced[each.Name].Gain, "0.000")} |");

        // The vestibule's walls round by round, which an engine's frames from none are set beside.
        var vestibule = scenes.Single(x => x.Name.StartsWith("a vestibule", StringComparison.Ordinal));
        var walls = vestibule.Parts.Select((part, p) => (part.Name, p)).Where(x => x.Name.StartsWith("wall", StringComparison.Ordinal)).Select(x => x.p).ToArray();
        var vestibuleRows = rows.Where(r => r.Scene == vestibule).ToArray();
        var most = vestibuleRows.Max(r => r.EachRound.Count);
        float Walls(float[] parts) => walls.Sum(p => parts[p]);
        file.AppendLine();
        file.AppendLine("## The vestibule round by round");
        file.AppendLine();
        file.AppendLine("Its two walls' light each round over the first round's, traced and by each merge.");
        file.AppendLine();
        file.AppendLine($"| Configuration | Merge | {string.Join(" | ", Enumerable.Range(1, most))} |");
        file.AppendLine($"|---|---|{string.Concat(Enumerable.Repeat("---|", most))}");
        string RoundByRound(List<float[]> rounds) => string.Join(" | ", Enumerable.Range(0, most).Select(k => k < rounds.Count ? Invariant(Walls(rounds[k]) / Walls(rounds[0]), "0.000") : ""));
        file.AppendLine($"| every one | traced | {RoundByRound(traced[vestibule.Name].EachRound.Select(round => ByPart(vestibule, round)).ToList())} |");
        foreach (var r in vestibuleRows)
            file.AppendLine($"| {ConfigurationName(r.Configuration)} | {MergeName(r.Merge)} | {RoundByRound(r.EachRound)} |");
        // The corridor walked at each configuration by each merge, and at an engine's by reach
        // sight with its levels and then its field held still.
        (Configuration Configuration, Merge Merge, Still Still)[] walked =
        [
            .. configurations.SelectMany(c => merges.Select(m => (c, m, Still.None))),
            (Configuration.Engine, Merge.ByReachSight, Still.Levels), (Configuration.Engine, Merge.ByReachSight, Still.Field),
        ];
        var walks = walked.Select(w => (w.Configuration, w.Merge, w.Still, Swings: CorridorSwings(CorridorWalk(w.Configuration, w.Merge, CorridorEyes(w.Configuration), w.Still))))
            .ToArray();
        file.AppendLine();
        file.AppendLine("## The corridor walked");
        file.AppendLine();
        foreach (var line in Wrapped(
            "A corridor along x, 2.5 wide and 3.5 high inside, its walls fixed, under a sun through a gap 2 wide across its roof and a lamp "
            + "in the roof beside it, its points on the floor and on one wall from -6 to 6. The eye walks along it in quarter steps, from -8 to "
            + "8 at an engine's configuration, the field's cascades and the levels snapped to their spacing about it, and from -4 to 4 at the "
            + "module's, its levels snapped about it in the field's one cascade, which holds still. Each point is gathered from the finest level "
            + "about it within half its spacing past its edge probes. The swing is each point's light at every eye over its mean over the walk, "
            + "taken whole, and the step each step's change over that mean, each the worst and the 99th part. At an engine's configuration by "
            + "reach sight the walk is read again with the levels and then the field held still about its middle."))
            file.AppendLine(line);
        file.AppendLine();
        file.AppendLine("| Configuration | Merge | Held still | Worst swing | Swing at the 99th part | Worst step | Step at the 99th part |");
        file.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var w in walks)
        {
            var held = w.Still switch { Still.Field => "the field", Still.Levels => "the levels", _ => "nothing" };
            var row = $"| {ConfigurationName(w.Configuration)} | {MergeName(w.Merge)} | {held} | {Invariant(w.Swings.Worst, "0.000")} | "
                + $"{Invariant(w.Swings.Part, "0.000")} | {Invariant(w.Swings.WorstStep, "0.000")} | {Invariant(w.Swings.StepPart, "0.000")} |";
            file.AppendLine(row);
            output.WriteLine(row);
        }
        // The cones' section, which their own reading writes, is kept as it stands.
        File.WriteAllText(path, file.ToString() + KeptConeSection(path));

        // The README's table under its ladder's heading takes the standing in place of the last.
        var readme = Path.Combine(Repository.Root, "README.md");
        var lines = File.ReadAllLines(readme).ToList();
        var heading = lines.IndexOf("## The ladder of merges");
        var start = heading < 0 ? -1 : lines.FindIndex(heading, l => l.StartsWith("| Configuration |", StringComparison.Ordinal)
            || l.StartsWith("| Merge |", StringComparison.Ordinal));
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
        Assert.True(rows.Length == configurations.Length * merges.Length * scenes.Length, "every merge reads every scene at every configuration");
    }
}
