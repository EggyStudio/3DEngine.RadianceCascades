using System.Numerics;
using Xunit;
using SceneFieldPlan = RadianceCascades.SceneFieldPlan<RadianceCascades.Tests.Field.TestMesh, int>;

namespace RadianceCascades.Tests.Field;

/// <summary>
/// The scene's distance field planned on the CPU, where its cascades lie and what each frame builds
/// and stamps, with the open edges of a mesh's triangles and the parts they are cut into.
/// </summary>
public sealed partial class SceneFieldPlanTests
{
    private static readonly TestMesh Cube = GenMeshCubeVertices(2);

    // A cube's eight corners, all the plan's callbacks read of a mesh.
    private static TestMesh GenMeshCubeVertices(float size) =>
        new([.. Enumerable.Range(0, 8).Select(c => new Vector3((c & 1) == 0 ? -size / 2 : size / 2, (c & 2) == 0 ? -size / 2 : size / 2,
            (c & 4) == 0 ? -size / 2 : size / 2))]);

    private static (SceneFieldPlan.Instance, bool) At(Vector3 position, TestMesh? vertices = null) =>
        (new SceneFieldPlan.Instance(1, vertices ?? Cube, Matrix4x4.CreateTranslation(position), false), false);

    // A plan that reads a mesh as the box around its positions and a triangle for every three of
    // them, the counts the tests' numbers were worked out with, unless a test gives its own.
    private static SceneFieldPlan NewPlan(int cascades, float cellSize, int budget, Func<SceneFieldPlan.Instance, Vector3>? tint = null,
        int triangleBudget = SceneFieldPlan.DefaultTriangleBudget, Func<TestMesh, int>? triangles = null, float farCell = 0) =>
        new(cascades, cellSize, budget, mesh => FieldBox.Of(mesh.Positions), triangles ?? (mesh => mesh.Positions.Length / 3), tint: tint,
            triangleBudget: triangleBudget, farCell: farCell);

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Cascade_Lies_Around_The_Eye_And_Moves_Only_Past_Its_Grid()
    {
        var plan = NewPlan(2, 0.25f, 2);
        plan.Update(new Vector3(0.1f, 2, 6), []);

        plan.BuiltOrigin(0).Should().Be(new Vector3(-8, -6, -2), "the corner is 32 cells of 0.25 short of the 2-unit grid point nearest the eye");
        plan.BuiltOrigin(1).Should().Be(new Vector3(-16, -12, -8), "the second cascade's cells and grid are twice as wide");
        plan.Builds.Select(build => build.Cascade).Should().Equal(0, 1);

        plan.Update(new Vector3(0.9f, 2, 6), []);
        plan.Builds.Should().BeEmpty("the grid point nearest the eye is the same");
        plan.Update(new Vector3(1.1f, 2, 6), []);
        plan.Builds.Select(build => build.Cascade).Should().Equal([0], "the first cascade's next grid point is nearer, the second's 4-unit one not");
        plan.BuiltOrigin(0).Should().Be(new Vector3(-6, -6, -2));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Mesh_Drawn_The_Same_For_Eight_Frames_Is_Built_And_Stands_In_For_Nothing_Until_Then()
    {
        var plan = NewPlan(1, 0.25f, 1);
        var eye = new Vector3(0, 2, 6);
        for (int frame = 1; frame < SceneFieldPlan.SettleFrames; frame++)
        {
            plan.Update(eye, [At(Vector3.Zero)]);
            plan.StillCount.Should().Be(0);
            plan.Shapes.Should().BeEmpty("a mesh that came and has not moved is not stamped as its box, which stands in for it badly where it is larger");
        }
        plan.Update(eye, [At(Vector3.Zero)]);
        plan.StillCount.Should().Be(1);
        plan.Builds.Should().ContainSingle().Which.Instances.Should().ContainSingle("the cascade it is in is built again with it");
        plan.Shapes.Should().BeEmpty("the build holds it");

        var moved = Vector3.UnitX;
        plan.Update(eye, [At(moved)]);
        plan.StillCount.Should().Be(0, "a mesh drawn somewhere else is another, which has not settled");
        plan.Builds.Should().ContainSingle().Which.Instances.Should().BeEmpty("the cascade it left is built again without it");
        plan.Shapes.Should().ContainSingle("a mesh that moved is stamped as its box").Which.ToOwn.Translation.Should().Be(-moved);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Still_Mesh_Waiting_For_Its_Cascades_Is_Stamped_Only_Where_It_Moved_Before_It_Settled()
    {
        // Two cascades and a budget of one mesh's triangles a frame, so a mesh that settles waits a
        // frame for the second cascade's region to be built with it. A large scene's meshes wait
        // hundreds of frames at the default budget, and the box of a building's outer walls would
        // fill the building meanwhile.
        var plan = NewPlan(2, 0.25f, 1, triangleBudget: 12, triangles: _ => 12);
        var eye = new Vector3(0.1f, 0.1f, 0.1f);
        plan.Update(eye, []);
        plan.Update(eye, []);
        for (int frame = 1; frame <= SceneFieldPlan.SettleFrames; frame++) plan.Update(eye, [At(Vector3.Zero)]);
        (plan.StillCount, plan.Builds.Single().Cascade).Should().Be((1, 0), "it settled and the first cascade is built with it");
        plan.Shapes.Should().BeEmpty("a mesh that came and never moved stands in for nothing while the second waits");

        // Moved, it is stamped until it settles again and the second cascade is built with it.
        var frames = 0;
        do
        {
            plan.Update(eye, [At(Vector3.UnitX)]);
            frames++;
            if (plan.StillCount == 0 && plan.Shapes.Count != 1) plan.Shapes.Should().ContainSingle($"a mesh that moved is stamped at frame {frames}");
        }
        while (plan.StillCount == 0 && frames <= SceneFieldPlan.MobileSettleFrames);
        frames.Should().Be(SceneFieldPlan.MobileSettleFrames, "a mesh that moved settles after the longer count");
        plan.Builds.Single().Cascade.Should().Be(0);
        plan.Shapes.Should().ContainSingle("it moved before it settled, so it is stamped while the second cascade waits");
        plan.Update(eye, [At(Vector3.UnitX)]);
        plan.Builds.Single().Cascade.Should().Be(1);
        plan.Shapes.Should().BeEmpty("both cascades hold it");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Still_Mesh_Replaced_In_Place_Stays_In_The_Field_Until_The_One_Replacing_It_Settles()
    {
        // A cube settled and built, then another mesh drawn through the same matrix where it was,
        // as an edited section of a game's world is, which the boxes it would be stamped as stood
        // in for badly while it settled.
        var plan = NewPlan(1, 0.25f, 1);
        var eye = new Vector3(0.1f, 0.1f, 0.1f);
        var replacement = GenMeshCubeVertices(2.5f);
        for (int frame = 0; frame <= SceneFieldPlan.SettleFrames; frame++) plan.Update(eye, [At(Vector3.Zero)]);
        plan.StillCount.Should().Be(1);

        for (int frame = 1; frame < SceneFieldPlan.SettleFrames; frame++)
        {
            plan.Update(eye, [At(Vector3.Zero, replacement)]);
            plan.StillCount.Should().Be(1, $"the cube stays in the field at frame {frame}");
            plan.Builds.Should().BeEmpty("nothing the field holds changed");
            plan.Shapes.Should().BeEmpty("the mesh replacing it is not stamped while it stands in");
        }
        plan.Update(eye, [At(Vector3.Zero, replacement)]);
        plan.StillCount.Should().Be(1, "the replacement is still and the cube gone");
        plan.Builds.Should().ContainSingle().Which.Instances.Should().ContainSingle()
            .Which.Vertices.Should().BeSameAs(replacement, "one build takes the cube out and puts its replacement in");
        plan.Shapes.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Mesh_Kept_For_A_Replacement_That_Never_Settles_Leaves_After_Twice_The_Frames_To_Settle()
    {
        var plan = NewPlan(1, 0.25f, 1);
        var eye = new Vector3(0.1f, 0.1f, 0.1f);
        for (int frame = 0; frame <= SceneFieldPlan.SettleFrames; frame++) plan.Update(eye, [At(Vector3.Zero)]);
        int frames = 0;
        do
        {
            // Vertices of their own each frame, as a mesh uploaded again each frame has.
            plan.Update(eye, [At(Vector3.Zero, GenMeshCubeVertices(2.5f))]);
            frames++;
        }
        while (plan.StillCount == 1 && frames < 100);
        frames.Should().Be(2 * SceneFieldPlan.SettleFrames + 2, "the cube is kept for twice the frames a mesh takes to settle, and then leaves");
        plan.Builds.Should().ContainSingle().Which.Instances.Should().BeEmpty();
        plan.Shapes.Should().ContainSingle("the mesh that never settles is stamped once nothing stands in for it");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void The_Budget_Builds_The_Finest_Dirty_Cascades_First()
    {
        var plan = NewPlan(3, 0.25f, 1);
        plan.Update(Vector3.Zero, []);
        plan.Builds.Select(build => build.Cascade).Should().Equal([0], "one cascade a frame");
        plan.BuiltOrigin(1).Should().BeNull("the second waits its turn");
        plan.Update(Vector3.Zero, []);
        plan.Builds.Select(build => build.Cascade).Should().Equal(1);
        plan.Update(Vector3.Zero, []);
        plan.Builds.Select(build => build.Cascade).Should().Equal(2);
        plan.Update(Vector3.Zero, []);
        plan.Builds.Should().BeEmpty();
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Moving_Mesh_Is_Stamped_Into_The_Bricks_Around_It_And_They_Are_Put_Back_When_It_Leaves()
    {
        var plan = NewPlan(1, 0.25f, 1);
        plan.Update(Vector3.Zero, []);
        // Drawn first far off, so the second frame finds it moved.
        plan.Update(Vector3.Zero, [At(new Vector3(0.1f, 30, 0))]);
        plan.Update(Vector3.Zero, [At(new Vector3(0.1f, 0, 0))]);

        // The cube's 2 units and the band's 1 on each side, over bricks of one unit from the
        // corner at -8, are bricks 6 to 10 along each axis.
        plan.Bricks.Should().HaveCount(5 * 5 * 5);
        plan.Bricks.Should().Contain((0, 6, 6, 6)).And.Contain((0, 10, 10, 10));
        var stamped = plan.Bricks.ToHashSet();

        plan.Update(Vector3.Zero, []);
        plan.Shapes.Should().BeEmpty();
        plan.Bricks.Should().BeEquivalentTo(stamped, "the bricks it was stamped into go back to the still field");
        plan.Update(Vector3.Zero, []);
        plan.Bricks.Should().BeEmpty("and need nothing after");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Each_Brick_Is_Given_The_Shapes_That_Come_Within_It_Alone()
    {
        // Three moving cubes in a row along X, their middles four units apart, whose bands meet in
        // the bricks between them, so a brick reads one or two of them and never the three.
        var plan = NewPlan(1, 0.25f, 1);
        plan.Update(Vector3.Zero, []);
        plan.Update(Vector3.Zero, [At(new Vector3(-4, 30, 0)), At(new Vector3(4, 30, 0)), At(new Vector3(0, 30, 0))]);
        plan.Update(Vector3.Zero, [At(new Vector3(-4, 0, 0)), At(new Vector3(4, 0, 0)), At(new Vector3(0, 0, 0))]);

        IEnumerable<int> Of(int brick) => plan.BrickShapes.Skip(plan.BrickRanges[brick].First).Take(plan.BrickRanges[brick].Count);
        var index = plan.Bricks.FindIndex(b => b == (0, 4, 8, 8));
        Of(index).Should().ContainSingle("the brick at x from -4 to -3 meets the left cube's alone");
        plan.Bricks.Select((_, b) => Of(b).Count()).Should().OnlyContain(n => n >= 1 && n <= 2, "no brick reads every shape");
        plan.BrickShapes.Should().HaveCount(plan.Bricks.Select((_, b) => Of(b).Count()).Sum());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void An_Edge_Is_Open_Where_No_Other_Triangle_Shares_Its_Ends_Whatever_Vertices_Hold_Them()
    {
        // A quad of two triangles, open at its four sides and closed along its diagonal, and the
        // same quad with each triangle keeping vertices of its own, as a cube's faces do.
        Vector3 At(float x, float z) => new(x, 0, z);
        Vector3[] shared = [At(0, 0), At(1, 0), At(1, 1), At(0, 1)];
        FieldTriangles.OpenEdges(shared, [0, 1, 2, 0, 2, 3]).Should().Equal([true, true, false, false, true, true]);
        Vector3[] apart = [At(0, 0), At(1, 0), At(1, 1), At(0, 0), At(1, 1), At(0, 1)];
        FieldTriangles.OpenEdges(apart, [0, 1, 2, 3, 4, 5]).Should().Equal([true, true, false, false, true, true]);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Scene_Drawn_In_The_Same_Order_Is_Read_By_Place_And_Planned_As_One_Drawn_In_Another_Order_Each_Frame()
    {
        // Three meshes that settle, one of which moves at frame 12 and settles again after the
        // longer count a mesh that moved takes, another left out from frame 25, and a skinned
        // figure whose limbs are posed afresh each frame, drawn to one plan in the same order every
        // frame and to another turned a place each frame, which never reads by place, so its plans
        // are those of working each frame out whole.
        var (byPlace, whole) = (NewPlan(2, 0.25f, 2), NewPlan(2, 0.25f, 2));
        var eye = new Vector3(0.3f, 0.2f, 0.1f);
        for (int frame = 0; frame < 14 + SceneFieldPlan.MobileSettleFrames; frame++)
        {
            var limbs = new[] { new FieldPart(new FieldBox(new Vector3(-0.2f), new Vector3(0.2f)), Matrix4x4.CreateTranslation(0, frame * 0.01f, 0)) };
            List<(SceneFieldPlan.Instance, bool)> drawn =
            [
                At(new Vector3(1, 0, 0)),
                .. frame < 25 ? [At(new Vector3(-2, 0, 1))] : Array.Empty<(SceneFieldPlan.Instance, bool)>(),
                At(frame < 12 ? new Vector3(0, 1, -2) : new Vector3(0.5f, 1, -2)),
                (new SceneFieldPlan.Instance(2, Cube, Matrix4x4.CreateTranslation(2, 0, 2), false, Parts: limbs), true),
            ];
            byPlace.Update(eye, drawn);
            whole.Update(eye, [.. drawn.Skip(frame % drawn.Count), .. drawn.Take(frame % drawn.Count)]);

            if (frame is > 0 and not 25) byPlace.ReadByPlace.Should().BeTrue($"frame {frame} draws what the one before did, in its order, one moved in its place");
            whole.ReadByPlace.Should().Be(frame % drawn.Count == (frame - 1) % drawn.Count && frame is > 0 and not 12 and not 25, "the other is turned a place each frame");
            byPlace.StillCount.Should().Be(whole.StillCount, $"frame {frame}");
            byPlace.Builds.Select(b => (b.Cascade, string.Join(";", b.Instances.Select(i => i.World.Translation.ToString()).Order(StringComparer.Ordinal))))
                .Should().Equal(whole.Builds.Select(b => (b.Cascade, string.Join(";", b.Instances.Select(i => i.World.Translation.ToString()).Order(StringComparer.Ordinal)))), $"frame {frame}");
            byPlace.Shapes.Select(shape => shape.ToOwn.Translation).Should().BeEquivalentTo(whole.Shapes.Select(shape => shape.ToOwn.Translation), $"frame {frame}");
            byPlace.Bricks.Should().BeEquivalentTo(whole.Bricks, $"frame {frame}");
        }
        byPlace.StillCount.Should().Be(2, "the mesh that moved has settled again and the one left out is gone");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Figures_Past_What_A_Frame_Stamps_Are_Boxes_Beyond_The_Nearest_And_None_Is_Left_Out()
    {
        // Thirty skinned figures of twenty limbs each, one after another away from the eye, where a
        // frame stamps 256 boxes, so the nearest take their limbs while the rest still have room
        // for a box each, eleven of them, and the nineteen beyond are a box each.
        var limbs = Enumerable.Range(0, 20).Select(j => new FieldPart(new FieldBox(new Vector3(-0.1f), new Vector3(0.1f)),
            Matrix4x4.CreateTranslation(0, j * 0.1f, 0))).ToArray();
        var plan = NewPlan(1, 0.25f, 1);
        plan.Update(Vector3.Zero, [.. Enumerable.Range(0, 30).Select(i =>
            (new SceneFieldPlan.Instance(1, Cube, Matrix4x4.CreateTranslation(i * 3 + 3, 0, 0), false, Parts: limbs), true))]);

        plan.Shapes.Should().HaveCount(11 * 20 + 19);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Moving_Mesh_That_Does_Not_Bend_Is_Stamped_As_The_Parts_Its_Triangles_Are_Cut_Into()
    {
        // An L of two boxes, one along X and one standing on its end, each face a grid of four by
        // four squares as a modeled mesh is cut finer than its shape, whose one box would fill the
        // corner between its arms.
        var (vertices, indices) = (new List<Vector3>(), new List<uint>());
        void Face(Vector3 corner, Vector3 across, Vector3 up)
        {
            var first = (uint)vertices.Count;
            for (int j = 0; j <= 4; j++)
                for (int i = 0; i <= 4; i++)
                    vertices.Add(corner + across * (i / 4f) + up * (j / 4f));
            for (uint j = 0; j < 4; j++)
                for (uint i = 0; i < 4; i++)
                {
                    uint a = first + j * 5 + i, b = a + 1, c = a + 6, d = a + 5;
                    indices.AddRange([a, b, c, a, c, d]);
                }
        }
        void Box(Vector3 min, Vector3 max)
        {
            var size = max - min;
            var (x, y, z) = (new Vector3(size.X, 0, 0), new Vector3(0, size.Y, 0), new Vector3(0, 0, size.Z));
            Face(min, x, y);
            Face(min + z, x, y);
            Face(min, x, z);
            Face(min + y, x, z);
            Face(min, y, z);
            Face(min + x, y, z);
        }
        Box(Vector3.Zero, new Vector3(3, 1, 1));
        Box(new Vector3(0, 1, 0), new Vector3(1, 3, 1));

        var parts = FieldTriangles.Cut([.. vertices], [.. indices]);

        parts.Should().NotBeNull().And.HaveCountGreaterThan(1);
        bool Inside(Vector3 point) => parts!.Any(part => Vector3.Clamp(point, part.Rest.Min, part.Rest.Max) == point);
        vertices.Where(v => !Inside(v)).Should().BeEmpty("every corner of the mesh is in a part");
        Inside(new Vector3(2.5f, 2.5f, 0.5f)).Should().BeFalse("and the corner between its arms in none");
        FieldTriangles.Cut([.. vertices], [.. indices.Take(3 * 15)]).Should().BeNull("a mesh of fewer than sixteen triangles is its one box");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Moving_Mesh_Is_Stamped_In_Its_Color_Times_Its_Vertices_Mean_Color()
    {
        var plan = NewPlan(1, 0.25f, 1, tint: _ => new Vector3(0.5f, 0.25f, 1));
        plan.Update(Vector3.Zero, []);
        plan.Update(Vector3.Zero, [(new SceneFieldPlan.Instance(1, Cube, Matrix4x4.CreateTranslation(0, 30, 0), false, Color: new Vector3(0.8f)), false)]);
        plan.Update(Vector3.Zero, [(new SceneFieldPlan.Instance(1, Cube, Matrix4x4.Identity, false, Color: new Vector3(0.8f)), false)]);
        plan.Shapes.Should().ContainSingle().Which.Color.Should().Be(new Vector3(0.4f, 0.2f, 0.8f));
    }
}
