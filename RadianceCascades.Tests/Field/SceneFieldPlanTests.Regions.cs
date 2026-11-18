using System.Numerics;
using Xunit;
using SceneFieldPlan = RadianceCascades.SceneFieldPlan<RadianceCascades.Tests.Field.TestMesh, int>;

namespace RadianceCascades.Tests.Field;

// A still mesh that comes or goes within a built cascade, whose cells within its band alone are
// built again, from the still meshes near them.
public sealed partial class SceneFieldPlanTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void A_Mesh_That_Settles_In_A_Built_Cascade_Builds_The_Cells_Within_Its_Band_From_The_Meshes_Near_Them_Alone()
    {
        // One cascade of quarter-unit cells, so its band is a unit, two cubes two units wide six
        // apart, built, and a third settling beside the first.
        var plan = NewPlan(1, 0.25f, 1);
        var eye = new Vector3(0.1f, 0.1f, 0.1f);
        var (near, far, beside) = (At(new Vector3(-3, 0, 0)), At(new Vector3(3, 0, 0)), At(new Vector3(-3, 0, 2.5f)));
        for (int frame = 0; frame <= SceneFieldPlan.SettleFrames + 1; frame++) plan.Update(eye, [near, far]);
        plan.Whole.Should().BeTrue("both cubes are built");

        for (int frame = 1; frame <= SceneFieldPlan.SettleFrames; frame++) plan.Update(eye, [near, far, beside]);
        var build = plan.Builds.Should().ContainSingle("the third settled").Which;
        build.Cells.Should().NotBeNull("the cascade is built where the third came alone");
        build.Instances.Select(i => i.World.Translation).Should().BeEquivalentTo([new Vector3(-3, 0, 0), new Vector3(-3, 0, 2.5f)],
            "the cubes within the band of its cells and a cell around them, and not the one six units off");
        var (origin, cells) = (plan.BuiltOrigin(0)!.Value, build.Cells!.Value);
        (origin.X + cells.X0 * 0.25f).Should().BeInRange(-5.25f, -5, "its cells reach the band past the third's side");
        (origin.Z + (cells.Z1 + 1) * 0.25f).Should().BeInRange(4.5f, 4.75f);
        (origin.X + (cells.X1 + 1) * 0.25f).Should().BeLessThan(1, "the far cube's band, which begins a unit past its side, is not among them");
        build.Changed.Should().Be(new FieldBox(new Vector3(-4, -1, 1.5f), new Vector3(-2, 1, 3.5f)));
        plan.Whole.Should().BeTrue("the region's build holds the third");
        plan.Update(eye, [near, far, beside]);
        plan.Builds.Should().BeEmpty();
    }
}
