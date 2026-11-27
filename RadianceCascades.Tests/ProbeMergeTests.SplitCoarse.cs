using System.Numerics;
using Xunit;

namespace RadianceCascades.Tests;

/// <summary>
/// The ladder's closed box, every face of it sending the same light, read on the split probes over
/// fields coarser than the module's own, where points by the room's edges see none of the finest
/// probes about them and still take that light.
/// </summary>
public sealed partial class ProbeMergeTests
{
    [CpuKernelFact]
    public void A_Closed_Room_Whose_Every_Face_Sends_The_Same_Light_Reads_That_Light_At_Every_Point_On_The_Split_Probes_Over_A_Coarse_Field()
    {
        // The lamp's closed box, 8 a side inside, each surface sending a light of 1 under a black
        // sky, read at points every half unit over its six faces, each facing into the room, from
        // an eye at the room's middle and one low by a wall, over fields of five cascades about
        // the eye whose finest cell is 0.5 and 1, two and four times the module's own. Every
        // point takes pi.
        var triangles = LampBoxes.SelectMany((box, i) => Cuboid(box.Low, box.High, i + 1)).ToList();
        const float step = 0.5f, off = 0.001f;
        var faces = new List<(Vector3, Vector3)>();
        for (var a = -4 + step / 2; a < 4; a += step)
            for (var b = step / 2; b < 8; b += step)
            {
                faces.Add((new(-4 + off, b, a), Vector3.UnitX));
                faces.Add((new(4 - off, b, a), -Vector3.UnitX));
                faces.Add((new(a, b, -4 + off), Vector3.UnitZ));
                faces.Add((new(a, b, 4 - off), -Vector3.UnitZ));
            }
        for (var a = -4 + step / 2; a < 4; a += step)
            for (var b = -4 + step / 2; b < 4; b += step)
            {
                faces.Add((new(a, off, b), Vector3.UnitY));
                faces.Add((new(a, 8 - off, b), -Vector3.UnitY));
            }
        var points = faces.ToArray();
        var said = new List<string>();
        var dark = 0;
        foreach (var cell in new[] { 0.5f, 1f })
            foreach (var eye in new Vector3[] { new(0, 4, 0), new(0, 1.7f, -2) })
            {
                Vector4[] field = [.. Enumerable.Range(0, 5).Select(c => cell * (1 << c)).Select(f => new Vector4(eye - new Vector3(32 * f), f))];
                var store = new SplitRun(CorridorSplitShape, points, CellLightBy(_ => 1f), 0);
                store.Field(eye, field, [.. field.SelectMany(f => FieldCascade(triangles, new(f.X, f.Y, f.Z), f.W))]);
                store.Seen(SplitRun.Floats(points));
                store.Frames(32);
                var read = store.Gather().Select(t => t / MathF.PI).ToArray();
                dark += read.Count(q => Math.Abs(q - 1) > 0.005f);
                said.Add($"field's cell {cell} from {eye}, {read.Min():0.0000} to {read.Max():0.0000}");
            }
        var line = $"the closed room's {points.Length} points over each field and eye, {string.Join("; ", said)}";
        output.WriteLine(line);
        // 1.0000 at every point. Over a field whose cells are coarse beside the lift of the rays'
        // start, the march from that start can meet a face at once along every way, and a point by
        // the room's edges toward which so none of the finest probes about it let any through takes
        // them by their trilinear weights alone.
        Assert.True(dark == 0, line);
    }
}
