using System.Numerics;
using Xunit;
using SceneFieldPlan = RadianceCascades.SceneFieldPlan<RadianceCascades.Tests.Field.SceneFieldPlanTests.Ahead.NumberedMesh, float>;

namespace RadianceCascades.Tests.Field;

public sealed partial class SceneFieldPlanTests
{
    // The builds' budget of triangles a frame with every form set, hundreds of meshes arriving in
    // one frame built over as many frames as it takes, nearest the eye first, each frame under the
    // budget on the plan's counts, a build in progress taking in a mesh that came and starting
    // again for one that left, and a finer cascade's build coming before a coarser one's.
    public sealed partial class Ahead
    {
        // Sections streamed in at once, 500 of 1,024 triangles each, nearly eight times a budget
        // of 65,536, flat boxes four meters a side on a grid of 25 by 20 about the eye.
        private const int Sections = 500, SectionTriangles = 1024;

        private static readonly FieldBox SectionBounds = new(Vector3.Zero, new Vector3(4, 1, 4));

        private static Vector3 SectionAt(int i) => new((i % 25 - 12) * 4, 0, (i / 25 - 10) * 4);

        private static List<(SceneFieldPlan.Instance, bool)> AllSections() =>
            [.. Enumerable.Range(0, Sections).Select(i => (Draw(1, 0, Matrix4x4.CreateTranslation(SectionAt(i)), SectionBounds), false))];

        // Runs a plan until it has settled the sections and built every cascade with them, and
        // answers each frame's triangles splatted from the frame they settled in, and the parts of
        // each cascade's build in order.
        private static (List<int> Frames, Dictionary<int, List<SceneFieldPlan.Build>> Parts) BuildSections(int triangleBudget)
        {
            var plan = NewPlan(4, 0.25f, 4, triangleBudget: triangleBudget, triangles: _ => SectionTriangles);
            var eye = new Vector3(0, 2, 0);
            var drawn = AllSections();
            var (frames, parts) = (new List<int>(), new Dictionary<int, List<SceneFieldPlan.Build>>());
            for (var frame = 1; frame < 200; frame++)
            {
                plan.Update(eye, drawn);
                if (frame < SceneFieldPlan.SettleFrames) continue;
                frames.Add(plan.Splatted);
                foreach (var build in plan.Builds) (parts.TryGetValue(build.Cascade, out var list) ? list : parts[build.Cascade] = []).Add(build);
                if (plan.Builds.Count == 0 && plan.Waiting == 0) break;
            }
            return (frames, parts);
        }

        /// <summary>
        /// Five hundred meshes settling in one frame are splatted over as many frames as the
        /// triangle budget takes, no frame over it, each cascade's build started once and read
        /// whole on its last part, from the meshes one build would have built it from, each part's
        /// as near the eye as the next part's or nearer.
        /// </summary>
        [Fact]
        [Trait("Category", "Unit")]
        public void FiveHundredMeshesArrivingInOneFrameAreBuiltOverSeveralFramesUnderTheTriangleBudgetTheNearestFirst()
        {
            // All at once, as the cascades' budget alone has it, the frame they settle in splats
            // every cascade's reach, 500 meshes into the coarsest alone.
            var (whole, wholeParts) = BuildSections(int.MaxValue);
            Assert.True(whole[0] >= Sections * SectionTriangles, $"the frame the sections settled in splatted {whole[0]} triangles into four cascades");
            Assert.All(wholeParts.Values, list => Assert.True(list is [{ First: true, Last: true }], "each cascade in one build of one part"));

            const int budget = 65_536;
            var (frames, parts) = BuildSections(budget);
            Assert.All(frames, splatted => Assert.InRange(splatted, 0, budget));
            Assert.True(frames.Count(splatted => splatted > 0) >= whole[0] / budget, $"the builds took {frames.Count(splatted => splatted > 0)} frames");

            foreach (var (cascade, list) in parts)
            {
                Assert.True(list[0].First, $"cascade {cascade}'s build starts on its first part");
                Assert.DoesNotContain(list.Skip(1), part => part.First);
                Assert.True(list[^1].Last, $"cascade {cascade}'s build ends on its last part");
                Assert.DoesNotContain(list.SkipLast(1), part => part.Last || part.Changed is not null);
                Assert.Equal(
                    wholeParts[cascade][0].Instances.Select(instance => instance.World.Translation).Order(Positions),
                    list.SelectMany(part => part.Instances).Select(instance => instance.World.Translation).Order(Positions));
            }

            // Each part's meshes are as near the eye as the next part's or nearer.
            var eye = new Vector3(0, 2, 0);
            float Distance(SceneFieldPlan.Instance instance) => Vector3.DistanceSquared(instance.World.Translation + new Vector3(2, 0.5f, 2), eye);
            foreach (var list in parts.Values.Where(list => list.Count > 1))
                for (var part = 0; part + 1 < list.Count; part++)
                    Assert.True(list[part].Instances.Max(Distance) <= list[part + 1].Instances.Min(Distance), "nearest the eye first");
        }

        /// <summary>
        /// A build in progress takes in a mesh that settles within its reach without starting
        /// again, and starts again without a mesh it splatted that leaves.
        /// </summary>
        [Fact]
        [Trait("Category", "Unit")]
        public void ABuildInProgressTakesInAMeshThatSettlesAndStartsAgainForASplattedOneThatLeaves()
        {
            // A budget of one mesh's triangles, so a build of ten meshes takes ten frames, the
            // meshes in a row going away from the eye.
            var plan = NewPlan(1, 1, 1, triangleBudget: 10, triangles: _ => 10);
            var eye = new Vector3(0, 2, 6);
            var row = Enumerable.Range(0, 10).Select(k => At(new Vector3(0, 0, 4 - 2 * k))).ToList();
            plan.Update(eye, []);
            Assert.Single(plan.Builds);
            for (var frame = 1; frame < SceneFieldPlan.SettleFrames; frame++) plan.Update(eye, row);

            // The frame they settle in starts the build with the nearest, and a newcomer drawn from
            // the frame after settles while the build runs, nearer the eye than the meshes left.
            plan.Update(eye, row);
            var started = Assert.Single(plan.Builds);
            Assert.True(started is { First: true, Last: false, Instances.Length: 1 }, "the build's first part splats one mesh");
            Assert.Equal(new Vector3(0, 0, 4), started.Instances[0].World.Translation);
            var newcomer = At(new Vector3(0, 0, 2.5f));
            var parts = new List<SceneFieldPlan.Build>();
            for (var frame = 0; frame < 30 && (parts.Count == 0 || !parts[^1].Last); frame++)
            {
                plan.Update(eye, [.. row, newcomer]);
                parts.AddRange(plan.Builds);
            }
            Assert.NotEmpty(parts);
            Assert.DoesNotContain(parts, part => part.First);
            Assert.True(parts[^1].Last);
            Assert.Contains(new Vector3(0, 0, 2.5f), parts.SelectMany(part => part.Instances).Select(i => i.World.Translation));
            plan.Update(eye, [.. row, newcomer]);
            Assert.Empty(plan.Builds);

            // The farthest leaves, and the cascade is built again. Its build's first part splats
            // the nearest, which then leaves too, and the build starts again without it.
            var left = row.Take(9).Append(newcomer).ToList();
            plan.Update(eye, left);
            var again = Assert.Single(plan.Builds);
            Assert.True(again is { First: true, Last: false });
            Assert.Equal(new Vector3(0, 0, 4), again.Instances[0].World.Translation);
            left.RemoveAt(0);
            plan.Update(eye, left);
            Assert.True(Assert.Single(plan.Builds).First, "a mesh the build splatted left, so it starts again");
            var splatted = plan.Builds.SelectMany(b => b.Instances).Select(i => i.World.Translation).ToList();
            for (var frame = 0; frame < 30 && !plan.Builds[^1].Last; frame++)
            {
                plan.Update(eye, left);
                splatted.AddRange(plan.Builds.SelectMany(b => b.Instances).Select(i => i.World.Translation));
            }
            Assert.Equal(left.Select(entry => entry.Item1.World.Translation).Order(Positions), splatted.Order(Positions));
        }

        /// <summary>
        /// A finer cascade the eye moves past is built before a coarser cascade's build in
        /// progress, which starts again after it.
        /// </summary>
        [Fact]
        [Trait("Category", "Unit")]
        public void AFinerCascadeTheEyeMovesPastIsBuiltBeforeACoarserBuildInProgressWhichStartsAgainAfter()
        {
            // Two cascades and a budget of one mesh a frame, so the coarser's build of the sections
            // it reaches takes many frames.
            var plan = NewPlan(2, 0.25f, 2, triangleBudget: SectionTriangles, triangles: _ => SectionTriangles);
            var eye = new Vector3(0, 2, 0);
            var drawn = AllSections();
            // The finer's sections take as many frames from the eighth, and the coarser's begin
            // after.
            for (var frame = 0; frame < 80; frame++) plan.Update(eye, drawn);
            var running = Assert.Single(plan.Builds);
            Assert.True(running is { Cascade: 1, First: false }, "the coarser cascade's build is under way");

            // The eye passes the finer cascade's grid, which is built at once, and the coarser's
            // build starts again the frame after it ends.
            eye += new Vector3(2.5f, 0, 0);
            plan.Update(eye, drawn);
            Assert.NotEmpty(plan.Builds);
            Assert.True(plan.Builds[0] is { Cascade: 0, First: true }, "the finer cascade comes first");
            var after = new List<SceneFieldPlan.Build>(plan.Builds);
            for (var frame = 0; frame < 60 && !after.Any(b => b.Cascade == 1); frame++)
            {
                plan.Update(eye, drawn);
                after.AddRange(plan.Builds);
            }
            Assert.True(after.First(b => b.Cascade == 1).First, "the coarser cascade's build starts again");
        }

        // Orders places by x, then y, then z, so two lists of the same places compare equal.
        private static readonly Comparer<Vector3> Positions = Comparer<Vector3>.Create((a, b) =>
            a.X != b.X ? a.X.CompareTo(b.X) : a.Y != b.Y ? a.Y.CompareTo(b.Y) : a.Z.CompareTo(b.Z));
    }
}
