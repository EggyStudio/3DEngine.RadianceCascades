using System.Collections.Concurrent;
using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The corridor's wall point at the lamp's edge read on the split probes against the light traced
/// there, from every face the eye sees and from the point's own rays alone.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // The point on the wall at the lamp's edge, 2.5 high, and the eye it is read from, whose
    // finest probes about it stand beside the wall's top, where the roof's lamp reaches.
    private const int CorridorApartPoint = 115;
    private const float CorridorApartEye = -3;

    // The light a point at origin facing normal takes in the corridor over 128 by 128 ways spread
    // by their cosine, their mean as the split gather weighs its ways, each way meeting a box
    // taking light by its place a hundredth short of it and each leaving the world the sky's,
    // the module's one cascade of the field bounding the world at its configuration, with the
    // sky's share of that light.
    private static (float Mean, float Sky) CorridorTraced(Configuration configuration, Vector3 origin, Vector3 normal, Func<Vector3, float> light, float sky)
    {
        var (u, v) = Basis(normal);
        const int side = 128;
        var (sum, open) = (0.0, 0.0);
        for (int i = 0; i < side; i++)
            for (int j = 0; j < side; j++)
            {
                var way = CosineWay(normal, u, v, (i + 0.5f) / side, (j + 0.5f) / side);
                var t = FirstBox(CorridorBoxes, origin, way);
                if (configuration == Configuration.Module ? t >= LeavesField(origin, way) : t == float.MaxValue)
                    (sum, open) = (sum + sky, open + sky);
                else
                    sum += light(origin + way * (t - 0.01f));
            }
        return ((float)(sum / (side * side)), sum > 0 ? (float)(open / sum) : 0);
    }

    // The point's light from the finest probes about it that it sees after the warm frames with
    // the eye still, the store fed by every face the eye sees or by the faces given, the weights of
    // those probes and its light traced. A store fed by the faces given takes the least pool of the
    // walk's cascades, which holds a point's probes and runs in an eighth of the time.
    private static (float Own, float Weights, float Traced) CorridorApartRead(Configuration configuration,
        ConcurrentDictionary<Vector4, float[]> cascades, Vector3 eye, (Vector3 Place, Vector3 Normal) point, float[]? seen = null)
    {
        var shape = seen is null ? CorridorSplitShape : CorridorSplitShape with { Slots = 1 << 2 * (CorridorSplitShape.Cascades - 1) };
        var store = new SplitCorridor(configuration, [point], cascades, shape: shape);
        store.At(eye, seen);
        store.Frames(CorridorSplitWarm);
        var taken = store.Lights();
        return (taken[0], taken[2], CorridorTraced(configuration, point.Place, point.Normal, CorridorLight, 1).Mean);
    }

    [CpuKernelFact]
    public void The_Corridors_Wall_Point_At_The_Lamps_Edge_Reads_Within_A_Twentieth_Of_Its_Trace_On_The_Split_Probes_From_Its_Own_Rays()
    {
        // The point on the wall at the lamp's edge, the eye held still. Fed by every face the eye
        // sees, the finest probes about it that it sees hold beside its rays those of the faces
        // about it within a spacing, those before the wall, and read it within an eighth of the
        // trace. Fed by the point's own rays alone they hold none but its rays, which go to the
        // nearest probe it sees (splitSeenCell), so they read within a twentieth of the trace, the
        // cascades' intervals, ways and merge giving the point its light.
        var failed = new List<string>();
        var cascades = new ConcurrentDictionary<Vector4, float[]>();
        var eye = new Vector3(CorridorApartEye, 1.5f, 0);
        var point = CorridorPoints[CorridorApartPoint];
        var (place, normal) = point;
        var alone = new[] { place.X, place.Y, place.Z, 1, normal.X, normal.Y, normal.Z, 0 };
        foreach (var configuration in Enum.GetValues<Configuration>())
        {
            var every = CorridorApartRead(configuration, cascades, eye, point);
            var own = CorridorApartRead(configuration, cascades, eye, point, alone);
            var said = $"at {configuration}'s configuration the point traced takes {every.Traced:0.0000}, and fed by every face the eye sees its "
                + $"probes read {every.Own / every.Traced:0.000} of that, fed by its own rays alone {own.Own / own.Traced:0.000}";
            output.WriteLine(said);
            if (!(MathF.Abs(own.Own / own.Traced - 1) < 0.05f && MathF.Abs(every.Own / every.Traced - 1) < 0.125f))
                failed.Add(said);
        }
        Assert.True(failed.Count == 0, string.Join("; ", failed));
    }
}
