namespace RadianceCascades;

/// <summary>
/// The forms the glow lights hold two ways, each the engine's to name, none taken for it.
/// </summary>
/// <remarks>
/// Each is a way two engines' glow lights parted before they were written once, kept so each
/// engine's tests and rooms read as they did, and taken out once one form serves both. KERNELS.md
/// tables each with the test or the pair of pictures that will choose between its two forms. Every
/// member is required, so the lights are written only with each form named. The forms cleared are
/// those most of the glow lights' tests hold.
/// </remarks>
public readonly record struct GlowForms
{
    /// <summary>
    /// Whether a light reaches as far as a surface there takes a thousandth of the light it emits,
    /// where otherwise it reaches as far as a surface takes a 300th, or as far as a surface takes a
    /// 300th of what one half the first cascade's probe spacing from it takes where that is
    /// farther, so a lamp's small bulb reaches the floor under it however bright it burns.
    /// </summary>
    public required bool FaintThousandth { get; init; }

    /// <summary>
    /// Whether the <see cref="GlowLights.Most"/> lights written are those nearest the eye, where
    /// otherwise they are those that send the most about the eye, each one's light times the
    /// surface it emits from over its squared distance from the eye, so a bright lantern down the
    /// street is written before a faint emitter beside the eye.
    /// </summary>
    public required bool NearestFirst { get; init; }

    /// <summary>
    /// Whether the block is the lights alone from its first byte, <see cref="GlowLights.Most"/> of
    /// <see cref="GlowLights.Bytes"/> each, the places they take and the bits of those kept from
    /// the frame before handed apart through <see cref="GlowPlaces"/>, where otherwise a head of
    /// <see cref="GlowLights.Head"/> bytes lies ahead of them with the count of places taken, the
    /// box every light's reach lies in and those bits.
    /// </summary>
    public required bool LightsAlone { get; init; }
}
