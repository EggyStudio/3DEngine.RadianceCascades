using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The README's section Binding it in your engine run as a host with no engine, so the binding a
/// stranger copies is the one the build proves.
/// </summary>
public sealed class BindingTests
{
    [Fact]
    public void The_Readmes_Binding_Builds_A_Floor_Into_The_Field_And_Lays_Out_Its_Triangles()
    {
        // The README's block from here
        // This engine knows a mesh's vertices by their positions, which the plan tells apart by
        // reference, and its own of a draw is an int it never sets.
        var plan = new SceneFieldPlan<Vector3[], int>(cascades: 4, cellSize: 0.25f, budget: 1,
            bounds: positions => FieldBox.Of(positions),
            triangles: positions => positions.Length / 3);

        // A floor eight meters a side, two triangles, its corners in its own space.
        Vector3[] floor = [new(-4, 0, -4), new(4, 0, -4), new(4, 0, 4), new(-4, 0, -4), new(4, 0, 4), new(-4, 0, 4)];
        uint[] corners = [0, 1, 2, 3, 4, 5];
        var drawn = new List<(SceneFieldPlan<Vector3[], int>.Instance Instance, bool Skinned)>
        {
            (new(Mesh: 1, Vertices: floor, World: Matrix4x4.Identity, DoubleSided: true, Color: new Vector3(0.8f)), false),
        };

        // Each frame the engine hands the plan the eye and what it drew, and lays out the
        // triangles of each build's meshes for its splat.
        var triangles = new uint[2 * FieldTriangles.Uints];
        for (int frame = 0; frame < 32; frame++)
        {
            plan.Update(new Vector3(0, 1.7f, 0), drawn);
            foreach (var build in plan.Builds)
                foreach (var instance in build.Instances)
                    FieldTriangles.Write(triangles, instance.Vertices, corners, []);
        }
        // The end marker.

        Assert.Equal(1, plan.StillCount);
        Assert.True(plan.Built, "the floor is built into every cascade it comes within");
        Assert.Equal(BitConverter.SingleToUInt32Bits(-4f), triangles[0]);
        Assert.Equal(FieldTriangles.White | 1u << 24, triangles[3]);
        Assert.True(triangles[11] == FieldTriangles.White, "the edge back from the third corner is the diagonal both triangles share");
    }
}
