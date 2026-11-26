namespace RadianceCascades;

/// <summary>
/// Which probes light the surfaces the eye sees, the dense levels by default, which an engine
/// keeps until the split cascades read its scenes nearer their trace.
/// </summary>
public enum ProbeLayout
{
    /// <summary>
    /// Levels of probes on a grid about the eye, every probe of each level standing whether a
    /// surface is near it or not, gathered through the levels an engine hands as
    /// <c>IGatherLevels</c>.
    /// </summary>
    Dense,

    /// <summary>
    /// The split cascades' probes, kept only near the surfaces the eye sees in a store laid out by
    /// <see cref="SplitStoreShape"/>, gathered through <c>gatherSurface</c> of <c>rc_split</c>.
    /// </summary>
    Split,
}
