using System.Numerics;

namespace RadianceCascades;

/// <summary>
/// The place each glow light is written in, kept from frame to frame. A light among the frame
/// before's keeps its place and a new one takes the first that is free, so what the screen's probes
/// saw of a light the frame before is found in its place as the lights move nearer the eye and
/// farther in the order they are taken in.
/// </summary>
/// <remarks>
/// An engine keeps one and hands it to <see cref="GlowLights.Write"/> each frame, and lets its
/// places go with <see cref="Forget"/> for a frame that writes no glow light.
/// </remarks>
public sealed class GlowPlaces
{
    private readonly Dictionary<Vector3, int> _before = [];
    private readonly Dictionary<Vector3, int> _now = [];
    private readonly Vector3[] _placed = new Vector3[GlowLights.Most];
    private readonly uint[] _kept = new uint[GlowLights.KeptVectors * 4];

    /// <summary>
    /// The places whose light is the one the frame before had there, a bit a place, the first 32
    /// in the first uint, as the head holds them.
    /// </summary>
    public ReadOnlySpan<uint> Kept => _kept;

    /// <summary>
    /// How many places the lights the last frame wrote take, up to the last taken, as the head's
    /// count holds it.
    /// </summary>
    public int Count { get; private set; }

    // Each place's light's middle as it was gathered against Against, which GlowLights.Place
    // writes, and the origin those were noted against, the last frame's that wrote its lights.
    internal ReadOnlySpan<Vector3> Placed => _placed;

    internal FrameOrigin? Against { get; private set; }

    // Notes the middle of the light in a place as it was gathered against the origin.
    internal void Note(int place, Vector3 middle, FrameOrigin origin) => (_placed[place], Against) = (middle, origin);

    // Places the first count of the lights, each known by its middle in world numbers
    // (GlowLight.Key), in placeOf, and gives how many places are taken up to the last.
    internal int Place(IReadOnlyList<GlowLight> lights, int count, Span<int> placeOf)
    {
        Span<bool> taken = stackalloc bool[GlowLights.Most];
        Array.Clear(_kept);
        _now.Clear();
        for (int n = 0; n < count; n++)
        {
            placeOf[n] = -1;
            if (_before.TryGetValue(lights[n].Key, out var place) && !taken[place])
            {
                (placeOf[n], taken[place]) = (place, true);
                _kept[place >> 5] |= 1u << (place & 31);
            }
        }
        int free = 0, used = 0;
        for (int n = 0; n < count; n++)
        {
            if (placeOf[n] < 0)
            {
                while (taken[free]) free++;
                (placeOf[n], taken[free]) = (free, true);
            }
            _now[lights[n].Key] = placeOf[n];
            used = Math.Max(used, placeOf[n] + 1);
        }
        // A place no light took holds nothing new.
        for (int place = 0; place < used; place++)
            if (!taken[place])
                _kept[place >> 5] |= 1u << (place & 31);
        _before.Clear();
        foreach (var (middle, place) in _now) _before[middle] = place;
        return Count = used;
    }

    /// <summary>
    /// Knows every light by its middle moved by a whole step the program moved its world by, worked
    /// in doubles, so each light keeps its place through the move.
    /// </summary>
    /// <param name="x">The step along x.</param>
    /// <param name="y">The step along y.</param>
    /// <param name="z">The step along z.</param>
    public void Move(double x, double y, double z)
    {
        var before = _before.ToArray();
        _before.Clear();
        foreach (var (middle, place) in before) _before[new Vector3((float)(middle.X + x), (float)(middle.Y + y), (float)(middle.Z + z))] = place;
        Against = null;
    }

    /// <summary>Lets every place go, for a frame that writes no glow light.</summary>
    public void Forget()
    {
        _before.Clear();
        Array.Clear(_kept);
        Count = 0;
        Against = null;
    }
}
