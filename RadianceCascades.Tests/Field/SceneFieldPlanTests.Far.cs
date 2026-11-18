using System.Numerics;
using Xunit;
using SceneFieldPlan = RadianceCascades.SceneFieldPlan<RadianceCascades.Tests.Field.TestMesh, int>;

namespace RadianceCascades.Tests.Field;

// The far cascade after the counted ones, its cells as wide as its reach takes, built under the
// budgets as the others are.
public sealed partial class SceneFieldPlanTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void A_Far_Cascade_Reaches_Its_Distance_Ahead_In_Cells_Of_Its_Own_And_Is_Left_Out_Of_The_Count()
    {
        // Four cascades of half a unit reaching 256 across, and a far cascade to a view of 384
        // units.
        var cell = SceneFieldPlan.FarCellFor(384, 4, 0.5f);
        var plan = NewPlan(4, 0.5f, 1, farCell: cell);
        plan.Counted.Should().Be(4);
        plan.Cascades.Should().Be(5, "the far cascade comes after the counted ones");
        plan.HasFar.Should().BeTrue();
        plan.CellOf(3).Should().Be(4, "the counted cascades double as before");
        plan.CellOf(4).Should().Be(cell);
        cell.Should().BeGreaterThan(plan.CellOf(3) * 2 - 1e-4f, "a far cascade is at least twice as coarse as the last");

        // Each of its sides lies 28 to 36 of its cells from the eye whichever way the eye looks,
        // its cells as fine as when its side ahead reached the 384 asked.
        var eye = new Vector3(3, 70, -5);
        var origin = plan.OriginAround(4, eye);
        var width = SceneFieldPlan.Resolution * cell;
        foreach (var side in new[] { eye.X - origin.X, eye.Z - origin.Z, origin.X + width - eye.X, origin.Z + width - eye.Z })
            (side / cell).Should().BeInRange(28, 36);
        (0.85f * width).Should().BeGreaterThanOrEqualTo(384, "the cells are sized for the reach at 0.85 of the width at the least");

        // A reach no further than twice the last's cells reach takes twice its cells, and a reach
        // read from a far plane a little off gives the same cells.
        SceneFieldPlan.FarCellFor(100, 4, 0.5f).Should().Be(8);
        SceneFieldPlan.FarCellFor(384.003f, 4, 0.5f).Should().Be(cell);
        SceneFieldPlan.FarCellFor(0, 4, 0.5f).Should().Be(0, "none was asked for");
        SceneFieldPlan.FarCellFor(float.PositiveInfinity, 4, 0.5f).Should().Be(0, "a camera with no far plane gives no reach to fit");
        NewPlan(4, 0.5f, 1).Cascades.Should().Be(4, "with no far cascade the count is the field's");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void The_Far_Cascade_Is_Built_After_The_Counted_Ones_Under_The_Budget_With_The_Meshes_Past_Them()
    {
        // Two cascades of a quarter reaching 32 across, and a far one for 200 units, which reaches
        // 103 to 133 around the eye, with the 500 sections and a row of them out to 90 units ahead,
        // under a budget of one cascade a frame and 65,536 triangles.
        var plan = NewPlan(2, 0.25f, 1, triangleBudget: 65_536, triangles: _ => SectionTriangles, farCell: SceneFieldPlan.FarCellFor(200, 2, 0.25f));
        var eye = new Vector3(0, 2, 0);
        var drawn = AllSections();
        drawn.AddRange(Enumerable.Range(1, 9).Select(i =>
            (new SceneFieldPlan.Instance(2, Section, Matrix4x4.CreateTranslation(0, 0, -10 * i), false), false)));
        var builds = new List<SceneFieldPlan.Build>();
        for (int frame = 1; frame < 200; frame++)
        {
            plan.Update(eye, drawn);
            // From the frame the meshes settle in, the builds before it being of no mesh.
            if (frame >= SceneFieldPlan.SettleFrames) builds.AddRange(plan.Builds);
            plan.Spent.Should().BeLessThanOrEqualTo(65_536, "the far cascade's build splats under the budget as the others' do");
            if (frame > SceneFieldPlan.SettleFrames && plan.Builds.Count == 0 && plan.Waiting == 0) break;
        }

        var far = builds.Where(build => build.Cascade == 2).ToList();
        far.Should().NotBeEmpty("the far cascade is built");
        far[0].Cell.Should().Be(plan.CellOf(2));
        builds.FindIndex(build => build.Cascade == 2).Should().BeGreaterThan(builds.FindLastIndex(build => build.Cascade == 1 && build.First),
            "the cascades are built finest first");
        far.Count.Should().BeGreaterThan(1, "the far cascade's 509 meshes are splatted over several frames");
        far.SelectMany(build => build.Instances).Should().Contain(instance => instance.World.Translation.Z <= -80,
            "the far cascade holds the meshes past the counted ones' reach");
        plan.BuiltOrigin(2).Should().NotBeNull();
    }
}
