using System.Numerics;

namespace RadianceCascades;

/// <summary>
/// The place a frame's places are taken against before the GPU sees them, in doubles, so a light
/// far from the world's origin stands in numbers as small as its distance from this place, and a
/// float's step there is not the step between neighboring points.
/// </summary>
/// <remarks>
/// An engine that takes every place in world numbers gives <see cref="Zero"/>, against which every
/// place is as given.
/// </remarks>
/// <param name="X">The origin along x.</param>
/// <param name="Y">The origin along y.</param>
/// <param name="Z">The origin along z.</param>
public readonly record struct FrameOrigin(double X, double Y, double Z)
{
    /// <summary>The world's own origin, against which every place is as given.</summary>
    public static FrameOrigin Zero => default;

    /// <summary>The place given less the origin, worked in doubles.</summary>
    public Vector3 Against(Vector3 place) => new((float)(place.X - X), (float)(place.Y - Y), (float)(place.Z - Z));

    /// <summary>
    /// A world matrix with its translation taken against the origin, worked in doubles.
    /// </summary>
    public Matrix4x4 Against(Matrix4x4 world) => world with
    {
        M41 = (float)(world.M41 - X), M42 = (float)(world.M42 - Y), M43 = (float)(world.M43 - Z),
    };
}
