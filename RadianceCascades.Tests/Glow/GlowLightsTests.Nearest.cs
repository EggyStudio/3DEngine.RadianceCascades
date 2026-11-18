using System.Numerics;
using System.Runtime.InteropServices;
using RadianceCascades.Tests.Field;
using Xunit;
using SceneFieldPlan = RadianceCascades.SceneFieldPlan<RadianceCascades.Tests.Field.TestMesh, int>;

namespace RadianceCascades.Tests.Glow;

public sealed partial class GlowLightsTests
{
    /// <summary>
    /// The glow lights with each of their forms set the other way from its default, the lights
    /// nearest the eye written first, each reaching as far as a surface takes a thousandth of its
    /// light, and the lights alone in the block with no head ahead of them.
    /// </summary>
    public sealed class Nearest
    {
        private static readonly GlowForms Forms = new() { FaintThousandth = true, NearestFirst = true, LightsAlone = true };

        [Fact]
        [Trait("Category", "Unit")]
        public void The_Light_Nearest_The_Eye_Is_Written_First_And_Reaches_Where_A_Surface_Takes_A_Thousandth_Of_Its_Light()
        {
            // A faint block two meters from the eye and a bright one ten off, as a small lit sign
            // before a street lantern. Nearest first, the sign takes the first place, at the
            // block's first byte, and the lantern the second, though the lantern sends more about
            // the eye. A block a meter wide emits from six faces of half sides 0.5, 4 times 1.5 of
            // surface at a share of 1, so it reaches the root of 6 over 4 thousandths, 38.73.
            var vertices = new TestMesh([new Vector3(-0.5f), new Vector3(0.5f)]);
            var sign = (new SceneFieldPlan.Instance(1, vertices, Matrix4x4.CreateTranslation(2, 0.5f, 0), false, Vector3.One, new Vector3(0.04f)), false);
            var lantern = (new SceneFieldPlan.Instance(2, vertices, Matrix4x4.CreateTranslation(0, 0.5f, 10), false, Vector3.One, new Vector3(40, 30, 20)), false);
            var (bytes, places) = (new byte[GlowLights.Most * GlowLights.Bytes], new GlowPlaces());

            GlowLights.Write(bytes, [lantern, sign], Vector3.Zero, 0.25f, mesh => FieldBox.Of(mesh.Positions), _ => 1, Forms, places).Should().Be(2);

            places.Count.Should().Be(2, "the two lights take the first two places, which the lights alone do not hold");
            var floats = MemoryMarshal.Cast<byte, float>(bytes.AsSpan()).ToArray();
            new Vector3(floats.AsSpan(0, 3)).Should().Be(new Vector3(2, 0.5f, 0), "the sign is nearest the eye and is written first, with no head ahead of it");
            floats[3].Should().BeApproximately(MathF.Sqrt(6 / (4 * 0.001f)), 1e-3f, "a block of six faces reaches where it lights a surface by a thousandth of its own light");
            floats[16..20].Should().Equal(0.04f, 0.04f, 0.04f, 1);
            new Vector3(floats.AsSpan(GlowLights.Bytes / 4, 3)).Should().Be(new Vector3(0, 0.5f, 10), "the lantern is farther from the eye");
        }
    }
}
