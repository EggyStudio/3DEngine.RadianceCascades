using System.Globalization;
using System.Numerics;
using System.Text;

namespace RadianceCascades.Tests;

/// <summary>
/// The split store's loop over a ladder's scene, read beside the dense merges' loops on the same
/// field, light and bounce.
/// </summary>
public sealed partial class ProbeMergeTests
{
    // The frames the store runs each round of a scene's loop from a store anew, so its ways have
    // taken a few tens of rays each before the round's gather.
    private const int LadderSplitFrames = 64;

    // How the ladder's tables name the split store's merge.
    private const string SplitMergeName = "the split store's, `mergeSplitCascade`";

    // The split store's reading of a scene at a configuration, each part's light over the light
    // traced there after one gather (First) and at the steady state of the bounce (Ratios), as a
    // merge's LadderRow reads it, the rounds its loop took and whether it settled within
    // MostRounds, the gain a round, the leak at the steady state and the store at the last round's
    // last frame, the probes standing in each cascade, the finest asked for and the rays a frame
    // each of those takes at the mean.
    private sealed record SplitRow(Configuration Configuration, LadderScene Scene, float[] First, float[] Ratios, int Rounds, bool Settles, float Gain,
        float Leak, (int[] Standing, int[] Short, int Asked, float Rays) Store)
    {
        public float[] Distances => Ratios.Select(r => MathF.Abs(MathF.Log(r))).ToArray();
        public float Mean => Distances.Average();
        public float Worst => Distances.Max();
    }

    // The split store's loop over a scene at a configuration, as MergedLoop runs a merge's. Each
    // round a store anew over the scene's field about its eye sees the scene's points, each where
    // its traced light is read from, and the bounce's cells' places, runs LadderSplitFrames frames
    // with every hit lit by its surface's own light and the light its bounce cell took of the round
    // before's gather, reflected, and gathers at the same points, until the first lit points' light
    // changes by under a hundredth, with the first round's light, each point's light at the last,
    // the rounds it took, whether it settled within MostRounds, the gain a round and the store at
    // the last round's last frame, the probes standing in each cascade, the finest asked for and
    // the rays a frame each of those takes at the mean. The store takes the walk's shape unless
    // another is given.
    private static (float[] First, float[] Light, int Rounds, bool Settles, float Gain, (int[] Standing, int[] Short, int Asked, float Rays) Store) SplitLoop(
        LadderScene scene, Transport transport,
        (Vector3, Vector3)[] points, int lit, SplitStoreShape? shape = null)
    {
        (Vector3, Vector3)[] all = [.. points.Select(p => (scene.Bounce!.From(p.Item1), p.Item2)), .. transport.Places];
        var seen = SplitRun.Floats(all);
        Vector4[] field = scene.Setting.FieldCascades ?? [new(Corner, Cell)];
        var (sums, each) = (new List<double>(), new List<float[]>());
        float[]? bounce = null;
        while (true)
        {
            var store = new SplitRun(shape ?? CorridorSplitShape, all, scene.Setting.CellLight!, scene.Setting.SkyLight, scene.Bounce!.Grid)
            {
                FieldCellLight = scene.Setting.FieldCascades is not null && !scene.Setting.LightByPlace,
            };
            store.Field(scene.Eye, field, scene.Cells);
            store.Seen(seen);
            if (bounce is not null)
                store.Bounce(bounce);
            store.Frames(LadderSplitFrames);
            var taken = store.Gather();
            var light = taken[..points.Length];
            sums.Add(light[..lit].Sum());
            each.Add(light);
            if (sums.Count == MostRounds || HasSettled(sums))
            {
                var asked = store.Asked();
                return (each[0], light, sums.Count, HasSettled(sums), Gain(sums), (store.Standing(), store.Short(), asked, store.SeenCount / (float)Math.Max(asked, 1)));
            }
            bounce = Spread(transport, taken[points.Length..]);
        }
    }

    // The ladder's section on the split store, its standing at each configuration over the scenes
    // the merges' standing is read over beside the dense grid's by the default merge, and its
    // reading of each scene beside that merge's.
    private static void SplitSection(StringBuilder file, SplitRow[] split, LadderRow[] rows, Standing[] standing, Merge dense)
    {
        string Number(float value, string format) => value.ToString(format, CultureInfo.InvariantCulture);
        file.AppendLine();
        file.AppendLine("## On the split probes");
        file.AppendLine();
        foreach (var line in Wrapped(
            "The split store, `ProbeLayout.Split`, runs over each scene at each configuration on the field, the light and the bounce the "
            + $"merges run on, at its default shape with {CorridorSplitShape.Slots.ToString("N0", CultureInfo.InvariantCulture)} slots to its "
            + $"finest pool and {CorridorSplitShape.Cascades} cascades on grids fixed in the world, the finest {Number(CorridorSplitShape.Spacing, "0.00")} "
            + $"apart and each above twice as far, in {Number(CorridorSplitShape.Bytes / 1048576f, "0.0")} MB. Each round a store anew sees the scene's points and "
            + $"the places of the bounce's cells, runs {LadderSplitFrames} frames with each hit taking its surface's own light and what the "
            + "surface reflects of the light gathered at its cell the round before, and gathers at the same points, the irradiance the dense "
            + "passes' gather keeps. Its parts are read as the merges' are, the dense grid's by the default merge, "
            + $"{MergeName(dense).Split(',')[0]}, beside them, and the store's probes standing in each cascade at the last round's last frame, "
            + "the finest asked for at that frame and the rays a frame each of those takes, a ray a seen point, at the mean."))
            file.AppendLine(line);
        file.AppendLine();
        file.AppendLine("| Configuration | Merge | Mean distance | Worst part | Leak | Most gain a round | Held out, mean and worst |");
        file.AppendLine("|---|---|---|---|---|---|---|");
        foreach (var configuration in split.Select(r => r.Configuration).Distinct())
        {
            var own = split.Where(r => r.Configuration == configuration && !r.Scene.HeldOut).ToArray();
            var held = split.Single(r => r.Configuration == configuration && r.Scene.HeldOut);
            var distances = own.SelectMany(r => r.Distances).ToArray();
            file.AppendLine($"| {ConfigurationName(configuration)} | {SplitMergeName} | {Number(distances.Average(), "0.000")} | "
                + $"{Number(distances.Max(), "0.000")} | {Number(own.Max(r => r.Leak), "0.00000")} | {Number(own.Max(r => r.Gain), "0.000")} | "
                + $"{Number(held.Mean, "0.000")} and {Number(held.Worst, "0.000")} |");
            var s = standing.Single(x => x.Configuration == configuration && x.Merge == dense);
            file.AppendLine($"| {ConfigurationName(configuration)} | {MergeName(dense)} | {Number(s.Mean, "0.000")} | {Number(s.Worst, "0.000")} | "
                + $"{Number(s.Leak, "0.00000")} | {Number(s.Gain, "0.000")} | {Number(s.Held.Mean, "0.000")} and {Number(s.Held.Worst, "0.000")} |");
        }
        file.AppendLine();
        file.AppendLine("| Configuration | Scene | Each part's ratio after one gather | Each part's ratio at the steady state | Rounds | Gain a round "
            + "| Mean distance | Worst part | Leak | The dense grid's mean and worst | Probes standing by cascade | Asked with no entry by cascade | The finest asked | Rays a probe |");
        file.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
        string Each(SplitRow r, float[] ratios) => string.Join(", ", r.Scene.Parts.Select((part, p) => $"{part.Name} {Number(ratios[p], "0.000")}"));
        foreach (var r in split)
        {
            var d = rows.Single(x => x.Configuration == r.Configuration && x.Scene == r.Scene && x.Merge == dense);
            file.AppendLine($"| {ConfigurationName(r.Configuration)} | {r.Scene.Name} | {Each(r, r.First)} | {Each(r, r.Ratios)} | "
                + $"{(r.Settles ? r.Rounds.ToString(CultureInfo.InvariantCulture) : $"{r.Rounds}, unsettled")} | {Number(r.Gain, "0.000")} | "
                + $"{Number(r.Mean, "0.000")} | {Number(r.Worst, "0.000")} | {(r.Scene.Dark is null ? "" : Number(r.Leak, "0.00000"))} | "
                + $"{Number(d.Mean, "0.000")} and {Number(d.Worst, "0.000")} | {string.Join(", ", r.Store.Standing)} | {string.Join(", ", r.Store.Short)} | {r.Store.Asked} | "
                + $"{Number(r.Store.Rays, "0.0")} |");
        }
    }
}
