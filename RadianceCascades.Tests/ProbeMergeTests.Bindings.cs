using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// What a binding gives the passes beyond the field, read by the passes on the CPU, a probe's
/// place moved off a surface by the binding's own rule, the light a ray's hit sends as the binding
/// shades it, and the sky along each way.
/// </summary>
public sealed partial class ProbeMergeTests
{
    [CpuKernelFact]
    public void Probes_Against_The_Floor_Moved_Off_It_By_The_Bindings_Rule_Read_The_Open_Boxs_Floor_By_Its_Share()
    {
        // The finest cascade's probes a unit apart, the lower row of them standing on the floor's
        // plane, read at a point of the floor where one of them stands, against the share of the
        // sky the probe takes where the rule moves it, a quarter of a unit over the floor.
        Level[] levels = [new(new(-1, 0, -1), 1, 3, 2, 3, NearTexels, NearReach), Pair(Floor)[1]];
        var moved = Passes(Field(OpenBox()), levels, Vector3.Zero, new Setting { Moved = true });
        var high = 8 - Margin;
        var expected = Albedo * 4 * CornerShare(4 / high, 4 / high) * Sky;
        var said = $"the open box's floor under probes moved off it {moved}, where the sky's share gives {expected:0.00000}";
        output.WriteLine(said);
        // 0.25396 against 0.25111, 1.1 percent over, as a probe set there reads it. At their grid
        // places, their rays beginning on the floor, the floor reads 0.00000.
        Assert.True(Math.Abs(moved.Reflected - expected) <= 0.05 * expected, said);
    }

    [CpuKernelFact]
    public void A_Wall_Lit_By_The_Bindings_Sun_Alone_Lights_A_Probe_Facing_It_Through_The_March_And_The_March_On()
    {
        // A wall 8 units a side and three cells thick, its face at x 3 looking toward -x, lit by a
        // sun 0.8 of the way straight onto it, under a black sky, so the wall's light is the
        // binding's shading alone, its albedo times the sun's irradiance times 0.8 over pi.
        var cells = Field(Cuboid(new(3, 0, -4), new(3 + Wall, 8, 4), 1));
        var sun = new Vector3(-0.8f, 0.6f, 0) * 3;
        var radiance = WallAlbedo * 3 * 0.8f / MathF.PI;
        var setting = new Setting { Normal = Vector3.UnitX, SkyLight = 0, Sun = sun };
        // A probe 3 units from the wall's middle, its rays reaching 8, so every ray toward the
        // wall meets it in its own march, and its rays reaching 2 with the far cascade's probes
        // outside the field, so every ray marches on itself past its reach to the wall.
        var at = new Vector3(0, 4, 0);
        var marched = Passes(cells, [new(at, 1, 1, 1, 1, NearTexels, 8), Pair(at)[1]], at, setting);
        var marchedOn = Passes(cells, [new(at, 1, 1, 1, 1, NearTexels, 2), new(new(100, 100, 100), FarSpacing, 2, 2, 2, FarTexels, FarEnd)], at, setting);
        // The wall's face seen from a point facing it straight on 3 units off, four corners of 4
        // by 4 at that distance.
        var expected = Albedo * 4 * CornerShare(4 / 3.0, 4 / 3.0) * radiance;
        var said = $"a probe 3 units from the sunlit wall through the march {marched}, and through the march-on {marchedOn}, where its share gives {expected:0.00000}";
        output.WriteLine(said);
        // 0.15742 both ways against 0.15751, 0.06 percent under, and 0.00000 both ways where a
        // hit gives its emission alone.
        Assert.True(Math.Abs(marched.Reflected - expected) <= 0.02 * expected && Math.Abs(marchedOn.Reflected - expected) <= 0.02 * expected, said);
        Assert.True(marchedOn.Marched > 0, said);
    }

    [CpuKernelFact]
    public void A_Floor_In_The_Open_Box_Under_A_Sky_That_Varies_With_The_Way_Reflects_Its_Integral_Over_The_Opening()
    {
        var varying = Passes(Field(OpenBox()), Pair(Floor), Floor, new Setting { VaryingSky = true });
        // The sky over the opening summed over a grid of 400 by 400 patches of it, each the sky
        // along the way to it times its solid angle and the cosine off the floor's normal, which a
        // patch of area a at height h and distance r gives as a h squared over r to the fourth.
        const int n = 400;
        var (high, patch, sum) = (8 - Floor.Y, 8.0 / n, 0.0);
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                var to = new Vector3((float)(-4 + (i + 0.5) * patch), high, (float)(-4 + (j + 0.5) * patch));
                var r = to.Length();
                sum += SkyAlong(to / r, Sky) * patch * patch * high * high / Math.Pow(r, 4);
            }
        var expected = Albedo / Math.PI * sum;
        var said = $"the open box's floor under the varying sky {varying}, where the sky over the opening gives {expected:0.00000}";
        output.WriteLine(said);
        // 0.47793 against 0.47889, 0.2 percent under, where the uniform sky of the same scale reads
        // 0.25396.
        Assert.True(Math.Abs(varying.Reflected - expected) <= 0.02 * expected, said);
    }

    [CpuKernelFact]
    public void A_Near_Texel_Takes_The_Far_Texels_Its_Solid_Angle_Covers_Where_The_Far_Cascade_Keeps_Twice_As_Many()
    {
        // An empty field under the varying sky, a probe of 8 by 8 texels reaching 4 handing its
        // rays to the far probes of 16 by 16, each near texel's merged light against its own ray
        // marched on to the far cascade's end, weighed by the texels' solid angles.
        var cells = Field([]);
        var at = new Vector3(0, 4, 0);
        Level[] levels = [new(at, 1, 1, 1, 1, 8, NearReach), Pair(at)[1]];
        var setting = new Setting { VaryingSky = true };
        var (merged, reference) = (Passes(cells, levels, at, setting), Passes(cells, levels, at, setting with { Reference = true }));
        var (off, whole) = (0.0, 0.0);
        for (int k = 0; k < 64; k++)
        {
            // A texel's octahedronShare, one over the cube of its middle's distance on the
            // octahedron, which its solid angle is in proportion to.
            float u = ((k % 8) + 0.5f) / 8 * 2 - 1, v = ((k / 8) + 0.5f) / 8 * 2 - 1;
            var (x, y) = MathF.Abs(u) + MathF.Abs(v) > 1 ? (1 - MathF.Abs(v), 1 - MathF.Abs(u)) : (u, v);
            var share = 1 / Math.Pow(new Vector3(x, y, 1 - MathF.Abs(u) - MathF.Abs(v)).Length(), 3);
            (off, whole) = (off + share * Math.Abs(merged.Light[k] - reference.Light[k]), whole + share * reference.Light[k]);
        }
        var said = $"8 near texels a side under 16 far lie {off / whole:0.0000} off their own rays marched on, the floor's reading {merged.Reflected:0.00000} against {reference.Reflected:0.00000}";
        output.WriteLine(said);
        // 0.0193, and the floor 1.0 percent under, where taking the far texel each near texel's
        // middle way falls in it lay 0.1046 off and the floor 5.0 percent over. What is left is
        // the sky's bend across a near texel, which the far texels' mean takes and its one ray
        // does not.
        Assert.True(off / whole <= 0.03, said);
    }

    [CpuKernelFact]
    public void A_Wall_Lit_By_The_Bindings_Sun_A_Little_Past_The_Near_Rays_Reach_Lights_A_Probe_By_Its_Share()
    {
        // The sunlit wall 3 units from a probe whose rays reach 4, so a ray toward the wall's outer
        // parts reaches its end 0.4 units or less in front of the wall, where the far probes about
        // that point stand up to a spacing back and their own rays pass the wall's edge.
        var cells = Field(Cuboid(new(3, 0, -4), new(3 + Wall, 8, 4), 1));
        var sun = new Vector3(-0.8f, 0.6f, 0) * 3;
        var radiance = WallAlbedo * 3 * 0.8f / MathF.PI;
        var at = new Vector3(0, 4, 0);
        var reading = Passes(cells, Pair(at), at, new Setting { Normal = Vector3.UnitX, SkyLight = 0, Sun = sun });
        var expected = Albedo * 4 * CornerShare(4 / 3.0, 4 / 3.0) * radiance;
        var said = $"a probe 3 units from the sunlit wall reaching 4 {reading}, where its share gives {expected:0.00000}";
        output.WriteLine(said);
        // 0.15742 against 0.15751, 0.06 percent under, 34 of the 256 texels marching on past their
        // end to open space in 4.2 steps on the mean and 8 at most, where handed on at their end
        // they read 0.09885, 37 percent under.
        Assert.True(Math.Abs(reading.Reflected - expected) <= 0.10 * expected, said);
    }
}
