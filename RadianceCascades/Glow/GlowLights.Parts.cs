using System.Numerics;

namespace RadianceCascades;

public static partial class GlowLights
{
    // Every face of a box, a bit a face, two an axis, the side toward the axis's way the higher.
    private const int All = 63;

    /// <summary>
    /// The parts of a mesh, its triangles joined where they share a corner, from its corners three
    /// a triangle in its own space, or the mesh whole, <paramref name="box"/> with
    /// <paramref name="share"/>, where its triangles are all joined or it has none.
    /// </summary>
    /// <remarks>
    /// A mesh may hold many small emitters apart from one another in a box as wide as the land,
    /// which taken whole is too large to be carried, so the ground beside each emitter would take
    /// its light through the screen probes' rays alone, in patches. Cut into parts, each group of
    /// triangles that share a corner is an emitter of its own. The cut reads every corner, so an
    /// engine keeps a mesh's parts while it is drawn rather than cutting it each frame.
    /// </remarks>
    /// <param name="corners">
    /// The corners of the mesh's triangles, three a triangle, in its own space, or null for none.
    /// </param>
    /// <param name="box">The box around the mesh's vertices in its own space.</param>
    /// <param name="share">
    /// The mesh's surface over its box's, which the mesh taken whole keeps.
    /// </param>
    public static GlowPart[] PartsOf(Vector3[]? corners, FieldBox box, float share)
    {
        GlowPart[] whole = [new GlowPart(box, share)];
        if (corners is not { Length: >= 6 }) return whole;
        var triangles = corners.Length / 3;
        var parent = new int[triangles];
        for (int i = 0; i < triangles; i++) parent[i] = i;
        int Root(int i) { while (parent[i] != i) i = parent[i] = parent[parent[i]]; return i; }
        // Corners are the same where they lie within a hundred thousandth of the mesh's size.
        var size = MathF.Max((box.Max - box.Min).Length(), 1e-6f);
        var scale = 1e5f / size;
        var first = new Dictionary<(int, int, int), int>();
        for (int c = 0; c < triangles * 3; c++)
        {
            var p = (corners[c] - box.Min) * scale;
            var key = ((int)MathF.Round(p.X), (int)MathF.Round(p.Y), (int)MathF.Round(p.Z));
            if (first.TryGetValue(key, out var other)) parent[Root(c / 3)] = Root(other);
            else first[key] = c / 3;
        }
        var groups = new Dictionary<int, (Vector3 Min, Vector3 Max, double Area)>();
        for (int t = 0; t < triangles; t++)
        {
            var (a, b, c) = (corners[t * 3], corners[t * 3 + 1], corners[t * 3 + 2]);
            var root = Root(t);
            var (min, max, area) = groups.TryGetValue(root, out var group) ? group : (new Vector3(float.MaxValue), new Vector3(float.MinValue), 0);
            groups[root] = (Vector3.Min(min, Vector3.Min(a, Vector3.Min(b, c))), Vector3.Max(max, Vector3.Max(a, Vector3.Max(b, c))), area);
        }
        // The surface each part sends its light out of its box from, its triangles facing away from
        // the box's middle, as a lantern's glass of two layers sends out from the outer alone, the
        // inner facing in and its light kept in the glass, as a path-traced reference keeps it.
        var outward = new Dictionary<int, double>();
        for (int t = 0; t < triangles; t++)
        {
            var (a, b, c) = (corners[t * 3], corners[t * 3 + 1], corners[t * 3 + 2]);
            var root = Root(t);
            var (min, max, _) = groups[root];
            var normal = Vector3.Cross(b - a, c - a);
            if (Vector3.Dot(normal, (a + b + c) / 3 - (min + max) / 2) >= 0)
                outward[root] = outward.GetValueOrDefault(root) + normal.Length() / 2;
        }
        foreach (var root in outward.Keys)
            groups[root] = groups[root] with { Area = outward[root] };
        // A mesh of one piece with depth is one part whose faces its triangles name, as two faces
        // of a block handed as one mesh emit from those two and not from the box's six. A flat one
        // is taken whole, lighting both its sides, as a path-traced reference takes a triangle's
        // emission from either side.
        if (groups.Count == 1 && groups.Values.First() is var (lowest, highest, _) && highest - lowest is var extent
            && MathF.Min(extent.X, MathF.Min(extent.Y, extent.Z)) < 0.01f * MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z)))
            return whole;

        // Which faces of its box each part's triangles send their light out of, each triangle
        // facing out of the box naming the face its normal leans toward most, so a lantern's
        // upright panels name its four sides and not its top and bottom, which send nothing, and a
        // triangle facing into the box naming none.
        var faces = new Dictionary<int, int>();
        for (int t = 0; t < triangles; t++)
        {
            var root = Root(t);
            var (a, b, c) = (corners[t * 3], corners[t * 3 + 1], corners[t * 3 + 2]);
            var normal = Vector3.Cross(b - a, c - a);
            var (min, max, _) = groups[root];
            if (normal.LengthSquared() <= 1e-24f || Vector3.Dot(normal, (a + b + c) / 3 - (min + max) / 2) < 0) continue;
            var k = MathF.Abs(normal.X) >= MathF.Abs(normal.Y) && MathF.Abs(normal.X) >= MathF.Abs(normal.Z) ? 0 : MathF.Abs(normal.Y) >= MathF.Abs(normal.Z) ? 1 : 2;
            faces[root] = faces.GetValueOrDefault(root) | 1 << (2 * k + (normal[k] > 0 ? 1 : 0));
        }
        // A part whose triangles all face in, as a lantern's inner layer lying apart from its
        // outer, sends nothing out of its box and is no light of its own, unless every part faces
        // in.
        var sending = groups.Where(g => outward.ContainsKey(g.Key)).ToList();
        return [.. (sending.Count > 0 ? sending : [.. groups]).Select(g =>
        {
            var (min, max, area) = g.Value;
            var box = new FieldBox(min, max);
            var mask = faces.GetValueOrDefault(g.Key, All);
            var surface = FacesSurface(max - min, mask);
            return new GlowPart(box, surface <= 1e-12f ? 1 : (float)Math.Clamp(area / surface, 0.05, 1), mask == All ? 0 : mask);
        })];
    }

    /// <summary>
    /// A mesh's surface over its box's, from its triangles' corners three a triangle in its own
    /// space, 1 where it has none.
    /// </summary>
    public static float ShareOf(Vector3[]? corners, FieldBox box)
    {
        var size = box.Max - box.Min;
        var boxSurface = 2 * (size.X * size.Y + size.Y * size.Z + size.Z * size.X);
        if (corners is not { Length: >= 3 } || boxSurface <= 1e-12f) return 1;
        double area = 0;
        for (int i = 0; i + 2 < corners.Length; i += 3)
            area += Vector3.Cross(corners[i + 1] - corners[i], corners[i + 2] - corners[i]).Length() / 2;
        return (float)Math.Clamp(area / boxSurface, 0.05, 1);
    }

    /// <summary>
    /// A mesh's surface over its box's, from the area of its triangles in its own space, 1 where
    /// that is not known, 0 or less.
    /// </summary>
    /// <remarks>
    /// For an engine that has a mesh's area and not its corners, and so takes every mesh whole. It
    /// gives what <see cref="ShareOf(Vector3[], FieldBox)"/> gives of corners whose triangles make
    /// that area, but for an area of none, which it takes as one not known.
    /// </remarks>
    public static float ShareOf(double area, FieldBox box)
    {
        var size = box.Max - box.Min;
        var boxSurface = 2 * (size.X * size.Y + size.Y * size.Z + size.Z * size.X);
        if (area <= 0 || boxSurface <= 1e-12f) return 1;
        return (float)Math.Clamp(area / boxSurface, 0.05, 1);
    }

    // The surface of the faces of a box of this size the bits name.
    private static float FacesSurface(Vector3 size, int faces)
    {
        float surface = 0;
        for (int k = 0; k < 3; k++)
        {
            var face = size[(k + 1) % 3] * size[(k + 2) % 3];
            surface += ((faces >> (2 * k)) & 1) * face + ((faces >> (2 * k + 1)) & 1) * face;
        }
        return surface;
    }

    // The faces the bits name seen from the other side of the box, as a mesh drawn from both sides
    // emits.
    private static int Mirrored(int faces) => ((faces & 0b010101) << 1) | ((faces & 0b101010) >> 1);
}
