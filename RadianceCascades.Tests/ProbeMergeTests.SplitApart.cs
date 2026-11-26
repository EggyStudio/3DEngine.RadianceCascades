using System.Collections.Concurrent;
using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The corridor's wall point at the lamp's edge read by each of two split levels against the light
/// traced there, from every face the eye sees and from the point's own rays alone, and where
/// RC_SPLIT_APART is set the walk that finds the points two levels read most apart, the probes of
/// each level about the one read most apart and its light with one thing of the scene taken out at
/// a time.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // The point on the wall at the lamp's edge, 2.5 high, and the eye it is read from, over the
    // overlap of the levels of spacing 2 and 4. Rays started a quarter of their own level's spacing
    // off the surface read its two levels most apart over the walks at both configurations, the
    // finer's from the wall's top reaching into the roof's lamp and the coarser's starting a unit
    // off the wall.
    private const int CorridorApartPoint = 115;
    private const float CorridorApartEye = -3;

    // The share of a level's reach past which a point takes the next level in, as the Slang
    // SplitLevelStart, by which the walk tells the faces that feed a level's probes.
    private const float CorridorLevelStart = 0.9f;

    // The level of the point at p and its share of the next, as splitLevelAt gives them.
    private static (int Level, float Next) SplitLevelOf(SplitStoreShape shape, Vector3 eye, Vector3 p)
    {
        var d3 = Vector3.Abs(p - eye);
        var d = MathF.Max(d3.X, MathF.Max(d3.Y, d3.Z));
        var (reach, level) = (shape.Reach, 0);
        while (d >= reach && level + 1 < shape.Levels)
        {
            reach *= 2;
            level++;
        }
        return (level, level + 1 < shape.Levels ? Math.Clamp((d / reach - CorridorLevelStart) / (1 - CorridorLevelStart), 0, 1) : 0);
    }

    // The way of index among the ways of a probe of the finest cascade, as splitWay gives it.
    private static Vector3 SplitBaseWay(int index)
    {
        const int side = 4;
        var texel = index % (side * side);
        var uv = new Vector2((texel % side + 0.5f) / side * 2 - 1, (texel / side + 0.5f) / side * 2 - 1);
        var phi = MathF.Abs(uv.X) > MathF.Abs(uv.Y) ? MathF.PI / 4 * uv.Y / uv.X : MathF.PI / 2 - MathF.PI / 4 * uv.X / uv.Y;
        var d = (MathF.Abs(uv.X) > MathF.Abs(uv.Y) ? uv.X : uv.Y) * new Vector2(MathF.Cos(phi), MathF.Sin(phi));
        var r2 = d.LengthSquared();
        return new(d * MathF.Sqrt(MathF.Max(2 - r2, 0)), index < side * side ? 1 - r2 : r2 - 1);
    }

    // The light a point at origin facing normal takes in the corridor over 128 by 128 ways spread
    // by their cosine, their mean as a split level's gather weighs its ways, each way meeting a box
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

    // The point's light from its level and from the next after the warm frames with the eye still,
    // the store fed by every face the eye sees or by the faces given, its weights of the probes of
    // each and its light traced, and the store, whose probes' ways pass 9 reads.
    private static (float Own, float Next, float Weights, float NextWeights, float Traced, SplitCorridor Store) CorridorApartRead(Configuration configuration,
        ConcurrentDictionary<Vector4, float[]> cascades, Vector3 eye, (Vector3 Place, Vector3 Normal) point, float[]? seen = null,
        Func<Vector3, float>? light = null, float sky = 1)
    {
        var store = new SplitCorridor(configuration, [point], cascades, light, sky);
        store.At(eye, seen);
        store.Frames(CorridorSplitWarm);
        var taken = store.Levels();
        var traced = CorridorTraced(configuration, point.Place, point.Normal, light ?? CorridorLight, sky).Mean;
        return (taken[0], taken[1], taken[2], taken[3], traced, store);
    }

    [CpuKernelFact]
    public void The_Corridors_Wall_Point_At_The_Lamps_Edge_Reads_Within_A_Twentieth_Of_Its_Trace_On_Both_Split_Levels_From_Its_Own_Rays()
    {
        // The point on the wall at the lamp's edge, the eye held still, the levels of spacing 2
        // and 4. Fed by every face the eye sees, each level reads the light of the faces its
        // probes about the point that it sees stand for, those before the wall, since those
        // behind its face stand inside it. The finer's stand within 2 of it and read it within a
        // quarter of the trace and above it, since the merge takes the light above a way of a
        // probe at an odd cell along an axis from the rays of a patch a cell of its own past it
        // along that axis, which here lies toward the lamp in the roof, as the walk's test has
        // it. The coarser's spread over 4 to 8 along the corridor, the faces past the overlap's
        // start alone feeding them, and read it over a third of the trace, which the test holds
        // as its reading until the coarser level's probes stand nearer. Fed by the point's own
        // rays alone each level holds none but its rays, which start from one place whichever
        // level takes them (splitRayOrigin) and go to the nearest probe the point sees
        // (splitSeenCell), so both read within a twentieth of the trace and of each other, the
        // levels' intervals, ways and merge giving the point its light.
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
            var said = $"at {configuration}'s configuration the point traced takes {every.Traced:0.0000}, and fed by every face the eye sees its level reads "
                + $"{every.Own / every.Traced:0.000} of that and the next {every.Next / every.Traced:0.000}, fed by its own rays alone "
                + $"{own.Own / own.Traced:0.000} and {own.Next / own.Traced:0.000}";
            output.WriteLine(said);
            if (!(MathF.Abs(own.Own / own.Traced - 1) < 0.05f && MathF.Abs(own.Next / own.Traced - 1) < 0.05f
                && MathF.Abs(own.Next / own.Own - 1) < 0.05f && MathF.Abs(every.Own / every.Traced - 1) < 0.25f && every.Next / every.Traced > 0.35f))
                failed.Add(said);
            if (Environment.GetEnvironmentVariable("RC_SPLIT_APART") == "1")
                CorridorApartWritten(configuration, cascades);
        }
        Assert.True(failed.Count == 0, string.Join("; ", failed));
    }

    // With RC_SPLIT_APART set, the walk at a configuration and the dozen points its two levels read
    // most apart, then at the one read most apart, from the eye they read it most apart at, its
    // light traced from the point and from a quarter of each level's spacing off it, the probes
    // of each level standing about it with the faces feeding each, their ways' coverage, light and
    // sky share, and its two levels' light fed by every face, by the point's own rays, by its own
    // face's points, under no sky, with the lamp out and with the floor under the gap out.
    private void CorridorApartWritten(Configuration configuration, ConcurrentDictionary<Vector4, float[]> cascades)
    {
        var shape = CorridorSplitShape;
        var eyes = CorridorEyes(configuration);
        var walker = new SplitCorridor(configuration, CorridorPoints, cascades);
        var worstAt = new (float Apart, int Eye, float Ratio)[CorridorPoints.Length];
        for (int e = 0; e < eyes.Length; e++)
        {
            walker.At(new(eyes[e], 1.5f, 0));
            walker.Frames(e == 0 ? CorridorSplitWarm + CorridorSplitStill * CorridorSplitFrames + CorridorSplitFrames : CorridorSplitFrames);
            var taken = walker.Levels();
            for (int k = 0; k < CorridorPoints.Length; k++)
                if (taken[4 * k + 2] > 0 && taken[4 * k + 3] > 0 && MathF.Abs(MathF.Log(taken[4 * k + 1] / taken[4 * k])) > worstAt[k].Apart)
                    worstAt[k] = (MathF.Abs(MathF.Log(taken[4 * k + 1] / taken[4 * k])), e, taken[4 * k + 1] / taken[4 * k]);
        }
        var order = Enumerable.Range(0, CorridorPoints.Length).OrderByDescending(k => worstAt[k].Apart).ToArray();
        foreach (var k in order.Take(12))
            output.WriteLine($"{configuration}: point {k} at {CorridorPoints[k].Item1} facing {CorridorPoints[k].Item2}, the next level reading "
                + $"{worstAt[k].Ratio:0.000} of its own from the eye at {eyes[worstAt[k].Eye]}");
        var worst = order[0];
        var eye = new Vector3(eyes[worstAt[worst].Eye], 1.5f, 0);
        var (place, normal) = CorridorPoints[worst];
        var level = SplitLevelOf(shape, eye, place).Level;
        foreach (var l in new[] { -1, level, level + 1 })
        {
            var origin = l < 0 ? place : place + normal * 0.25f * shape.Spacing * (1 << l);
            var (mean, skyShare) = CorridorTraced(configuration, origin, normal, CorridorLight, 1);
            output.WriteLine($"  traced from {(l < 0 ? "the point" : $"a quarter of level {l}'s spacing off it")}: {mean:0.0000}, the sky's share {skyShare:0.000}");
        }
        var (low, high) = configuration == Configuration.Module ? (Corner.X + Cell / 2, Corner.X + Size * Cell - Cell / 2) : (-60f, 60f);
        var faces = CorridorSeenFaces(MathF.Max(eye.X - 16, low), MathF.Min(eye.X + 16, high));
        var ownFace = Enumerable.Range(0, faces.Length / 8).Where(s => new Vector3(faces[8 * s + 4], faces[8 * s + 5], faces[8 * s + 6]) == normal)
            .SelectMany(s => faces[(8 * s)..(8 * s + 8)]).ToArray();
        var reads = new (string What, float[]? Seen, Func<Vector3, float>? Light, float Sky)[]
        {
            ("fed by every face", null, null, 1),
            ("fed by its own rays alone", [place.X, place.Y, place.Z, 1, normal.X, normal.Y, normal.Z, 0], null, 1),
            ("fed by its own face's points", ownFace, null, 1),
            ("under no sky", null, null, 0),
            ("with the lamp out", null, p => CorridorLight(p) == 5 ? 0.05f : CorridorLight(p), 1),
            ("with the floor under the gap out", null, p => CorridorLight(p) == 0.6f ? 0.05f : CorridorLight(p), 1),
        };
        foreach (var (what, seen, light, sky) in reads)
        {
            var r = CorridorApartRead(configuration, cascades, eye, CorridorPoints[worst], seen, light, sky);
            output.WriteLine($"  {what}: level {level} {r.Own:0.0000}, {r.Own / r.Traced:0.000} of the trace, level {level + 1} {r.Next:0.0000}, "
                + $"{r.Next / r.Traced:0.000}, the next over its own {r.Next / r.Own:0.000}, the weights {r.Weights:0.000} and {r.NextWeights:0.000}, "
                + $"traced {r.Traced:0.0000}");
            if (seen is not null || light is not null || sky < 1)
                continue;
            r.Store.Run(9, 2 * 8 * SplitStoreShape.BaseWays, level: level);
            var ways = r.Store.Taken;
            for (int l = 0; l < 2; l++)
            {
                var spacing = shape.Spacing * (1 << (level + l));
                var q = place / spacing;
                var b = new Vector3(MathF.Floor(q.X), MathF.Floor(q.Y), MathF.Floor(q.Z));
                for (int c = 0; c < 8; c++)
                {
                    var off = new Vector3(c & 1, (c >> 1) & 1, c >> 2);
                    var lerps = Vector3.One - off + (2 * off - Vector3.One) * (q - b);
                    var (cosAll, cosKept, lit, open, rayWays, held, stands) = (0f, 0f, 0f, 0f, 0, 0f, false);
                    for (int way = 0; way < SplitStoreShape.BaseWays; way++)
                    {
                        var i = 8 * ((l * 8 + c) * SplitStoreShape.BaseWays + way);
                        stands = ways[i + 7] > 0;
                        var cos = MathF.Max(Vector3.Dot(normal, SplitBaseWay(way)), 0);
                        cosAll += cos;
                        rayWays += ways[i + 5] >= 0 ? 1 : 0;
                        held += ways[i + 6];
                        if (ways[i + 3] >= 0)
                            (cosKept, lit, open) = (cosKept + cos, lit + cos * ways[i], open + cos * ways[i + 3]);
                    }
                    var cell = b + off;
                    var fed = Enumerable.Range(0, faces.Length / 8).Where(s =>
                    {
                        var fp = new Vector3(faces[8 * s], faces[8 * s + 1], faces[8 * s + 2]);
                        var (fl, fnext) = SplitLevelOf(shape, eye, fp);
                        return (fl == level + l || (fl + 1 == level + l && fnext > 0))
                            && new Vector3(MathF.Floor(fp.X / spacing + 0.5f), MathF.Floor(fp.Y / spacing + 0.5f), MathF.Floor(fp.Z / spacing + 0.5f)) == cell;
                    }).ToArray();
                    var kinds = fed.GroupBy(s => faces[8 * s + 5] > 0.5f ? "floor" : faces[8 * s + 5] < -0.5f ? "roof" : faces[8 * s + 6] > 0.5f ? "wall at -z" : "wall at +z")
                        .Select(g => $"{g.Count()} of the {g.Key}");
                    output.WriteLine($"    level {level + l}'s probe at {cell * spacing}, weight {lerps.X * lerps.Y * lerps.Z:0.000}, "
                        + (stands ? $"{rayWays} of 32 ways with rays, {held / 32:0} rays a way, {cosKept / cosAll:0.000} of the facing cosine kept, "
                            + $"light {(cosKept > 0 ? lit / cosKept : 0):0.0000}, {(lit > 0 ? open / lit : 0):0.000} of it let through to the sky, " : "absent, ")
                        + (fed.Length == 0 ? "fed by no face" : $"fed by {fed.Length} faces, {string.Join(", ", kinds)}, x from "
                            + $"{fed.Min(s => faces[8 * s]):0.00} to {fed.Max(s => faces[8 * s]):0.00}"));
                }
            }
        }
    }
}
