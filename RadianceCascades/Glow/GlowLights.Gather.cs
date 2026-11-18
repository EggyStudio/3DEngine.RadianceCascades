using System.Numerics;

namespace RadianceCascades;

public static partial class GlowLights
{
    /// <summary>
    /// The lights the carried emitters among <paramref name="drawn"/> make, one for each, or, for
    /// each group of boxes square to the world's axes that touch and emit the same light, one for
    /// the group, its box around them all and its share of that box's surface what of theirs no
    /// other covers.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Four emissive blocks stacked two by two would otherwise be four lights, each marched to
    /// through the others and each emitting from the faces the others cover, so the stack would
    /// throw a ring of light that comes and goes around it. As one light of a box two blocks wide
    /// its light is the stack's.
    /// </para>
    /// <para>
    /// The boxes are moved by each world matrix with its translation taken against
    /// <paramref name="origin"/> in doubles, so a box far from the world's origin keeps its size
    /// and its touching, where its two corners moved into the world in floats each round to the
    /// step there. Each light's <see cref="GlowLight.Key"/> is its middle from the world matrices
    /// as they are.
    /// </para>
    /// </remarks>
    /// <typeparam name="TMesh">
    /// What the engine knows a mesh's vertices by, as its plan does.
    /// </typeparam>
    /// <typeparam name="TTag">The engine's own of each draw, as its plan carries it.</typeparam>
    /// <param name="drawn">The meshes the field gathered this frame.</param>
    /// <param name="firstCell">
    /// The width of a cell of the field's first cascade, which says which emitters are carried.
    /// </param>
    /// <param name="boxOf">The box around a mesh's vertices in its own space.</param>
    /// <param name="shareOf">A mesh's surface over its box's, 1 for a box.</param>
    /// <param name="partsOf">
    /// The parts a mesh is cut into (<see cref="PartsOf"/>), or null to take every mesh whole.
    /// </param>
    /// <param name="origin">The frame's origin, which the boxes are gathered against.</param>
    public static List<GlowLight> Gather<TMesh, TTag>(IReadOnlyList<(SceneFieldPlan<TMesh, TTag>.Instance Instance, bool Skinned)> drawn, float firstCell,
        Func<TMesh, FieldBox> boxOf, Func<TMesh, float> shareOf, Func<TMesh, IReadOnlyList<GlowPart>>? partsOf = null, FrameOrigin origin = default)
        where TMesh : notnull where TTag : IEquatable<TTag>
    {
        var lights = new List<GlowLight>();
        var square = new List<(FieldBox Box, FieldBox World, Vector3 Glow, float Surface, float Share, int Faces)>();
        void Add(FieldBox box, Matrix4x4 w, Vector3 glow, float share, int faces = 0)
        {
            // A flat emitter, such as a single face showing above the ground around it, is a box a
            // hundredth of its size thick about its face, which lights each side by the face's
            // whole surface. A box of no thickness would leave its faces along its thin side out of
            // its light and count its surface on both sides at half, and the light on the picture
            // would draw the face itself dark where the screen's probes read it.
            var size = box.Max - box.Min;
            var largest = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
            var least = 0.01f * largest;
            if (largest > 0 && MathF.Min(size.X, MathF.Min(size.Y, size.Z)) < least)
            {
                var grow = Vector3.Max(Vector3.Zero, new Vector3(least) - size) / 2;
                box = new FieldBox(box.Min - grow, box.Max + grow);
                // Its two sides emit, and the edges the hundredth gives it are no faces of its own.
                if (faces == 0)
                {
                    share = MathF.Min(1, share * 2);
                    faces = 3 << (2 * (size.X <= size.Y && size.X <= size.Z ? 0 : size.Y <= size.Z ? 1 : 2));
                }
            }
            var placed = origin == FrameOrigin.Zero ? w : origin.Against(w);
            if (Square(w))
            {
                var (moved, world) = (box.Transformed(placed), box.Transformed(w));
                // A side of the box a negative scale turns to the other way names the other face.
                for (int k = 0; k < 3; k++)
                    if ((k == 0 ? w.M11 : k == 1 ? w.M22 : w.M33) < 0 && faces != 0)
                        faces = (faces & ~(3 << (2 * k))) | (((faces >> (2 * k)) & 1) << (2 * k + 1)) | (((faces >> (2 * k + 1)) & 1) << (2 * k));
                square.Add((moved, world, glow, FacesSurface(moved.Max - moved.Min, faces == 0 ? All : faces) * share, share, faces));
                return;
            }
            var (half, middle) = ((box.Max - box.Min) / 2, (box.Min + box.Max) / 2);
            lights.Add(new GlowLight(Vector3.Transform(middle, placed), new Vector3(w.M11, w.M12, w.M13) * half.X,
                new Vector3(w.M21, w.M22, w.M23) * half.Y, new Vector3(w.M31, w.M32, w.M33) * half.Z, glow, share, faces) { Key = Vector3.Transform(middle, w) });
        }
        // A copy recorded twice, as a stereo pair's second eye or a scene drawn into two targets
        // records it, is one emitter.
        HashSet<(TMesh Vertices, Matrix4x4 World, Vector3 Emission, bool DoubleSided)>? taken = null;
        foreach (var (instance, _) in drawn)
        {
            if (instance.Emission is { X: <= 0, Y: <= 0, Z: <= 0 }) continue;
            if (!(taken ??= []).Add((instance.Vertices, instance.World, instance.Emission, instance.DoubleSided))) continue;
            var w = instance.World;
            if (partsOf?.Invoke(instance.Vertices) is { Count: > 0 } parts)
            {
                if (!Carries(instance.Emission, parts, w, firstCell)) continue;
                foreach (var part in parts)
                {
                    // A mesh drawn from both sides emits from the faces behind its triangles too.
                    var (share, faces) = (part.Share, part.Faces);
                    if (instance.DoubleSided && faces != 0)
                    {
                        var both = faces | Mirrored(faces);
                        var size = part.Box.Max - part.Box.Min;
                        share = MathF.Min(1, share * 2 * FacesSurface(size, faces) / MathF.Max(FacesSurface(size, both), 1e-12f));
                        faces = both;
                    }
                    Add(part.Box, w, instance.Emission, share, faces);
                }
                continue;
            }
            var box = boxOf(instance.Vertices);
            if (!Carries(instance.Emission, box, w, firstCell)) continue;
            Add(box, w, instance.Emission, shareOf(instance.Vertices));
        }

        // The boxes that touch, found through a grid of the largest box's size, and joined.
        var parent = Enumerable.Range(0, square.Count).ToArray();
        int Root(int i) { while (parent[i] != i) i = parent[i] = parent[parent[i]]; return i; }
        var touching = new List<(int A, int B)>();
        if (square.Count > 1)
        {
            var size = square.Max(s => MathF.Max(s.Box.Max.X - s.Box.Min.X, MathF.Max(s.Box.Max.Y - s.Box.Min.Y, s.Box.Max.Z - s.Box.Min.Z)));
            size = MathF.Max(size, 1e-4f);
            var grid = new Dictionary<(int, int, int), List<int>>();
            (int, int, int) Key(Vector3 p) => ((int)MathF.Floor(p.X / size), (int)MathF.Floor(p.Y / size), (int)MathF.Floor(p.Z / size));
            for (int i = 0; i < square.Count; i++)
            {
                var key = Key(square[i].Box.Min);
                if (!grid.TryGetValue(key, out var list)) grid[key] = list = [];
                list.Add(i);
            }
            for (int i = 0; i < square.Count; i++)
            {
                var (kx, ky, kz) = Key(square[i].Box.Min);
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dz = -1; dz <= 1; dz++)
                            if (grid.TryGetValue((kx + dx, ky + dy, kz + dz), out var near))
                                foreach (var j in near)
                                    if (j > i && Touch(square[i].Box, square[j].Box) && Vector3.DistanceSquared(square[i].Glow, square[j].Glow) < 1e-8f)
                                    {
                                        touching.Add((i, j));
                                        parent[Root(i)] = Root(j);
                                    }
            }
        }
        var covered = new Dictionary<int, float>();
        foreach (var (a, b) in touching)
            covered[Root(a)] = covered.GetValueOrDefault(Root(a)) + 2 * Contact(square[a].Box, square[b].Box);
        foreach (var group in Enumerable.Range(0, square.Count).GroupBy(Root))
        {
            // A box alone keeps its faces and share.
            if (group.Count() == 1 && square[group.Key] is var one)
            {
                var halves = (one.Box.Max - one.Box.Min) / 2;
                lights.Add(new GlowLight((one.Box.Min + one.Box.Max) / 2, new Vector3(halves.X, 0, 0), new Vector3(0, halves.Y, 0), new Vector3(0, 0, halves.Z),
                    one.Glow, one.Share, one.Faces) { Key = (one.World.Min + one.World.Max) / 2 });
                continue;
            }
            var (min, max) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
            var (worldMin, worldMax) = (min, max);
            float surface = 0;
            foreach (var i in group)
            {
                (min, max) = (Vector3.Min(min, square[i].Box.Min), Vector3.Max(max, square[i].Box.Max));
                (worldMin, worldMax) = (Vector3.Min(worldMin, square[i].World.Min), Vector3.Max(worldMax, square[i].World.Max));
                surface += square[i].Surface;
            }
            // The union's faces that emit, each a member's that lies on the union's side there, so
            // the faces of a corner block handed face by face emit from the two that show and not
            // from the six of the box around them.
            var near = 1e-4f * MathF.Max((max - min).Length(), 1e-6f);
            var faces = 0;
            foreach (var i in group)
            {
                var (box, own) = (square[i].Box, square[i].Faces == 0 ? All : square[i].Faces);
                for (int k = 0; k < 3; k++)
                {
                    if (((own >> (2 * k)) & 1) != 0 && MathF.Abs(box.Min[k] - min[k]) <= near) faces |= 1 << (2 * k);
                    if (((own >> (2 * k + 1)) & 1) != 0 && MathF.Abs(box.Max[k] - max[k]) <= near) faces |= 1 << (2 * k + 1);
                }
            }
            if (faces == 0) faces = All;
            var share = Math.Clamp((surface - covered.GetValueOrDefault(group.Key)) / Math.Max(FacesSurface(max - min, faces), 1e-8f), 0.05f, 1);
            var half = (max - min) / 2;
            lights.Add(new GlowLight((min + max) / 2, new Vector3(half.X, 0, 0), new Vector3(0, half.Y, 0), new Vector3(0, 0, half.Z), square[group.Key].Glow, share,
                faces == All ? 0 : faces) { Key = (worldMin + worldMax) / 2 });
        }
        return lights;
    }

    // Whether a world matrix turns nothing, so a box it moves stays square to the axes.
    private static bool Square(Matrix4x4 w)
    {
        var scale = MathF.Max(MathF.Abs(w.M11), MathF.Max(MathF.Abs(w.M22), MathF.Abs(w.M33)));
        var turned = MathF.Abs(w.M12) + MathF.Abs(w.M13) + MathF.Abs(w.M21) + MathF.Abs(w.M23) + MathF.Abs(w.M31) + MathF.Abs(w.M32);
        return turned <= 1e-5f * MathF.Max(scale, 1e-6f);
    }

    // Whether two boxes touch or overlap, within a thousandth of the smaller one's size.
    private static bool Touch(FieldBox a, FieldBox b)
    {
        var slack = 1e-3f * MathF.Min((a.Max - a.Min).Length(), (b.Max - b.Min).Length());
        return a.Min.X <= b.Max.X + slack && b.Min.X <= a.Max.X + slack && a.Min.Y <= b.Max.Y + slack && b.Min.Y <= a.Max.Y + slack
            && a.Min.Z <= b.Max.Z + slack && b.Min.Z <= a.Max.Z + slack;
    }

    // The area of the faces two touching boxes press together, where one's side lies on the
    // other's along an axis, or none where they overlap or meet at an edge.
    private static float Contact(FieldBox a, FieldBox b)
    {
        var slack = 1e-3f * MathF.Min((a.Max - a.Min).Length(), (b.Max - b.Min).Length());
        var overlap = Vector3.Max(Vector3.Zero, Vector3.Min(a.Max, b.Max) - Vector3.Max(a.Min, b.Min));
        for (int axis = 0; axis < 3; axis++)
        {
            var meet = MathF.Abs(a.Max[axis] - b.Min[axis]) <= slack || MathF.Abs(b.Max[axis] - a.Min[axis]) <= slack;
            if (meet && overlap[axis] <= slack) return overlap[(axis + 1) % 3] * overlap[(axis + 2) % 3];
        }
        return 0;
    }
}
