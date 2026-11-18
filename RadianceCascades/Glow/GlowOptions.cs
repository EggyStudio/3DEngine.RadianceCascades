namespace RadianceCascades;

/// <summary>
/// How far the glow lights reach, which of the carried emitters are written, and where the block
/// carries its count, each setting at a default an engine may leave.
/// </summary>
public sealed record GlowOptions
{
    /// <summary>
    /// The share of a light's own light a surface takes at its reach, past which the light is let
    /// go, a 300th by default.
    /// </summary>
    /// <remarks>
    /// A share of its own light, so every light keeps the same share of its light whatever its
    /// brightness. A smaller share reaches farther, and more lights are weighed at each point.
    /// </remarks>
    public float Faint { get; init; } = 1f / 300;

    /// <summary>
    /// Whether a light reaches at least as far as a surface takes <see cref="Faint"/> of what one
    /// half the first cascade's probe spacing from it takes, so a lamp's small bulb reaches the
    /// floor under it however dim it burns, on by default.
    /// </summary>
    public bool ReachFloor { get; init; } = true;

    /// <summary>
    /// Whether the <see cref="GlowLights.Most"/> lights written are those nearest the eye, where by
    /// default they are those that send the most about the eye, each one's light times the surface
    /// it emits from over its squared distance from the eye.
    /// </summary>
    /// <remarks>
    /// Nearest first writes the faint emitters beside the eye before a bright lantern down the
    /// street, whose light then reaches nothing it lights.
    /// </remarks>
    public bool NearestFirst { get; init; }

    /// <summary>
    /// Whether the block is the lights alone from its first byte, the places they take and the bits
    /// of those kept from the frame before handed apart through <see cref="GlowPlaces"/>, where by
    /// default a head of <see cref="GlowLights.Head"/> bytes lies ahead of the lights with their
    /// count, the box every light's reach lies in and those bits.
    /// </summary>
    /// <remarks>
    /// The head's box lets a point outside every light's reach pass over the lights at once, and
    /// the lights alone are an array a kernel reads with no offset.
    /// </remarks>
    public bool LightsAlone { get; init; }
}
