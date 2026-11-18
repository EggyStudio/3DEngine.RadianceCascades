using System.Numerics;

namespace RadianceCascades;

/// <summary>
/// The triangles the field's builds read, laid out as the splat reads them, with the open edges a
/// mesh's corners carry and the parts a mesh that does not bend is stamped as.
/// </summary>
/// <remarks>
/// A triangle is 48 bytes, a uint4 a corner, x, y and z the bits of the corner's place in the
/// mesh's own space and w its vertex's color, three sRGB bytes, red lowest, with a bit at 24 set
/// where the edge from that corner to the triangle's next is open, no other triangle's, which the
/// splat reads to keep a cell beyond the edge in front. The buffer the engine keeps them in, how it
/// grows and where it lies in memory, are the engine's own.
/// </remarks>
public static class FieldTriangles
{
    /// <summary>The uints a triangle takes, four a corner.</summary>
    public const int Uints = 12;

    /// <summary>The color a corner takes where its mesh has none, white in sRGB bytes.</summary>
    public const uint White = 0xFFFFFFu;

    /// <summary>
    /// Writes the corners of the triangles <paramref name="indices"/> makes of
    /// <paramref name="positions"/> into <paramref name="into"/>, <see cref="Uints"/> a triangle,
    /// each corner's color from <paramref name="colors"/> by its vertex, three sRGB bytes red
    /// lowest, or <see cref="White"/> where it is empty, and the open edges <see cref="OpenEdges"/>
    /// finds.
    /// </summary>
    /// <returns>How many triangles were written.</returns>
    public static int Write(Span<uint> into, ReadOnlySpan<Vector3> positions, ReadOnlySpan<uint> indices, ReadOnlySpan<uint> colors)
    {
        var count = indices.Length / 3;
        if (into.Length < count * Uints)
            throw new ArgumentException($"{count} triangles take {count * Uints} uints, and {into.Length} were given.", nameof(into));
        var open = OpenEdges(positions, indices);
        for (int i = 0; i < count * 3; i++)
        {
            var at = i * 4;
            var position = positions[(int)indices[i]];
            (into[at], into[at + 1], into[at + 2]) =
                (BitConverter.SingleToUInt32Bits(position.X), BitConverter.SingleToUInt32Bits(position.Y), BitConverter.SingleToUInt32Bits(position.Z));
            var color = colors.IsEmpty ? White : colors[(int)indices[i]] & White;
            into[at + 3] = color | (open[i] ? 1u << 24 : 0u);
        }
        return count;
    }

    /// <summary>
    /// Whether each edge of a mesh's triangles is open, its edge from each corner to the next,
    /// which no other triangle shares by the places of its ends, so a mesh whose faces keep
    /// vertices of their own, as a cube's do, is closed where its faces meet and a ground plane
    /// open at its rim.
    /// </summary>
    /// <remarks>
    /// The ends are compared by their values, so a corner at -0 and one at +0 are one place.
    /// </remarks>
    public static bool[] OpenEdges(ReadOnlySpan<Vector3> positions, ReadOnlySpan<uint> indices)
    {
        var count = indices.Length / 3 * 3;
        var edges = new (Vector3, Vector3)[count];
        var shared = new Dictionary<(Vector3, Vector3), int>(count);
        for (int i = 0; i < count; i++)
        {
            var (a, b) = (positions[(int)indices[i]], positions[(int)indices[i / 3 * 3 + (i % 3 + 1) % 3]]);
            var edge = a.X < b.X || a.X == b.X && (a.Y < b.Y || a.Y == b.Y && a.Z <= b.Z) ? (a, b) : (b, a);
            edges[i] = edge;
            shared[edge] = shared.GetValueOrDefault(edge) + 1;
        }
        var open = new bool[count];
        for (int i = 0; i < count; i++) open[i] = shared[edges[i]] == 1;
        return open;
    }

    /// <summary>
    /// How many triangles a pool is made for, twice <paramref name="wanted"/>, those of the build
    /// in progress, rounded up to a power of two, so its later parts fit, or where the device has
    /// less than twice that free, as many as half what it has free holds and those of this frame's
    /// meshes, <paramref name="needed"/>, at least.
    /// </summary>
    /// <param name="wanted">The triangles of the build in progress.</param>
    /// <param name="needed">The triangles of this frame's meshes.</param>
    /// <param name="free">The bytes the device has free, or null where it does not say.</param>
    public static int PoolCapacity(int wanted, int needed, ulong? free)
    {
        var whole = Math.Max(1024, (int)BitOperations.RoundUpToPowerOf2((uint)Math.Max(1, wanted * 2)));
        if (free is not { } bytes || (ulong)whole * Uints * 4 * 2 <= bytes) return whole;
        return Math.Max(Math.Max(1024, needed), (int)Math.Min(wanted, (long)(bytes / 2 / (Uints * 4))));
    }

    /// <summary>
    /// A mesh that does not bend cut by its triangles into up to eight parts, each the box around
    /// its triangles, so a moving table or car is stamped as its shape rather than the box around
    /// all of it, or null for a mesh of fewer than 16 triangles, as a box or a slab, which is its
    /// one box.
    /// </summary>
    /// <remarks>
    /// Each cut is the one along an axis, between the triangles sorted by their middles, that
    /// leaves the two boxes around them the least volume, as a bounding volume hierarchy is built
    /// by the volume it encloses, and is made only where it takes away a third of the box's volume
    /// or more, so a box cut finer than its shape stays one box rather than a shell of thin slabs.
    /// </remarks>
    public static FieldPart[]? Cut(ReadOnlySpan<Vector3> positions, ReadOnlySpan<uint> indices)
    {
        if (indices.Length < 3 * 16) return null;
        var count = indices.Length / 3;
        var (lows, highs, middles) = (new Vector3[count], new Vector3[count], new Vector3[count]);
        for (int t = 0; t < count; t++)
        {
            var (a, b, c) = (positions[(int)indices[t * 3]], positions[(int)indices[t * 3 + 1]], positions[(int)indices[t * 3 + 2]]);
            (lows[t], highs[t], middles[t]) = (Vector3.Min(a, Vector3.Min(b, c)), Vector3.Max(a, Vector3.Max(b, c)), (a + b + c) / 3);
        }
        static float Volume(Vector3 low, Vector3 high) => MathF.Max(0, high.X - low.X) * MathF.Max(0, high.Y - low.Y) * MathF.Max(0, high.Z - low.Z);

        var parts = new List<FieldPart>();
        void Halve(int[] triangles, int depth)
        {
            var (low, high) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
            foreach (var t in triangles) (low, high) = (Vector3.Min(low, lows[t]), Vector3.Max(high, highs[t]));
            var (best, cut, sorted) = (0.67f * Volume(low, high), -1, Array.Empty<int>());
            for (int axis = 0; depth > 0 && triangles.Length >= 8 && axis < 3; axis++)
            {
                var order = SortedAlong(triangles, middles, axis);
                // The boxes around the triangles before each place and from it on.
                var (beforeLow, beforeHigh) = (new Vector3[order.Length], new Vector3[order.Length]);
                var (runLow, runHigh) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
                for (int i = 0; i < order.Length; i++)
                    (beforeLow[i], beforeHigh[i]) = (runLow, runHigh) = (Vector3.Min(runLow, lows[order[i]]), Vector3.Max(runHigh, highs[order[i]]));
                (runLow, runHigh) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
                for (int i = order.Length - 1; i >= 4; i--)
                {
                    (runLow, runHigh) = (Vector3.Min(runLow, lows[order[i]]), Vector3.Max(runHigh, highs[order[i]]));
                    if (order.Length - i < 4) continue;
                    var cost = Volume(beforeLow[i - 1], beforeHigh[i - 1]) + Volume(runLow, runHigh);
                    if (cost < best) (best, cut, sorted) = (cost, i, order);
                }
            }
            if (cut < 0)
            {
                parts.Add(new FieldPart(new FieldBox(low, high), Matrix4x4.Identity));
                return;
            }
            Halve(sorted[..cut], depth - 1);
            Halve(sorted[cut..], depth - 1);
        }
        Halve([.. Enumerable.Range(0, count)], 3);
        return parts.Count > 1 ? [.. parts] : null;
    }

    // The triangles sorted by their middles along an axis, those as far along in the order given,
    // as a stable sort leaves them, each keyed by its middle's bits made to order as the floats do
    // above its place, rather than by LINQ's OrderBy, which is too slow to cut the many meshes that
    // come back into the field in one frame, as a streamed world's sections do at a crossing.
    private static int[] SortedAlong(int[] triangles, Vector3[] middles, int axis)
    {
        var keyed = new ulong[triangles.Length];
        for (int i = 0; i < triangles.Length; i++)
        {
            var bits = BitConverter.SingleToUInt32Bits(middles[triangles[i]][axis] + 0f);
            keyed[i] = (ulong)((bits & 0x80000000u) != 0 ? ~bits : bits | 0x80000000u) << 32 | (uint)i;
        }
        Array.Sort(keyed);
        var order = new int[triangles.Length];
        for (int i = 0; i < order.Length; i++) order[i] = triangles[(int)(keyed[i] & 0xFFFFFFFFu)];
        return order;
    }
}
