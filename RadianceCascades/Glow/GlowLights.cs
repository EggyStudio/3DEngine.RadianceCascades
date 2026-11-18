using System.Numerics;
using System.Runtime.InteropServices;

namespace RadianceCascades;

/// <summary>
/// The small emissive surfaces, which global illumination carries as lights of their own rather
/// than by the rays that meet them, which of them are lights, and the block the glow kernel reads
/// them from, each light 80 bytes as the kernel's <c>GlowLight</c> reads it.
/// </summary>
/// <remarks>
/// <para>
/// A probe's rays meet a small emitter seldom and take it for a whole texel of the sky where they
/// do, and the world's probes stand above a floor at their spacing, so the faces a floor reads see
/// none of a lamp lying on it. Carried as a light, its light reaches a surface in closed form from
/// the faces of its box, shadowed by a march through the field. Emitters square to the axes that
/// touch and emit the same light are one light, so a stack of emissive blocks is not four lights
/// each hidden by the others. The field marks the cells such an emitter paints, so a ray that meets
/// it does not take its light a second time.
/// </para>
/// <para>
/// An emitter is carried where its box's longest side in the world is no more than twice the first
/// cascade's probe spacing, which does not change while it stands still, so the field's mark holds
/// between its builds. A mesh of many separate emitters whose corners the engine gives is cut into
/// its parts, its triangles joined where they share a corner, and is carried where each part is,
/// each a light of its own (<see cref="PartsOf"/>), and a mesh given whole is one light of its box.
/// How far a light reaches, and which <see cref="Most"/> of the carried emitters are lights each
/// frame, are forms of <see cref="GlowForms"/>. One past them lights nothing a probe carries to a
/// floor, its cells marked all the same, so a level of more lamps than that lights only those. The
/// field marks a mesh whole, so a mesh of many parts past them lights none of the ground around
/// those parts.
/// </para>
/// </remarks>
public static partial class GlowLights
{
    /// <summary>
    /// How many carried emitters are lights at most, as the glow kernel's <c>MostGlowLights</c>.
    /// </summary>
    public const int Most = 64;

    /// <summary>
    /// The uint4 values of a bit a place in the head, saying which places hold the frame before's
    /// light, as the glow kernel's <c>GlowKeptVectors</c>.
    /// </summary>
    public const int KeptVectors = (Most + 127) / 128;

    /// <summary>
    /// The bytes of a light, its middle and reach, its three half sides and its light, as the glow
    /// kernel's <c>GlowLight</c>.
    /// </summary>
    public const int Bytes = 80;

    /// <summary>
    /// The bytes ahead of the lights, the count and the least corner of the box every light's
    /// reach lies in, the box's most corner, and a bit a place saying whether it holds the frame
    /// before's light, as the glow kernel's <c>GlowLightsHead</c>.
    /// </summary>
    /// <remarks>
    /// The block has no head where <see cref="GlowForms.LightsAlone"/> is set, and its lights start
    /// at its first byte.
    /// </remarks>
    public const int Head = 32 + KeptVectors * 16;

    /// <summary>The bytes the head and the lights take.</summary>
    /// <remarks>
    /// Where <see cref="GlowForms.LightsAlone"/> is set the block is <see cref="Most"/> times
    /// <see cref="Bytes"/> long.
    /// </remarks>
    public const int Size = Head + Most * Bytes;

    // How many cells of a cascade apart its probes stand, the unit the carried emitters' longest
    // side and the reach's floor are measured in.
    private const int ProbeSpacing = 8;

    // The light a surface at the reach takes from one, a 300th of the light it emits, past which
    // the light is let go, over a quarter of the box's surface as the box's mean face seen from
    // afar. A share of its own light, so every light keeps the same share of its light whatever its
    // brightness, where a light let go at a fixed level reaches less far the dimmer it is, and six
    // emitters of a sixth of one's light each less far still, so a closed room lit by six takes
    // far less light than one lit by one.
    private const float Faint = 1f / 300;

    // The share where GlowForms.FaintThousandth is set, a thousandth, which keeps the reach of one
    // bright emitter as far as a camera exposed for the room it lights sees it.
    private const float Thousandth = 0.001f;

    // How far from an emitter, in the first cascade's probe spacings, a surface's light is the one
    // the reach is measured against where that reaches further than the emitter's own light does.
    // An emitter smaller than the probes resolve lights the surfaces about it as a point does, its
    // light falling with the square of the distance, so a reach measured against its own light
    // alone lies a few of its widths from it, short of the floor under a lamp's bulb however
    // bright it burns, and one measured here lies past it.
    private const float MeasuredAt = 0.5f;

    /// <summary>
    /// Whether an instance emitting <paramref name="emission"/> is carried as lights, every part of
    /// its mesh carried as <see cref="Carries(Vector3, FieldBox, Matrix4x4, float)"/> carries a
    /// box.
    /// </summary>
    /// <remarks>
    /// An engine's <c>carried</c> function for its plan answers with this of a mesh's parts where
    /// it cuts the mesh, and with the box's where it does not, as <see cref="Gather"/> chooses
    /// which are lights, so the field marks the meshes that are.
    /// </remarks>
    public static bool Carries(Vector3 emission, IReadOnlyList<GlowPart> parts, Matrix4x4 world, float firstCell)
    {
        if (parts.Count == 0) return false;
        foreach (var part in parts)
            if (!Carries(emission, part.Box, world, firstCell)) return false;
        return true;
    }

    /// <summary>
    /// Whether an instance emitting <paramref name="emission"/> is carried as a light, its box in
    /// the world no longer than twice the first cascade's probe spacing.
    /// </summary>
    public static bool Carries(Vector3 emission, FieldBox box, Matrix4x4 world, float firstCell)
    {
        if (emission is { X: <= 0, Y: <= 0, Z: <= 0 }) return false;
        var size = box.Max - box.Min;
        var longest = MathF.Max(size.X * new Vector3(world.M11, world.M12, world.M13).Length(),
            MathF.Max(size.Y * new Vector3(world.M21, world.M22, world.M23).Length(), size.Z * new Vector3(world.M31, world.M32, world.M33).Length()));
        return longest <= 2 * ProbeSpacing * firstCell;
    }

    /// <summary>
    /// Writes the lights the carried emitters among <paramref name="drawn"/> make, those the forms
    /// rank first about <paramref name="eye"/>, into <paramref name="bytes"/>, and gives how many
    /// there were in all, written or not.
    /// </summary>
    /// <remarks>
    /// A place no light takes, up to the last taken, is left a light of no reach. Where
    /// <see cref="GlowForms.LightsAlone"/> is set no head is written, so the count of places taken
    /// is <paramref name="places"/>' <see cref="GlowPlaces.Count"/>, or the lesser of the count
    /// given back and <see cref="Most"/> where there are no places, and the bits of those kept are
    /// its <see cref="GlowPlaces.Kept"/>.
    /// </remarks>
    /// <typeparam name="TMesh">
    /// What the engine knows a mesh's vertices by, as its plan does.
    /// </typeparam>
    /// <typeparam name="TTag">The engine's own of each draw, as its plan carries it.</typeparam>
    /// <param name="bytes">
    /// The head's bytes, <see cref="Head"/>, and the lights', <see cref="Size"/> in all, or the
    /// lights' alone where <see cref="GlowForms.LightsAlone"/> is set.
    /// </param>
    /// <param name="drawn">The meshes the field gathered this frame.</param>
    /// <param name="eye">The eye, about which the lights are ranked.</param>
    /// <param name="firstCell">
    /// The width of a cell of the field's first cascade, which says which emitters are carried.
    /// </param>
    /// <param name="boxOf">The box around a mesh's vertices in its own space.</param>
    /// <param name="shareOf">
    /// A mesh's surface over its box's, 1 for a box, which
    /// <see cref="ShareOf(Vector3[], FieldBox)"/> works out from its corners and
    /// <see cref="ShareOf(double, FieldBox)"/> from its area.
    /// </param>
    /// <param name="forms">The forms the engine chose where the glow lights hold two.</param>
    /// <param name="places">
    /// The places the lights kept the frame before, which a light still among them is written in
    /// again, or null to write them in the order they are ranked in.
    /// </param>
    /// <param name="partsOf">
    /// The parts a mesh is cut into (<see cref="PartsOf"/>), or null to take every mesh whole.
    /// </param>
    /// <param name="origin">
    /// The frame's origin, which the lights' boxes are gathered against (<see cref="Gather"/>),
    /// their middles kept in <paramref name="places"/> for <see cref="Place"/>, those written being
    /// in world numbers.
    /// </param>
    public static int Write<TMesh, TTag>(Span<byte> bytes, IReadOnlyList<(SceneFieldPlan<TMesh, TTag>.Instance Instance, bool Skinned)> drawn, Vector3 eye,
        float firstCell, Func<TMesh, FieldBox> boxOf, Func<TMesh, float> shareOf, GlowForms forms, GlowPlaces? places = null,
        Func<TMesh, IReadOnlyList<GlowPart>>? partsOf = null, FrameOrigin origin = default)
        where TMesh : notnull where TTag : IEquatable<TTag>
    {
        var lights = Gather(drawn, firstCell, boxOf, shareOf, partsOf, origin);
        Rank(lights, eye, firstCell, forms);
        var floats = MemoryMarshal.Cast<byte, float>(bytes);
        var count = Math.Min(lights.Count, Most);
        Span<int> placeOf = stackalloc int[Most];
        var used = places?.Place(lights, count, placeOf) ?? count;
        var start = forms.LightsAlone ? 0 : Head / 4;
        floats.Slice(start, used * Bytes / 4).Clear();
        // The box no light reaches past, empty where there is none, so a point outside it, as one
        // of the far land, takes no light without being weighed against each.
        var (low, high) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
        var faint = forms.FaintThousandth ? Thousandth : Faint;
        for (int n = 0; n < count; n++)
        {
            if (places is null) placeOf[n] = n;
            var (_, x, y, z, glow, share, faces) = lights[n];
            var middle = lights[n].Key;
            places?.Note(placeOf[n], lights[n].Middle, origin);
            var surface = 4 * FacesSurface(new Vector3(x.Length(), y.Length(), z.Length()), faces == 0 ? All : faces) * share;
            var reach = MathF.Sqrt(surface / (4 * faint));
            if (!forms.FaintThousandth) reach = MathF.Max(reach, MeasuredAt * ProbeSpacing * firstCell / MathF.Sqrt(faint));
            var light = floats.Slice(start + placeOf[n] * Bytes / 4, Bytes / 4);
            (light[0], light[1], light[2], light[3]) = (middle.X, middle.Y, middle.Z, reach);
            (low, high) = (Vector3.Min(low, middle - new Vector3(reach)), Vector3.Max(high, middle + new Vector3(reach)));
            (light[4], light[5], light[6], light[7]) = (x.X, x.Y, x.Z, x.Length());
            (light[8], light[9], light[10], light[11]) = (y.X, y.Y, y.Z, y.Length());
            (light[12], light[13], light[14], light[15]) = (z.X, z.Y, z.Z, z.Length());
            // The share and the faces in one value, the share below 2 and twice the faces above it,
            // as the glow kernel's glowShare and glowFaces read them.
            (light[16], light[17], light[18], light[19]) = (glow.X, glow.Y, glow.Z, share + 2 * faces);
        }
        if (!forms.LightsAlone)
        {
            (floats[0], floats[7]) = (used, 0);
            (floats[1], floats[2], floats[3]) = (low.X, low.Y, low.Z);
            (floats[4], floats[5], floats[6]) = (high.X, high.Y, high.Z);
            var keptBits = MemoryMarshal.Cast<byte, uint>(bytes)[8..(Head / 4)];
            keptBits.Clear();
            if (places is not null) places.Kept.CopyTo(keptBits);
        }
        return lights.Count;
    }

    // The lights ranked by what each sends about the eye, its luminance times the surface it emits
    // from over its distance from the eye squared, a cell's at least, so a bright lantern down the
    // street is lit before a faint emitter beside the eye, or nearest the eye first where the forms
    // say. Both measure from the middle in world numbers, as the eye is given.
    private static void Rank(List<GlowLight> lights, Vector3 eye, float firstCell, GlowForms forms)
    {
        if (forms.NearestFirst)
        {
            lights.Sort((a, b) => Vector3.DistanceSquared(a.Key, eye).CompareTo(Vector3.DistanceSquared(b.Key, eye)));
            return;
        }
        float Sent(GlowLight light)
        {
            var surface = FacesSurface(new Vector3(light.X.Length(), light.Y.Length(), light.Z.Length()), light.Faces == 0 ? All : light.Faces) * light.Share;
            var luminance = 0.2126f * light.Glow.X + 0.7152f * light.Glow.Y + 0.0722f * light.Glow.Z;
            return luminance * surface / MathF.Max(Vector3.DistanceSquared(light.Key, eye), firstCell * firstCell);
        }
        lights.Sort((a, b) => Sent(b).CompareTo(Sent(a)));
    }

    /// <summary>
    /// The lights <see cref="Write"/> wrote with their middles and the box about their reach taken
    /// against <paramref name="origin"/>, a place no light holds left empty. The middles are those
    /// <paramref name="places"/> kept as they were gathered against the origin, where it kept them
    /// against this one, and the box is found again about them, so a light far from the world's
    /// origin is not moved by its middle rounded to the step there.
    /// </summary>
    /// <remarks>
    /// It reads the count from the head, so it places a block written with one, where
    /// <see cref="GlowForms.LightsAlone"/> is cleared.
    /// </remarks>
    public static void Place(Span<byte> bytes, FrameOrigin origin, GlowPlaces? places = null)
    {
        var floats = MemoryMarshal.Cast<byte, float>(bytes);
        if (floats[0] <= 0) return;
        var kept = places?.Against == origin;
        var (low, high) = (new Vector3(float.MaxValue), new Vector3(float.MinValue));
        for (int n = 0; n < (int)floats[0]; n++)
        {
            var light = floats.Slice(Head / 4 + n * Bytes / 4, Bytes / 4);
            if (light[3] <= 0) continue;
            var middle = kept ? places!.Placed[n] : origin.Against(new Vector3(light[0], light[1], light[2]));
            (light[0], light[1], light[2]) = (middle.X, middle.Y, middle.Z);
            (low, high) = (Vector3.Min(low, middle - new Vector3(light[3])), Vector3.Max(high, middle + new Vector3(light[3])));
        }
        if (!kept) (low, high) = (origin.Against(new Vector3(floats[1], floats[2], floats[3])), origin.Against(new Vector3(floats[4], floats[5], floats[6])));
        (floats[1], floats[2], floats[3]) = (low.X, low.Y, low.Z);
        (floats[4], floats[5], floats[6]) = (high.X, high.Y, high.Z);
    }
}
