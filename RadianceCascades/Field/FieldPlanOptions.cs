namespace RadianceCascades;

/// <summary>
/// How the field's plan places its cascades, stamps what has not settled, builds and settles a
/// mesh painted again, each setting at a default an engine may leave.
/// </summary>
public sealed record FieldPlanOptions
{
    /// <summary>
    /// The share of its width a cascade's middle lies ahead of the eye along the way it looks, its
    /// corner snapped down and stepped clear of the eye, or 0 for a cascade around the eye on the
    /// snap nearest it.
    /// </summary>
    /// <remarks>
    /// A cascade ahead holds more of what the eye sees, and a turn of the eye carries it to the
    /// other side and builds it again whole, where a cascade around the eye builds nothing again
    /// for a turn.
    /// </remarks>
    public float Lead { get; init; }

    /// <summary>
    /// Whether a mesh that came and has not moved, and a still one waiting for its cascades, are
    /// stamped as boxes until they are built, where by default only a mesh that moved is.
    /// </summary>
    /// <remarks>
    /// Stamped, a mesh that came stands in the field from its first frame, and a box stands in
    /// badly for a mesh larger than what it holds, as the box around a building's outer walls fills
    /// the building.
    /// </remarks>
    public bool StampArrivals { get; init; }

    /// <summary>
    /// Whether every build is of a whole cascade, where by default a cascade the eye moved by whole
    /// cells shifts and what came or went within a built cascade is built as a region of it.
    /// </summary>
    /// <remarks>
    /// Whole builds take field kernels that build no region and shift no cascade, and every move
    /// of the eye past a snap or change within a cascade builds the cascade again from every still
    /// mesh in its band.
    /// </remarks>
    public bool WholeBuilds { get; init; }

    /// <summary>
    /// Whether a still mesh painted again where it stands is still at once only where it had stood
    /// still for <see cref="SceneFieldPlan{TMesh, TTag}.SettleFrames"/> frames since it settled,
    /// staying in the field as it was until its new paint settles, where by default it goes on
    /// from the frames the mesh painted had.
    /// </summary>
    /// <remarks>
    /// Waiting keeps a mesh painted again every frame, as one whose paint is animated, from being
    /// built into its cascades each frame, and holds a mesh's new paint out of the field for a few
    /// frames where it was painted again soon after it settled.
    /// </remarks>
    public bool RepaintWaits { get; init; }
}
