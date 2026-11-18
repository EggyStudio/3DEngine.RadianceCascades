namespace RadianceCascades;

/// <summary>
/// The forms the plan holds two ways, each the engine's to name, none taken for it.
/// </summary>
/// <remarks>
/// Each is a way two engines' plans parted before the plan was written once, kept so each engine's
/// tests and rooms read as they did, and taken out once one form serves both. KERNELS.md tables
/// each with the test or the pair of pictures that will choose between its two forms. Every member
/// is required, so a plan is made only with each form named, and the README says which an engine
/// new to the module takes.
/// </remarks>
public readonly record struct FieldForms
{
    /// <summary>
    /// Whether a cascade's middle lies <see cref="SceneFieldPlan{TMesh, TTag}.Lead"/> of its width
    /// ahead of the eye the way it looks, its corner snapped down and stepped clear of the eye, so
    /// more of it holds what the eye sees, where otherwise it lies around the eye on the snap
    /// nearest it, so a turn builds nothing again.
    /// </summary>
    public required bool Ahead { get; init; }

    /// <summary>
    /// Whether a mesh that came and has not moved, and a still one that waits for its cascades, are
    /// stamped as boxes until they are built, where otherwise only a mesh that moved is, since a
    /// box stands in badly for a mesh larger than what it holds, as a building's outer walls.
    /// </summary>
    public required bool StampsArrivals { get; init; }

    /// <summary>
    /// Whether every build is of a whole cascade, for field kernels that build no region and shift
    /// no cascade, where otherwise a cascade the eye moved by whole cells shifts and what came or
    /// went within a built one is built as a region of it.
    /// </summary>
    public required bool WholeBuilds { get; init; }

    /// <summary>
    /// Whether a still mesh painted again where it stands is still at once only where it had stood
    /// still for <see cref="SceneFieldPlan{TMesh, TTag}.SettleFrames"/> frames since it settled,
    /// and otherwise stays in the field as it was until its new paint settles, as one replaced in
    /// place does, so a mesh painted each frame is not built in each frame, where otherwise the
    /// mesh painted again goes on from the frames the one painted had.
    /// </summary>
    public required bool RepaintWaits { get; init; }
}
