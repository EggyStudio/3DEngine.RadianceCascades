using System.Numerics;

namespace RadianceCascades;

/// <summary>A box along the axes.</summary>
/// <param name="Min">Its least corner.</param>
/// <param name="Max">Its greatest corner.</param>
public readonly record struct FieldBox(Vector3 Min, Vector3 Max)
{
    /// <summary>
    /// Whether this box and <paramref name="other"/> share a point, a face touching among them.
    /// </summary>
    public bool Overlaps(FieldBox other) => Min.X <= other.Max.X && Max.X >= other.Min.X && Min.Y <= other.Max.Y && Max.Y >= other.Min.Y
                                            && Min.Z <= other.Max.Z && Max.Z >= other.Min.Z;

    /// <summary>The box grown by <paramref name="by"/> on every side.</summary>
    public FieldBox Grown(float by) => new(Min - new Vector3(by), Max + new Vector3(by));

    /// <summary>The box around this one and <paramref name="other"/>.</summary>
    public FieldBox With(FieldBox other) => new(Vector3.Min(Min, other.Min), Vector3.Max(Max, other.Max));

    /// <summary>
    /// The box around this one's eight corners moved by <paramref name="world"/>.
    /// </summary>
    public FieldBox Transformed(Matrix4x4 world)
    {
        // Moved without a perspective, its middle moved and its half extents taken along each
        // axis of the world by the matrix's sizes, as the corners give it.
        if (world.M14 == 0 && world.M24 == 0 && world.M34 == 0 && world.M44 == 1)
        {
            var (middle, half) = (Vector3.Transform((Min + Max) * 0.5f, world), (Max - Min) * 0.5f);
            var extents = new Vector3(
                MathF.Abs(world.M11) * half.X + MathF.Abs(world.M21) * half.Y + MathF.Abs(world.M31) * half.Z,
                MathF.Abs(world.M12) * half.X + MathF.Abs(world.M22) * half.Y + MathF.Abs(world.M32) * half.Z,
                MathF.Abs(world.M13) * half.X + MathF.Abs(world.M23) * half.Y + MathF.Abs(world.M33) * half.Z);
            return new FieldBox(middle - extents, middle + extents);
        }
        var (min, max) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
        for (int c = 0; c < 8; c++)
        {
            var corner = Vector3.Transform(new Vector3((c & 1) == 0 ? Min.X : Max.X, (c & 2) == 0 ? Min.Y : Max.Y, (c & 4) == 0 ? Min.Z : Max.Z), world);
            (min, max) = (Vector3.Min(min, corner), Vector3.Max(max, corner));
        }
        return new FieldBox(min, max);
    }

    /// <summary>
    /// The box around <paramref name="positions"/>, or a box of no size at the origin for none.
    /// </summary>
    public static FieldBox Of(ReadOnlySpan<Vector3> positions)
    {
        if (positions.IsEmpty) return new FieldBox(Vector3.Zero, Vector3.Zero);
        var (min, max) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
        foreach (var position in positions) (min, max) = (Vector3.Min(min, position), Vector3.Max(max, position));
        return new FieldBox(min, max);
    }
}

/// <summary>
/// A part of a mesh stamped as a box of its own, the box around the part's vertices at rest in the
/// mesh's own space, and where the part's pose moves that space to, a joint's matrix for a skinned
/// mesh's limb and none for a part of a mesh that does not bend.
/// </summary>
/// <param name="Rest">The box around the part's vertices at rest, in the mesh's own space.</param>
/// <param name="Pose">Where the part's pose moves the mesh's space to, identity for none.</param>
public readonly record struct FieldPart(FieldBox Rest, Matrix4x4 Pose);

/// <summary>
/// The cells of a cascade from the first to the last along each axis, both within.
/// </summary>
/// <param name="X0">The first cell along x.</param>
/// <param name="Y0">The first cell along y.</param>
/// <param name="Z0">The first cell along z.</param>
/// <param name="X1">The last cell along x.</param>
/// <param name="Y1">The last cell along y.</param>
/// <param name="Z1">The last cell along z.</param>
public readonly record struct FieldCells(int X0, int Y0, int Z0, int X1, int Y1, int Z1)
{
    /// <summary>Whether every cell of <paramref name="other"/> is one of these.</summary>
    public bool Holds(FieldCells other) => X0 <= other.X0 && Y0 <= other.Y0 && Z0 <= other.Z0 && X1 >= other.X1 && Y1 >= other.Y1 && Z1 >= other.Z1;

    /// <summary>How many cells.</summary>
    public int Count => (X1 - X0 + 1) * (Y1 - Y0 + 1) * (Z1 - Z0 + 1);

    /// <summary>The cells around these and <paramref name="other"/>.</summary>
    public FieldCells With(FieldCells other) => new(Math.Min(X0, other.X0), Math.Min(Y0, other.Y0), Math.Min(Z0, other.Z0),
        Math.Max(X1, other.X1), Math.Max(Y1, other.Y1), Math.Max(Z1, other.Z1));
}

/// <summary>
/// A mesh stamped as a box, from its own space to the world, the box's middle and half extents
/// there, the least scale from there to the world, the mesh's color in linear light, the share of
/// light that passes a unit of its depth, its emission, linear, none where the engine carries the
/// mesh as a glow light, its gloss, and the share of its diffuse light it sends through.
/// </summary>
/// <param name="World">From the box's own space to the world.</param>
/// <param name="Center">The box's middle in its own space.</param>
/// <param name="Extents">The box's half extents in its own space.</param>
/// <param name="Scale">The least scale from its own space to the world.</param>
/// <param name="Color">The mesh's color, linear.</param>
/// <param name="LightThrough">The share of light that passes a unit of its depth.</param>
/// <param name="Emission">The mesh's emission, linear.</param>
/// <param name="Gloss">The mesh's perceptual roughness, metallic share and reflectance.</param>
/// <param name="Transmission">
/// The share of its diffuse light the mesh sends through to its far side.
/// </param>
public readonly record struct FieldShape(Matrix4x4 World, Vector3 Center, Vector3 Extents, float Scale, Vector3 Color = default, float LightThrough = 0,
    Vector3 Emission = default, Vector3 Gloss = default, float Transmission = 0)
{
    /// <summary>
    /// From the world into its own space, or nothing where its matrix has no inverse.
    /// </summary>
    public Matrix4x4 ToOwn => Matrix4x4.Invert(World, out var toOwn) ? toOwn : default;

    /// <summary>Whether a matrix has an inverse, which its translation leaves as it is.</summary>
    public static bool Turns(Matrix4x4 world) => Matrix4x4.Invert(world with { M41 = 0, M42 = 0, M43 = 0 }, out _);

    /// <summary>The floats a shape takes as the stamp reads it, eight groups of four.</summary>
    public const int StampFloats = 32;

    /// <summary>
    /// Writes the shape as the library's stamp reads it (<c>StampShape</c> in
    /// <c>rc_field_stamp.slang</c>) into <paramref name="into"/>, <see cref="StampFloats"/> floats:
    /// the three rows of the matrix from the world into its own space, its middle and least scale,
    /// its half extents, its color and the share of light that passes a unit of its depth, its
    /// emission and the share of its diffuse light it sends through, and its gloss.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="into"/> is shorter than
    /// <see cref="StampFloats"/>.</exception>
    public void WriteStamp(Span<float> into)
    {
        if (into.Length < StampFloats)
            throw new ArgumentException($"A shape takes {StampFloats} floats, and {into.Length} were given.", nameof(into));
        var t = ToOwn;
        // A point goes into the shape's own space as a row vector times the matrix, so each row the
        // stamp takes a dot product with is a column of it.
        ReadOnlySpan<float> floats =
        [
            t.M11, t.M21, t.M31, t.M41,
            t.M12, t.M22, t.M32, t.M42,
            t.M13, t.M23, t.M33, t.M43,
            Center.X, Center.Y, Center.Z, Scale,
            Extents.X, Extents.Y, Extents.Z, 0,
            Color.X, Color.Y, Color.Z, LightThrough,
            Emission.X, Emission.Y, Emission.Z, Transmission,
            Gloss.X, Gloss.Y, Gloss.Z, 0,
        ];
        floats.CopyTo(into);
    }
}
