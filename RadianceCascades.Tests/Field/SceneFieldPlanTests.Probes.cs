using Xunit;
using SceneFieldPlan = RadianceCascades.SceneFieldPlan<RadianceCascades.Tests.Field.TestMesh, int>;

namespace RadianceCascades.Tests.Field;

// The cascade of the field each cascade of probes stands in, its own but for the last, which stands
// in the far cascade where there is one.
public sealed partial class SceneFieldPlanTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void The_Last_Cascade_Of_Probes_Stands_In_The_Far_Cascade_And_Each_Other_In_Its_Own()
    {
        // Three cascades of probes over four counted cascades, the third standing in the field's
        // fifth, the far one.
        var plan = NewPlan(4, 0.5f, 1, farCell: 16);
        plan.CascadeOfProbes(0, 3).Should().Be(0);
        plan.CascadeOfProbes(1, 3).Should().Be(1);
        plan.CascadeOfProbes(2, 3).Should().Be(4, "the probes' last cascade stands in the far one");
        NewPlan(4, 0.5f, 1).CascadeOfProbes(1, 2).Should().Be(1, "with no far cascade each stands in its own");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Every_Cascade_Of_Probes_But_The_Last_Stands_In_Its_Own_However_Many_There_Are()
    {
        var far = NewPlan(4, 0.25f, 1, farCell: 8);
        var near = NewPlan(4, 0.25f, 1);
        for (int count = 1; count <= 5; count++)
            for (int probes = 0; probes < count; probes++)
            {
                far.CascadeOfProbes(probes, count).Should().Be(probes == count - 1 ? far.Cascades - 1 : probes,
                    $"cascade {probes} of {count} over a field with a far cascade");
                near.CascadeOfProbes(probes, count).Should().Be(probes, $"cascade {probes} of {count} over a field with none");
            }
        far.CellOf(far.CascadeOfProbes(2, 3)).Should().Be(far.FarCell, "the last cascade of probes reads the far cascade's cells");
    }
}
