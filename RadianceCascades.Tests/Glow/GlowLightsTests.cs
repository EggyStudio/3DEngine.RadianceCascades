using System.Numerics;
using System.Runtime.InteropServices;
using RadianceCascades.Tests.Field;
using Xunit;
using SceneFieldPlan = RadianceCascades.SceneFieldPlan<RadianceCascades.Tests.Field.TestMesh, int>;

namespace RadianceCascades.Tests.Glow;

/// <summary>
/// The small emissive surfaces, which global illumination carries as lights of their own
/// (<see cref="GlowLights"/>), with which are carried, how they are written for the kernels, and
/// how a mesh is cut into its parts and the faces they emit from.
/// </summary>
public sealed partial class GlowLightsTests
{
    // The forms these tests hold, a light reaching as far as a surface takes a 300th of its light
    // or of what a surface half a probe spacing from it takes, the lights ranked by what each sends
    // about the eye, and a head ahead of the lights.
    private static readonly GlowForms Ranked = new() { FaintThousandth = false, NearestFirst = false, LightsAlone = false };

    // The box around a mesh's positions in its own space.
    private static FieldBox BoxOf(TestMesh mesh) => FieldBox.Of(mesh.Positions);

    // How far apart the places a frame's origin stands at are along each axis, and the origin for
    // an eye, the eye rounded to the nearest whole count of them along each axis.
    private const double Step = 16;

    private static FrameOrigin Near(Vector3 eye) =>
        new(Math.Floor(eye.X / Step + 0.5) * Step, Math.Floor(eye.Y / Step + 0.5) * Step, Math.Floor(eye.Z / Step + 0.5) * Step);

    [Fact]
    [Trait("Category", "Unit")]
    public void An_Emitter_No_Longer_Than_Twice_The_First_Cascades_Spacing_Is_Carried()
    {
        var unit = new FieldBox(new Vector3(-0.5f), new Vector3(0.5f));
        var glow = new Vector3(4, 2.3f, 0.75f);
        GlowLights.Carries(glow, unit, Matrix4x4.Identity, 0.25f).Should().BeTrue("a block is shorter than the four blocks of twice the spacing of eight cells of a quarter");
        GlowLights.Carries(glow, unit, Matrix4x4.CreateScale(4, 0.1f, 0.1f), 0.25f).Should().BeTrue("a strip four blocks long is no longer");
        GlowLights.Carries(glow, unit, Matrix4x4.CreateScale(4.5f, 0.1f, 0.1f), 0.25f).Should().BeFalse("a strip longer is left to the probes' rays");
        GlowLights.Carries(glow, unit, Matrix4x4.Identity, 0.05f).Should().BeFalse("at a finer cell the probes stand close enough to see the block");
        GlowLights.Carries(Vector3.Zero, unit, Matrix4x4.Identity, 0.25f).Should().BeFalse("a surface that emits no light is no light");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void The_Lights_Written_Are_The_Nearest_The_Eye_With_Their_Boxes_And_Reach()
    {
        // Seventy blocks alike along x, every one carried, the eye past the last, so the 64 nearest
        // it send the most about it and are written, and the six nearest the origin are not.
        var vertices = new TestMesh([new Vector3(-0.5f), new Vector3(0.5f)]);
        var drawn = Enumerable.Range(0, 70).Select(i => (new SceneFieldPlan.Instance(1, vertices, Matrix4x4.CreateTranslation(i * 2, 0.5f, 0),
            false, Vector3.One, new Vector3(4, 2, 1)), false)).ToList();
        var bytes = new byte[GlowLights.Size];
        var found = GlowLights.Write(bytes, drawn, new Vector3(200, 0, 0), 0.25f, BoxOf, _ => 1, Ranked);

        found.Should().Be(70);
        var floats = MemoryMarshal.Cast<byte, float>(bytes.AsSpan()).ToArray();
        floats[0].Should().Be(GlowLights.Most);
        var middles = Enumerable.Range(0, GlowLights.Most).Select(n => floats[GlowLights.Head / 4 + n * GlowLights.Bytes / 4]).ToList();
        middles.Should().Equal(Enumerable.Range(0, GlowLights.Most).Select(n => (float)(69 - n) * 2), "the nearest the eye first");
        var first = floats[(GlowLights.Head / 4)..(GlowLights.Head / 4 + GlowLights.Bytes / 4)];
        first[1].Should().BeApproximately(0.5f, 1e-6f);
        first[3].Should().BeApproximately(MathF.Sqrt(6 / (4 / 300f)), 1e-3f, "a block of six faces reaches where it lights a surface by a 300th of its own light, however bright it is");
        first[4..8].Should().Equal(0.5f, 0, 0, 0.5f);
        first[16..20].Should().Equal(4, 2, 1, 1);
        var reach = first[3];
        floats[1..4].Should().Equal([12 - reach, 0.5f - reach, -reach], "the box every light's reach lies in, from the nearest light at 12 to the furthest at 138");
        floats[4..7].Should().Equal([138 + reach, 0.5f + reach, reach]);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Touching_Emitters_That_Give_Off_The_Same_Light_Are_One_Light()
    {
        // Four blocks stacked two by two, a fifth beside them emitting another light and a sixth
        // apart. The four are one light of the stack's box, the faces they press together left out
        // of its surface, which is then the whole of its box's.
        var vertices = new TestMesh([new Vector3(-0.5f), new Vector3(0.5f)]);
        var (glow, other) = (new Vector3(4, 2, 1), new Vector3(1, 2, 4));
        (SceneFieldPlan.Instance, bool) Block(float x, float z, Vector3 light) =>
            (new SceneFieldPlan.Instance(1, vertices, Matrix4x4.CreateTranslation(x, 0.5f, z), false, Vector3.One, light), false);
        var lights = GlowLights.Gather([Block(0.5f, 0.5f, glow), Block(1.5f, 0.5f, glow), Block(0.5f, 1.5f, glow), Block(1.5f, 1.5f, glow),
            Block(2.5f, 0.5f, other), Block(6.5f, 0.5f, glow)], 0.25f, BoxOf, _ => 1);

        lights.Should().HaveCount(3);
        var stack = lights.Single(l => l.Middle == new Vector3(1, 0.5f, 1));
        (stack.X, stack.Y, stack.Z).Should().Be((new Vector3(1, 0, 0), new Vector3(0, 0.5f, 0), new Vector3(0, 0, 1)));
        stack.Share.Should().BeApproximately(1, 1e-5f, "four blocks of 6 less the four faces they press together, twice each, is the 16 of their box");
        lights.Should().Contain(l => l.Middle == new Vector3(2.5f, 0.5f, 0.5f) && l.Glow == other, "a block giving off another light stays its own");
        lights.Should().Contain(l => l.Middle == new Vector3(6.5f, 0.5f, 0.5f), "and so does one that touches none");
    }

    // A lamp 0.3 wide, and two pieces 0.3 wide stacked, each emitting the same light, their middles
    // on a float's step at 1,140,736 and their sides off it, at a place in the world.
    private static List<(SceneFieldPlan.Instance, bool)> Lamps(Vector3 at)
    {
        var vertices = new TestMesh([new Vector3(-0.15f), new Vector3(0.15f)]);
        Vector3[] middles = [new(0.5f, 0.15f, 0.5f), new(3, 0.15f, 0.5f), new(3, 0.45f, 0.5f)];
        return [.. middles.Select(m => (new SceneFieldPlan.Instance(1, vertices, Matrix4x4.CreateTranslation(at + m), false, Vector3.One, new Vector3(4, 2, 1)), false))];
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(71680f, 17408f)]
    [InlineData(1140736f, 285696f)]
    public void Emitters_Far_From_The_Worlds_Origin_Are_Carried_As_At_It(float x, float z)
    {
        var at = new Vector3(x, 0, z);
        var home = GlowLights.Gather(Lamps(Vector3.Zero), 0.25f, BoxOf, _ => 1);
        var far = GlowLights.Gather(Lamps(at), 0.25f, BoxOf, _ => 1, origin: Near(at));
        home.Should().HaveCount(2, "the lamp and the stack of two");
        far.Should().HaveCount(2, "the two pieces far out touch and are one light");
        // A box moved into the world in floats has its two sides each rounded to the step there,
        // so a lamp 0.3 wide is carried as 0.25 or 0.375 wide.
        static float Apart(Vector3 a, Vector3 b) => (a - b).Length();
        for (int i = 0; i < 2; i++)
        {
            (Apart(far[i].X, home[i].X) + Apart(far[i].Y, home[i].Y) + Apart(far[i].Z, home[i].Z)).Should().BeLessThan(1e-5f, "each light is as large as at the world's origin");
            Apart(far[i].Middle, home[i].Middle).Should().BeLessThan(1e-5f, "each light's middle is taken against the origin as at the world's");
            far[i].Share.Should().BeApproximately(home[i].Share, 1e-5f);
        }

        // Written with their middles in world numbers, which the lights are known by from frame to
        // frame, and placed against the origin from their middles as they were gathered.
        float[] Placed(Vector3 place)
        {
            var (bytes, places, origin) = (new byte[GlowLights.Size], new GlowPlaces(), Near(place));
            GlowLights.Write(bytes, Lamps(place), place + new Vector3(0, 2, 6), 0.25f, BoxOf, _ => 1, Ranked, places, origin: origin);
            GlowLights.Place(bytes, origin, places);
            return MemoryMarshal.Cast<byte, float>(bytes.AsSpan()).ToArray();
        }
        var (placedHome, placedFar) = (Placed(Vector3.Zero), Placed(at));
        placedFar.Zip(placedHome, (f, h) => MathF.Abs(f - h)).Max().Should().BeLessThan(1e-5f, "the lights are written against the origin as at the world's");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Mesh_Of_Many_Blocks_Is_Cut_Into_Its_Parts_With_The_Faces_They_Show()
    {
        // A whole block at the middle and, ten off, a block whose top alone shows, as a block of
        // ore sunk in the ground, in one mesh, as a voxel game draws its blocks of a kind.
        var corners = new List<Vector3>();
        void Face(Vector3 middle, Vector3 normal)
        {
            var side = MathF.Abs(normal.Y) > 0.5f ? Vector3.UnitX : Vector3.UnitY;
            var up = Vector3.Cross(normal, side);
            Vector3 At(float a, float b) => middle + (normal + side * a + up * b) * 0.5f;
            var (p0, p1, p2, p3) = (At(-1, -1), At(1, -1), At(1, 1), At(-1, 1));
            if (Vector3.Dot(Vector3.Cross(side, up), normal) > 0) corners.AddRange([p0, p1, p2, p0, p2, p3]);
            else corners.AddRange([p0, p2, p1, p0, p3, p2]);
        }
        foreach (var normal in new[] { Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ })
            Face(Vector3.Zero, normal);
        Face(new Vector3(10, 0, 0), Vector3.UnitY);
        var box = new FieldBox(new Vector3(-0.5f), new Vector3(10.5f, 0.5f, 0.5f));
        var glow = new Vector3(4, 2, 1);

        var parts = GlowLights.PartsOf([.. corners], box, 0.1f);

        parts.Should().HaveCount(2);
        var whole = parts.Single(p => p.Box.Min.X < 1);
        whole.Faces.Should().Be(0, "a block showing all six faces emits from every face");
        whole.Share.Should().BeApproximately(1, 1e-5f);
        var sunk = parts.Single(p => p.Box.Min.X > 9);
        sunk.Faces.Should().Be(1 << 3, "the sunk block emits from its top alone, the side y points to");
        sunk.Share.Should().BeApproximately(1, 1e-5f, "its face is the whole of the box's face it lies on");
        GlowLights.Carries(glow, parts, Matrix4x4.Identity, 0.25f).Should().BeTrue("each part is a block, though the mesh is eleven long");
        GlowLights.Carries(glow, box, Matrix4x4.Identity, 0.25f).Should().BeFalse("taken whole it is too long");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void An_Emitter_Recorded_Twice_Is_One_Light()
    {
        // A turned block and a square one, each recorded twice as a stereo pair's second eye
        // repeats the window's draws, are one light each, as recorded once.
        var vertices = new TestMesh([new Vector3(-0.5f), new Vector3(0.5f)]);
        var glow = new Vector3(4, 2, 1);
        (SceneFieldPlan.Instance, bool) Block(Matrix4x4 world) => (new SceneFieldPlan.Instance(1, vertices, world, false, Vector3.One, glow), false);
        var turned = Matrix4x4.CreateRotationY(0.5f) * Matrix4x4.CreateTranslation(0, 0.5f, 0);
        var square = Matrix4x4.CreateTranslation(4, 0.5f, 0);

        var once = GlowLights.Gather([Block(turned), Block(square)], 0.25f, BoxOf, _ => 1);
        var twice = GlowLights.Gather([Block(turned), Block(square), Block(turned), Block(square)], 0.25f, BoxOf, _ => 1);

        once.Should().HaveCount(2);
        twice.Should().Equal(once, "a copy recorded twice is the one emitter it draws");
    }

    [Theory]
    [Trait("Category", "Unit")]
    [InlineData(1, 1 << 0 | 1 << 1)]
    [InlineData(2, 1 << 0 | 1 << 3)]
    [InlineData(3, 1 << 0 | 1 << 3 | 1 << 4)]
    public void A_Mesh_Of_Some_Faces_Of_A_Block_Emits_From_Those_Faces_At_Their_Whole_Surface(int count, int faces)
    {
        // The faces of a block that show, handed in one mesh as a program hands them. The first
        // case is the face toward -x alone, a flat emitter lighting both its sides, the second that
        // and the top, which share an edge, and the third those and the face toward -z, a corner of
        // three.
        var corners = new List<Vector3>();
        void Face(Vector3 normal)
        {
            var side = MathF.Abs(normal.Y) > 0.5f ? Vector3.UnitX : Vector3.UnitY;
            var up = Vector3.Cross(normal, side);
            Vector3 At(float a, float b) => (normal + side * a + up * b) * 0.5f;
            var (p0, p1, p2, p3) = (At(-1, -1), At(1, -1), At(1, 1), At(-1, 1));
            if (Vector3.Dot(Vector3.Cross(side, up), normal) > 0) corners.AddRange([p0, p1, p2, p0, p2, p3]);
            else corners.AddRange([p0, p2, p1, p0, p3, p2]);
        }
        foreach (var normal in new[] { -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitZ }.Take(count))
            Face(normal);
        Vector3[] points = [.. corners];
        var box = new FieldBox(points.Aggregate(Vector3.Min), points.Aggregate(Vector3.Max));
        var vertices = new TestMesh(points);
        var parts = GlowLights.PartsOf(points, box, GlowLights.ShareOf(points, box));

        var lights = GlowLights.Gather([(new SceneFieldPlan.Instance(1, vertices, Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f), false, Vector3.One, new Vector3(4, 2, 1)), false)],
            0.25f, _ => box, _ => GlowLights.ShareOf(points, box), _ => parts);

        var light = lights.Should().ContainSingle().Subject;
        light.Faces.Should().Be(faces, $"a mesh of {count} of a block's faces emits from those, and from both sides where it is one face alone");
        light.Share.Should().BeApproximately(1, 1e-4f, "each face it emits from is whole");
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void A_Lanterns_Glass_Of_Two_Layers_Of_Leaning_Panels_Emits_From_Its_Sides_At_Its_Outer_Surface()
    {
        // A street lantern's glass, twelve panels about an upright axis narrowing toward the top,
        // so each faces a little up, and the same twelve three thousandths inside, facing in, apart
        // from them. Its light leaves by the outer panels alone, out of the box's four sides, and
        // none from its top or its floor.
        // Taken by every face of its box at both layers' surface, it sent twice its light, a part
        // of it straight down from a face it has none of.
        var corners = new List<Vector3>();
        var outer = 0f;
        void Panel(float radiusLow, float radiusHigh, int i, bool outward)
        {
            Vector3 At(float radius, float y, int k) => new(radius * MathF.Cos(k * MathF.PI / 6), y, radius * MathF.Sin(k * MathF.PI / 6));
            var (a, b, c, d) = (At(radiusLow, 0, i), At(radiusLow, 0, i + 1), At(radiusHigh, 0.37f, i + 1), At(radiusHigh, 0.37f, i));
            foreach (var (p, q, r) in new[] { (a, b, c), (a, c, d) })
            {
                var radial = ((p + q + r) / 3) with { Y = 0 };
                var faces = Vector3.Dot(Vector3.Cross(q - p, r - p), radial) > 0 == outward;
                corners.AddRange(faces ? [p, q, r] : [p, r, q]);
                if (outward) outer += Vector3.Cross(q - p, r - p).Length() / 2;
            }
        }
        for (int i = 0; i < 12; i++)
        {
            Panel(0.16f, 0.14f, i, true);
            Panel(0.157f, 0.137f, i, false);
        }
        Vector3[] points = [.. corners];
        var box = new FieldBox(points.Aggregate(Vector3.Min), points.Aggregate(Vector3.Max));
        var size = box.Max - box.Min;
        var sides = 2 * size.Y * size.Z + 2 * size.X * size.Y;

        var part = GlowLights.PartsOf(points, box, GlowLights.ShareOf(points, box)).Should().ContainSingle().Subject;

        part.Faces.Should().Be(1 << 0 | 1 << 1 | 1 << 4 | 1 << 5, "the glass sends its light out of the box's four sides");
        part.Share.Should().BeApproximately(outer / sides, 1e-3f, "at the outer panels' surface, the inner ones facing in");
    }
}
