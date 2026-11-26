namespace RadianceCascades;

/// <summary>
/// The shape of the split cascades' store, which an engine hands its passes as the Slang
/// <c>SplitStore</c> and lays out its two buffers by, each setting at a default an engine may
/// leave.
/// </summary>
/// <remarks>
/// <para>
/// The store keeps a pool of entries for each cascade, the slots for the finest and a quarter as
/// many for each above, since a probe of the cascade above stands for the four below it along a
/// surface, and a table four times the finest pool's slots from each probe's key to its entry. Its
/// light takes the same span for each cascade, a quarter as many probes with four times the ways,
/// once for the probes' own light and once for the light merged from above, and the sums of the
/// seen points' rays take five uints for each way of the probes' own light.
/// </para>
/// <para>
/// A probe's key keeps the low nine bits of its cell along each axis, which tell apart the probes
/// within 256 cells of the eye, so a level reaches at most 128 of its spacings and the store holds
/// no probe past it.
/// </para>
/// </remarks>
public sealed record SplitStoreShape
{
    /// <summary>
    /// The entries of the finest cascade's pool, a power of two at least as large as the coarsest
    /// cascade's ways over the finest's, 16384 by default.
    /// </summary>
    public int Slots { get; init; } = 16384;

    /// <summary>
    /// The spacing of the finest level's probes in the engine's world unit, 0.25 by default.
    /// </summary>
    public float Spacing { get; init; } = 0.25f;

    /// <summary>
    /// The count of levels, each spaced twice the one below and reaching twice as far from the eye,
    /// from 1 to 7, 5 by default.
    /// </summary>
    public int Levels { get; init; } = 5;

    /// <summary>
    /// The count of cascades of each level, each with four times the ways of the one below, from 1
    /// to 4, 4 by default.
    /// </summary>
    public int Cascades { get; init; } = 4;

    /// <summary>
    /// How far the finest level reaches from the eye along each axis, at most 128 spacings, 1 by
    /// default, which is 4 spacings as a dense level reaches.
    /// </summary>
    public float Reach { get; init; } = 1f;

    /// <summary>
    /// How many frames a probe no seen point asks for is kept before its entry is freed, 32 by
    /// default.
    /// </summary>
    public int Age { get; init; } = 32;

    /// <summary>
    /// The ways of a probe of the finest cascade, as the Slang <c>SplitBaseWays</c>.
    /// </summary>
    public const int BaseWays = 32;

    // The uints ahead of the table, as the Slang SplitCounts.
    private const int Counts = 16;

    /// <summary>
    /// The entries of a cascade's pool, the slots over four for each cascade below it.
    /// </summary>
    public int PoolProbes(int cascade) => Checked().Slots >> (2 * cascade);

    /// <summary>The entries of every pool.</summary>
    public int Entries => Enumerable.Range(0, Checked().Cascades).Sum(PoolProbes);

    /// <summary>
    /// The uints the store lays out, its counts, its table, its pools, its free lists and the sums
    /// the seen points' rays add to each way, five a way, which the engine's buffer of uints holds.
    /// </summary>
    public int Uints => Counts + 2 * 4 * Checked().Slots + 5 * Entries + 5 * Cascades * BaseWays * Slots;

    /// <summary>
    /// The float4 of light the store lays out, its probes' own light and the light merged from
    /// above, which the engine's buffer of light holds.
    /// </summary>
    public int Lights => 2 * Checked().Cascades * BaseWays * Slots;

    // This shape, or the setting out of its bounds thrown.
    private SplitStoreShape Checked()
    {
        if (Cascades is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(Cascades), Cascades, "takes 1 to 4 cascades");
        if (Levels is < 1 or > 7)
            throw new ArgumentOutOfRangeException(nameof(Levels), Levels, "takes 1 to 7 levels");
        if (Slots < 1 << (2 * (Cascades - 1)) || (Slots & (Slots - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(Slots), Slots, "takes a power of two no smaller than 4 to the cascades above the finest");
        if (!(Spacing > 0) || !(Reach > 0) || Reach > 128 * Spacing)
            throw new ArgumentOutOfRangeException(nameof(Reach), Reach, "takes a spacing above 0 and a reach above 0 of at most 128 spacings");
        if (Age < 0)
            throw new ArgumentOutOfRangeException(nameof(Age), Age, "takes an age of 0 or more");
        return this;
    }
}
