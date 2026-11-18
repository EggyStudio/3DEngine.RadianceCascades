using System.Numerics;

namespace RadianceCascades;

/// <summary>
/// A part of an emitter's mesh, the box around its triangles in the mesh's space, its surface over
/// the box's faces it emits from, and which of those faces its triangles lie on.
/// </summary>
/// <param name="Box">The box around the part's triangles in the mesh's own space.</param>
/// <param name="Share">The part's surface over that of the faces of its box it emits from.</param>
/// <param name="Faces">
/// The faces of its box it emits from, as <see cref="GlowLight.Faces"/> names them, or 0 for every
/// face.
/// </param>
public readonly record struct GlowPart(FieldBox Box, float Share, int Faces = 0);

/// <summary>
/// A light as it is written, the middle of its box, half its side along each of the box's axes,
/// its light, and its surface over that of the faces of its box it emits from.
/// </summary>
/// <remarks>
/// <see cref="Faces"/> names which faces of its box emit, a bit a face, two an axis of
/// <see cref="X"/>, <see cref="Y"/> and <see cref="Z"/>, the side the axis points to the higher,
/// or 0 for every face, its surface spread over them by <see cref="Share"/>, so a box that emits
/// from its top and one side lights the ground before that side by the whole of the side's light,
/// where its surface spread over all six faces would give the ground a third of it.
/// </remarks>
/// <param name="Middle">
/// The middle of its box, against the origin the lights were gathered against.
/// </param>
/// <param name="X">Half its box's side along the box's first axis.</param>
/// <param name="Y">Half its box's side along the box's second axis.</param>
/// <param name="Z">Half its box's side along the box's third axis.</param>
/// <param name="Glow">Its light, the emission of the meshes it is made of, linear.</param>
/// <param name="Share">Its surface over that of the faces of its box it emits from.</param>
/// <param name="Faces">The faces of its box it emits from, or 0 for every face.</param>
public readonly record struct GlowLight(Vector3 Middle, Vector3 X, Vector3 Y, Vector3 Z, Vector3 Glow, float Share, int Faces = 0)
{
    /// <summary>
    /// Its middle in world numbers, in floats, worked from the world matrices as they are, which
    /// the lights are known by from frame to frame (<see cref="GlowPlaces"/>), ranked by and
    /// written with, the same wherever the frame's origin stands, where <see cref="Middle"/> takes
    /// a place against the origin the lights were gathered against.
    /// </summary>
    public Vector3 Key { get; init; } = Middle;
}
