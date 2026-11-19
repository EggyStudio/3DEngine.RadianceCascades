using System.Numerics;
using Xunit;
using Xunit.Abstractions;

namespace RadianceCascades.Tests;

/// <summary>
/// The light a probe a quarter of a unit over a floor gathers from a near cascade of probes merged
/// with a far one, traced through the field of rooms built and read on the CPU from the library's
/// Slang, a box open to a uniform sky, the same box closed by a solid lid and by a sheet thinner
/// than a cell, and two rooms parted by a wall that lets light through.
/// </summary>
public sealed class ProbeMergeTests(ITestOutputHelper output)
{
    // One cascade of the field, 64 cells of a quarter unit a side, from -8 to 8 across and from -4
    // to 12 up.
    private const int Size = 64;
    private const float Cell = 0.25f;
    private static readonly Vector3 Corner = new(-8, -4, -8);

    private const float Sky = 2f;
    private const float Albedo = 0.5f;

    // The near cascade's probe, its 16 by 16 texels reaching 4 units, and the far cascade's probes,
    // 8 along each axis 2 units apart from (-7, -3, -7), 16 by 16 texels each, whose rays run from
    // the probe itself past the field's edge.
    private const int NearTexels = 16;
    private const float NearReach = 4f;
    private const int FarTexels = 16;
    private const float FarSpacing = 2f;
    private const int FarCount = 8;
    private static readonly Vector3 FarCorner = new(-7, -3, -7);
    private const float FarEnd = 32f;
    private const int ReachTexels = 16;

    // The box's walls and lid stand three cells thick and its floor six, 8 units inside each way,
    // the lid at 8 over the walls' tops. The far probes, at odd places, stand a cell outside the
    // walls and the lid, and inside the floor.
    private const float Wall = 0.75f;
    private static readonly Vector3 Floor = new(0, 0.25f, 0);

    private readonly record struct Triangle(Vector3 A, Vector3 B, Vector3 C, int Instance, bool DoubleSided, float Through);

    private readonly record struct Reading(float Reflected, float Trilinear, int Marched, int Steps, int Most)
    {
        public float SelfShare => Marched / (float)(NearTexels * NearTexels);
        public float MeanSteps => Marched == 0 ? 0 : Steps / (float)Marched;
        public override string ToString() =>
            $"reflects {Reflected:0.00000} by the rule and {Trilinear:0.00000} by trilinear weights alone, {SelfShare:0.0000} of the texels marching on themselves in {MeanSteps:0.0} steps on the mean and {Most} at most";
    }

    // A box from low to high, its twelve triangles facing out.
    private static IEnumerable<Triangle> Cuboid(Vector3 low, Vector3 high, int instance, float through = 0)
    {
        var middle = (low + high) * 0.5f;
        var half = (high - low) * 0.5f;
        for (int axis = 0; axis < 3; axis++)
            foreach (var sign in new[] { -1f, 1f })
            {
                Vector3 n = default, u = default, v = default;
                n[axis] = sign; u[(axis + 1) % 3] = 1; v[(axis + 2) % 3] = 1;
                if (sign < 0) (u, v) = (v, u);
                var (a, b, c, d) = (middle + (n - u - v) * half, middle + (n + u - v) * half, middle + (n + u + v) * half, middle + (n - u + v) * half);
                yield return new(a, b, c, instance, false, through);
                yield return new(a, c, d, instance, false, through);
            }
    }

    // A double-sided sheet across y at height, half wide either way along x and z.
    private static IEnumerable<Triangle> Sheet(float height, float half, int instance)
    {
        yield return new(new(-half, height, -half), new(half, height, -half), new(half, height, half), instance, true, 0);
        yield return new(new(-half, height, -half), new(half, height, half), new(-half, height, half), instance, true, 0);
    }

    // The box open at its top, its floor from -1.5 to 0 and its walls up to 8.
    private static IEnumerable<Triangle> OpenBox()
    {
        const float o = 4 + Wall;
        return Cuboid(new(-o, -1.5f, -o), new(o, 0, o), 1)
            .Concat(Cuboid(new(4, 0, -o), new(o, 8, o), 2))
            .Concat(Cuboid(new(-o, 0, -o), new(-4, 8, o), 3))
            .Concat(Cuboid(new(-4, 0, 4), new(4, 8, o), 4))
            .Concat(Cuboid(new(-4, 0, -o), new(4, 8, -4), 5));
    }

    // Two rooms 8 units high and 8 across along z, from -6 to 6 along x, the dark one closed but
    // for the side it shares with the lit one, which is open to the sky at its far side at 6.
    // Parted, a wall half a unit thick stands between them at 0, letting through half the light
    // that passes a unit of its depth.
    private static IEnumerable<Triangle> Rooms(bool parted)
    {
        const float o = 4 + Wall;
        var rooms = Cuboid(new(-6 - Wall, -1.5f, -o), new(6, 0, o), 1)
            .Concat(Cuboid(new(-6 - Wall, 8, -o), new(6, 8 + Wall, o), 2))
            .Concat(Cuboid(new(-6 - Wall, 0, -o), new(-6, 8, o), 3))
            .Concat(Cuboid(new(-6, 0, 4), new(6, 8, o), 4))
            .Concat(Cuboid(new(-6, 0, -o), new(6, 8, -4), 5));
        return parted ? rooms.Concat(Cuboid(new(-0.25f, 0, -4), new(0.25f, 8, 4), 6, WallThrough)) : rooms;
    }

    private const float WallThrough = 0.5f;
    private const float WallDepth = 0.5f;

    // The cells the triangles resolve to, five floats each.
    private static float[] Field(IEnumerable<Triangle> triangles)
    {
        var all = triangles.ToList();
        var floats = new List<float> { all.Count };
        foreach (var t in all)
            floats.AddRange([t.A.X, t.A.Y, t.A.Z, t.B.X, t.B.Y, t.B.Z, t.C.X, t.C.Y, t.C.Z, t.Instance, t.DoubleSided ? 1 : 0, t.Through]);
        var cells = new float[5 * Size * Size * Size];
        float[] setup = [Corner.X, Corner.Y, Corner.Z, Cell, Size];
        CpuKernel.Run("probe_field_cpu.slang", (Size / 4, Size / 4, Size / 4), setup, floats.ToArray(), cells);
        return cells;
    }

    // What a floor facing up at place reflects of the light the probe there gathers.
    private static Reading Gather(float[] cells, Vector3 place)
    {
        float[] setup =
        [
            Corner.X, Corner.Y, Corner.Z, Cell, Size, Sky,
            place.X, place.Y, place.Z, 1, 1, 1, 1, NearTexels, 0, NearReach,
            FarCorner.X, FarCorner.Y, FarCorner.Z, FarSpacing, FarCount, FarCount, FarCount, FarTexels, 0, FarEnd,
            0, 1, 0, Albedo,
        ];
        const int probes = FarCount * FarCount * FarCount;
        var (far, reach) = (new float[probes * FarTexels * FarTexels * 3], new float[probes * ReachTexels * ReachTexels]);
        var (near, result) = (new float[NearTexels * NearTexels * 8], new float[16]);
        CpuKernel.Run("probe_gather_cpu.slang", (1, 1, 1), cells, setup, far, reach, near, result);
        return new(result[3], result[9], (int)result[12], (int)result[13], (int)result[14]);
    }

    private static readonly Lazy<Reading> Open = new(() => Gather(Field(OpenBox()), Floor));

    // The share of its dome a point sees of a rectangle parallel over it, one corner straight over
    // the point, x and y its sides over its height.
    private static double CornerShare(double x, double y) =>
        (x / Math.Sqrt(1 + x * x) * Math.Atan(y / Math.Sqrt(1 + x * x)) + y / Math.Sqrt(1 + y * y) * Math.Atan(x / Math.Sqrt(1 + y * y))) / (2 * Math.PI);

    [CpuKernelFact]
    public void A_Floor_In_A_Box_Open_To_The_Sky_Reflects_The_Skys_Share_Of_Its_Dome_Through_The_Opening()
    {
        var open = Open.Value;
        // The square opening seen from the probe, a quarter of a unit over the floor's middle, is
        // four corners of 4 by 4 at 7.75 over it, a share of 0.2511 where the floor's middle has
        // 0.239.
        var high = 8 - Floor.Y;
        var share = 4 * CornerShare(4 / high, 4 / high);
        var expected = Albedo * share * Sky;
        var said = $"the open box's floor {open}, where the sky's share of {share:0.0000} gives {expected:0.00000}";
        output.WriteLine(said);
        // 0.25396 against 0.25111, 1.1 percent over.
        Assert.True(Math.Abs(open.Reflected - expected) <= 0.15 * expected, said);
        // Every far probe at its trilinear weight, those outside the walls lending the sky along
        // ways that meet the walls, reads 0.31194, 24 percent over.
        Assert.True(open.Trilinear >= 1.2 * expected, said);
        // 0 of the 256 texels marched on themselves, every end of a near ray seen by a far probe
        // about it, so the march took no steps.
        Assert.True(open.Marched <= 4 && open.Most <= 16, said);
    }

    [CpuKernelFact]
    public void A_Floor_In_The_Box_Under_A_Solid_Lid_Reflects_Under_A_Hundredth_Of_The_Open_Boxs()
    {
        const float o = 4 + Wall;
        var lidded = Gather(Field(OpenBox().Concat(Cuboid(new(-o, 8, -o), new(o, 8 + Wall, o), 6))), Floor);
        var said = $"the lidded box's floor {lidded}, the open box's reflecting {Open.Value.Reflected:0.00000}";
        output.WriteLine(said);
        // 0.00000 against the open box's 0.25396.
        Assert.True(lidded.Reflected < 0.01f * Open.Value.Reflected, said);
        // By trilinear weights alone 0.05798, 23 percent of the open box's.
        Assert.True(lidded.Trilinear > 0.2f * Open.Value.Reflected, said);
        // 0 of the 256 texels marched on themselves.
        Assert.True(lidded.Marched <= 4 && lidded.Most <= 16, said);
    }

    [CpuKernelFact]
    public void A_Floor_In_The_Box_Under_A_Sheet_Thinner_Than_A_Cell_Reflects_Under_A_Hundredth_Of_The_Open_Boxs()
    {
        var sheeted = Gather(Field(OpenBox().Concat(Sheet(8, 4 + Wall, 6))), Floor);
        var said = $"the sheeted box's floor {sheeted}, the open box's reflecting {Open.Value.Reflected:0.00000}";
        output.WriteLine(said);
        // 0.00177, 0.70 percent of the open box's, the light a few far probes' rays bring past the
        // sheet.
        Assert.True(sheeted.Reflected < 0.01f * Open.Value.Reflected, said);
        // By trilinear weights alone 0.05975, 24 percent of the open box's.
        Assert.True(sheeted.Trilinear > 0.2f * Open.Value.Reflected, said);
        // 0 of the 256 texels marched on themselves.
        Assert.True(sheeted.Marched <= 4 && sheeted.Most <= 16, said);
    }

    [CpuKernelFact]
    public void A_Floor_Behind_A_Wall_That_Lets_Light_Through_Reflects_The_Walls_Share_Of_What_It_Does_Without_The_Wall()
    {
        // The dark room's floor 4.25 units from the wall, so its near rays end short of it and the
        // far probes' rays cross it.
        var place = new Vector3(-4.5f, 0.25f, 0);
        var parted = Gather(Field(Rooms(parted: true)), place);
        var whole = Gather(Field(Rooms(parted: false)), place);
        // Straight through the wall the share is half to the power of its depth, 0.707.
        var passes = MathF.Pow(WallThrough, WallDepth);
        var said = $"the dark room's floor {parted}, and with the wall taken away {whole}, the wall passing {passes:0.0000} straight through";
        output.WriteLine(said);
        // 0.02369 against 0.03509 without the wall, a share of 0.675, 4.5 percent under 0.707,
        // since a ray that crosses the wall aslant crosses more of its depth.
        Assert.True(parted.Reflected > 0 && MathF.Abs(parted.Reflected - passes * whole.Reflected) <= 0.15f * passes * whole.Reflected, said);
        // 0 of the 256 texels marched on themselves either way.
        Assert.True(parted.Marched <= 4 && parted.Most <= 16 && whole.Marched <= 4 && whole.Most <= 16, said);
    }
}
