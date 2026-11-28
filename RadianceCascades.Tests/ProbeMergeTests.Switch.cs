using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

public sealed partial class ProbeMergeTests
{
    // The lamp wall's second lamp switched on and off and the court's sunlit strip stepped a unit
    // along it, at the module's configuration, each lit point seen by 16 by 16 points over its
    // quarter of a cell so its probes' ways hold their cap as an engine's do. After 64 frames in
    // the light before, each of 48 frames' gather is read by its parts against the store's own
    // steady state in the light after, the mean of the last 16 of 112 frames there, and the floor
    // is how far those 16 lie from their mean at most. Given SplitAgreeSpread, as an engine whose
    // ways take many rays a frame passes it, a way follows the change once its frames' sum past
    // its light reaches it, so a switched lamp lies within 0.05 by frame 16 and stays, and the
    // floor stays under 0.02, since a still scene's frames take from that sum as they add. The
    // court's strip is a small part of its parts, so its points' 90th part, which starts far, is
    // held to fall below 0.7 of its start by frame 16. The store's own, which keeps each way's
    // history always, is read beside it.
    [CpuKernelFact]
    public void A_Lamp_Switched_Or_The_Sun_Stepped_On_The_Split_Probes_Given_Spreads_Shows_Within_16_Frames()
    {
        static float LampB(Vector3 p) => p.Y > 3.75f && p.X is > 1 and < 3 && MathF.Abs(p.Z) < 1 ? 5 : 0;
        static float Both(Vector3 p) => LampWallLight(p) + LampB(p);
        static float Stepped(Vector3 p) => p.Y < 0.25f && p.Z is > -0.5f and < 2.5f && MathF.Abs(p.X) < 7.25f ? 1 : 0;
        // The spreads of the Slang SplitAgreeSpread, and 0 for the store's own.
        var spreads = new[] { 5f, 0f };
        var cases = spreads.SelectMany(spread => new (string Name, float Spread, Func<LadderScene> Scene, float[] Before, float[] After)[]
        {
            ("lamp on", spread, WallUnderALamp, CellLightBy(LampWallLight), CellLightBy(Both)),
            ("lamp off", spread, WallUnderALamp, CellLightBy(Both), CellLightBy(LampWallLight)),
            ("sun step", spread, ArcadeCourt, CellLightBy(CourtLight), CellLightBy(Stepped)),
        }).ToArray();
        const int After = 48, Warm = 64, Kept = 16, Dense = 16;
        var read = new (string Said, bool Holds)[cases.Length];
        Parallel.For(0, cases.Length, j =>
        {
            var k = cases[j];
            var scene = k.Scene();
            var (points, lit) = LadderPoints(scene);
            (Vector3, Vector3)[] all = [.. points.Select(p => (scene.Bounce!.From(p.Item1), p.Item2))];
            var seen = SplitRun.Floats([.. points[..lit].SelectMany(p =>
            {
                var (u, v) = Basis(p.Item2);
                return Enumerable.Range(0, Dense * Dense).Select(q => (scene.Bounce!.From(p.Item1 + 0.25f * (((q % Dense + 0.5f) / Dense - 0.5f) * u
                    + ((q / Dense + 0.5f) / Dense - 0.5f) * v)), p.Item2));
            })]);
            Vector4[] field = scene.Setting.FieldCascades ?? [new(Corner, Cell)];
            SplitRun Made(float[] light)
            {
                var store = new SplitRun(CorridorSplitShape, all, [.. light], scene.Setting.SkyLight)
                {
                    FieldCellLight = scene.Setting.FieldCascades is not null && !scene.Setting.LightByPlace,
                    AgreeSpread = k.Spread,
                };
                store.Field(scene.Eye, field, scene.Cells);
                store.Seen(seen);
                return store;
            }
            float[] Taken(SplitRun store) => store.Gather()[..lit];
            var steady = Made(k.After);
            steady.Frames(Warm + After - Kept);
            var last = new List<float[]>();
            for (int f = 0; f < Kept; f++)
            {
                steady.Frames(1);
                last.Add(Taken(steady));
            }
            var target = Enumerable.Range(0, lit).Select(i => last.Average(l => l[i])).ToArray();
            var targetParts = ByPart(scene, target);
            float PartOff(float[] light) => ByPart(scene, light).Select((v, p) => MathF.Abs(MathF.Log(MathF.Max(v, 1e-6f) / targetParts[p]))).Max();
            float PointOff(float[] light)
            {
                var offs = Enumerable.Range(0, lit).Where(i => target[i] > 1e-4f)
                    .Select(i => MathF.Abs(MathF.Log(MathF.Max(light[i], 1e-5f) / target[i]))).Order().ToArray();
                return offs[(int)(0.9 * (offs.Length - 1))];
            }
            var floor = last.Max(PartOff);
            var store = Made(k.Before);
            store.Frames(Warm);
            var startPoints = PointOff(Taken(store));
            var (parts, pointsOff) = (new float[After], new float[After]);
            store.Lit(k.After);
            for (int f = 0; f < After; f++)
            {
                store.Frames(1);
                var light = Taken(store);
                (parts[f], pointsOff[f]) = (PartOff(light), PointOff(light));
            }
            var within = Enumerable.Range(0, After).FirstOrDefault(f => parts[f..].All(o => o < 0.05f), -1);
            var holds = floor < 0.02f && (k.Spread == 0 || (k.Name == "sun step" ? pointsOff[15] < 0.7f * startPoints : within >= 0 && within < 16));
            read[j] = ($"{(k.Spread > 0 ? "given spreads" : "keeping its history")} {k.Name} within 0.05 from frame {(within < 0 ? "none" : within + 1)}, {parts[0]:0.000} at frame 1, {parts[3]:0.000} at 4, "
                + $"{parts[7]:0.000} at 8, {parts[15]:0.000} at 16 and {parts[After - 1]:0.000} at {After}, its points' 90th part {startPoints:0.000} "
                + $"before and {pointsOff[15]:0.000} at 16, the floor {floor:0.000}", holds);
        });
        var said = string.Join("; ", read.Select(r => r.Said));
        output.WriteLine(said);
        Assert.True(read.All(r => r.Holds), said);
    }
}
