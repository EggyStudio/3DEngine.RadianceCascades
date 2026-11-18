using System.Numerics;
using Xunit;
using SceneFieldPlan = RadianceCascades.SceneFieldPlan<RadianceCascades.Tests.Field.TestMesh, int>;

namespace RadianceCascades.Tests.Field;

// What the engine's own of a draw does to the plan, and the budgets at the largest a field takes.
public sealed partial class SceneFieldPlanTests
{
    [Fact]
    [Trait("Category", "Unit")]
    public void A_Still_Mesh_Whose_Tag_Changes_Is_That_Mesh_Painted_Again_And_Held_With_Its_New_Tag()
    {
        var plan = NewPlan(1, 0.25f, 1);
        var eye = new Vector3(0, 2, 6);
        var first = new SceneFieldPlan.Instance(1, Cube, Matrix4x4.Identity, false, Tag: 1);
        for (int frame = 0; frame < SceneFieldPlan.SettleFrames + 2; frame++) plan.Update(eye, [(first, false)]);
        Assert.True(plan.IsStill(first), "the mesh settled with its first tag");

        var retagged = first with { Tag = 2 };
        plan.Update(eye, [(retagged, false)]);
        Assert.True(plan.IsStill(retagged), "a draw whose tag changed is the mesh painted again where it stands, still at once with its new tag");
        Assert.False(plan.IsStill(first), "the draw with the old tag left the field");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Tag_Made_Again_Each_Frame_Equal_To_The_Last_Leaves_The_Mesh_Still()
    {
        var plan = new SceneFieldPlan<TestMesh, Look>(1, 0.25f, 1, mesh => FieldBox.Of(mesh.Positions), mesh => mesh.Positions.Length / 3, Around);
        var eye = new Vector3(0, 2, 6);
        SceneFieldPlan<TestMesh, Look>.Instance Drawn() => new(1, Cube, Matrix4x4.Identity, false, Tag: new Look(7));
        for (int frame = 0; frame < SceneFieldPlan.SettleFrames + 2; frame++) plan.Update(eye, [(Drawn(), false)]);
        Assert.True(plan.IsStill(Drawn()), "a tag equal to the last one's is the same draw");
        var builds = 0;
        for (int frame = 0; frame < 20; frame++)
        {
            plan.Update(eye, [(Drawn(), false)]);
            builds += plan.Builds.Count;
        }
        Assert.True(builds == 0, $"the mesh stood still through twenty frames of fresh tags, built {builds} times");
    }

    // A tag of the engine's own, made again each time a mesh is drawn, compared by its value.
    private sealed record Look(int Texture);

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Budget_Of_Every_Triangle_Is_Set_Raised_And_Lifted_After_A_Cut_With_No_Overflow()
    {
        var plan = NewPlan(2, 0.25f, 1, triangleBudget: int.MaxValue);
        plan.SetBudgets(2, int.MaxValue);
        Assert.Equal(int.MaxValue, plan.TriangleBudget);
        plan.Update(new Vector3(0, 2, 0), [At(Vector3.Zero)]);
        plan.Update(new Vector3(400, 2, 0), [At(Vector3.Zero)]);
        plan.Raise(roomMs: 8, fieldMs: 1);
        Assert.True(plan.TriangleBudget > 0, "the budget stays a count of triangles");
    }
}
