using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The same floors read by the library's passes, each a function a compute kernel calls once a
/// thread with its index, dispatched over every texel of each level of a chain of cascades and
/// merged from the coarsest down, against the kernel that reads them on one thread.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // A level of a chain of cascades, its first probe's place, its spacing, its probes along each
    // axis, its texels along each side, the end of its rays, which begin at the probe, and the
    // texels along a side the level below and the gather read it at, each the mean of the traced
    // texels it covers, 0 for those it traces, and those the level below's merge reads it at where
    // they differ from those, 0 where they do not.
    private readonly record struct Level(Vector3 Corner, float Spacing, int X, int Y, int Z, int Texels, float End, int Kept = 0, int Read = 0)
    {
        public int Probes => X * Y * Z;
        public int TexelCount => Probes * Texels * Texels;
        public int ReachCount => Probes * ReachTexels * ReachTexels;
    }

    // Marched, Steps and Most count the finest level's texels no probe above saw the end of,
    // Onward, OnwardSteps and OnwardMost those that took any step past their end, Capped those
    // whose trace stopped at its cap of steps, and SkyShare is the first point's share of the sky.
    private readonly record struct Chained(float Reflected, int Marched, int Steps, int Most, float[] Light, int Onward, int OnwardSteps, int OnwardMost,
        int Capped, float[] Taken)
    {
        public float SkyShare { get; init; }

        public override string ToString() =>
            $"reflects {Reflected:0.00000}, {Marched} of the finest level's texels marching on themselves in {Steps} steps in all and {Most} at most, "
            + $"{Onward} stepping past their end in {(Onward == 0 ? 0 : OnwardSteps / (float)Onward):0.0} steps on the mean and {OnwardMost} at most, "
            + $"{Capped} stopped at their cap";
    }

    // The pair the one-thread kernel reads, a near probe at place and the far cascade.
    private static Level[] Pair(Vector3 place) =>
        [new(place, 1, 1, 1, 1, NearTexels, NearReach), new(FarCorner, FarSpacing, FarCount, FarCount, FarCount, FarTexels, FarEnd)];

    // What a binding sets beyond the levels, each test's own, at the defaults the tests above read
    // with, a floor facing up under the uniform sky lit by nothing else, the probes at their grid
    // places. Moved moves a probe within a quarter unit of a surface out along the field's normal
    // to a quarter unit off it, VaryingSky takes the sky along each way (SkyAlong), Sun is the way
    // toward a sun times its irradiance, which lights a surface of albedo WallAlbedo, a sheet from
    // either side, Reference marches each of the finest level's texels on itself to the coarsest
    // level's end in place of the chain, and Caps holds each level's cap of steps a march takes, 0
    // for none. FieldCascades gives the field's cascades, each its least corner and its cell, the
    // finest first, the cells built for each one after another, CellLight the light a hit takes
    // from each of their cells, and Points the surface points read and their normals, in place of
    // the one at place, and Traced gathers each point along its own rays (gatherTraced).
    private sealed record Setting
    {
        public Vector4[]? FieldCascades { get; init; }
        public float[]? CellLight { get; init; }
        public (Vector3 Place, Vector3 Normal)[]? Points { get; init; }
        public Vector3 Normal { get; init; } = Vector3.UnitY;
        public float SkyLight { get; init; } = Sky;
        public bool VaryingSky { get; init; }
        public Vector3 Sun { get; init; }
        public bool Moved { get; init; }
        public bool Reference { get; init; }
        public int[]? Caps { get; init; }
        public bool Traced { get; init; }
    }

    private const float WallAlbedo = 0.6f;
    private const float Margin = 0.25f;

    // The sky along a way when it varies, brightest toward the top and toward x, as the test kernel
    // gives it.
    private static float SkyAlong(Vector3 way, float light) =>
        light * (0.6f + 0.4f * way.X + 1.5f * MathF.Pow(MathF.Max(way.Y, 0), 2));

    // What a surface at place facing as the setting says reflects of the light the chain's passes
    // merge, where the finest level's probes about it stand, each pass a dispatch of a thread a
    // texel, with the light each of the finest level's first probe's texels brings.
    private static Chained Passes(float[] cells, Level[] levels, Vector3 place, Setting? setting = null)
    {
        setting ??= new Setting();
        var points = setting.Points ?? [(place, setting.Normal)];
        var setup = new float[129 + 7 * points.Length];
        (setup[0], setup[1], setup[2], setup[3], setup[4], setup[5]) = (Corner.X, Corner.Y, Corner.Z, Cell, Size, setting.SkyLight);
        float[] binding = [setting.Moved ? 1 : 0, Margin, setting.VaryingSky ? 1 : 0, setting.Sun.X, setting.Sun.Y, setting.Sun.Z, WallAlbedo,
            setting.CellLight is null ? 0 : 1];
        binding.CopyTo(setup, 80);
        if (setting.FieldCascades is { } field)
        {
            setup[96] = field.Length;
            for (int c = 0; c < field.Length; c++)
                (setup[97 + c * 4], setup[98 + c * 4], setup[99 + c * 4], setup[100 + c * 4]) = (field[c].X, field[c].Y, field[c].Z, field[c].W);
        }
        (setup[8], setup[9]) = (levels[^1].End, levels.Length);
        var (texels, reaches) = (0, 0);
        for (int l = 0; l < levels.Length; l++)
        {
            var (level, at) = (levels[l], 16 + l * 16);
            float[] cascade = [level.Corner.X, level.Corner.Y, level.Corner.Z, level.Spacing, level.X, level.Y, level.Z, level.Texels, 0, level.End, texels, reaches, texels,
                setting.Caps?[l] ?? 0, level.Kept, level.Read];
            cascade.CopyTo(setup, at);
            (texels, reaches) = (texels + level.TexelCount, reaches + level.ReachCount);
        }
        setup[128] = points.Length;
        for (int k = 0; k < points.Length; k++)
        {
            float[] point = [points[k].Place.X, points[k].Place.Y, points[k].Place.Z, points[k].Normal.X, points[k].Normal.Y, points[k].Normal.Z, Albedo];
            point.CopyTo(setup, 129 + k * 7);
        }
        var (rays, reach, merged, marches, surfaces) = (new float[6 * texels], new float[reaches], new float[3 * texels], new float[2 * texels], new float[6 * points.Length]);
        // Each texel's share of the sky, then each point's.
        var skyShares = new float[texels + points.Length];
        setup[124] = texels;
        var cellLight = setting.CellLight ?? [0];
        void Pass(int pass, int level, int threads)
        {
            (setup[6], setup[7]) = (pass, level);
            CpuKernel.Run("probe_passes_cpu.slang", ((uint)(threads + 63) / 64, 1, 1), cells, setup, rays, reach, merged, marches, surfaces, cellLight,
                skyShares);
        }
        if (setting.Reference)
        {
            // The finest level's reach map, which the gather reads, and its texels marched on.
            Pass(1, 0, levels[0].ReachCount);
            Pass(5, 0, levels[0].TexelCount);
        }
        else
        {
            for (int l = 0; l < levels.Length; l++)
            {
                Pass(0, l, levels[l].TexelCount);
                Pass(1, l, levels[l].ReachCount);
            }
            Pass(2, levels.Length - 1, levels[^1].TexelCount);
            for (int l = levels.Length - 2; l >= 0; l--)
                Pass(3, l, levels[l].TexelCount);
        }
        Pass(setting.Traced ? 6 : 4, 0, points.Length);
        var (marched, steps, most) = (0, 0, 0);
        var (onward, onwardSteps, onwardMost, capped) = (0, 0, 0, 0);
        for (int k = 0; k < levels[0].TexelCount; k++)
        {
            var taken = (int)marches[k * 2 + 1];
            if (marches[k * 2] > 0)
                (marched, steps, most) = (marched + 1, steps + taken, Math.Max(most, taken));
            if (taken > 0)
                (onward, onwardSteps, onwardMost) = (onward + 1, onwardSteps + taken, Math.Max(onwardMost, taken));
            // A ray that stopped at its cap ends as the fourth way, RayCapped.
            if (rays[k * 6] == 3)
                capped++;
        }
        var light = new float[levels[0].Texels * levels[0].Texels];
        for (int k = 0; k < light.Length; k++)
            light[k] = merged[k * 3];
        var takenAt = Enumerable.Range(0, points.Length).Select(k => surfaces[k * 6]).ToArray();
        return new(surfaces[3], marched, steps, most, light, onward, onwardSteps, onwardMost, capped, takenAt) { SkyShare = skyShares[texels] };
    }

    [CpuKernelFact]
    public void The_Passes_Run_A_Thread_A_Texel_Read_The_Four_Scenes_Floors_As_The_One_Thread_Kernel_Does()
    {
        const float o = 4 + Wall;
        var dark = new Vector3(-4.5f, 0.25f, 0);
        var scenes = new (string Name, IEnumerable<Triangle> Triangles, Vector3 Place)[]
        {
            ("the open box's", OpenBox(), Floor),
            ("the lidded box's", OpenBox().Concat(Cuboid(new(-o, 8, -o), new(o, 8 + Wall, o), 6)), Floor),
            ("the sheeted box's", OpenBox().Concat(Sheet(8, o, 6)), Floor),
            ("the parted rooms' dark", Rooms(parted: true), dark),
            ("the whole rooms' dark", Rooms(parted: false), dark),
        };
        foreach (var (name, triangles, place) in scenes)
        {
            var cells = Field(triangles);
            var (one, passes) = (Gather(cells, place), Passes(cells, Pair(place), place));
            var said = $"{name} floor on one thread {one}, and by the passes {passes}, {passes.Reflected - one.Reflected:E2} apart";
            output.WriteLine(said);
            // The passes call the one-thread kernel's functions on the same values in the same
            // order, so they read alike to the last bit.
            Assert.True(MathF.Abs(passes.Reflected - one.Reflected) <= 1e-6f + 1e-5f * one.Reflected && passes.Marched == one.Marched, said);
        }
    }

    [CpuKernelFact]
    public void A_Chain_Of_Three_Cascades_Reads_The_Open_Boxs_Floor_By_Its_Share_And_The_Lidded_Boxes_Dark()
    {
        // A near probe reaching 2 units, a middle cascade of probes a unit apart over the floor's
        // middle reaching 2, twice its spacing, and the far cascade past the field's edge, so a ray
        // from the floor reaches the far cascade 4 units out, as the pair hands it.
        Level[] chain =
        [
            new(Floor, 1, 1, 1, 1, NearTexels, 2),
            new(new(-3.5f, -0.5f, -3.5f), 1, 8, 5, 8, 16, 2),
            new(FarCorner, FarSpacing, FarCount, FarCount, FarCount, FarTexels, FarEnd),
        ];
        const float o = 4 + Wall;
        var open = Passes(Field(OpenBox()), chain, Floor);
        var lidded = Passes(Field(OpenBox().Concat(Cuboid(new(-o, 8, -o), new(o, 8 + Wall, o), 6))), chain, Floor);
        var sheeted = Passes(Field(OpenBox().Concat(Sheet(8, o, 6))), chain, Floor);
        var high = 8 - Floor.Y;
        var expected = Albedo * 4 * CornerShare(4 / high, 4 / high) * Sky;
        var said = $"by three cascades the open box's floor {open}, where the sky's share gives {expected:0.00000}, the lidded box's {lidded} and the sheeted box's {sheeted}";
        output.WriteLine(said);
        // 0.25037 against 0.25111, 0.3 percent under.
        Assert.True(Math.Abs(open.Reflected - expected) <= 0.05 * expected, said);
        // Nothing under either lid, the sheet's seam with the walls' tops closed in the field
        // (seamSheet).
        Assert.True(lidded.Reflected == 0 && sheeted.Reflected == 0, said);
    }

    [CpuKernelFact]
    public void A_Chain_Of_Four_Cascades_At_The_Ends_Rule_Reads_The_Open_Boxs_Floor_Within_A_Tenth_Of_Its_Share()
    {
        // Probes 8 along each axis 1, 2, 4 and 8 apart, reaching 2, 4, 8 and past the field's edge,
        // each end twice its spacing, the finest level's lowest row a quarter of a meter over the
        // floor.
        Level[] chain =
        [
            new(new(-3.5f, 0.25f, -3.5f), 1, 8, 8, 8, NearTexels, 2),
            new(FarCorner, FarSpacing, FarCount, FarCount, FarCount, FarTexels, 4),
            new(new(-14, -10, -14), 4, 8, 8, 8, 16, 8),
            new(new(-28, -28, -28), 8, 8, 8, 8, 16, FarEnd),
        ];
        // The finest level's row across the floor at z 0.5 and the floor's middle as gatherAt reads
        // it from the four probes about it, each against the light the sky sends its point through
        // the opening, the sky times pi times the share of the dome the opening covers there, four
        // corner rectangles over it, which a trace against the box's black walls and floor gives.
        var points = Enumerable.Range(0, 8).Select(i => (new Vector3(-3.5f + i, 0.25f, 0.5f), Vector3.UnitY)).Append((Floor, Vector3.UnitY)).ToArray();
        var reading = Passes(Field(OpenBox()), chain, Floor, new Setting { Points = points });
        double Share(Vector3 p)
        {
            var high = 8 - p.Y;
            return CornerShare((4 - p.X) / high, (4 - p.Z) / high) + CornerShare((4 + p.X) / high, (4 - p.Z) / high)
                + CornerShare((4 - p.X) / high, (4 + p.Z) / high) + CornerShare((4 + p.X) / high, (4 + p.Z) / high);
        }
        var over = points.Select((p, i) => reading.Taken[i] / (Sky * Math.PI * Share(p.Item1))).ToArray();
        var said = $"by four cascades at the ends rule the open box's floor row reads {string.Join(", ", over[..8].Select(q => q.ToString("0.000")))} of its share, "
            + $"and the floor's middle by gatherAt {over[8]:0.000} of it, {reading}";
        output.WriteLine(said);
        // The row reads 0.929 to 1.010 of its share, 0.953 on the mean, and the floor's middle
        // 0.920, where with every far probe about a ray's end taken as it stands the row read 1.042
        // on the mean and the middle 1.040. Each lies within a tenth of its share.
        Assert.True(over.All(q => q is >= 0.9 and <= 1.1), said);
    }
}
