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
            triangles: positions => positions.Length / 3,
            forms: new FieldForms { Ahead = false, StampsArrivals = false, WholeBuilds = false, RepaintWaits = false });

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

    [Fact]
    public void The_Readmes_Binding_Block_Is_The_Hosts_Line_For_Line()
    {
        // The first C# block of the README's section and the lines of this file between the two
        // markers above, their common indent taken off.
        var readme = File.ReadAllText(Path.Combine(Repository.Root, "README.md")).ReplaceLineEndings("\n");
        var at = readme.IndexOf("\n## Binding it in your engine\n", StringComparison.Ordinal);
        Assert.True(at >= 0, "the README has a section Binding it in your engine");
        var next = readme.IndexOf("\n## ", at + 1, StringComparison.Ordinal);
        var section = next < 0 ? readme[at..] : readme[at..next];
        var open = section.IndexOf("```csharp\n", StringComparison.Ordinal);
        Assert.True(open >= 0, "the section holds a C# block");
        open += "```csharp\n".Length;
        var block = section[open..section.IndexOf("```\n", open, StringComparison.Ordinal)].TrimEnd('\n').Split('\n');

        var source = File.ReadAllText(Path.Combine(Repository.Root, "RadianceCascades.Tests", "BindingTests.cs")).ReplaceLineEndings("\n").Split('\n');
        var from = Array.FindIndex(source, line => line.Trim() == "// The README's block from here");
        var to = Array.FindIndex(source, line => line.Trim() == "// The end marker.");
        var hosted = source[(from + 1)..to];
        var indent = hosted.Where(line => line.Trim().Length > 0).Min(line => line.Length - line.TrimStart().Length);
        hosted = [.. hosted.Select(line => line.Length >= indent ? line[indent..] : line.TrimStart())];

        Assert.Equal(hosted, block);
    }
}
