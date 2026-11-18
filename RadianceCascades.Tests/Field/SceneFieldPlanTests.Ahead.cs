using System.Numerics;
using Xunit;
using SceneFieldPlan = RadianceCascades.SceneFieldPlan<RadianceCascades.Tests.Field.SceneFieldPlanTests.Ahead.NumberedMesh, float>;

namespace RadianceCascades.Tests.Field;

public sealed partial class SceneFieldPlanTests
{
    /// <summary>
    /// The plan with each of its forms set the other way from its default, each cascade lying ahead
    /// of the eye, a mesh that came stamped as its box until it is built, every build of a whole
    /// cascade, and a mesh painted again kept as it was until its new paint settles, where its
    /// cascades lie and what each frame builds and stamps.
    /// </summary>
    /// <remarks>
    /// A mesh is known by its number and the generation of its data, with the box around it in its
    /// own space, so a mesh whose data was written again is another mesh, and is stamped as that
    /// box alone.
    /// </remarks>
    public sealed partial class Ahead
    {
        /// <summary>
        /// A mesh by its number, the generation its data is at and the box around it in its own
        /// space, told apart by all three.
        /// </summary>
        internal readonly record struct NumberedMesh(uint Mesh, uint Generation, FieldBox Bounds);

        private static readonly FieldPlanOptions Options = new() { Lead = 0.35f, StampArrivals = true, WholeBuilds = true, RepaintWaits = true };

        private static readonly FieldBox CubeBounds = new(new Vector3(-1), new Vector3(1));

        // A plan of the forms above that reads a mesh as its box and as a thousand triangles where
        // a test gives no count, as the tests' numbers were worked out with.
        private static SceneFieldPlan NewPlan(int cascades, float cellSize, int budget, int triangleBudget = SceneFieldPlan.DefaultTriangleBudget,
            Func<NumberedMesh, int>? triangles = null) =>
            new(cascades, cellSize, budget, mesh => mesh.Bounds, triangles ?? (_ => 1000), triangleBudget: triangleBudget, options: Options);

        // A draw of a mesh that has no inside, its gloss none, its roughness, metallic share and
        // reflectance each 0, as the draws the tests were written with carry it.
        private static SceneFieldPlan.Instance Draw(uint mesh, uint generation, Matrix4x4 world, FieldBox bounds) =>
            new((int)mesh, new NumberedMesh(mesh, generation, bounds), world, false, Roughness: 0, Metallic: 0, Reflectance: 0);

        private static (SceneFieldPlan.Instance, bool) At(Vector3 position, uint mesh = 1, uint generation = 0) =>
            (Draw(mesh, generation, Matrix4x4.CreateTranslation(position), CubeBounds), false);

        [Fact]
        [Trait("Category", "Unit")]
        public void ACascadeLiesAroundTheEyeAndMovesOnlyPastItsGrid()
        {
            var plan = NewPlan(2, 0.25f, 2);
            plan.Update(new Vector3(0.1f, 2, 6), []);

            // The corner is 32 cells of 0.25 short of the eye's 2-unit grid point, and the second
            // cascade's cells and grid are twice as wide.
            Assert.Equal(new Vector3(-8, -6, -2), plan.BuiltOrigin(0));
            Assert.Equal(new Vector3(-16, -16, -12), plan.BuiltOrigin(1));
            Assert.Equal([0, 1], plan.Builds.Select(build => build.Cascade));

            // Within the 2-unit cell of the grid nothing moves, and past it the first cascade
            // alone.
            plan.Update(new Vector3(1.9f, 2, 6), []);
            Assert.Empty(plan.Builds);
            plan.Update(new Vector3(2.1f, 2, 6), []);
            Assert.Equal([0], plan.Builds.Select(build => build.Cascade));
            Assert.Equal(new Vector3(-6, -6, -2), plan.BuiltOrigin(0));
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void AMeshDrawnTheSameForEightFramesIsBuiltAndStampedUntilThen()
        {
            var plan = NewPlan(1, 0.25f, 1);
            var eye = new Vector3(0, 2, 6);
            for (var frame = 1; frame < SceneFieldPlan.SettleFrames; frame++)
            {
                plan.Update(eye, [At(Vector3.Zero)]);
                Assert.Equal(0, plan.StillCount);
                Assert.Single(plan.Shapes);
            }

            // Still, and the cascade it is in built again with it rather than stamped.
            plan.Update(eye, [At(Vector3.Zero)]);
            Assert.Equal(1, plan.StillCount);
            Assert.Single(Assert.Single(plan.Builds).Instances);
            Assert.Empty(plan.Shapes);

            // Drawn somewhere else, another that has not settled, and the cascade it left built
            // without it.
            plan.Update(eye, [At(Vector3.UnitX)]);
            Assert.Equal(0, plan.StillCount);
            Assert.Empty(Assert.Single(plan.Builds).Instances);
            Assert.Equal(-Vector3.UnitX, Assert.Single(plan.Shapes).ToOwn.Translation);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void AStillMeshReplacedInPlaceStaysInTheFieldUntilTheOneReplacingItSettles()
        {
            // A cube settled and built, then its mesh written again in place, a generation on, as
            // an edited section of a world is.
            var plan = NewPlan(1, 0.25f, 1);
            var eye = new Vector3(0.1f, 0.1f, 0.1f);
            for (var frame = 0; frame <= SceneFieldPlan.SettleFrames; frame++) plan.Update(eye, [At(Vector3.Zero)]);
            Assert.Equal(1, plan.StillCount);

            for (var frame = 1; frame < SceneFieldPlan.SettleFrames; frame++)
            {
                plan.Update(eye, [At(Vector3.Zero, generation: 1)]);
                Assert.Equal(1, plan.StillCount);
                Assert.Empty(plan.Builds);
                Assert.Empty(plan.Shapes);
            }

            // One build takes the cube out and puts its replacement in.
            plan.Update(eye, [At(Vector3.Zero, generation: 1)]);
            Assert.Equal(1, plan.StillCount);
            Assert.Equal(1u, Assert.Single(Assert.Single(plan.Builds).Instances).Vertices.Generation);
            Assert.Empty(plan.Shapes);
        }

        /// <summary>
        /// A mesh painted again where it stands, as its texture's mean paints it once the texture
        /// loads after the mesh was first drawn, has not moved, and settles as a mesh drawn the
        /// same does. One painted again soon after it settled stays in the field as it was until
        /// its new paint settles, and one that has stood still longer, as a light put out, is built
        /// in with its new paint at once, neither stamped meanwhile.
        /// </summary>
        /// <remarks>
        /// A mesh taken as moved waits <see cref="SceneFieldPlan.MobileSettleFrames"/> frames
        /// before it is still, stamped as its box until then and until the cascades around it are
        /// built again, so a scene some of whose textures load after its meshes are first drawn
        /// would stand as boxes for that long, its walls and floors filling the probes' cells and
        /// the light they add gone. Held as it was until its new paint settled, a panel put out
        /// would light its room for as many frames.
        /// </remarks>
        [Fact]
        [Trait("Category", "Unit")]
        public void AMeshPaintedAgainWhereItStandsHasNotMovedAndKeepsItsPlaceInTheField()
        {
            static (SceneFieldPlan.Instance, bool) Painted(float mean) => (At(Vector3.Zero).Item1 with { Color = new Vector3(mean) }, false);
            var plan = NewPlan(1, 0.25f, 1);
            var eye = new Vector3(0.1f, 0.1f, 0.1f);

            // Drawn by its factor for a few frames, then by its texture's mean.
            for (var frame = 0; frame < 3; frame++) plan.Update(eye, [Painted(1f)]);
            for (var frame = 1; frame < SceneFieldPlan.SettleFrames; frame++)
            {
                plan.Update(eye, [Painted(0.4f)]);
                Assert.Equal(0, plan.StillCount);
            }
            plan.Update(eye, [Painted(0.4f)]);
            Assert.Equal(1, plan.StillCount);
            Assert.Equal(new Vector3(0.4f), Assert.Single(Assert.Single(plan.Builds).Instances).Color);

            // Painted again once still, as a map that loads later does, kept as it was and not
            // stamped.
            for (var frame = 1; frame < SceneFieldPlan.SettleFrames; frame++)
            {
                plan.Update(eye, [Painted(0.6f)]);
                Assert.Equal(1, plan.StillCount);
                Assert.Empty(plan.Builds);
                Assert.Empty(plan.Shapes);
            }

            // One build puts the new paint in.
            plan.Update(eye, [Painted(0.6f)]);
            Assert.Equal(1, plan.StillCount);
            Assert.Equal(new Vector3(0.6f), Assert.Single(Assert.Single(plan.Builds).Instances).Color);
            Assert.Empty(plan.Shapes);

            // Painted again once it has stood still as long as it took to settle, as a light put
            // out, built in with its new paint in the frame it comes.
            for (var frame = 0; frame < SceneFieldPlan.SettleFrames; frame++) plan.Update(eye, [Painted(0.6f)]);
            plan.Update(eye, [Painted(0f)]);
            Assert.Equal(1, plan.StillCount);
            Assert.Equal(Vector3.Zero, Assert.Single(Assert.Single(plan.Builds).Instances).Color);
            Assert.Empty(plan.Shapes);
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void TheBudgetBuildsTheFinestDirtyCascadesFirst()
        {
            var plan = NewPlan(3, 0.25f, 1);
            plan.Update(Vector3.Zero, []);
            Assert.Equal([0], plan.Builds.Select(build => build.Cascade));
            Assert.Null(plan.BuiltOrigin(1));
            plan.Update(Vector3.Zero, []);
            Assert.Equal([1], plan.Builds.Select(build => build.Cascade));
            plan.Update(Vector3.Zero, []);
            Assert.Equal([2], plan.Builds.Select(build => build.Cascade));
            plan.Update(Vector3.Zero, []);
            Assert.Empty(plan.Builds);
        }

        /// <summary>
        /// A mesh drawn twice in one place keeps every frame from being read by place, and once
        /// told, the frames drawn without the second copy are.
        /// </summary>
        [Fact]
        [Trait("Category", "Unit")]
        public void AMeshDrawnTwiceInOnePlaceIsToldAndTheFramesWithoutItsCopyAreReadByPlace()
        {
            var plan = NewPlan(1, 0.25f, 1);
            (SceneFieldPlan.Instance, bool)[] twice = [At(Vector3.Zero), At(Vector3.Zero), At(Vector3.UnitX, mesh: 2)];
            plan.Update(Vector3.Zero, twice);
            plan.Update(Vector3.Zero, twice);
            Assert.True(plan.DrawnTwiceInPlace);
            Assert.False(plan.ReadByPlace);

            (SceneFieldPlan.Instance, bool)[] once = [At(Vector3.Zero), At(Vector3.UnitX, mesh: 2)];
            plan.Update(Vector3.Zero, once);
            Assert.False(plan.ReadByPlace);
            plan.Update(Vector3.Zero, once);
            Assert.True(plan.ReadByPlace);
            Assert.True(plan.DrawnTwiceInPlace, "told once, the field keeps gathering each mesh once");
        }

        [Fact]
        [Trait("Category", "Unit")]
        public void AMovingMeshIsStampedIntoTheBricksAroundItAndTheyArePutBackWhenItLeaves()
        {
            var plan = NewPlan(1, 0.25f, 1);
            plan.Update(Vector3.Zero, []);
            plan.Update(Vector3.Zero, [At(new Vector3(0.1f, 0, 0))]);

            // The cube's 2 units and the band's 1 on each side, over bricks of one unit from the
            // corner at -8, are bricks 6 to 10 along each axis.
            Assert.Equal(5 * 5 * 5, plan.Bricks.Count);
            Assert.Contains((0, 6, 6, 6), plan.Bricks);
            Assert.Contains((0, 10, 10, 10), plan.Bricks);
            var stamped = plan.Bricks.ToHashSet();

            plan.Update(Vector3.Zero, []);
            Assert.Empty(plan.Shapes);
            Assert.True(stamped.SetEquals(plan.Bricks), "the bricks it was stamped into go back to the still field");
            plan.Update(Vector3.Zero, []);
            Assert.Empty(plan.Bricks);
        }
    }
}
