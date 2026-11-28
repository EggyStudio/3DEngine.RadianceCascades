namespace RadianceCascades;

/// <summary>
/// The shape of the split cascades' store, which an engine hands its passes as the Slang
/// <c>SplitStore</c> and lays out its two buffers by, each setting at a default an engine may
/// leave.
/// </summary>
/// <remarks>
/// <para>
/// The store keeps a pool of entries for each cascade, the slots for the finest, and for each
/// above the probes of its spacing over the square the finest pool's slots cover with a row more
/// along each edge (<see cref="PoolProbes"/>), and a table four times the finest pool's slots from
/// each probe's key to its entry. Its light takes a float4 for each way of each entry, once for the
/// probes' own light and once for the light merged from above, and the sums of the seen points'
/// rays take six uints for each way of the probes' own light. A probe of a coarse cascade holds
/// many ways, 32,768 at the sixth, so the coarse pools take most of the memory the rows past the
/// square's edges add.
/// </para>
/// <para>
/// A probe's key keeps the low nine bits of its cell along each axis, which tell apart the probes
/// within 256 cells of the eye, so the store holds probes for the points within 254 of the finest
/// spacings of the eye along each axis and none past them, 63.5 at the default spacing.
/// </para>
/// <para>
/// A probe is kept from frame to frame while the eye moves and turns, its light with it, until
/// its pool has too little room for the probes the next frame takes, when those asked for longest
/// ago are shed first. The store counts its probes by how many frames ago each was asked for, in
/// <see cref="AgeBins"/> bins for each cascade, for each frame's parity.
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
    /// The spacing of the finest cascade's probes in the engine's world unit, 0.25 by default.
    /// </summary>
    public float Spacing { get; init; } = 0.25f;

    /// <summary>
    /// The count of cascades, each spaced twice the one below on a grid fixed in the world, with
    /// four times its ways and an interval four times as long, from 1 to 8, 6 by default, whose
    /// coarsest interval ends 546 of the engine's units from a ray's start at the default spacing.
    /// </summary>
    public int Cascades { get; init; } = 6;

    /// <summary>
    /// The ways of a probe of the finest cascade, as the Slang <c>SplitBaseWays</c>.
    /// </summary>
    public const int BaseWays = 32;

    /// <summary>
    /// The bins each cascade's probes are counted in by how many frames ago each was asked for, as
    /// the Slang <c>SplitAgeBins</c>.
    /// </summary>
    public const int AgeBins = 32;

    /// <summary>
    /// The uints of an entry of a pool, as the Slang <c>SplitEntryUints</c>.
    /// </summary>
    public const int EntryUints = 8;

    /// <summary>
    /// The uints of the sums of each probe's way, as the Slang <c>SplitSumUints</c>, its light in
    /// red, green and blue, the share let through, its count of rays and the mean of the squares of
    /// the light its frames brought, with the sum of its frames past its light in that uint's low
    /// 8 bits.
    /// </summary>
    public const int SumUints = 6;

    // The uints ahead of the ages' counts, as the Slang SplitCounts.
    private const int Counts = 32;

    /// <summary>
    /// The entries of a cascade's pool, as the Slang <c>splitPoolProbes</c>, the slots for the
    /// finest, and for each above the probes of its spacing over the square of the finest's probes
    /// the slots cover, the fewest a side whose square holds them, with a row more along each edge,
    /// no more than the slots.
    /// </summary>
    /// <remarks>
    /// The probes a surface's points ask for of a cascade above run past the surface's edges and
    /// across its thickness, so a quarter of the pool below, as the probes of a surface at twice
    /// the spacing are, would leave the coarse pools short where a side holds few probes beside
    /// those rows.
    /// </remarks>
    public int PoolProbes(int cascade)
    {
        var slots = Checked().Slots;
        if (cascade == 0)
            return slots;
        var side = (int)Math.Sqrt(slots);
        while (side * side < slots)
            side++;
        var along = ((side + (1 << cascade) - 1) >> cascade) + 2;
        return Math.Min(along * along, slots);
    }

    /// <summary>The entries of every pool.</summary>
    public int Entries => Enumerable.Range(0, Checked().Cascades).Sum(PoolProbes);

    /// <summary>
    /// The float4 of a cascade's own light, a way of each entry of its pool, which an engine
    /// dispatches <c>mergeSplitCascade</c> over for that cascade, a thread each.
    /// </summary>
    public int CascadeLights(int cascade) => PoolProbes(cascade) * (BaseWays << (2 * cascade));

    /// <summary>
    /// The float4 of every cascade's own light, which an engine dispatches <c>keepSplitLight</c>
    /// over, a thread each, half of <see cref="Lights"/>.
    /// </summary>
    public int OwnLights => Enumerable.Range(0, Checked().Cascades).Sum(CascadeLights);

    /// <summary>
    /// The uints the store lays out, its counts, the ages' counts of both parities, its table, its
    /// pools, its free lists and the sums the seen points' rays add to each way,
    /// <see cref="SumUints"/> a way, which the engine's buffer of uints holds.
    /// </summary>
    public int Uints => Counts + 2 * AgeBins * Checked().Cascades + 2 * 4 * Slots + (EntryUints + 1) * Entries
        + SumUints * OwnLights;

    /// <summary>
    /// The float4 of light the store lays out, its probes' own light and the light merged from
    /// above, which the engine's buffer of light holds.
    /// </summary>
    public int Lights => 2 * OwnLights;

    /// <summary>
    /// The bytes the store's two buffers take, four a uint of <see cref="Uints"/> and sixteen a
    /// float4 of <see cref="Lights"/>, which an engine reads its memory by.
    /// </summary>
    public long Bytes => 4L * Uints + 16L * Lights;

    // This shape, or the setting out of its bounds thrown.
    private SplitStoreShape Checked()
    {
        if (Cascades is < 1 or > 8)
            throw new ArgumentOutOfRangeException(nameof(Cascades), Cascades, "takes 1 to 8 cascades");
        if (Slots < 1 << (2 * (Cascades - 1)) || (Slots & (Slots - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(Slots), Slots, "takes a power of two no smaller than 4 to the cascades above the finest");
        if (!(Spacing > 0))
            throw new ArgumentOutOfRangeException(nameof(Spacing), Spacing, "takes a spacing above 0");
        return this;
    }
}
