namespace RadianceCascades;

/// <summary>
/// The forms the plan holds two ways, each the engine's to choose, every default the form that
/// stamps least and builds least.
/// </summary>
/// <remarks>
/// Each is a way two engines' plans parted before the plan was written once, kept so each engine's
/// tests and rooms read as they did, and taken out once one form serves both. KERNELS.md tables
/// each with the test or the pair of pictures that will choose between its two forms.
/// </remarks>
/// <param name="Ahead">
/// A cascade's middle lies <see cref="SceneFieldPlan{TMesh, TTag}.Lead"/> of its width ahead of
/// the eye the way it looks, its corner snapped down and stepped clear of the eye, where by default
/// it lies around the eye on the snap nearest it.
/// </param>
/// <param name="StampsArrivals">
/// A mesh that came and has not moved, and a still one that waits for its cascades, are stamped as
/// boxes until they are built, where by default only a mesh that moved is.
/// </param>
/// <param name="WholeBuilds">
/// Every build is of a whole cascade, for field kernels that build no region and shift no cascade,
/// where by default a cascade the eye moved by whole cells shifts and what came or went within a
/// built one is built as a region of it.
/// </param>
/// <param name="RepaintWaits">
/// A still mesh painted again where it stands is still at once only where it had stood still for
/// <see cref="SceneFieldPlan{TMesh, TTag}.SettleFrames"/> frames since it settled, and otherwise
/// stays in the field as it was until its new paint settles, as one replaced in place does, where
/// by default the mesh painted again goes on from the frames the one painted had.
/// </param>
public readonly record struct FieldForms(bool Ahead = false, bool StampsArrivals = false, bool WholeBuilds = false, bool RepaintWaits = false);
