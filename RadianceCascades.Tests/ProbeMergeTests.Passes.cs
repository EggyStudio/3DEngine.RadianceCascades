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
    // axis, its texels along each side and the end of its rays, which begin at the probe.
    private readonly record struct Level(Vector3 Corner, float Spacing, int X, int Y, int Z, int Texels, float End)
    {
        public int Probes => X * Y * Z;
        public int TexelCount => Probes * Texels * Texels;
        public int ReachCount => Probes * ReachTexels * ReachTexels;
    }

    private readonly record struct Chained(float Reflected, int Marched, int Steps, int Most)
    {
        public override string ToString() =>
            $"reflects {Reflected:0.00000}, {Marched} of the finest level's texels marching on themselves in {Steps} steps in all and {Most} at most";
    }

    // The pair the one-thread kernel reads, a near probe at place and the far cascade.
    private static Level[] Pair(Vector3 place) =>
        [new(place, 1, 1, 1, 1, NearTexels, NearReach), new(FarCorner, FarSpacing, FarCount, FarCount, FarCount, FarTexels, FarEnd)];

    // What a floor facing up at place, where the finest level's one probe stands, reflects of the
    // light the chain's passes merge, each pass a dispatch of a thread a texel.
    private static Chained Passes(float[] cells, Level[] levels, Vector3 place)
    {
        var setup = new float[64 + 1 + 7];
        (setup[0], setup[1], setup[2], setup[3], setup[4], setup[5]) = (Corner.X, Corner.Y, Corner.Z, Cell, Size, Sky);
        (setup[8], setup[9]) = (levels[^1].End, levels.Length);
        var (texels, reaches) = (0, 0);
        for (int l = 0; l < levels.Length; l++)
        {
            var (level, at) = (levels[l], 16 + l * 16);
            float[] cascade = [level.Corner.X, level.Corner.Y, level.Corner.Z, level.Spacing, level.X, level.Y, level.Z, level.Texels, 0, level.End, texels, reaches, texels];
            cascade.CopyTo(setup, at);
            (texels, reaches) = (texels + level.TexelCount, reaches + level.ReachCount);
        }
        float[] point = [1, place.X, place.Y, place.Z, 0, 1, 0, Albedo];
        point.CopyTo(setup, 64);
        var (rays, reach, merged, marches, surfaces) = (new float[6 * texels], new float[reaches], new float[3 * texels], new float[2 * texels], new float[6]);
        void Pass(int pass, int level, int threads)
        {
            (setup[6], setup[7]) = (pass, level);
            CpuKernel.Run("probe_passes_cpu.slang", ((uint)(threads + 63) / 64, 1, 1), cells, setup, rays, reach, merged, marches, surfaces);
        }
        for (int l = 0; l < levels.Length; l++)
        {
            Pass(0, l, levels[l].TexelCount);
            Pass(1, l, levels[l].ReachCount);
        }
        Pass(2, levels.Length - 1, levels[^1].TexelCount);
        for (int l = levels.Length - 2; l >= 0; l--)
            Pass(3, l, levels[l].TexelCount);
        Pass(4, 0, 1);
        var (marched, steps, most) = (0, 0, 0);
        for (int k = 0; k < levels[0].TexelCount; k++)
            if (marches[k * 2] > 0)
                (marched, steps, most) = (marched + 1, steps + (int)marches[k * 2 + 1], Math.Max(most, (int)marches[k * 2 + 1]));
        return new(surfaces[3], marched, steps, most);
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
    public void A_Chain_Of_Three_Cascades_Reads_The_Open_Boxs_Floor_By_Its_Share_And_The_Lidded_Boxes_Under_A_Hundredth()
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
        // 0.25480 against 0.25111, 1.5 percent over. With the middle reaching 4 it read 0.23165,
        // 7.7 percent under.
        Assert.True(Math.Abs(open.Reflected - expected) <= 0.05 * expected, said);
        // 0.00000, and 0.00207, 0.81 percent of the open box's, where with the middle reaching 4
        // the sheeted box read 4.3 percent.
        Assert.True(lidded.Reflected < 0.01f * open.Reflected && sheeted.Reflected < 0.01f * open.Reflected, said);
    }
}
